"use client"

import { useEffect, useMemo, useState } from "react"
import { useRouter } from "next/navigation"
import { ArrowLeft, BadgeInfo, DownloadCloud, FolderInput, History, Share2, Tag } from "lucide-react"

import { buildDocumentDownloadUrl, fetchFileDetails } from "@/lib/api"
import type { FileDetail } from "@/lib/types"
import type { ViewerPreference } from "@/lib/viewer-utils"
import { resolveViewerConfig } from "@/lib/viewer-utils"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"
import { FileViewer } from "@/components/file-viewer"

const MAIN_APP_ROUTE = "/app/"

const dateFormatter = new Intl.DateTimeFormat("vi-VN", {
  dateStyle: "medium",
  timeStyle: "short",
})

export type FileViewClientProps = {
  fileId: string
  preference?: ViewerPreference
  targetPath: string
  isAuthenticated: boolean
  isChecking: boolean
}

export default function FileViewClient({
  fileId,
  preference,
  targetPath,
  isAuthenticated,
  isChecking,
}: FileViewClientProps) {
  const router = useRouter()
  const [file, setFile] = useState<FileDetail | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const viewerUrl = useMemo(
    () => (file?.latestVersionId ? buildDocumentDownloadUrl(file.latestVersionId) : undefined),
    [file?.latestVersionId],
  )

  useEffect(() => {
    console.debug(
      "[viewer] FileViewClient mounted with fileId=%s, preference=%o, targetPath=%s",
      fileId,
      preference,
      targetPath,
    )
  }, [fileId, preference, targetPath])

  useEffect(() => {
    console.debug(
      "[viewer] Auth guard resolved for %s -> isAuthenticated=%s, isChecking=%s",
      targetPath,
      isAuthenticated,
      isChecking,
    )
  }, [isAuthenticated, isChecking, targetPath])

  useEffect(() => {
    if (!isAuthenticated) {
      return
    }

    let cancelled = false
    console.debug(
      "[viewer] Starting file detail fetch for fileId=%s (targetPath=%s)",
      fileId,
      targetPath,
    )
    setLoading(true)
    setError(null)

    fetchFileDetails(fileId)
      .then((detail) => {
        if (!cancelled) {
          setFile(detail)
        }
      })
      .catch((err) => {
        console.error("[ui] Failed to load file details", err)
        if (!cancelled) {
          setError("Không thể tải thông tin chi tiết. Vui lòng thử lại sau.")
        }
      })
      .finally(() => {
        if (!cancelled) {
          setLoading(false)
        }
      })

    return () => {
      cancelled = true
    }
  }, [isAuthenticated, fileId, targetPath])

  const viewerConfig = useMemo(() => (file ? resolveViewerConfig(file, preference) : undefined), [file, preference])

  const handleDownload = () => {
    if (!viewerUrl) {
      return
    }

    window.open(viewerUrl, "_blank", "noopener,noreferrer")
  }

  if (isChecking || loading) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-slate-100 dark:bg-slate-950">
        <div className="rounded-xl border border-border bg-background/90 px-6 py-10 text-center shadow-sm">
          <p className="text-base text-muted-foreground">Đang tải trình xem tệp…</p>
        </div>
      </div>
    )
  }

  if (!isAuthenticated) {
    return null
  }

  if (error || !file) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-slate-100 dark:bg-slate-950">
        <div className="max-w-md space-y-4 rounded-2xl border border-border bg-background/95 p-6 text-center shadow-sm">
          <p className="text-lg font-semibold text-foreground">Không thể mở tệp</p>
          <p className="text-sm text-muted-foreground">{error ?? "Tệp đã bị xóa hoặc bạn không có quyền truy cập."}</p>
          <Button onClick={() => router.push(MAIN_APP_ROUTE)}>Quay lại thư viện</Button>
        </div>
      </div>
    )
  }

  if (!viewerConfig) {
    return null
  }

  return (
    <div className="min-h-screen bg-slate-100/80 px-2 pb-6 pt-2 dark:bg-slate-950">
      <header className="sticky top-0 z-10 border-b border-border/80 bg-background/90 px-2 py-3 shadow-sm backdrop-blur">
        <div className="mx-auto flex max-w-[1600px] flex-wrap items-center gap-3 px-2">
          <Button variant="ghost" size="sm" onClick={() => router.push(MAIN_APP_ROUTE)} className="gap-2">
            <ArrowLeft className="h-4 w-4" />
            Trở lại thư viện
          </Button>
          <div className="flex items-center gap-2 text-sm text-muted-foreground">
            <FolderInput className="h-4 w-4" />
            <span className="font-medium text-foreground">{file.folder}</span>
            <span className="text-muted-foreground">/</span>
            <span className="font-semibold text-foreground">{file.name}</span>
          </div>
          <div className="ml-auto flex flex-wrap items-center gap-2">
            <Badge variant="secondary" className="text-xs uppercase">
              {viewerConfig.category === "pdf" ? "PDF" : viewerConfig.category}
            </Badge>
            <Button variant="outline" size="sm" className="gap-2" disabled={!viewerUrl} onClick={handleDownload}>
              <DownloadCloud className="h-4 w-4" />
              Tải xuống
            </Button>
            <Button variant="secondary" size="sm" className="gap-2" disabled>
              <Share2 className="h-4 w-4" />
              Chia sẻ (sắp ra mắt)
            </Button>
          </div>
        </div>
      </header>

      <div className="mx-auto mt-4 grid max-w-[1600px] gap-4 px-2 lg:grid-cols-[280px,minmax(0,1fr),340px]">
        <Card className="hidden h-fit border-border/80 bg-background/80 shadow-sm lg:block">
          <CardHeader className="pb-3">
            <CardTitle className="text-base">Trang xem trước</CardTitle>
            <CardDescription>Chuyển nhanh qua từng trang tài liệu.</CardDescription>
          </CardHeader>
          <CardContent className="space-y-3">
            {file.preview.kind === "document" && file.preview.pages?.length ? (
              file.preview.pages.map((page) => (
                <div key={page.number} className="space-y-2 rounded-lg border border-border/60 bg-muted/30 p-2">
                  <div
                    className="aspect-[3/4] w-full rounded-md bg-gradient-to-br from-slate-100 via-white to-slate-50"
                    style={{
                      backgroundImage: page.thumbnail ? `url(${page.thumbnail})` : undefined,
                      backgroundSize: "cover",
                      backgroundPosition: "center",
                    }}
                  />
                  <div className="space-y-1 px-1">
                    <p className="text-xs font-semibold text-foreground">Trang {page.number}</p>
                    <p className="line-clamp-2 text-xs text-muted-foreground">{page.excerpt}</p>
                  </div>
                </div>
              ))
            ) : (
              <p className="text-sm text-muted-foreground">Chưa có bản xem trước.</p>
            )}
          </CardContent>
        </Card>

        <Card className="overflow-hidden border-border/70 bg-background/95 shadow-md">
          <CardHeader className="border-b border-border/70 bg-muted/20">
            <div className="flex flex-wrap items-center justify-between gap-3">
              <div>
                <CardTitle className="text-lg text-foreground">{file.name}</CardTitle>
                <CardDescription>{viewerConfig.category === "pdf" ? "Trình xem PDF" : "Xem trước tệp"}</CardDescription>
              </div>
              <div className="flex items-center gap-2 text-xs text-muted-foreground">
                <History className="h-4 w-4" />
                Cập nhật {file.modifiedAtUtc ? dateFormatter.format(new Date(file.modifiedAtUtc)) : file.modified}
              </div>
            </div>
          </CardHeader>
          <CardContent className="bg-slate-50/60 p-0 dark:bg-slate-950/50">
            <div className="p-4">
              <FileViewer file={file} viewerConfig={viewerConfig} viewerUrl={viewerUrl} />
            </div>
          </CardContent>
        </Card>

        <div className="space-y-4">
          <Card className="border-border/80 bg-background/95">
            <CardHeader className="pb-3">
              <CardTitle>Thông tin tệp</CardTitle>
              <CardDescription>Chi tiết đồng bộ từ backend.</CardDescription>
            </CardHeader>
            <CardContent className="space-y-4 text-sm">
              <div className="flex items-center gap-3">
                <Avatar className="h-11 w-11">
                  <AvatarImage src={file.ownerAvatar} alt={file.owner} />
                  <AvatarFallback>{file.owner.slice(0, 2).toUpperCase()}</AvatarFallback>
                </Avatar>
                <div>
                  <p className="text-muted-foreground">Chủ sở hữu</p>
                  <p className="font-semibold text-foreground">{file.owner}</p>
                </div>
              </div>
              <div className="space-y-2 rounded-lg border border-dashed border-border/70 bg-muted/20 p-3">
                <div className="flex items-center justify-between">
                  <span className="text-muted-foreground">Kích thước</span>
                  <span className="font-medium text-foreground">{file.size}</span>
                </div>
                <div className="flex items-center justify-between">
                  <span className="text-muted-foreground">Phiên bản</span>
                  <span className="font-medium text-foreground">{file.latestVersionNumber ?? "--"}</span>
                </div>
                <div className="flex items-center justify-between">
                  <span className="text-muted-foreground">Ngày tạo</span>
                  <span className="font-medium text-foreground">
                    {file.createdAtUtc ? dateFormatter.format(new Date(file.createdAtUtc)) : "--"}
                  </span>
                </div>
              </div>
              <div className="space-y-2">
                <p className="text-sm font-semibold text-foreground">Thẻ gợi ý</p>
                <div className="flex flex-wrap gap-2">
                  {file.tags.map((tag) => (
                    <Badge key={tag.id} variant={tag.color ? "secondary" : "outline"} className="flex items-center gap-1 text-xs">
                      <Tag className="h-3 w-3" />
                      {tag.name}
                    </Badge>
                  ))}
                </div>
              </div>
            </CardContent>
          </Card>

          <Card className="border-border/80 bg-background/95">
            <CardHeader className="pb-3">
              <CardTitle>Lịch sử phiên bản</CardTitle>
              <CardDescription>Thông tin lấy từ backend nếu có.</CardDescription>
            </CardHeader>
            <CardContent className="space-y-3 text-sm">
              {file.versions.map((version) => (
                <div key={version.id} className="rounded-lg border border-border/60 bg-muted/20 p-3">
                  <div className="flex items-center justify-between font-medium text-foreground">
                    <span>{version.label}</span>
                    <span>{version.size}</span>
                  </div>
                  <p className="text-muted-foreground">{version.notes}</p>
                  <p className="text-xs text-muted-foreground/90">{dateFormatter.format(new Date(version.createdAt))}</p>
                </div>
              ))}
            </CardContent>
          </Card>

          <Card className="border-border/80 bg-background/95">
            <CardHeader className="pb-3">
              <CardTitle>Cập nhật gần đây</CardTitle>
              <CardDescription>Tự động đồng bộ với hoạt động chia sẻ.</CardDescription>
            </CardHeader>
            <CardContent className="space-y-3 text-sm">
              {file.activity.map((entry) => (
                <div key={entry.id} className="flex items-start gap-3 rounded-lg border border-border/60 bg-muted/20 p-3">
                  <BadgeInfo className="mt-0.5 h-4 w-4 text-primary" />
                  <div className="space-y-1">
                    <p className="font-semibold text-foreground">{entry.actor}</p>
                    <p className="text-muted-foreground">{entry.description}</p>
                    <p className="text-xs text-muted-foreground/80">{dateFormatter.format(new Date(entry.timestamp))}</p>
                  </div>
                </div>
              ))}
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  )
}
