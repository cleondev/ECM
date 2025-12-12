// app/components/Globe.tsx
"use client"

import { useEffect, useRef } from "react"
import * as THREE from "three"
import { OrbitControls } from "three/examples/jsm/controls/OrbitControls.js"
import { texture } from 'three/tsl';

const FRAGMENT_SHADER_MAP = `
uniform sampler2D u_map_tex;
varying float vOpacity;
varying vec2 vUv;

void main() {
    vec3 texColor = texture2D(u_map_tex, vUv).rgb;
    vec3 base = mix(vec3(0.85), texColor, 0.35);

    float d = length(gl_PointCoord.xy - vec2(0.5));
    base -= 0.12 * d;

    float dot = 1.0 - smoothstep(0.42, 0.50, d);
    if (dot < 0.4) discard;

    vec3 inverted = vec3(1.0) - base;
    vec3 color = mix(vec3(1.0), inverted, 0.7);

    gl_FragColor = vec4(color, dot * vOpacity * 0.99);
}
`

const VERTEX_SHADER_MAP = `
uniform sampler2D u_map_tex;
uniform float u_dot_size;
uniform float u_time_since_click;
uniform vec3 u_pointer;

varying float vOpacity;
varying vec2 vUv;

#define PI 3.14159265359

void main() {
    vUv = uv;

    float visibility = step(.2, texture2D(u_map_tex, uv).r);
    gl_PointSize = visibility * u_dot_size;

    vec4 mvPosition = modelViewMatrix * vec4(position, 1.0);
    vOpacity = (1. / length(mvPosition.xyz) - .7);
    vOpacity = clamp(vOpacity, .03, 1.);

    float t = max(0., u_time_since_click - .1);
    float dist = 1. - .5 * length(position - u_pointer);
    float damping = 1. / (1. + 20. * t);
    float delta = .15 * damping * sin(5. * t * (1. + 2. * dist) - PI);
    delta *= 1. - smoothstep(.8, 1., dist);

    vec3 pos = position * (1. + delta);

    gl_Position = projectionMatrix * modelViewMatrix * vec4(pos, 1.);
}
`

export function Globe() {
  const containerRef = useRef<HTMLDivElement | null>(null)
  const canvasRef = useRef<HTMLCanvasElement | null>(null)

  useEffect(() => {
    const wrapper = containerRef.current
    const canvas3D = canvasRef.current
    if (!wrapper || !canvas3D) return

    let renderer: THREE.WebGLRenderer
    let scene: THREE.Scene
    let camera: THREE.OrthographicCamera
    let controls: OrbitControls
    let globe: THREE.Points
    let globeMesh: THREE.Mesh
    let pointer: THREE.Mesh
    let mapMaterial: THREE.ShaderMaterial
    const clock = new THREE.Clock()

    const landPoints: THREE.Vector3[] = []
    const activeArcs: Arc[] = []

    const ARC_SEGMENTS = 80
    const ARC_MIN_COUNT = 10
    const ARC_MAX_COUNT = 12

    init()
    window.addEventListener("resize", resize)

    function init() {
      renderer = new THREE.WebGLRenderer({ canvas: canvas3D, alpha: true, antialias: true })
      renderer.setPixelRatio(window.devicePixelRatio || 1)

      scene = new THREE.Scene()

      camera = new THREE.OrthographicCamera(-1.1, 1.1, 1.1, -1.1, 0, 3)
      camera.position.z = 1.1

      addControls()

      new THREE.TextureLoader().load("/earth-map-colored.png",
        (tex) => {
          createGlobe(tex)
          extractLandPoints(tex)
          initArcs()
          addEvents()
          resize()
          render()
        }
      )
    }

    function addControls() {
      controls = new OrbitControls(camera, canvas3D)
      controls.enablePan = false
      controls.enableZoom = false
      controls.enableDamping = true
      controls.autoRotate = true
      controls.autoRotateSpeed = 0.6
      controls.minPolarAngle = 0.4 * Math.PI
      controls.maxPolarAngle = 0.4 * Math.PI
    }

    function createGlobe(tex: THREE.Texture) {
      const geo = new THREE.IcosahedronGeometry(1, 50)

      mapMaterial = new THREE.ShaderMaterial({
        vertexShader: VERTEX_SHADER_MAP,
        fragmentShader: FRAGMENT_SHADER_MAP,
        uniforms: {
          u_map_tex: { value: tex },
          u_dot_size: { value: 0 },
          u_pointer: { value: new THREE.Vector3(0, 0, 1) },
          u_time_since_click: { value: 0 },
        },
        transparent: true,
      })

      globe = new THREE.Points(geo, mapMaterial)
      scene.add(globe)

      globeMesh = new THREE.Mesh(
        geo,
        new THREE.MeshBasicMaterial({ color: 0xffffff, transparent: true, opacity: 0 })
      )
      scene.add(globeMesh)
    }

    function extractLandPoints(tex: THREE.Texture) {
      const img = tex.image as HTMLImageElement
      const c = document.createElement("canvas")
      c.width = img.width
      c.height = img.height
      const ctx = c.getContext("2d")
      if (!ctx) return

      ctx.drawImage(img, 0, 0)
      const data = ctx.getImageData(0, 0, c.width, c.height).data

      const uvAttr = (globe.geometry as THREE.BufferGeometry)
        .attributes.uv as THREE.BufferAttribute
      const posAttr = (globe.geometry as THREE.BufferGeometry)
        .attributes.position as THREE.BufferAttribute

      for (let i = 0; i < uvAttr.count; i++) {
        const u = uvAttr.getX(i)
        const v = uvAttr.getY(i)

        const x = Math.floor(u * c.width)
        const y = Math.floor((1 - v) * c.height)
        const idx = (y * c.width + x) * 4

        const r = data[idx]
        const g = data[idx + 1]
        const b = data[idx + 2]
        const brightness = (r + g + b) / 3

        if (brightness > 130) continue

        const vx = posAttr.getX(i)
        const vy = posAttr.getY(i)
        const vz = posAttr.getZ(i)
        landPoints.push(new THREE.Vector3(vx, vy, vz))
      }
    }

    class Arc {
      mesh: THREE.Line | null = null
      progress = 0
      speed = 0.01 + Math.random() * 0.01
      dead = false
      maxPoints = ARC_SEGMENTS + 1
      geometry!: THREE.BufferGeometry
      material!: THREE.LineBasicMaterial

      constructor() {
        this.create()
      }

      create() {
        if (landPoints.length < 2) return

        const p1 = landPoints[Math.floor(Math.random() * landPoints.length)]
        let p2 = landPoints[Math.floor(Math.random() * landPoints.length)]

        let tries = 0
        while (p1.distanceTo(p2) < 0.4 && tries++ < 10) {
          p2 = landPoints[Math.floor(Math.random() * landPoints.length)]
        }

        const mid = p1.clone().add(p2).normalize().multiplyScalar(1.4)
        const curve = new THREE.QuadraticBezierCurve3(p1, mid, p2)

        const pts = curve.getPoints(ARC_SEGMENTS)
        const geometry = new THREE.BufferGeometry().setFromPoints(pts)
        geometry.setDrawRange(0, 0)

        const material = new THREE.LineBasicMaterial({
          color: 0x33ccff,
          transparent: true,
          opacity: 0.85,
        })

        this.geometry = geometry
        this.material = material
        this.mesh = new THREE.Line(geometry, material)

        scene.add(this.mesh)
      }

      update() {
        this.progress += this.speed

        const max = this.maxPoints
        let start = 0
        let count = 0

        if (this.progress <= 1.0) {
          const head = Math.floor(max * this.progress)
          start = 0
          count = Math.max(0, head - start)
        } else if (this.progress <= 2.0) {
          const t = this.progress - 1.0
          const tail = Math.floor(max * t)
          const head = max
          start = tail
          count = Math.max(0, head - tail)
        } else {
          this.dead = true
        }

        this.geometry.setDrawRange(start, count)

        const life = Math.min(1.0, this.progress / 2.0)
        const fade = life < 0.5 ? life * 2.0 : 1.0 - (life - 0.5) * 2.0
        this.material.opacity = 0.15 + 0.7 * Math.max(0.0, fade)
      }

      dispose() {
        if (!this.mesh) return
        scene.remove(this.mesh)
        this.geometry.dispose()
        this.material.dispose()
      }
    }

    function initArcs() {
      const initialCount =
        ARC_MIN_COUNT + Math.floor(Math.random() * (ARC_MAX_COUNT - ARC_MIN_COUNT + 1))
      for (let i = 0; i < initialCount; i++) {
        setTimeout(() => activeArcs.push(new Arc()), i * 200)
      }
    }

    function updateArcs() {
      for (let i = activeArcs.length - 1; i >= 0; i--) {
        const arc = activeArcs[i]
        arc.update()
        if (arc.dead) {
          arc.dispose()
          activeArcs.splice(i, 1)
        }
      }

      while (activeArcs.length < ARC_MIN_COUNT) {
        activeArcs.push(new Arc())
      }
      if (activeArcs.length < ARC_MAX_COUNT && Math.random() < 0.02) {
        activeArcs.push(new Arc())
      }
    }

    function addEvents() {
      wrapper.addEventListener("click", () => {
        if (!mapMaterial) return
        mapMaterial.uniforms.u_time_since_click.value = 0
        clock.start()
      })
    }

    function render() {
      if (mapMaterial) {
        mapMaterial.uniforms.u_time_since_click.value = clock.getElapsedTime()
      }

      updateArcs()
      controls.update()
      renderer.render(scene, camera)
      requestAnimationFrame(render)
    }

    function resize() {
      const rect = wrapper.getBoundingClientRect()
      const size = Math.min(rect.width, rect.height || rect.width)

      renderer.setSize(size, size)
      canvas3D.style.width = size + "px"
      canvas3D.style.height = size + "px"

      if (mapMaterial) {
        mapMaterial.uniforms.u_dot_size.value = size * 0.01
      }
    }

    return () => {
      window.removeEventListener("resize", resize)
      renderer?.dispose()
      activeArcs.forEach((a) => a.dispose())
    }
  }, [])

  return (
    <div className="globe-container" ref={containerRef}>
      <canvas id="globe-3d" ref={canvasRef} />
    </div>
  )
}

export default Globe
