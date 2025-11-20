"use client"

import { useEffect, useState } from "react"
import { useRouter } from "next/navigation"

import { checkLogin, fetchCurrentUserProfile } from "@/lib/api"
import {
  clearCachedAuthSnapshot,
  getCachedAuthSnapshot,
  updateCachedAuthSnapshot,
} from "@/lib/auth-state"
import { createSignInRedirectPath, normalizeRedirectTarget } from "@/lib/utils"

const APP_HOME_FALLBACK = "/app/"

type AuthGuardState = {
  isAuthenticated: boolean
  isChecking: boolean
}

export function useAuthGuard(targetPath: string): AuthGuardState {
  const router = useRouter()
  const normalizedTargetPath = normalizeRedirectTarget(targetPath, APP_HOME_FALLBACK)

  useEffect(() => {
    const locationSnapshot =
      typeof window === "undefined"
        ? "(window unavailable)"
        : `${window.location.pathname}${window.location.search}${window.location.hash}`

    console.debug(
      "[auth] Khởi tạo useAuthGuard với targetPath=%s, normalizedTargetPath=%s, currentLocation=%s",
      targetPath,
      normalizedTargetPath,
      locationSnapshot,
    )
  }, [targetPath, normalizedTargetPath])
  const [isAuthenticated, setIsAuthenticated] = useState<boolean>(() => {
    const cached = getCachedAuthSnapshot()
    return cached?.isAuthenticated ?? false
  })
  const [isChecking, setIsChecking] = useState(true)

  useEffect(() => {
    let active = true

    const redirectToLanding = () => {
      const locationSnapshot =
        typeof window === "undefined"
          ? "(window unavailable)"
          : `${window.location.pathname}${window.location.search}${window.location.hash}`

      console.debug(
        "[auth] redirectToLanding() được gọi tại vị trí hiện tại=%s cho guard hướng tới %s.",
        locationSnapshot,
        normalizedTargetPath,
      )
      clearCachedAuthSnapshot()
      setIsAuthenticated(false)
      const signInPath = createSignInRedirectPath(normalizedTargetPath, APP_HOME_FALLBACK)
      console.debug("[auth] Chuyển hướng tới trang đăng nhập:", signInPath)
      router.replace(signInPath)
    }

    const verify = async () => {
      console.debug(
        "[auth] Bắt đầu xác thực quyền truy cập cho đường dẫn=%s (targetPath=%s).",
        normalizedTargetPath,
        targetPath,
      )
      try {
        const profile = await fetchCurrentUserProfile()
        if (!active) {
          return
        }

        if (profile) {
          console.debug(
            "[auth] Đã xác nhận người dùng với id %s từ profile, tiếp tục truy cập:",
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
          "[auth] Không tìm thấy hồ sơ người dùng sau khi gọi API, thử check-login trước khi chuyển hướng.",
        )
        const checkLoginResult = await checkLogin(normalizedTargetPath)

        if (!active) {
          return
        }

        if (checkLoginResult.isAuthenticated && checkLoginResult.user) {
          console.debug(
            "[auth] check-login xác nhận người dùng %s, cập nhật cache và tiếp tục tại %s.",
            checkLoginResult.user.id,
            normalizedTargetPath,
          )
          updateCachedAuthSnapshot({
            isAuthenticated: true,
            redirectPath: normalizedTargetPath,
            user: checkLoginResult.user,
          })
          setIsAuthenticated(true)
          return
        }

        console.warn(
          "[auth] check-login cho biết chưa đăng nhập, chuyển về trang giới thiệu.",
        )
        redirectToLanding()
      } catch (error) {
        console.error("[auth] Không lấy được hồ sơ người dùng:", error)
        if (!active) {
          return
        }

        try {
          console.debug(
            "[auth] Thử xác minh trạng thái đăng nhập bằng check-login sau lỗi profile…",
          )
          const fallbackCheck = await checkLogin(normalizedTargetPath)
          if (!active) {
            return
          }

          if (fallbackCheck.isAuthenticated && fallbackCheck.user) {
            console.debug(
              "[auth] Đã xác nhận phiên từ check-login sau lỗi profile, user=%s.",
              fallbackCheck.user.id,
            )
            updateCachedAuthSnapshot({
              isAuthenticated: true,
              redirectPath: normalizedTargetPath,
              user: fallbackCheck.user,
            })
            setIsAuthenticated(true)
            return
          }

          console.warn(
            "[auth] Phiên không hợp lệ sau khi thử check-login, chuyển hướng về landing.",
          )
          redirectToLanding()
        } catch (fallbackError) {
          console.error(
            "[auth] check-login cũng thất bại, chuyển hướng về trang giới thiệu:",
            fallbackError,
          )
          redirectToLanding()
        }
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
