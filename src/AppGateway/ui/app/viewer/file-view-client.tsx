"use client"

import { useEffect, useMemo, useRef, useState } from "react"
import { useRouter } from "next/navigation"
import { ArrowLeft, BadgeCheck, Download, FileText, FileWarning, PanelRight, Share2 } from "lucide-react"

import {
  DocumentEditorContainerComponent,
  Inject as DocumentEditorInject,
  Toolbar as DocumentEditorToolbar,
} from "@syncfusion/ej2-react-documenteditor"
import { SpreadsheetComponent } from "@syncfusion/ej2-react-spreadsheet"
import "@syncfusion/ej2-base/styles/material.css"
import "@syncfusion/ej2-buttons/styles/material.css"
import "@syncfusion/ej2-dropdowns/styles/material.css"
import "@syncfusion/ej2-grids/styles/material.css"
import "@syncfusion/ej2-inputs/styles/material.css"
import "@syncfusion/ej2-lists/styles/material.css"
import "@syncfusion/ej2-navigations/styles/material.css"
import "@syncfusion/ej2-popups/styles/material.css"
import "@syncfusion/ej2-react-documenteditor/styles/material.css"
import "@syncfusion/ej2-react-pdfviewer/styles/material.css"
import "@syncfusion/ej2-react-spreadsheet/styles/material.css"
import "@syncfusion/ej2-splitbuttons/styles/material.css"

import { buildDocumentDownloadUrl, fetchFileDetails, fetchFlows, fetchViewerDescriptor } from "@/lib/api"
import { ensureSyncfusionLicense } from "@/lib/syncfusion"
import type { FileDetail, Flow, ViewerDescriptor } from "@/lib/types"
import { resolveViewerConfig, type ViewerCategory } from "@/lib/viewer-utils"
import { RightSidebar } from "@/components/right-sidebar"
import { ResizableHandle } from "@/components/resizable-handle"
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { formatBytes, formatDate, getExtension } from "@/components/shared/sidebar-tabs"

import { PdfViewer } from "./pdf-viewer"
import { Separator } from "@/components/ui/separator"

const MAIN_APP_ROUTE = "/app/"

ensureSyncfusionLicense()

export type FileViewClientProps = {
  fileId: string
  targetPath: string
  isAuthenticated: boolean
  isChecking: boolean
}

type ViewerPanelProps = {
  file: FileDetail
  viewerCategory: ViewerCategory
  previewUrl?: string
  thumbnailUrl?: string
  wordViewerUrl?: string | null
  excelViewerUrl?: string | null
}

function PdfViewerPanel({ previewUrl, file }: { previewUrl?: string; file: FileDetail }) {
  if (!previewUrl) {
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

  return <PdfViewer documentUrl={previewUrl} />
}

function ImageViewerPanel({ file, previewUrl, thumbnailUrl }: { file: FileDetail; previewUrl?: string; thumbnailUrl?: string }) {
  const source =
    previewUrl ||
    (file.preview.kind === "image" || file.preview.kind === "design"
      ? file.preview.url
      : undefined)

  const fallback = thumbnailUrl

  if (!source && !fallback) {
    return <UnsupportedViewerPanel file={file} message="Không có nội dung hình ảnh để hiển thị." />
  }

  return (
    <div className="flex items-center justify-center rounded-lg border border-border bg-background p-4">
      <img src={source ?? fallback} alt={file.name} className="max-h-[70vh] w-auto max-w-full rounded-md shadow" />
    </div>
  )
}

function VideoViewerPanel({ file, previewUrl, thumbnailUrl }: { file: FileDetail; previewUrl?: string; thumbnailUrl?: string }) {
  const source = file.preview.kind === "video" ? file.preview.url : previewUrl
  const poster = file.preview.kind === "video" ? file.preview.poster : thumbnailUrl

  if (!source) {
    return <UnsupportedViewerPanel file={file} message="Không có nội dung video để hiển thị." />
  }

  return (
    <div className="overflow-hidden rounded-lg border border-border bg-background">
      <video controls className="h-[75vh] w-full bg-black" poster={poster} src={source} />
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

function DocumentEditorPanel({ file, sfdtUrl }: { file: FileDetail; sfdtUrl?: string | null }) {
  const editorRef = useRef<DocumentEditorContainerComponent>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!sfdtUrl) {
      setError("Không có nội dung Word để hiển thị.")
      return
    }

    let cancelled = false
    setLoading(true)
    setError(null)

    fetch(sfdtUrl, { credentials: "include" })
      .then(async (response) => {
        if (!response.ok) {
          throw new Error(`Viewer endpoint returned status ${response.status}`)
        }

        const content = await response.text()

        if (!cancelled) {
          editorRef.current?.documentEditor?.open(content, "Sfdt")
        }
      })
      .catch((err) => {
        console.error("[viewer] Failed to load Word viewer content", err)
        if (!cancelled) {
          setError("Không thể tải nội dung Word từ máy chủ.")
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
  }, [sfdtUrl])

  if (error) {
    return <UnsupportedViewerPanel file={file} message={error} />
  }

  if (!sfdtUrl) {
    return <UnsupportedViewerPanel file={file} />
  }

  return (
    <div className="space-y-3">
      {loading ? (
        <div className="rounded-lg border border-border bg-muted/60 p-3 text-sm text-muted-foreground">
          Đang tải nội dung tài liệu…
        </div>
      ) : null}
      <DocumentEditorContainerComponent
        ref={editorRef}
        enableToolbar
        height="75vh"
        serviceUrl="https://services.syncfusion.com/react/production/api/documenteditor/"
        style={{ border: "1px solid hsl(var(--border))" }}
      >
        <DocumentEditorInject services={[DocumentEditorToolbar]} />
      </DocumentEditorContainerComponent>
    </div>
  )
}

function SpreadsheetViewerPanel({ file, spreadsheetUrl }: { file: FileDetail; spreadsheetUrl?: string | null }) {
  const spreadsheetRef = useRef<SpreadsheetComponent>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!spreadsheetUrl) {
      setError("Không có nội dung bảng tính để hiển thị.")
      return
    }

    let cancelled = false
    setLoading(true)
    setError(null)

    fetch(spreadsheetUrl, { credentials: "include" })
      .then(async (response) => {
        if (!response.ok) {
          throw new Error(`Viewer endpoint returned status ${response.status}`)
        }

        const payload = await response.json()

        if (!cancelled) {
          spreadsheetRef.current?.openFromJson({ file: payload })
        }
      })
      .catch((err) => {
        console.error("[viewer] Failed to load spreadsheet viewer content", err)
        if (!cancelled) {
          setError("Không thể tải dữ liệu bảng tính từ máy chủ.")
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
  }, [spreadsheetUrl])

  if (error) {
    return <UnsupportedViewerPanel file={file} message={error} />
  }

  if (!spreadsheetUrl) {
    return <UnsupportedViewerPanel file={file} />
  }

  return (
    <div className="space-y-3">
      {loading ? (
        <div className="rounded-lg border border-border bg-muted/60 p-3 text-sm text-muted-foreground">
          Đang tải nội dung bảng tính…
        </div>
      ) : null}
      <div className="overflow-hidden rounded-lg border border-border bg-background">
        <SpreadsheetComponent ref={spreadsheetRef} height={"75vh"} allowOpen allowSave />
      </div>
    </div>
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

function ViewerPanel({ file, viewerCategory, previewUrl, thumbnailUrl, wordViewerUrl, excelViewerUrl }: ViewerPanelProps) {
  switch (viewerCategory) {
    case "pdf":
      return <PdfViewerPanel file={file} previewUrl={previewUrl} />
    case "image":
      return <ImageViewerPanel file={file} previewUrl={previewUrl} thumbnailUrl={thumbnailUrl} />
    case "video":
      return <VideoViewerPanel file={file} previewUrl={previewUrl} thumbnailUrl={thumbnailUrl} />
    case "word":
      return <DocumentEditorPanel file={file} sfdtUrl={wordViewerUrl ?? previewUrl} />
    case "excel":
      return <SpreadsheetViewerPanel file={file} spreadsheetUrl={excelViewerUrl ?? previewUrl} />
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
  const [activeTab, setActiveTab] = useState<"info" | "flow" | "form" | "chat">("info")
  const [isRightSidebarOpen, setIsRightSidebarOpen] = useState(true)
  const [rightSidebarWidth, setRightSidebarWidth] = useState(360)
  const [viewerDescriptor, setViewerDescriptor] = useState<ViewerDescriptor | null>(null)
  const [viewerDescriptorError, setViewerDescriptorError] = useState<string | null>(null)
  const [viewerDescriptorLoading, setViewerDescriptorLoading] = useState(false)

  const previewUrl = viewerDescriptor?.previewUrl ?? (file?.latestVersionId ? buildDocumentDownloadUrl(file.latestVersionId) : undefined)
  const thumbnailUrl = viewerDescriptor?.thumbnailUrl ?? file?.thumbnail
  const wordViewerUrl = viewerDescriptor?.wordViewerUrl
  const excelViewerUrl = viewerDescriptor?.excelViewerUrl
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
