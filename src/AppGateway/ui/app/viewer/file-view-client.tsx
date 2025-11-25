"use client"

import { useEffect, useMemo, useRef, useState } from "react"
import { useRouter } from "next/navigation"
import {
  ArrowLeft,
  ChevronLeft,
  ChevronRight,
  Download,
  DownloadCloud,
  FolderInput,
  Image,
  LayoutList,
  List,
  Maximize2,
  MessageCircle,
  MoreVertical,
  PanelLeft,
  Paperclip,
  Percent,
  Printer,
  RotateCw,
  Send,
  Smile,
  ZoomIn,
  ZoomOut,
} from "lucide-react"

import { buildDocumentDownloadUrl, fetchFileDetails } from "@/lib/api"
import type { FileDetail, FileDocumentPreviewPage } from "@/lib/types"
import type { ViewerPreference } from "@/lib/viewer-utils"
import { resolveViewerConfig } from "@/lib/viewer-utils"
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"

const MAIN_APP_ROUTE = "/app/"

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

  const pages: FileDocumentPreviewPage[] = useMemo(() => {
    if (file?.preview.kind === "document" && file.preview.pages?.length) {
      return file.preview.pages
    }

    return []
  }, [file?.preview])

  const [sidebarCollapsed, setSidebarCollapsed] = useState(false)
  const [selectedPage, setSelectedPage] = useState<number | null>(null)
  const [zoom, setZoom] = useState(100)
  const [rotation, setRotation] = useState(0)
  const [messages, setMessages] = useState<
    { id: string; author: string; initials: string; content: string; timestamp: string; self?: boolean }[]
  >([
    {
      id: "1",
      author: "Trần Tuấn",
      initials: "TT",
      content: "Mình muốn kiểm tra lại phần tóm tắt chương 2, mọi người xem giúp nhé.",
      timestamp: "10:20",
    },
    {
      id: "2",
      author: "Lan Vũ",
      initials: "LV",
      content: "Đã note, mình đang đọc đoạn này.",
      timestamp: "10:24",
    },
    {
      id: "3",
      author: "Bạn",
      initials: "BN",
      content: "Mình sẽ highlight lại số liệu trang 5 rồi gửi mọi người.",
      timestamp: "10:27",
      self: true,
    },
  ])
  const [draft, setDraft] = useState("")
  const messagesEndRef = useRef<HTMLDivElement | null>(null)

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

  const activePage = selectedPage ?? pages[0]?.number ?? null

  const updateZoom = (delta: number) => {
    setZoom((current) => {
      const next = Math.max(50, Math.min(200, current + delta))
      return Math.round(next)
    })
  }

  const rotateClockwise = () => setRotation((value) => (value + 90) % 360)

  const sendMessage = () => {
    const trimmed = draft.trim()
    if (!trimmed) return

    setMessages((prev) => [
      ...prev,
      {
        id: crypto.randomUUID(),
        author: "Bạn",
        initials: "BN",
        content: trimmed,
        timestamp: new Date().toLocaleTimeString("vi-VN", { hour: "2-digit", minute: "2-digit" }),
        self: true,
      },
    ])
    setDraft("")
  }

  useEffect(() => {
    setSelectedPage((current) => current ?? pages[0]?.number ?? null)
  }, [pages])

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth" })
  }, [messages])

  return (
    <div className="flex min-h-screen bg-[#0f1116] text-slate-50">
      <div className="flex min-w-0 flex-[3] flex-col border-r border-slate-800 bg-[#12151c]">
      <header className="flex h-12 items-center justify-between bg-[#2a2a2a] px-4 text-xs text-slate-100">
          <div className="flex items-center gap-3">
            <Button variant="ghost" size="sm" onClick={() => router.push(MAIN_APP_ROUTE)} className="h-8 gap-2 px-2">
              <ArrowLeft className="h-4 w-4" />
              Thoát
            </Button>
            <div className="flex flex-col leading-tight">
              <span className="text-[13px] text-slate-200">{file.name}</span>
              <span className="text-sm font-semibold text-white">{file.latestVersionId ?? file.id}</span>
            </div>
          </div>

          <div className="flex items-center gap-2 rounded-lg bg-[#3a3a3a] px-2 py-1 text-slate-100">
            <button
              className="flex h-8 w-8 items-center justify-center rounded-md hover:bg-white/10"
              onClick={() => setSelectedPage((page) => Math.max(1, (page ?? 1) - 1))}
              aria-label="Trang trước"
            >
              <ChevronLeft className="h-4 w-4" />
            </button>
            <div className="min-w-[120px] text-center text-sm font-medium">
              Trang {activePage ?? 1} / {pages.length || 1}
            </div>
            <button
              className="flex h-8 w-8 items-center justify-center rounded-md hover:bg-white/10"
              onClick={() => setSelectedPage((page) => Math.min(pages.length || 1, (page ?? 1) + 1))}
              aria-label="Trang sau"
            >
              <ChevronRight className="h-4 w-4" />
            </button>
            <div className="mx-2 h-8 w-px bg-white/20" />
            <button
              className="flex h-8 w-8 items-center justify-center rounded-md hover:bg-white/10"
              onClick={() => updateZoom(-10)}
              aria-label="Thu nhỏ"
            >
              <ZoomOut className="h-4 w-4" />
            </button>
            <div className="flex items-center gap-1 rounded-md bg-black/20 px-2 text-sm">
              <Percent className="h-3 w-3" />
              <span>{zoom}%</span>
            </div>
            <button
              className="flex h-8 w-8 items-center justify-center rounded-md hover:bg-white/10"
              onClick={() => updateZoom(10)}
              aria-label="Phóng to"
            >
              <ZoomIn className="h-4 w-4" />
            </button>
            <div className="mx-2 h-8 w-px bg-white/20" />
            <button
              className="flex h-8 w-8 items-center justify-center rounded-md hover:bg-white/10"
              onClick={rotateClockwise}
              aria-label="Xoay"
            >
              <RotateCw className="h-4 w-4" />
            </button>
            <button className="flex h-8 w-8 items-center justify-center rounded-md hover:bg-white/10" aria-label="In">
              <Printer className="h-4 w-4" />
            </button>
            <button
              className="flex h-8 w-8 items-center justify-center rounded-md hover:bg-white/10"
              onClick={handleDownload}
              aria-label="Tải xuống"
              disabled={!viewerUrl}
            >
              <Download className="h-4 w-4" />
            </button>
          </div>

          <div className="flex items-center gap-2">
            <button className="flex h-9 w-9 items-center justify-center rounded-md bg-white/10 hover:bg-white/20" aria-label="Toàn màn hình">
              <Maximize2 className="h-4 w-4" />
            </button>
            <button className="flex h-9 w-9 items-center justify-center rounded-md bg-white/10 hover:bg-white/20" aria-label="Tùy chọn">
              <MoreVertical className="h-4 w-4" />
            </button>
          </div>
      </header>

        <div className="flex min-h-0 flex-1">
          <div
            className={`hidden h-full flex-col bg-[#2f2f2f] text-slate-100 transition-all duration-200 md:flex ${sidebarCollapsed ? "w-14" : "w-[120px]"}`}
          >
            <div className="flex items-center justify-between px-2 py-3 text-xs font-medium">
              <button
                className="flex h-9 w-9 items-center justify-center rounded-md hover:bg-white/10"
                onClick={() => setSidebarCollapsed((value) => !value)}
                aria-label="Thu gọn thanh trang"
              >
                <PanelLeft className="h-4 w-4" />
              </button>
              {!sidebarCollapsed ? (
                <div className="flex items-center gap-1 text-[11px] uppercase tracking-wide text-slate-300">
                  <span>Preview</span>
                </div>
              ) : null}
            </div>
            {!sidebarCollapsed ? (
              <div className="flex items-center gap-1 px-2 text-[11px] text-slate-300">
                <Image className="h-4 w-4" />
                <span>Ảnh</span>
                <LayoutList className="h-4 w-4" />
              </div>
            ) : null}

            <div className="mt-3 flex-1 space-y-3 overflow-y-auto px-2 pb-4 pr-1">
              {pages.length ? (
                pages.map((page) => (
                  <button
                    key={page.number}
                    className={`group flex w-full flex-col items-center gap-1 rounded-md border-2 border-transparent bg-[#3a3a3a] p-2 text-xs transition hover:border-slate-300 ${activePage === page.number ? "border-[#2b7bff]" : ""}`}
                    onClick={() => setSelectedPage(page.number)}
                  >
                    <div
                      className="aspect-[3/4] w-full rounded-[6px] bg-gradient-to-br from-slate-200 via-white to-slate-100 shadow-sm group-hover:brightness-95"
                      style={{
                        backgroundImage: page.thumbnail ? `url(${page.thumbnail})` : undefined,
                        backgroundSize: "cover",
                        backgroundPosition: "center",
                      }}
                    />
                    <span className="font-semibold text-white">Trang {page.number}</span>
                  </button>
                ))
              ) : (
                <div className="flex flex-col items-center justify-center gap-2 rounded-md bg-[#3a3a3a] p-4 text-center text-xs text-slate-300">
                  <List className="h-4 w-4" />
                  Chưa có thumbnail
                </div>
              )}
            </div>
          </div>

          <div className="flex min-w-0 flex-1 flex-col bg-[#1e1e1e]">
            <div className="flex items-center justify-between border-b border-black/40 px-6 py-3 text-xs text-slate-200">
              <div className="flex items-center gap-2">
                <FolderInput className="h-4 w-4" />
                <span className="font-medium">{file.folder}</span>
              </div>
              <div className="flex items-center gap-2 text-[11px] uppercase tracking-wide text-slate-300">
                <Badge variant="secondary" className="bg-[#2b7bff] text-[11px] font-semibold text-white">
                  PDF Viewer
                </Badge>
                <span className="rounded-full bg-white/10 px-2 py-1">{file.size}</span>
                <button
                  className="flex h-8 items-center gap-2 rounded-md bg-white/10 px-3 text-[12px] font-medium hover:bg-white/20"
                  onClick={handleDownload}
                  disabled={!viewerUrl}
                >
                  <DownloadCloud className="h-4 w-4" />
                  Tải xuống
                </button>
              </div>
            </div>

            <div className="relative flex-1 overflow-y-auto px-10 py-10">
              <div className="flex justify-center">
                <div
                  className="inline-block rounded-xl bg-white/95 p-4 shadow-2xl"
                  style={{
                    transform: `scale(${zoom / 100}) rotate(${rotation}deg)`,
                    transformOrigin: "top center",
                    transition: "transform 120ms ease",
                  }}
                >
                  {viewerUrl ? (
                    <iframe
                      className="h-[min(90vh,1200px)] w-[min(100%,960px)] rounded-lg border border-slate-200 bg-white shadow"
                      src={`${viewerUrl}${activePage ? `#page=${activePage}` : ""}`}
                      title={`${file.name} - PDF viewer`}
                    />
                  ) : activePage ? (
                    <div className="relative overflow-hidden rounded-lg border border-slate-200 bg-white shadow">
                      <div
                        className="aspect-[3/4] w-[min(100%,860px)] bg-gradient-to-br from-slate-100 via-white to-slate-50"
                        style={{
                          backgroundImage: pages.find((p) => p.number === activePage)?.thumbnail
                            ? `url(${pages.find((p) => p.number === activePage)?.thumbnail})`
                            : undefined,
                          backgroundSize: "contain",
                          backgroundRepeat: "no-repeat",
                          backgroundPosition: "center",
                        }}
                      />
                    </div>
                  ) : (
                    <div className="flex h-[620px] w-[min(100%,960px)] flex-col items-center justify-center gap-4 rounded-lg border border-dashed border-slate-500/60 bg-[#161616] text-center text-slate-300">
                      <MessageCircle className="h-6 w-6" />
                      <p className="text-sm">Không tìm thấy trang để hiển thị.</p>
                    </div>
                  )}
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <aside className="flex w-full max-w-[420px] flex-col bg-white text-slate-900 md:max-w-[360px] lg:max-w-[420px]">
        <div className="flex h-12 items-center justify-between border-b border-slate-200 px-4 text-sm font-semibold">
          <div className="flex items-center gap-2">
            <span className="h-2.5 w-2.5 rounded-full bg-green-500" />
            <span>3 người đang xem tài liệu</span>
          </div>
          <div className="flex items-center gap-2 text-xs text-slate-500">
            <Avatar className="h-7 w-7">
              <AvatarImage src={file.ownerAvatar} alt={file.owner} />
              <AvatarFallback>{file.owner.slice(0, 2).toUpperCase()}</AvatarFallback>
            </Avatar>
            <Avatar className="h-7 w-7">
              <AvatarFallback>LV</AvatarFallback>
            </Avatar>
            <Avatar className="h-7 w-7">
              <AvatarFallback>TT</AvatarFallback>
            </Avatar>
          </div>
        </div>

        <div className="flex-1 overflow-y-auto bg-slate-50 px-4 py-3">
          <div className="flex flex-col gap-4">
            {messages.map((message) => (
              <div key={message.id} className={`flex ${message.self ? "justify-end" : "justify-start"}`}>
                <div className={`flex max-w-[75%] flex-col gap-1 ${message.self ? "items-end" : "items-start"}`}>
                  <div className="flex items-center gap-2 text-xs text-slate-500">
                    <Avatar className="h-8 w-8">
                      <AvatarFallback>{message.initials}</AvatarFallback>
                    </Avatar>
                    <span className="font-semibold text-slate-800">{message.author}</span>
                    <span className="text-[11px] text-slate-400">{message.timestamp}</span>
                  </div>
                  <div
                    className={`w-fit rounded-2xl px-3 py-2 text-sm shadow ${
                      message.self ? "bg-[#1a73e8] text-white" : "bg-[#f1f1f1] text-black"
                    }`}
                  >
                    {message.content}
                  </div>
                </div>
              </div>
            ))}
            <div className="flex items-center gap-2 text-xs text-slate-500">
              <span className="h-2 w-2 rounded-full bg-green-500" />
              <span>Lan Vũ đang nhập…</span>
            </div>
            <div ref={messagesEndRef} />
          </div>
        </div>

        <div className="border-t border-slate-200 bg-white p-3">
          <div className="flex items-center gap-2">
            <Button variant="ghost" size="icon" className="h-10 w-10 text-slate-600">
              <Smile className="h-5 w-5" />
            </Button>
            <Button variant="ghost" size="icon" className="h-10 w-10 text-slate-600">
              <Paperclip className="h-5 w-5" />
            </Button>
            <Input
              value={draft}
              onChange={(event) => setDraft(event.target.value)}
              onKeyDown={(event) => {
                if (event.key === "Enter" && !event.shiftKey) {
                  event.preventDefault()
                  sendMessage()
                }
              }}
              placeholder="Nhập tin nhắn về tài liệu…"
              className="flex-1 rounded-full bg-slate-50"
            />
            <Button className="h-10 w-10 rounded-full bg-[#1a73e8]" onClick={sendMessage}>
              <Send className="h-4 w-4" />
            </Button>
          </div>
        </div>
      </aside>
    </div>
  )
}
