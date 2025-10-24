"use client"

import { useEffect, useState } from "react"
import { useRouter } from "next/navigation"

import { fetchCurrentUserProfile } from "@/lib/api"
import {
  clearCachedAuthSnapshot,
  getCachedAuthSnapshot,
  updateCachedAuthSnapshot,
} from "@/lib/auth-state"
import { normalizeRedirectTarget } from "@/lib/utils"

const LANDING_REDIRECT = "/"
const APP_HOME_FALLBACK = "/app/"

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

    const redirectToLanding = () => {
      clearCachedAuthSnapshot()
      setIsAuthenticated(false)
      router.replace(LANDING_REDIRECT)
    }

    const verify = async () => {
      try {
        const profile = await fetchCurrentUserProfile()
        if (!active) {
          return
        }

        if (profile) {
          updateCachedAuthSnapshot({
            isAuthenticated: true,
            redirectPath: normalizedTargetPath,
            user: profile,
          })
          setIsAuthenticated(true)
          return
        }

        console.warn("[auth] Không tìm thấy hồ sơ người dùng, chuyển về trang giới thiệu.")
        redirectToLanding()
      } catch (error) {
        console.error("[auth] Không lấy được hồ sơ người dùng:", error)
        if (!active) {
          return
        }

        redirectToLanding()
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
