"use client"

import type React from "react"

import { useState } from "react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Separator } from "@/components/ui/separator"
import Link from "next/link"
import { useRouter } from "next/navigation"

export default function SignInPage() {
  const [email, setEmail] = useState("")
  const [password, setPassword] = useState("")
  const [isLoading, setIsLoading] = useState(false)
  const [status, setStatus] = useState("")
  const router = useRouter()

  // Thay đổi logic đăng nhập Azure
  const handleAzureSignIn = async () => {
    setIsLoading(true)
    setStatus("Đang chuẩn bị đăng nhập…")
    try {
      const redirectUri = encodeURIComponent('/home')
      const res = await fetch(`/signin-azure/url?redirectUri=${redirectUri}`, { cache: 'no-store' })
      if (!res.ok) throw new Error('Không lấy được đường dẫn.')
      const data = await res.json()
      if (data?.url) {
        window.location.href = data.url
        return
      }
      throw new Error('Thiếu đường dẫn đăng nhập.')
    } catch (error) {
      setStatus("Đi thẳng tới trang đăng nhập mặc định.")
      window.location.href = '/signin-azure?redirectUri=%2Fhome'
    } finally {
      setIsLoading(false)
    }
  }

  const handleEmailSignIn = async (e: React.FormEvent) => {
    e.preventDefault()
    setIsLoading(true)
    try {
      // Giữ nguyên logic cũ cho email
      // Nếu bạn có logic riêng, hãy thay thế ở đây
      // await signInWithEmail(email, password)
      router.push("/")
    } catch (error) {
      console.error("[v0] Email sign in error:", error)
    } finally {
      setIsLoading(false)
    }
  }

  return (
    <div className="min-h-screen bg-gradient-to-b from-background to-muted flex items-center justify-center p-4">
      <Card className="w-full max-w-md">
        <CardHeader className="text-center">
          <div className="h-12 w-12 rounded-lg bg-primary flex items-center justify-center mx-auto mb-4">
            <span className="text-primary-foreground font-bold text-xl">FM</span>
          </div>
          <CardTitle className="text-2xl">Welcome back</CardTitle>
          <CardDescription>Sign in to your File Manager account</CardDescription>
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

          <form onSubmit={handleEmailSignIn} className="space-y-4">
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
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                required
              />
            </div>
            <Button type="submit" className="w-full" disabled={isLoading}>
              Sign In
            </Button>
          </form>

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
