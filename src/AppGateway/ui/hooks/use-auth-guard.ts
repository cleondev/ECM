"use client"

import { useEffect, useMemo, useState } from "react"
import { useRouter } from "next/navigation"

import { checkLogin, fetchCurrentUserProfile } from "@/lib/api"
import {
  clearCachedAuthSnapshot,
  getCachedAuthSnapshot,
  updateCachedAuthSnapshot,
} from "@/lib/auth-state"
import {
  createSignInRedirectPath,
  normalizeRedirectTargetWithDiagnostics,
} from "@/lib/utils"

const APP_HOME_FALLBACK = "/app/"

type AuthGuardState = {
  isAuthenticated: boolean
  isChecking: boolean
}

export function useAuthGuard(targetPath: string): AuthGuardState {
  const router = useRouter()
  const { normalizedTargetPath, resolvedFrom } = useMemo(() => {
    const { normalized, normalizedCandidate, normalizedFallback } =
      normalizeRedirectTargetWithDiagnostics(targetPath, APP_HOME_FALLBACK)

    if (!normalizedCandidate) {
      if (typeof window !== "undefined") {
        const liveLocation = `${window.location.pathname}${window.location.search}${window.location.hash}`
        const liveNormalization = normalizeRedirectTargetWithDiagnostics(
          liveLocation,
          normalizedFallback,
        )

        if (liveNormalization.normalizedCandidate) {
          console.warn(
            "[auth] Guard targetPath=%s is invalid; using live location=%s instead of fallback=%s.",
            targetPath,
            liveNormalization.normalizedCandidate,
            normalizedFallback,
          )
          return {
            normalizedTargetPath: liveNormalization.normalizedCandidate,
            resolvedFrom: "live-location" as const,
          }
        }

        console.warn(
          "[auth] Guard targetPath=%s is invalid and live location could not be normalized (snapshot=%s); using fallback=%s.",
          targetPath,
          liveLocation,
          normalizedFallback,
        )
      } else {
        console.warn(
          "[auth] Guard targetPath=%s is invalid; defaulting to fallback=%s while window is unavailable.",
          targetPath,
          normalizedFallback,
        )
      }
    }

    return {
      normalizedTargetPath: normalized,
      resolvedFrom: normalizedCandidate ? "candidate" : "fallback",
    }
  }, [targetPath])

  useEffect(() => {
    const locationSnapshot =
      typeof window === "undefined"
        ? "(window unavailable)"
        : `${window.location.pathname}${window.location.search}${window.location.hash}`

    console.debug(
      "[auth] Initializing useAuthGuard with targetPath=%s, normalizedTargetPath=%s, currentLocation=%s (resolvedFrom=%s)",
      targetPath,
      normalizedTargetPath,
      locationSnapshot,
      resolvedFrom,
    )
  }, [targetPath, normalizedTargetPath, resolvedFrom])
  const [isAuthenticated, setIsAuthenticated] = useState<boolean>(() => {
    const cached = getCachedAuthSnapshot()
    return cached?.isAuthenticated ?? false
  })
  const [isChecking, setIsChecking] = useState(true)

  useEffect(() => {
    console.debug(
      "[auth] Guard state updated for %s -> isAuthenticated=%s, isChecking=%s (resolvedFrom=%s)",
      normalizedTargetPath,
      isAuthenticated,
      isChecking,
      resolvedFrom,
    )
  }, [isAuthenticated, isChecking, normalizedTargetPath, resolvedFrom])

  useEffect(() => {
    let active = true

    const redirectToLanding = () => {
      const locationSnapshot =
        typeof window === "undefined"
          ? "(window unavailable)"
          : `${window.location.pathname}${window.location.search}${window.location.hash}`

      console.debug(
        "[auth] redirectToLanding() invoked at currentLocation=%s for guard targeting %s.",
        locationSnapshot,
        normalizedTargetPath,
      )
      clearCachedAuthSnapshot()
      setIsAuthenticated(false)
      const signInPath = createSignInRedirectPath(normalizedTargetPath, APP_HOME_FALLBACK)
      console.debug("[auth] Redirecting to sign-in page:", signInPath)
      router.replace(signInPath)
    }

    const verify = async () => {
      console.debug(
        "[auth] Beginning authentication check for path=%s (targetPath=%s).",
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
            "[auth] Confirmed user with id %s from profile; allowing access:",
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
          "[auth] User profile missing after API call; attempting check-login before redirecting.",
        )
        const checkLoginResult = await checkLogin(normalizedTargetPath)

        if (!active) {
          return
        }

        if (checkLoginResult.isAuthenticated && checkLoginResult.user) {
          console.debug(
            "[auth] check-login confirmed user %s; updating cache and continuing at %s.",
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
          "[auth] check-login indicates unauthenticated; redirecting to landing.",
        )
        redirectToLanding()
      } catch (error) {
        console.error("[auth] Failed to retrieve user profile:", error)
        if (!active) {
          return
        }

        try {
          console.debug(
            "[auth] Attempting check-login after profile error…",
          )
          const fallbackCheck = await checkLogin(normalizedTargetPath)
          if (!active) {
            return
          }

          if (fallbackCheck.isAuthenticated && fallbackCheck.user) {
            console.debug(
              "[auth] check-login confirmed session after profile error, user=%s.",
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
            "[auth] Session invalid after check-login attempt; redirecting to landing.",
          )
          redirectToLanding()
        } catch (fallbackError) {
          console.error(
            "[auth] check-login also failed; redirecting to landing:",
            fallbackError,
          )
          redirectToLanding()
        }
      } finally {
        if (active) {
          console.debug("[auth] Completed authentication check for:", normalizedTargetPath)
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
