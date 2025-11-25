"use client"

import { useEffect, useState } from "react"
import { usePathname, useRouter, useSearchParams } from "next/navigation"

import FileViewClient from "./file-view-client"
import { Button } from "@/components/ui/button"
import { useAuthGuard } from "@/hooks/use-auth-guard"

const MAIN_APP_ROUTE = "/app/"
const VIEWER_ROUTE = "/viewer/"

export function ViewerPageClient() {
  const router = useRouter()
  const pathname = usePathname()
  const searchParams = useSearchParams()
  const [viewerParams, setViewerParams] = useState<{
    fileId?: string
    viewerTargetPath: string
  }>()

  useEffect(() => {
    const fileId = searchParams.get("fileId")?.trim()
    const searchParamsString = searchParams.toString()
    const normalizedPath = pathname && pathname.endsWith("/") ? pathname : `${pathname ?? VIEWER_ROUTE}/`
    const viewerTargetPath = searchParamsString ? `${normalizedPath}?${searchParamsString}` : normalizedPath

    setViewerParams({ fileId, viewerTargetPath })
  }, [pathname, searchParams])

  const { isAuthenticated, isChecking } = useAuthGuard(viewerParams?.viewerTargetPath || VIEWER_ROUTE)

  useEffect(() => {
    if (!viewerParams) return

    console.debug(
      "[viewer] Initialized viewer page with fileId=%s, targetPath=%s",
      viewerParams.fileId ?? "(missing)",
      viewerParams.viewerTargetPath,
    )
  }, [viewerParams])

  if (!viewerParams) {
    return <ViewerLoading />
  }

  if (!viewerParams.fileId) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-slate-50 dark:bg-slate-950">
        <div className="max-w-md space-y-4 rounded-2xl border border-border bg-background/95 p-6 text-center shadow-sm">
          <p className="text-lg font-semibold text-foreground">Không thể mở trình xem tệp</p>
          <p className="text-sm text-muted-foreground">
            Liên kết xem trước không hợp lệ hoặc đã thiếu thông tin cần thiết.
          </p>
          <Button onClick={() => router.push(MAIN_APP_ROUTE)}>Quay lại thư viện</Button>
        </div>
      </div>
    )
  }

  return (
    <FileViewClient
      fileId={viewerParams.fileId}
      targetPath={viewerParams.viewerTargetPath}
      isAuthenticated={isAuthenticated}
      isChecking={isChecking}
    />
  )
}

export function ViewerLoading() {
  return (
    <div className="flex min-h-screen items-center justify-center bg-slate-50 dark:bg-slate-950">
      <div className="max-w-md space-y-4 rounded-2xl border border-border bg-background/95 p-6 text-center shadow-sm">
        <p className="text-lg font-semibold text-foreground">Đang tải trình xem tệp…</p>
        <p className="text-sm text-muted-foreground">Đang khởi tạo liên kết xem trước, vui lòng chờ trong giây lát.</p>
      </div>
    </div>
  )
}
