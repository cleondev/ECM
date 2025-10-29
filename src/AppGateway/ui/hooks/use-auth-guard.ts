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
      console.debug(
        "[auth] Bắt đầu xác thực quyền truy cập cho đường dẫn:",
        normalizedTargetPath,
      )
      try {
        const profile = await fetchCurrentUserProfile()
        if (!active) {
          return
        }

        if (profile) {
          console.debug(
            "[auth] Đã xác nhận người dùng với id %s, tiếp tục truy cập:",
            profile.id,
            normalizedTargetPath,
          )
          updateCachedAuthSnapshot({
            isAuthenticated: true,
            redirectPath: normalizedTargetPath,
            user: profile,
          })
          setIsAuthenticated(true)
          return
        }

        console.warn(
          "[auth] Không tìm thấy hồ sơ người dùng sau khi gọi API, chuyển về trang giới thiệu.",
        )
        redirectToLanding()
      } catch (error) {
        console.error("[auth] Không lấy được hồ sơ người dùng:", error)
        if (!active) {
          return
        }

        console.warn("[auth] Chuyển hướng về trang giới thiệu do lỗi xác thực.")
        redirectToLanding()
      } finally {
        if (active) {
          console.debug("[auth] Hoàn tất kiểm tra xác thực cho:", normalizedTargetPath)
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
