"use client"

import { useEffect, useMemo, useState } from "react"
import { useRouter } from "next/navigation"
import { ArrowLeft, BadgeCheck, Download, FileText, PanelRight, Share2 } from "lucide-react"

import { buildDocumentDownloadUrl, fetchFileDetails, fetchFlows } from "@/lib/api"
import type { FileDetail, Flow } from "@/lib/types"
import type { ViewerDescriptor } from "@/lib/viewer-types"
import { resolveViewerConfig, type ViewerCategory } from "@/lib/viewer-utils"
import { fetchViewerDescriptor } from "@/lib/viewer-api"
import { RightSidebar } from "@/components/right-sidebar"
import { ResizableHandle } from "@/components/resizable-handle"
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { formatBytes, formatDate, getExtension } from "@/components/shared/sidebar-tabs"
import { Separator } from "@/components/ui/separator"
import { ViewerPanel } from "@/components/viewers/ViewerPanel"

const MAIN_APP_ROUTE = "/app/"

export type FileViewClientProps = {
  fileId: string
  targetPath: string
  isAuthenticated: boolean
  isChecking: boolean
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
  const [activeTab, setActiveTab] = useState<"info" | "flow" | "form" | "chat">("info")
  const [isRightSidebarOpen, setIsRightSidebarOpen] = useState(true)
  const [rightSidebarWidth, setRightSidebarWidth] = useState(360)
  const [viewerDescriptor, setViewerDescriptor] = useState<ViewerDescriptor | null>(null)
  const [viewerDescriptorError, setViewerDescriptorError] = useState<string | null>(null)
  const [viewerDescriptorLoading, setViewerDescriptorLoading] = useState(false)

  const previewUrl =
    viewerDescriptor?.view?.url ??
    viewerDescriptor?.previewUrl ??
    (file?.latestVersionId ? buildDocumentDownloadUrl(file.latestVersionId) : undefined)
  const thumbnailUrl = viewerDescriptor?.thumbnailUrl ?? file?.thumbnail
  const wordViewerUrl = viewerDescriptor?.sfdtUrl ?? viewerDescriptor?.view?.url ?? viewerDescriptor?.previewUrl
  const excelViewerUrl = viewerDescriptor?.excelJsonUrl ?? viewerDescriptor?.view?.url ?? viewerDescriptor?.previewUrl
  const downloadUrl = useMemo(
    () => viewerDescriptor?.downloadUrl ?? (file?.latestVersionId ? buildDocumentDownloadUrl(file.latestVersionId) : undefined),
    [file?.latestVersionId, viewerDescriptor?.downloadUrl],
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
    if (!file?.latestVersionId) {
      setViewerDescriptor(null)
      setViewerDescriptorError(null)
      return
    }

    let cancelled = false
    setViewerDescriptorLoading(true)
    setViewerDescriptorError(null)

    fetchViewerDescriptor(file.latestVersionId)
      .then((descriptor) => {
        if (!cancelled) {
          setViewerDescriptor(descriptor)
        }
      })
      .catch((err) => {
        console.error("[viewer] Failed to load viewer descriptor", err)
        if (!cancelled) {
          setViewerDescriptorError("Không thể tải cấu hình trình xem từ máy chủ.")
          setViewerDescriptor(null)
        }
      })
      .finally(() => {
        if (!cancelled) {
          setViewerDescriptorLoading(false)
        }
      })

    return () => {
      cancelled = true
    }
  }, [file?.latestVersionId])

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

  const viewerConfig = useMemo(
    () => (file ? resolveViewerConfig(file, viewerDescriptor ?? undefined) : undefined),
    [file, viewerDescriptor],
  )

  const handleDownload = () => {
    if (!downloadUrl) {
      return
    }

    window.open(downloadUrl, "_blank", "noopener,noreferrer")
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

  if (isChecking || loading || viewerDescriptorLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-background">
        <div className="rounded-xl border border-border bg-background px-6 py-10 text-center shadow-sm">
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
      <div className="flex min-h-screen items-center justify-center bg-background">
        <div className="max-w-md space-y-4 rounded-2xl border border-border bg-background p-6 text-center shadow-sm">
          <p className="text-lg font-semibold text-foreground">Không thể mở tệp</p>
          <p className="text-sm text-muted-foreground">{error ?? "Tệp đã bị xóa hoặc bạn không có quyền truy cập."}</p>
          <Button onClick={() => router.push(MAIN_APP_ROUTE)}>Quay lại thư viện</Button>
        </div>
      </div>
    )
  }

  const extension = getExtension(file.name)
  const viewerLabelMap: Record<ViewerCategory, string> = {
    pdf: "PDF",
    image: "Hình ảnh",
    video: "Video",
    code: "Mã nguồn",
    word: "Word",
    excel: "Excel",
    unsupported: "Chưa hỗ trợ",
  }
  const viewerLabel = viewerLabelMap[viewerConfig.viewerType] ?? viewerLabelMap[viewerConfig.category] ?? viewerConfig.category

  return (
    <div className="flex min-h-screen flex-col bg-background text-foreground">
      <header className="flex h-14 items-center gap-3 border-b border-border bg-background px-4">
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
          <Button variant="ghost" size="icon" disabled={!downloadUrl} onClick={handleDownload}>
            <Download className="h-4 w-4" />
          </Button>
          <Button variant="ghost" size="icon">
            <Share2 className="h-4 w-4" />
          </Button>
          <Button
            variant="ghost"
            size="icon"
            onClick={() => setIsRightSidebarOpen((previous) => !previous)}
            aria-label={isRightSidebarOpen ? "Ẩn thanh thông tin" : "Hiển thị thanh thông tin"}
          >
            <PanelRight className="h-4 w-4" />
          </Button>
        </div>
      </header>

      <div className="flex flex-1 overflow-hidden bg-muted/40">
        <main className="flex-1 overflow-auto p-6">
          <div className="mx-auto max-w-6xl space-y-4">
            <div className="rounded-2xl border border-border bg-background shadow-sm">
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
              {viewerDescriptorError ? (
                <div className="mb-4 rounded-lg border border-destructive/30 bg-destructive/10 px-3 py-2 text-sm text-destructive">
                  {viewerDescriptorError}
                </div>
              ) : null}
              <ViewerPanel
                file={file}
                viewerCategory={viewerConfig.category}
                viewerType={viewerConfig.viewerType}
                descriptor={viewerDescriptor}
                previewUrl={previewUrl}
                thumbnailUrl={thumbnailUrl ?? undefined}
                wordViewerUrl={wordViewerUrl}
                excelViewerUrl={excelViewerUrl}
              />
            </div>
          </div>
        </div>
        </main>

        {isRightSidebarOpen && (
          <ResizableHandle
            onResize={(delta) =>
              setRightSidebarWidth((previous) => Math.max(300, Math.min(560, previous + delta)))
            }
          />
        )}

        {isRightSidebarOpen ? (
          <aside
            className="hidden h-full shrink-0 border-l border-border bg-background text-foreground lg:block"
            style={{ width: rightSidebarWidth }}
          >
            <RightSidebar
              selectedFile={file}
              activeTab={activeTab}
              onTabChange={setActiveTab}
              onClose={() => setIsRightSidebarOpen(false)}
              onFileUpdate={(updatedFile) => setFile(updatedFile)}
              showTabShortcuts
            />
          </aside>
        ) : null}
      </div>
    </div>
  )
}
