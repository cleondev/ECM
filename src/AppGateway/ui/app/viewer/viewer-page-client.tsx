"use client"

import { useMemo } from "react"
import { usePathname, useRouter, useSearchParams } from "next/navigation"

import FileViewClient from "./file-view-client"
import { Button } from "@/components/ui/button"
import { parseViewerPreference } from "@/lib/viewer-utils"

const MAIN_APP_ROUTE = "/app/"

export function ViewerPageClient() {
  const router = useRouter()
  const pathname = usePathname()
  const searchParams = useSearchParams()
  const fileId = searchParams.get("fileId")?.trim()
  const viewer = searchParams.get("viewer") ?? undefined
  const office = searchParams.get("office") ?? undefined
  const preference = useMemo(() => parseViewerPreference(viewer, office), [viewer, office])
  const searchParamsString = useMemo(() => searchParams.toString(), [searchParams])
  const viewerTargetPath = useMemo(() => {
    const normalizedPath = pathname && pathname.endsWith("/") ? pathname : `${pathname ?? "/viewer/"}/`
    return searchParamsString ? `${normalizedPath}?${searchParamsString}` : normalizedPath
  }, [pathname, searchParamsString])

  if (!fileId) {
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

  return <FileViewClient fileId={fileId} preference={preference} targetPath={viewerTargetPath} />
}
