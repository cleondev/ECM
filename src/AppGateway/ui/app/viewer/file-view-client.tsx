"use client"

import { useEffect, useMemo, useState } from "react"
import { useRouter } from "next/navigation"
import { ArrowLeft, Download, MoreHorizontal, PanelRight, Share2 } from "lucide-react"

import { buildDocumentDownloadUrl, fetchFileDetails, fetchFlows } from "@/lib/api"
import type { FileDetail, Flow } from "@/lib/types"
import type { ViewerDescriptor } from "@/lib/viewer-types"
import { resolveViewerConfig, type ViewerCategory } from "@/lib/viewer-utils"
import { fetchViewerDescriptor } from "@/lib/viewer-api"
import { BrandLogo } from "@/components/brand-logo"
import { RightSidebar } from "@/components/right-sidebar"
import { ResizableHandle } from "@/components/resizable-handle"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from "@/components/ui/dropdown-menu"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { formatBytes, formatDate, getExtension } from "@/components/shared/sidebar-tabs"
import { Separator } from "@/components/ui/separator"
import { UserIdentity } from "@/components/user/user-identity"
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
  const [selectedVersionId, setSelectedVersionId] = useState<string | undefined>(undefined)

  const previewUrl =
    viewerDescriptor?.view?.url ??
    viewerDescriptor?.previewUrl ??
    (selectedVersionId ? buildDocumentDownloadUrl(selectedVersionId) : undefined)
  const thumbnailUrl = viewerDescriptor?.thumbnailUrl ?? file?.thumbnail
  const wordViewerUrl = viewerDescriptor?.sfdtUrl ?? viewerDescriptor?.view?.url ?? viewerDescriptor?.previewUrl
  const excelViewerUrl = viewerDescriptor?.excelJsonUrl ?? viewerDescriptor?.view?.url ?? viewerDescriptor?.previewUrl
  const downloadUrl = useMemo(
    () => viewerDescriptor?.downloadUrl ?? (selectedVersionId ? buildDocumentDownloadUrl(selectedVersionId) : undefined),
    [selectedVersionId, viewerDescriptor?.downloadUrl],
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
    if (!selectedVersionId) {
      setViewerDescriptor(null)
      setViewerDescriptorError(null)
      return
    }

    let cancelled = false
    setViewerDescriptorLoading(true)
    setViewerDescriptorError(null)

    fetchViewerDescriptor(selectedVersionId)
      .then((descriptor) => {
        if (!cancelled) {
          setViewerDescriptor(descriptor)
        }
      })
      .catch((err) => {
        console.error("[viewer] Failed to load viewer descriptor", err)
        if (!cancelled) {
          setViewerDescriptorError("Không thể tải trình xem cho phiên bản đã chọn. Vui lòng thử lại sau.")
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
  }, [selectedVersionId])

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

  useEffect(() => {
    if (!file) return

    const preferredVersion = file.latestVersionId ?? file.versions?.[0]?.id
    setSelectedVersionId(preferredVersion)
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
  const selectedVersion = file.versions.find((version) => version.id === selectedVersionId)
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
      <header className="flex h-16 items-center gap-3 border-b border-border bg-background px-4">
        <div className="flex items-center gap-3">
          <BrandLogo size={32} />
          <Button variant="ghost" size="icon" onClick={() => router.push(MAIN_APP_ROUTE)} aria-label="Quay lại thư viện">
            <ArrowLeft className="h-4 w-4" />
          </Button>
        </div>
        <div className="flex min-w-0 flex-1 flex-col gap-1">
          <div className="flex flex-wrap items-center gap-2">
            <span className="truncate text-base font-semibold text-foreground">{file.name}</span>
            <Badge variant="secondary" className="capitalize">
              {viewerLabel}
            </Badge>
          </div>
          {selectedVersion ? (
            <span className="text-xs text-muted-foreground">{selectedVersion.label}</span>
          ) : null}
        </div>
        <div className="hidden items-center gap-2 md:flex">
          <Select value={selectedVersionId} onValueChange={setSelectedVersionId}>
            <SelectTrigger className="w-52">
              <SelectValue placeholder="Chọn phiên bản" />
            </SelectTrigger>
            <SelectContent align="end">
              {file.versions.map((version) => (
                <SelectItem key={version.id} value={version.id}>
                  <div className="flex flex-col text-left">
                    <span className="font-medium">{version.label}</span>
                    <span className="text-xs text-muted-foreground">
                      {formatDate(version.createdAt)} • {version.size}
                    </span>
                  </div>
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
          <Button variant="ghost" size="icon" disabled={!downloadUrl} onClick={handleDownload} aria-label="Tải xuống">
            <Download className="h-4 w-4" />
          </Button>
          <Button variant="ghost" size="icon" aria-label="Chia sẻ">
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
          <UserIdentity
            userId={file.ownerId}
            size="sm"
            density="compact"
            shape="circle"
            interactive={false}
            className="ml-2"
          />
        </div>
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="ghost" size="icon" className="md:hidden" aria-label="Thao tác khác">
              <MoreHorizontal className="h-5 w-5" />
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end" className="w-64">
            <div className="px-2 py-1 text-xs font-semibold text-muted-foreground">Phiên bản</div>
            <div className="px-2 pb-2">
              <Select value={selectedVersionId} onValueChange={setSelectedVersionId}>
                <SelectTrigger>
                  <SelectValue placeholder="Chọn phiên bản" />
                </SelectTrigger>
                <SelectContent align="end">
                  {file.versions.map((version) => (
                    <SelectItem key={version.id} value={version.id}>
                      {version.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <DropdownMenuItem onSelect={handleDownload} disabled={!downloadUrl}>
              <Download className="mr-2 h-4 w-4" /> Tải xuống
            </DropdownMenuItem>
            <DropdownMenuItem>
              <Share2 className="mr-2 h-4 w-4" /> Chia sẻ
            </DropdownMenuItem>
            <DropdownMenuItem onSelect={() => setIsRightSidebarOpen((previous) => !previous)}>
              <PanelRight className="mr-2 h-4 w-4" /> {isRightSidebarOpen ? "Ẩn sidebar" : "Hiển thị sidebar"}
            </DropdownMenuItem>
            <DropdownMenuItem asChild>
              <div className="mt-1 w-full cursor-default px-1 py-1.5">
                <UserIdentity
                  userId={file.ownerId}
                  size="sm"
                  density="compact"
                  shape="circle"
                  interactive={false}
                  className="w-full"
                />
              </div>
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </header>

      <div className="flex flex-1 overflow-hidden bg-muted/40">
        <main className="flex-1 overflow-auto p-6">
          <div className="mx-auto max-w-6xl space-y-4">
            <div className="rounded-2xl border border-border bg-background shadow-sm">
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
