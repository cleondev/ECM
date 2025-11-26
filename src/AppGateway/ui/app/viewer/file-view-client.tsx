"use client"

import { useEffect, useMemo, useState } from "react"
import { useRouter } from "next/navigation"
import { ArrowLeft, BadgeCheck, Download, FileText, FileWarning, Share2, Tag } from "lucide-react"

import { buildDocumentDownloadUrl, fetchFileDetails, fetchFlows } from "@/lib/api"
import type { FileDetail, Flow } from "@/lib/types"
import { resolveViewerConfig, type ViewerCategory } from "@/lib/viewer-utils"
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Separator, TabsContent } from "@/components/ui/tabs"
import {
  SidebarChatTab,
  SidebarFlowTab,
  SidebarFormTab,
  SidebarInfoTab,
  SidebarShell,
  formatBytes,
  formatDate,
  getExtension,
} from "@/components/shared/sidebar-tabs"

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
  const [flows, setFlows] = useState<Flow[]>([])
  const [flowsLoading, setFlowsLoading] = useState(false)
  const [comments, setComments] = useState<FileDetail["comments"]>([])
  const [draftMessage, setDraftMessage] = useState("")
  const [activeTab, setActiveTab] = useState("info")
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

  useEffect(() => {
    if (!file?.id) return

    setFlowsLoading(true)
    fetchFlows(file.id)
      .then((items) => setFlows(items))
      .catch((err) => console.warn("[viewer] Failed to fetch flows", err))
      .finally(() => setFlowsLoading(false))
  }, [file?.id])

  useEffect(() => {
    if (!file) return

    setComments(file.comments)
  }, [file])

  const viewerConfig = useMemo(() => (file ? resolveViewerConfig(file) : undefined), [file])

  const handleDownload = () => {
    if (!viewerUrl) {
      return
    }

    window.open(viewerUrl, "_blank", "noopener,noreferrer")
  }

  const handleAddComment = () => {
    if (!draftMessage.trim() || !file) return

    const newComment = {
      id: `local-${Date.now()}`,
      author: file.owner,
      avatar: file.ownerAvatar,
      message: draftMessage.trim(),
      createdAt: new Date().toLocaleString("vi-VN"),
      role: "Ghi chú",
    }

    setComments((prev) => [...prev, newComment])
    setDraftMessage("")
  }

  if (isChecking || loading) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-neutral-950">
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
      <div className="flex min-h-screen items-center justify-center bg-neutral-950">
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
    <div className="flex min-h-screen flex-col bg-neutral-950 text-slate-50">
      <header className="flex h-14 items-center gap-3 border-b border-border bg-background/95 px-4">
        <Button variant="ghost" size="icon" onClick={() => router.push(MAIN_APP_ROUTE)}>
          <ArrowLeft className="h-4 w-4" />
        </Button>
        <div className="flex min-w-0 flex-1 flex-col gap-1">
          <div className="flex flex-wrap items-center gap-2">
            <Badge variant="secondary" className="capitalize">
              {viewerLabel}
            </Badge>
            <span className="truncate text-sm font-semibold text-foreground">{file.name}</span>
          </div>
          <div className="flex flex-wrap items-center gap-3 text-xs text-muted-foreground">
            <div className="flex items-center gap-1">
              <FileText className="h-3.5 w-3.5" />
              <span>{extension ? `.${extension}` : "Định dạng chưa xác định"}</span>
            </div>
            <Separator orientation="vertical" className="h-4" />
            <div className="flex items-center gap-2">
              <Avatar className="h-6 w-6">
                <AvatarImage src={file.ownerAvatar} alt={file.owner} />
                <AvatarFallback>{file.owner.slice(0, 2).toUpperCase()}</AvatarFallback>
              </Avatar>
              <span>{file.owner}</span>
            </div>
            <Separator orientation="vertical" className="h-4" />
            <span>Cập nhật: {formatDate(file.modifiedAtUtc ?? file.modified)}</span>
          </div>
        </div>
        <div className="flex items-center gap-2">
          <Button variant="ghost" size="icon" disabled={!viewerUrl} onClick={handleDownload}>
            <Download className="h-4 w-4" />
          </Button>
          <Button variant="ghost" size="icon">
            <Share2 className="h-4 w-4" />
          </Button>
        </div>
      </header>

      <div className="flex flex-1 overflow-hidden">
        <main className="flex-1 overflow-auto bg-gradient-to-b from-neutral-900 via-neutral-950 to-black p-6">
          <div className="mx-auto max-w-6xl space-y-4">
            <div className="rounded-2xl border border-border/60 bg-background/80 shadow-lg shadow-black/30">
              <div className="flex items-center justify-between border-b border-border/70 px-5 py-3 text-xs text-muted-foreground">
                <div className="flex flex-wrap items-center gap-2">
                  <BadgeCheck className="h-4 w-4 text-emerald-500" />
                  <span>Trình xem được chọn tự động dựa trên định dạng tệp.</span>
                </div>
                <div className="flex items-center gap-2">
                  <Badge variant="outline" className="flex items-center gap-1 text-xs">
                    <FileText className="h-3 w-3" />
                    {extension ? `.${extension}` : "Không rõ"}
                  </Badge>
                </div>
              </div>
              <div className="p-5">
                <ViewerPanel file={file} viewerCategory={viewerConfig.category} viewerUrl={viewerUrl} />
              </div>
            </div>
          </div>
        </main>

        <aside className="hidden w-full max-w-xs shrink-0 border-l border-border/70 bg-background/95 text-foreground lg:block">
          <SidebarShell
            tabs={{ info: true, flow: true, form: true, chat: true }}
            activeTab={activeTab}
            onTabChange={setActiveTab}
            headerBadge={`Phiên bản ${file.latestVersionNumber ?? file.latestVersionId ?? "N/A"}`}
          >
            <TabsContent value="info" className="mt-0 h-full">
              <SidebarInfoTab file={file} />
            </TabsContent>
            <TabsContent value="flow" className="mt-0 h-full">
              <SidebarFlowTab flows={flows} loading={flowsLoading} />
            </TabsContent>
            <TabsContent value="form" className="mt-0 h-full">
              <SidebarFormTab file={file} />
            </TabsContent>
            <TabsContent value="chat" className="mt-0 h-full">
              <SidebarChatTab
                comments={comments}
                draftMessage={draftMessage}
                onDraftChange={setDraftMessage}
                onSubmit={handleAddComment}
              />
            </TabsContent>
          </SidebarShell>
        </aside>
      </div>
    </div>
  )
}
