"use client"

import { useEffect } from "react"
import Link from "next/link"
import { useRouter } from "next/navigation"
import gsap from "gsap"
import Globe from "./Globe"
import { BarChart3, Cloud, Search, Shield, Users, Zap } from "lucide-react"
import { Button } from "@/components/ui/button"
import { BrandLogo } from "@/components/brand-logo"
import { checkLogin } from "@/lib/api"
import { getCachedAuthSnapshot } from "@/lib/auth-state"
import { normalizeRedirectTarget } from "@/lib/utils"

import "./landing.css"

type IconType = typeof BarChart3

type CardItemProps = {
  Icon: IconType
  title: string
  description: string
}

function CardItem({ Icon, title, description }: CardItemProps) {
  return (
    <div className="card-item">
      <div
        data-slot="card"
        className="flex flex-col gap-4 rounded-xl border border-border bg-card text-card-foreground shadow-sm feature-card glow-card group"
      >
        <Icon className="h-8 w-8 text-primary mb-2 transition-transform group-hover:scale-110" />
        <h3 className="font-semibold text-foreground">{title}</h3>
        <p className="text-muted-foreground leading-relaxed">{description}</p>
      </div>
    </div>
  )
}

const cards: CardItemProps[] = [
  {
    Icon: BarChart3,
    title: "Analytics & Insights",
    description:
      "Track document usage, team productivity, and compliance with detailed analytics.",
  },
  {
    Icon: Search,
    title: "Intelligent Search",
    description:
      "Find any document instantly with AI-powered search across all your content and metadata.",
  },
  {
    Icon: Shield,
    title: "Enterprise Security",
    description:
      "Bank-level encryption, role-based access control, and compliance with global standards.",
  },
  {
    Icon: Zap,
    title: "Workflow Automation",
    description:
      "Automate document routing, approvals, and notifications to save time and reduce errors.",
  },
  {
    Icon: Cloud,
    title: "Cloud Storage",
    description:
      "Unlimited scalable storage with automatic backups and disaster recovery built-in.",
  },
  {
    Icon: Users,
    title: "Team Collaboration",
    description:
      "Work together seamlessly with version control, comments, and instant notifications.",
  },
]

export default function Page() {
  const router = useRouter()

  useEffect(() => {
    let isMounted = true

    const cached = getCachedAuthSnapshot()
    if (cached?.isAuthenticated) {
      router.replace(normalizeRedirectTarget(cached.redirectPath, "/app/"))
      return () => {
        isMounted = false
      }
    }

    checkLogin("/app/")
      .then((result) => {
        if (!isMounted || !result.isAuthenticated) {
          return
        }

        router.replace(normalizeRedirectTarget(result.redirectPath, "/app/"))
      })
      .catch((error) => {
        console.error("[landing] Unable to verify sign-in state", error)
      })

    return () => {
      isMounted = false
    }
  }, [router])

  // GSAP hero text rotation (giống HTML gốc nhưng viết trong useEffect)
  useEffect(() => {
    const titleEl = document.querySelector(".hero-text h1") as HTMLElement | null
    const bodyEl = document.querySelector(".hero-text p") as HTMLElement | null
    if (!titleEl || !bodyEl) return

    const variants = [
      {
        title: "The complete platform for document management",
        body: "Securely store, organize, and collaborate on documents. Streamline your workflow with intelligent automation and enterprise-grade security.",
      },
      {
        title: "Everything you need to manage documents at scale",
        body: "Built for modern teams. Powerful features that help you work faster and smarter.",
      },
    ]

    let current = 0

    function switchHero() {
      const next = (current + 1) % variants.length
      const nextData = variants[next]

      const tl = gsap.timeline()
      tl.to([titleEl, bodyEl], {
        duration: 0.45,
        opacity: 0,
        rotateY: -90,
        transformOrigin: "left center",
        ease: "power2.in",
      })
        .add(() => {
          titleEl.textContent = nextData.title
          bodyEl.textContent = nextData.body
        })
        .fromTo(
          [titleEl, bodyEl],
          {
            opacity: 0,
            rotateY: 90,
            transformOrigin: "right center",
          },
          {
            duration: 1.5,
            opacity: 1,
            rotateY: 0,
            ease: "power2.out",
          }
        )

      current = next
    }

    const id = setInterval(switchHero, 8000)
    return () => clearInterval(id)
  }, [])

  const marqueeCards = [...cards, ...cards]

  return (
    <div className="landing-page">
      <header>
          <Link href="/" className="flex items-center">
            <BrandLogo
              priority
              textClassName="text-xl font-semibold text-foreground"
              imageClassName="h-10 w-10"
            />
          </Link>
        <nav>
          <a href="#">Features</a>
          <a href="#">AI</a>
          <a href="#">Docs</a>
        </nav>
        <div className="header-buttons">
          <Button asChild variant="outline" className="shadow-sm">
            <Link href="/signin/?returnUrl=/app/">Sign In</Link>
          </Button>
        </div>
      </header>

      {/* MAIN */}
      <main className="page-main">
        {/* HERO */}
        <section className="hero">
          <div className="hero-text">
            <h1>The complete platform for document management</h1>
            <p>
              Securely store, organize, and collaborate on documents. Streamline your workflow with
              intelligent automation and enterprise-grade security.
            </p>
            <div className="buttons">
              <Button asChild size="lg" className="btn-primary">
                <Link href="/signup/?returnUrl=/app/">Get Started ↓</Link>
              </Button>
              <Button asChild size="lg" variant="outline" className="btn-secondary">
                <Link href="#features">View Demo ▶</Link>
              </Button>
            </div>
          </div>

          {/* Globe đã tách ra component riêng, nhưng vẫn render trong hero cột phải */}
          <Globe />
        </section>

        {/* SLIDE / CARDS SECTION */}
        <section className="stats" id="features">
          <div className="cards-wrapper">
            <div className="cards-track">
              {marqueeCards.map((card, index) => (
                <CardItem
                  key={index}
                  Icon={card.Icon}
                  title={card.title}
                  description={card.description}
                />
              ))}
            </div>
          </div>
        </section>
      </main>

      {/* FOOTER */}
      <footer className="site-footer">© 2025 ECM. All rights reserved.</footer>
    </div>
  )
}