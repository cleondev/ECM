"use client"

import { useEffect, useMemo, useState } from "react"
import { useRouter } from "next/navigation"
import { ArrowLeft, Download, FileText, FileWarning } from "lucide-react"

import { buildDocumentDownloadUrl, fetchFileDetails } from "@/lib/api"
import type { FileDetail } from "@/lib/types"
import { resolveViewerConfig, type ViewerCategory } from "@/lib/viewer-utils"
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Separator } from "@/components/ui/separator"

import { PdfViewer } from "./pdf-viewer"

const MAIN_APP_ROUTE = "/app/"

export type FileViewClientProps = {
  fileId: string
  targetPath: string
  isAuthenticated: boolean
  isChecking: boolean
}

type ViewerPanelProps = {
  file: FileDetail
  viewerCategory: ViewerCategory
  viewerUrl?: string
}

function getExtension(name?: string) {
  if (!name) {
    return undefined
  }

  const lastDot = name.lastIndexOf(".")
  if (lastDot <= 0 || lastDot === name.length - 1) {
    return undefined
  }

  return name.slice(lastDot + 1).toLowerCase()
}

function PdfViewerPanel({ viewerUrl, file }: { viewerUrl?: string; file: FileDetail }) {
  if (!viewerUrl) {
    return (
      <div className="flex h-[75vh] flex-col items-center justify-center gap-3 rounded-lg border border-dashed border-border bg-muted/40 text-center">
        <FileWarning className="h-10 w-10 text-muted-foreground" />
        <div className="space-y-1">
          <p className="text-base font-semibold text-foreground">Không tìm thấy file PDF</p>
          <p className="text-sm text-muted-foreground">Không có đường dẫn hợp lệ để hiển thị tài liệu {file.name}.</p>
        </div>
      </div>
    )
  }

  return <PdfViewer documentUrl={viewerUrl} />
}

function ImageViewerPanel({ file, viewerUrl }: { file: FileDetail; viewerUrl?: string }) {
  const source =
    file.preview.kind === "image" || file.preview.kind === "design"
      ? file.preview.url
      : viewerUrl

  if (!source) {
    return <UnsupportedViewerPanel file={file} message="Không có nội dung hình ảnh để hiển thị." />
  }

  return (
    <div className="flex items-center justify-center rounded-lg border border-border bg-background p-4">
      <img src={source} alt={file.name} className="max-h-[70vh] w-auto max-w-full rounded-md shadow" />
    </div>
  )
}

function VideoViewerPanel({ file }: { file: FileDetail }) {
  if (file.preview.kind !== "video") {
    return <UnsupportedViewerPanel file={file} message="Không có nội dung video để hiển thị." />
  }

  return (
    <div className="overflow-hidden rounded-lg border border-border bg-background">
      <video
        controls
        className="h-[75vh] w-full bg-black"
        poster={file.preview.poster}
        src={file.preview.url}
      />
    </div>
  )
}

function CodeViewerPanel({ file }: { file: FileDetail }) {
  if (file.preview.kind !== "code") {
    return <UnsupportedViewerPanel file={file} message="Không có nội dung mã nguồn để hiển thị." />
  }

  return (
    <pre className="h-[75vh] overflow-auto rounded-lg border border-border bg-slate-900 p-4 text-sm text-slate-100">
      {file.preview.content}
    </pre>
  )
}

function UnsupportedViewerPanel({ file, message }: { file: FileDetail; message?: string }) {
  const ext = getExtension(file.name)
  return (
    <div className="flex h-[60vh] flex-col items-center justify-center gap-3 rounded-lg border border-dashed border-border bg-muted/40 text-center">
      <FileWarning className="h-10 w-10 text-muted-foreground" />
      <div className="space-y-1">
        <p className="text-base font-semibold text-foreground">Định dạng chưa được hỗ trợ</p>
        <p className="text-sm text-muted-foreground">
          {message ?? `Không có trình xem phù hợp cho tệp${ext ? ` .${ext}` : ""}. Vui lòng tải xuống để xem chi tiết.`}
        </p>
      </div>
    </div>
  )
}

function ViewerPanel({ file, viewerCategory, viewerUrl }: ViewerPanelProps) {
  switch (viewerCategory) {
    case "pdf":
      return <PdfViewerPanel file={file} viewerUrl={viewerUrl} />
    case "image":
      return <ImageViewerPanel file={file} viewerUrl={viewerUrl} />
    case "video":
      return <VideoViewerPanel file={file} />
    case "code":
      return <CodeViewerPanel file={file} />
    default:
      return <UnsupportedViewerPanel file={file} />
  }
}

export default function FileViewClient({ fileId, targetPath, isAuthenticated, isChecking }: FileViewClientProps) {
  const router = useRouter()
  const [file, setFile] = useState<FileDetail | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const viewerUrl = useMemo(
    () => (file?.latestVersionId ? buildDocumentDownloadUrl(file.latestVersionId) : undefined),
    [file?.latestVersionId],
  )

  useEffect(() => {
    console.debug("[viewer] FileViewClient mounted with fileId=%s, targetPath=%s", fileId, targetPath)
  }, [fileId, targetPath])

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
    console.debug("[viewer] Starting file detail fetch for fileId=%s (targetPath=%s)", fileId, targetPath)
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

  const viewerConfig = useMemo(() => (file ? resolveViewerConfig(file) : undefined), [file])

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

  if (error || !file || !viewerConfig) {
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

  const extension = getExtension(file.name)
  const viewerLabel = viewerConfig.officeKind ? `Office - ${viewerConfig.officeKind}` : viewerConfig.category

  return (
    <div className="min-h-screen bg-slate-50 dark:bg-slate-950">
      <div className="mx-auto max-w-6xl space-y-5 px-4 py-6">
        <div className="flex flex-col gap-4 rounded-2xl border border-border bg-background/95 p-5 shadow-sm sm:flex-row sm:items-center sm:justify-between">
          <div className="flex items-start gap-3 sm:items-center">
            <Button variant="outline" size="sm" onClick={() => router.push(MAIN_APP_ROUTE)} className="hidden sm:inline-flex">
              <ArrowLeft className="mr-2 h-4 w-4" /> Thoát
            </Button>
            <div>
              <div className="text-base font-semibold text-foreground">{file.name}</div>
              <div className="text-sm text-muted-foreground">
                {extension ? `.${extension}` : "Định dạng chưa xác định"} • {file.owner}
              </div>
              <div className="mt-1 flex items-center gap-2 text-xs text-muted-foreground">
                <Avatar className="h-6 w-6">
                  <AvatarImage src={file.ownerAvatar} alt={file.owner} />
                  <AvatarFallback>{file.owner.slice(0, 2).toUpperCase()}</AvatarFallback>
                </Avatar>
                <span>Phiên bản mới nhất: {file.latestVersionId ?? "N/A"}</span>
              </div>
            </div>
          </div>
          <div className="flex flex-wrap items-center gap-2">
            <Badge variant="secondary" className="capitalize">
              {viewerLabel}
            </Badge>
            <Button onClick={handleDownload} disabled={!viewerUrl} variant="default" size="sm">
              <Download className="mr-2 h-4 w-4" />
              Tải xuống
            </Button>
          </div>
        </div>

        <Card className="overflow-hidden">
          <CardHeader className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
            <div>
              <CardTitle>Trình xem tệp</CardTitle>
              <p className="text-sm text-muted-foreground">Trình xem được chọn tự động dựa trên định dạng của tệp.</p>
            </div>
            <div className="flex items-center gap-2 text-xs text-muted-foreground">
              <Badge variant="outline" className="flex items-center gap-1">
                <FileText className="h-3.5 w-3.5" />
                {extension ? `.${extension}` : "Không rõ"}
              </Badge>
            </div>
          </CardHeader>
          <Separator />
          <CardContent className="pt-6">
            <ViewerPanel file={file} viewerCategory={viewerConfig.category} viewerUrl={viewerUrl} />
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
