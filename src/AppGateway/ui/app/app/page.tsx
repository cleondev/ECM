"use client"

import { FileManager } from "@/components/file-manager"
import { useAuthGuard } from "@/hooks/use-auth-guard"

const MAIN_APP_ROUTE = "/app"

export default function AppHomePage() {
  const { isAuthenticated, isChecking } = useAuthGuard(MAIN_APP_ROUTE)

  if (isChecking) {
    return (
      <div className="flex h-screen items-center justify-center bg-background text-muted-foreground">
        Đang xác thực phiên đăng nhập…
      </div>
    )
  }

  if (!isAuthenticated) {
    return null
  }

  return <FileManager />
}
