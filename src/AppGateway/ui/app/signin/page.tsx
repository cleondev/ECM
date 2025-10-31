"use client"

import type React from "react"

import { Suspense, useEffect, useMemo, useRef, useState } from "react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Separator } from "@/components/ui/separator"
import { BrandLogo } from "@/components/brand-logo"
import Link from "next/link"
import { useRouter, useSearchParams } from "next/navigation"
import { checkLogin, passwordLogin, PasswordLoginError } from "@/lib/api"
import { attemptSilentLogin, resolveGatewayUrl } from "@/lib/auth"
import { getCachedAuthSnapshot } from "@/lib/auth-state"
import { normalizeRedirectTarget } from "@/lib/utils"

export default function SignInPage() {
  return (
    <Suspense fallback={<SignInPageFallback />}>
      <SignInPageContent />
    </Suspense>
  )
}

function SignInPageContent() {
  const [email, setEmail] = useState("")
  const [password, setPassword] = useState("")
  const [isLoading, setIsLoading] = useState(false)
  const [status, setStatus] = useState("")
  const [passwordStatus, setPasswordStatus] = useState<string | null>(null)
  const router = useRouter()
  const searchParams = useSearchParams()
  const silentLoginAttempted = useRef(false)
  const targetAfterLogin = useMemo(() => {
    const candidate = searchParams?.get("redirectUri") ?? searchParams?.get("redirect")
    return normalizeRedirectTarget(candidate)
  }, [searchParams])

  useEffect(() => {
    const cached = getCachedAuthSnapshot()
    if (cached?.isAuthenticated) {
      router.replace(cached.redirectPath ?? targetAfterLogin)
    }
  }, [router, targetAfterLogin])

  useEffect(() => {
    if (typeof window === "undefined") {
      return
    }

    let cancelled = false

    async function attemptAutoSignIn() {
      setIsLoading(true)
      setStatus("Đang kiểm tra trạng thái đăng nhập…")

      try {
        const initial = await checkLogin(targetAfterLogin)
        if (cancelled) {
          return
        }

        let safeRedirect = normalizeRedirectTarget(initial.redirectPath, targetAfterLogin)

        if (initial.isAuthenticated) {
          setStatus("Đã đăng nhập, đang chuyển hướng…")
          router.replace(safeRedirect)
          return
        }

        if (!silentLoginAttempted.current && initial.silentLoginUrl) {
          silentLoginAttempted.current = true
          setStatus("Đang thử đăng nhập tự động…")

          await attemptSilentLogin(initial.silentLoginUrl)

          if (cancelled) {
            return
          }

          const followUp = await checkLogin(targetAfterLogin)
          if (cancelled) {
            return
          }

          safeRedirect = normalizeRedirectTarget(followUp.redirectPath, targetAfterLogin)

          if (followUp.isAuthenticated) {
            setStatus("Đã đăng nhập, đang chuyển hướng…")
            router.replace(safeRedirect)
            return
          }
        }

        setStatus("")
      } catch (error) {
        console.error("[ui] Không thể kiểm tra trạng thái đăng nhập tự động:", error)
        setStatus("")
      } finally {
        if (!cancelled) {
          setIsLoading(false)
        }
      }
    }

    attemptAutoSignIn()

    return () => {
      cancelled = true
    }
  }, [router, targetAfterLogin])

  // Thay đổi logic đăng nhập Azure
  const handleAzureSignIn = async () => {
    setIsLoading(true)
    setStatus("Đang kiểm tra trạng thái đăng nhập…")
    let safeRedirect = targetAfterLogin
    try {
      const result = await checkLogin(targetAfterLogin)
      safeRedirect = normalizeRedirectTarget(result.redirectPath, targetAfterLogin)

      if (result.isAuthenticated) {
        setStatus("Đã đăng nhập, đang chuyển hướng…")
        router.replace(safeRedirect)
        return
      }

      if (result.loginUrl) {
        setStatus("Đang chuyển tới đăng nhập Microsoft…")
        window.location.href = resolveGatewayUrl(result.loginUrl)
        return
      }

      throw new Error('Thiếu đường dẫn đăng nhập.')
    } catch (error) {
      console.error("[ui] Không lấy được đường dẫn đăng nhập Azure:", error)
      setStatus("Đi thẳng tới trang đăng nhập mặc định.")
      window.location.href = resolveGatewayUrl(
        `/signin-azure?redirectUri=${encodeURIComponent(safeRedirect)}`,
      )
    } finally {
      setIsLoading(false)
    }
  }

  const handlePasswordSignIn = async (e: React.FormEvent) => {
    e.preventDefault()
    setIsLoading(true)
    setPasswordStatus(null)
    try {
      const result = await passwordLogin({
        email,
        password,
        redirectUri: targetAfterLogin,
      })

      if (result.isAuthenticated) {
        setPasswordStatus("Đăng nhập thành công, đang chuyển hướng…")
        router.replace(normalizeRedirectTarget(result.redirectPath, targetAfterLogin))
        return
      }

      setPasswordStatus(
        "Không thể xác thực người dùng. Vui lòng kiểm tra lại thông tin.",
      )
    } catch (error) {
      if (error instanceof PasswordLoginError) {
        if (error.reason === "invalid") {
          setPasswordStatus("Email hoặc password không đúng.")
        } else if (error.reason === "unavailable") {
          setPasswordStatus("Dịch vụ đăng nhập tạm thời không khả dụng. Vui lòng thử lại sau.")
        } else if (error.reason === "validation") {
          setPasswordStatus(error.message)
        } else {
          setPasswordStatus("Không thể hoàn thành đăng nhập. Vui lòng thử lại sau.")
        }
      } else {
        console.error("[ui] Password login error:", error)
        setPasswordStatus("Không thể hoàn thành đăng nhập. Vui lòng thử lại sau.")
      }
    } finally {
      setIsLoading(false)
    }
  }

  return (
    <div className="min-h-screen bg-gradient-to-b from-background to-muted flex items-center justify-center p-4">
      <Card className="w-full max-w-md">
        <CardHeader className="text-center">
          <BrandLogo
            size={48}
            imageClassName="h-12 w-12"
            className="mb-4 justify-center"
          />
          <CardTitle className="text-2xl">Welcome back</CardTitle>
          <CardDescription>Sign in to your ECM account</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <Button className="w-full" size="lg" onClick={handleAzureSignIn} disabled={isLoading}>
            <svg className="mr-2 h-5 w-5" viewBox="0 0 23 23" fill="currentColor">
              <path d="M0 0h10.931v10.931H0zm12.069 0H23v10.931H12.069zM0 12.069h10.931V23H0zm12.069 0H23V23H12.069z" />
            </svg>
            Sign in with Microsoft
          </Button>
          <p className="text-center text-xs text-muted-foreground min-h-[1.4em]">{status}</p>
          <div className="relative">
            <div className="absolute inset-0 flex items-center">
              <Separator />
            </div>
            <div className="relative flex justify-center text-xs uppercase">
              <span className="bg-card px-2 text-muted-foreground">Or continue with</span>
            </div>
          </div>

          <form onSubmit={handlePasswordSignIn} className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="email">Email</Label>
              <Input
                id="email"
                type="email"
                placeholder="name@company.com"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="password">Password</Label>
              <Input
                id="password"
                type="password"
                placeholder="Nhập password của bạn"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                required
              />
            </div>
            <Button type="submit" className="w-full" disabled={isLoading}>
              Sign in with email
            </Button>
          </form>
          <p className="text-center text-xs text-muted-foreground min-h-[1.4em]">
            {passwordStatus}
          </p>

          <div className="text-center text-sm">
            <span className="text-muted-foreground">Don't have an account? </span>
            <Link href="/signup" className="text-primary hover:underline">
              Sign up
            </Link>
          </div>
        </CardContent>
      </Card>
    </div>
  )
}

function SignInPageFallback() {
  return (
    <div className="min-h-screen bg-gradient-to-b from-background to-muted flex items-center justify-center p-4">
      <Card className="w-full max-w-md">
        <CardHeader className="text-center space-y-2">
          <BrandLogo
            size={48}
            imageClassName="h-12 w-12"
            className="mb-4 justify-center"
          />
          <CardTitle className="text-2xl">Loading sign-in…</CardTitle>
          <CardDescription>Please wait while we prepare your sign-in experience.</CardDescription>
        </CardHeader>
        <CardContent>
          <p className="text-center text-sm text-muted-foreground">Loading…</p>
        </CardContent>
      </Card>
    </div>
  )
}
