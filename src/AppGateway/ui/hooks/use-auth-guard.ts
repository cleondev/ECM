"use client"

import { useEffect, useState } from "react"
import { useRouter } from "next/navigation"

import { checkLogin } from "@/lib/api"
import { getCachedAuthSnapshot, clearCachedAuthSnapshot } from "@/lib/auth-state"

const DEFAULT_REDIRECT = "/signin"

function buildSignInUrl(target: string) {
  const redirectTarget = target || "/app"
  return `${DEFAULT_REDIRECT}?redirectUri=${encodeURIComponent(redirectTarget)}`
}

type AuthGuardState = {
  isAuthenticated: boolean
  isChecking: boolean
}

export function useAuthGuard(targetPath: string): AuthGuardState {
  const router = useRouter()
  const [isAuthenticated, setIsAuthenticated] = useState<boolean>(() => {
    const cached = getCachedAuthSnapshot()
    return cached?.isAuthenticated ?? false
  })
  const [isChecking, setIsChecking] = useState(true)

  useEffect(() => {
    let active = true

    const verify = async () => {
      try {
        const result = await checkLogin(targetPath)
        if (!active) {
          return
        }

        if (result.isAuthenticated) {
          setIsAuthenticated(true)
          return
        }

        clearCachedAuthSnapshot()
        setIsAuthenticated(false)
        router.replace(buildSignInUrl(result.redirectPath || targetPath))
      } catch (error) {
        console.error("[auth] Không kiểm tra được trạng thái đăng nhập:", error)
        if (!active) {
          return
        }

        clearCachedAuthSnapshot()
        setIsAuthenticated(false)
        router.replace(buildSignInUrl(targetPath))
      } finally {
        if (active) {
          setIsChecking(false)
        }
      }
    }

    verify()

    return () => {
      active = false
    }
  }, [router, targetPath])

  return { isAuthenticated, isChecking }
}
