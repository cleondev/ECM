"use client"

import Link from "next/link"
import { useEffect, useMemo, useRef, useState } from "react"

import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"
import { Skeleton } from "@/components/ui/skeleton"
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from "@/components/ui/tooltip"
import { fetchCurrentUserIdentity } from "@/lib/api"
import type { UserIdentity } from "@/lib/types"
import { cn } from "@/lib/utils"

type CurrentUserIdentityProps = {
  size?: "sm" | "md" | "lg"
  shape?: "circle" | "square"
  profileHref?: string
  className?: string
  lazy?: boolean
  hint?: string
  interactive?: boolean
}

const sizeStyles: Record<
  Required<CurrentUserIdentityProps>["size"],
  { avatar: string; label: string; hint: string; nameWidth: string; hintWidth: string }
> = {
  sm: { avatar: "h-8 w-8 text-xs", label: "text-sm", hint: "text-[11px]", nameWidth: "w-20", hintWidth: "w-14" },
  md: { avatar: "h-10 w-10 text-sm", label: "text-base", hint: "text-xs", nameWidth: "w-24", hintWidth: "w-16" },
  lg: { avatar: "h-12 w-12 text-base", label: "text-lg", hint: "text-sm", nameWidth: "w-28", hintWidth: "w-20" },
}

const shapeStyles: Record<Required<CurrentUserIdentityProps>["shape"], string> = {
  circle: "rounded-full [&_[data-slot=avatar-image]]:rounded-full [&_[data-slot=avatar-fallback]]:rounded-full",
  square: "rounded-md [&_[data-slot=avatar-image]]:rounded-md [&_[data-slot=avatar-fallback]]:rounded-md",
}

function getInitials(name?: string): string {
  if (!name) {
    return "?"
  }

  const trimmed = name.trim()
  if (!trimmed) {
    return "?"
  }

  const parts = trimmed.split(/\s+/).filter(Boolean)
  if (parts.length === 1) {
    return parts[0]!.slice(0, 2).toUpperCase()
  }

  const [first, ...rest] = parts
  const last = rest.pop()
  const initials = `${first?.[0] ?? ""}${last?.[0] ?? ""}`.toUpperCase()
  return initials || "?"
}

export function CurrentUserIdentity({
  size = "md",
  shape = "circle",
  profileHref = "/me",
  className,
  lazy = true,
  hint,
  interactive = true,
}: CurrentUserIdentityProps) {
  const containerRef = useRef<HTMLDivElement | null>(null)
  const [user, setUser] = useState<UserIdentity | null>(null)
  const [isLoading, setIsLoading] = useState(!lazy)
  const [shouldLoad, setShouldLoad] = useState(!lazy)
  const [hasAttempted, setHasAttempted] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!lazy) {
      setShouldLoad(true)
      return
    }

    const observer = new IntersectionObserver(
      (entries) => {
        const entry = entries[0]
        if (entry?.isIntersecting) {
          setShouldLoad(true)
          observer.disconnect()
        }
      },
      { rootMargin: "64px" },
    )

    if (containerRef.current) {
      observer.observe(containerRef.current)
    }

    return () => observer.disconnect()
  }, [lazy])

  useEffect(() => {
    if (!shouldLoad || isLoading || hasAttempted) {
      return
    }

    let isMounted = true
    setHasAttempted(true)
    setIsLoading(true)
    setError(null)

    fetchCurrentUserIdentity()
      .then((identity) => {
        if (!isMounted) return
        setUser(identity)
        if (!identity) {
          setError("Không tìm thấy thông tin người dùng")
        }
      })
      .catch((reason) => {
        console.error("[ui] Unable to load current user identity:", reason)
        if (!isMounted) return
        setError("Không thể tải thông tin người dùng")
      })
      .finally(() => {
        if (isMounted) {
          setIsLoading(false)
        }
      })

    return () => {
      isMounted = false
    }
  }, [shouldLoad, isLoading, hasAttempted])

  const fallbackText = useMemo(
    () => getInitials(user?.displayName || user?.email || undefined),
    [user?.displayName, user?.email],
  )

  const shapeClassName = shapeStyles[shape]
  const { avatar: avatarClassName, label: labelClassName, hint: hintClassName, nameWidth, hintWidth } =
    sizeStyles[size]

  const hasEmail = Boolean(user?.email)
  const hintText = hint ?? (user?.isAuthenticated ? "Mở trang hồ sơ" : "Khách truy cập")

  const skeleton = (
    <div ref={containerRef} className={cn("inline-flex items-center gap-3", className)}>
      <Skeleton className={cn(avatarClassName, shape === "square" ? "rounded-md" : "rounded-full")} />
      <div className="space-y-1">
        <Skeleton className={cn("h-3", nameWidth)} />
        <Skeleton className={cn("h-3", hintWidth)} />
      </div>
    </div>
  )

  if (isLoading || (!user && !error)) {
    return skeleton
  }

  if (!user) {
    return (
      <div
        ref={containerRef}
        className={cn(
          "inline-flex items-center gap-2 rounded-md border border-dashed px-3 py-2 text-xs text-muted-foreground",
          className,
        )}
      >
        <span aria-live="polite">{error ?? "Không thể hiển thị thông tin người dùng"}</span>
      </div>
    )
  }

  const sharedContent = (
    <>
      <Avatar className={cn(avatarClassName, shapeClassName)}>
        <AvatarImage src={user.avatarUrl ?? undefined} alt={user.displayName} loading="lazy" />
        <AvatarFallback className={shapeClassName}>{fallbackText}</AvatarFallback>
      </Avatar>
      <div className="flex flex-col">
        <span className={cn("font-semibold leading-tight", labelClassName)}>{user.displayName}</span>
        <span className={cn("text-muted-foreground leading-tight", hintClassName)}>{hintText}</span>
      </div>
    </>
  )

  const triggerClassName = cn(
    "group inline-flex items-center gap-3 rounded-md px-2 py-1 transition-colors hover:bg-muted/60",
    className,
  )

  return (
    <TooltipProvider delayDuration={100}>
      <Tooltip>
        <TooltipTrigger asChild>
          {interactive ? (
            <Link href={profileHref} prefetch={false} className={triggerClassName} ref={containerRef}>
              {sharedContent}
            </Link>
          ) : (
            <div className={triggerClassName} ref={containerRef}>
              {sharedContent}
            </div>
          )}
        </TooltipTrigger>
        {hasEmail ? (
          <TooltipContent side="bottom" align="start">
            <div className="space-y-1">
              <p className="text-sm font-semibold leading-tight">{user.displayName}</p>
              <p className="text-xs text-muted-foreground leading-tight">{user.email}</p>
            </div>
          </TooltipContent>
        ) : null}
      </Tooltip>
    </TooltipProvider>
  )
}
