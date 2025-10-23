"use client"

import { useEffect, useState } from "react"
import { useRouter } from "next/navigation"

import { checkLogin } from "@/lib/api"
import { getCachedAuthSnapshot, clearCachedAuthSnapshot } from "@/lib/auth-state"
import { normalizeRedirectTarget } from "@/lib/utils"

const DEFAULT_REDIRECT = "/signin/"
const APP_HOME_FALLBACK = "/app/"

function buildSignInUrl(target: string) {
  const redirectTarget = normalizeRedirectTarget(target, APP_HOME_FALLBACK)
  return `${DEFAULT_REDIRECT}?redirectUri=${encodeURIComponent(redirectTarget)}`
}

type AuthGuardState = {
  isAuthenticated: boolean
  isChecking: boolean
}

export function useAuthGuard(targetPath: string): AuthGuardState {
  const router = useRouter()
  const normalizedTargetPath = normalizeRedirectTarget(targetPath, APP_HOME_FALLBACK)
  const [isAuthenticated, setIsAuthenticated] = useState<boolean>(() => {
    const cached = getCachedAuthSnapshot()
    return cached?.isAuthenticated ?? false
  })
  const [isChecking, setIsChecking] = useState(true)

  useEffect(() => {
    let active = true

    const verify = async () => {
      try {
        const result = await checkLogin(normalizedTargetPath)
        if (!active) {
          return
        }

        if (result.isAuthenticated) {
          setIsAuthenticated(true)
          return
        }

        clearCachedAuthSnapshot()
        setIsAuthenticated(false)
        router.replace(buildSignInUrl(result.redirectPath || normalizedTargetPath))
      } catch (error) {
        console.error("[auth] Không kiểm tra được trạng thái đăng nhập:", error)
        if (!active) {
          return
        }

        clearCachedAuthSnapshot()
        setIsAuthenticated(false)
        router.replace(buildSignInUrl(normalizedTargetPath))
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
  }, [router, normalizedTargetPath])

  return { isAuthenticated, isChecking }
}
