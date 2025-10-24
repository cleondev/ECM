"use client"

import { BrandLogo } from "@/components/brand-logo"
import { FileManager } from "@/components/file-manager"
import { useAuthGuard } from "@/hooks/use-auth-guard"

const MAIN_APP_ROUTE = "/app/"

export default function AppHomePage() {
  const { isAuthenticated, isChecking } = useAuthGuard(MAIN_APP_ROUTE)

  if (isChecking) {
    return (
      <div className="flex h-screen flex-col items-center justify-center gap-6 bg-background text-muted-foreground">
        <BrandLogo className="flex-col items-center gap-3" imageClassName="h-16 w-16" textClassName="text-2xl" />
        <p className="text-center text-base">Đang xác thực phiên đăng nhập…</p>
      </div>
    )
  }

  if (!isAuthenticated) {
    return null
  }

  return <FileManager />
}
