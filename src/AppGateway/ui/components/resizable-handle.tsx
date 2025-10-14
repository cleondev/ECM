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

    document.addEventListener("mousemove", handleMouseMove)
    document.addEventListener("mouseup", handleMouseUp)

    return () => {
      document.removeEventListener("mousemove", handleMouseMove)
      document.removeEventListener("mouseup", handleMouseUp)
    }
  }, [isDragging, onResize])

  return (
    <div
      className={cn(
        "w-1 bg-border hover:bg-primary/50 cursor-col-resize transition-colors flex-shrink-0 relative group",
        isDragging && "bg-primary",
      )}
      onMouseDown={handleMouseDown}
    >
      <div className="absolute inset-y-0 -left-1 -right-1" />
    </div>
  )
}
