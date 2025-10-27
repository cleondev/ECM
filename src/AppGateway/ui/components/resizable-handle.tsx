"use client"

import { useState, useCallback, useEffect } from "react"

import { cn } from "@/lib/utils"

type ResizableHandleProps = {
  onResize: (delta: number) => void
}

export function ResizableHandle({ onResize }: ResizableHandleProps) {
  const [isDragging, setIsDragging] = useState(false)

  const handleMouseDown = useCallback(() => {
    setIsDragging(true)
  }, [])

  useEffect(() => {
    if (!isDragging) return

    const handleMouseMove = (e: MouseEvent) => {
      onResize(e.movementX)
    }

    const handleMouseUp = () => {
      setIsDragging(false)
    }

    const previousCursor = document.body.style.cursor
    document.body.style.cursor = "ew-resize"

    document.addEventListener("mousemove", handleMouseMove)
    document.addEventListener("mouseup", handleMouseUp)

    return () => {
      document.removeEventListener("mousemove", handleMouseMove)
      document.removeEventListener("mouseup", handleMouseUp)
      document.body.style.cursor = previousCursor
    }
  }, [isDragging, onResize])

  return (
    <div
      className={cn(
        "group relative flex-shrink-0 w-px cursor-ew-resize transition-colors",
        "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2",
      )}
      onMouseDown={handleMouseDown}
    >
      <span
        aria-hidden
        className={cn(
          "pointer-events-none absolute inset-y-2 left-1/2 w-px -translate-x-1/2 rounded-full bg-border/60 transition-colors",
          "group-hover:bg-primary/40",
          isDragging && "bg-primary/60",
        )}
      />

      <div aria-hidden className="absolute inset-y-0 -left-3 -right-3 cursor-ew-resize" />
    </div>
  )
}
