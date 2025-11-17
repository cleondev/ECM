"use client"

import { useEffect, useState } from "react"
import { useRouter } from "next/navigation"
import { ArrowLeft, DownloadCloud, Share2, Tag } from "lucide-react"

import { useAuthGuard } from "@/hooks/use-auth-guard"
import { buildDocumentDownloadUrl, fetchFileDetails } from "@/lib/api"
import type { FileDetail } from "@/lib/types"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"
import { Separator } from "@/components/ui/separator"
import { FileViewer } from "@/components/file-viewer"

const MAIN_APP_ROUTE = "/app/"

const dateFormatter = new Intl.DateTimeFormat("vi-VN", {
  dateStyle: "medium",
  timeStyle: "short",
})

export type FileViewClientProps = { params: { fileId: string } }

export default function FileViewClient({ params }: FileViewClientProps) {
  const router = useRouter()
  const { isAuthenticated, isChecking } = useAuthGuard(MAIN_APP_ROUTE)
  const [file, setFile] = useState<FileDetail | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!isAuthenticated) {
      return
    }

    let cancelled = false
    setLoading(true)
    setError(null)

    fetchFileDetails(params.fileId)
      .then((detail) => {
        if (!cancelled) {
          setFile(detail)
        }
      })
      .catch((err) => {
        console.error("[ui] Failed to fetch file details", err)
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
  }, [isAuthenticated, params.fileId])

  const handleDownload = () => {
    if (!file?.latestVersionId) {
      return
    }

    const url = buildDocumentDownloadUrl(file.latestVersionId)
    window.open(url, "_blank", "noopener,noreferrer")
  }

  if (isChecking || loading) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-slate-50 dark:bg-slate-950">
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
      <div className="flex min-h-screen items-center justify-center bg-slate-50 dark:bg-slate-950">
        <div className="max-w-md space-y-4 rounded-2xl border border-border bg-background/95 p-6 text-center shadow-sm">
          <p className="text-lg font-semibold text-foreground">Không thể mở tệp</p>
          <p className="text-sm text-muted-foreground">{error ?? "Tệp đã bị xóa hoặc bạn không có quyền truy cập."}</p>
          <Button onClick={() => router.push(MAIN_APP_ROUTE)}>Quay lại thư viện</Button>
        </div>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-gradient-to-b from-slate-50 via-white to-slate-100 px-4 py-6 dark:from-slate-950 dark:via-slate-900 dark:to-slate-950">
      <div className="mx-auto flex max-w-[1440px] flex-col gap-6">
        <div className="flex flex-wrap items-center gap-4">
          <Button variant="ghost" size="sm" onClick={() => router.push(MAIN_APP_ROUTE)}>
            <ArrowLeft className="h-4 w-4" />
            Trở lại thư viện
          </Button>
          <div>
            <h1 className="text-2xl font-semibold text-foreground">{file.name}</h1>
            <p className="text-sm text-muted-foreground">Xem chi tiết và nội dung trực tuyến của tệp được chia sẻ.</p>
          </div>
          <div className="ml-auto flex flex-wrap gap-3">
            <Button variant="outline" disabled={!file.latestVersionId} onClick={handleDownload} className="gap-2">
              <DownloadCloud className="h-4 w-4" />
              Tải xuống
            </Button>
            <Button variant="secondary" className="gap-2" disabled>
              <Share2 className="h-4 w-4" />
              Chia sẻ (sắp ra mắt)
            </Button>
          </div>
        </div>

        <div className="grid gap-6 lg:grid-cols-[minmax(0,2fr),minmax(320px,1fr)]">
          <div className="space-y-6">
            <Card className="shadow-lg">
              <CardHeader>
                <CardTitle>Trình xem trực tuyến</CardTitle>
                <CardDescription>Tương thích với nhiều định dạng tài liệu và media.</CardDescription>
              </CardHeader>
              <CardContent>
                <FileViewer file={file} />
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle>Lịch sử phiên bản</CardTitle>
                <CardDescription>Theo dõi các lần chỉnh sửa đáng chú ý của tệp.</CardDescription>
              </CardHeader>
              <CardContent>
                <div className="space-y-4">
                  {file.versions.map((version) => (
                    <div key={version.id} className="rounded-xl border border-border/80 bg-background/80 p-4">
                      <div className="flex items-center justify-between text-sm font-medium text-foreground">
                        <span>{version.label}</span>
                        <span>{version.size}</span>
                      </div>
                      <p className="mt-1 text-sm text-muted-foreground">{version.notes}</p>
                      <div className="mt-3 flex flex-wrap items-center gap-3 text-xs text-muted-foreground">
                        <span>Người cập nhật: {version.author}</span>
                        <Separator orientation="vertical" className="h-4" />
                        <span>{dateFormatter.format(new Date(version.createdAt))}</span>
                      </div>
                    </div>
                  ))}
                </div>
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle>Bình luận nội bộ</CardTitle>
                <CardDescription>Các góp ý gần nhất của cộng tác viên (dữ liệu minh họa).</CardDescription>
              </CardHeader>
              <CardContent>
                <div className="space-y-4">
                  {file.comments.map((comment) => (
                    <div key={comment.id} className="flex gap-3 rounded-xl border border-border/80 bg-background/90 p-3">
                      <Avatar className="h-10 w-10">
                        <AvatarImage src={comment.avatar} alt={comment.author} />
                        <AvatarFallback>{comment.author.slice(0, 2).toUpperCase()}</AvatarFallback>
                      </Avatar>
                      <div className="space-y-1 text-sm">
                        <div className="flex flex-wrap items-center gap-2">
                          <span className="font-semibold text-foreground">{comment.author}</span>
                          {comment.role ? (
                            <Badge variant="secondary" className="text-xs">
                              {comment.role}
                            </Badge>
                          ) : null}
                          <span className="text-xs text-muted-foreground">
                            {dateFormatter.format(new Date(comment.createdAt))}
                          </span>
                        </div>
                        <p className="text-muted-foreground">{comment.message}</p>
                      </div>
                    </div>
                  ))}
                </div>
                <div className="mt-4 rounded-lg border border-dashed border-border/70 bg-muted/30 px-4 py-3 text-sm text-muted-foreground">
                  Tính năng thảo luận trực tiếp sẽ hỗ trợ phản hồi realtime trong các bản phát hành tiếp theo.
                </div>
              </CardContent>
            </Card>
          </div>

          <div className="space-y-6">
            <Card>
              <CardHeader>
                <CardTitle>Thông tin tệp</CardTitle>
                <CardDescription>Thông tin tổng quan và nhãn phân loại.</CardDescription>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="flex items-center gap-3">
                  <Avatar className="h-12 w-12">
                    <AvatarImage src={file.ownerAvatar} alt={file.owner} />
                    <AvatarFallback>{file.owner.slice(0, 2).toUpperCase()}</AvatarFallback>
                  </Avatar>
                  <div>
                    <p className="text-sm text-muted-foreground">Chủ sở hữu</p>
                    <p className="text-base font-semibold text-foreground">{file.owner}</p>
                  </div>
                </div>
                <dl className="grid gap-3 text-sm">
                  <div className="flex justify-between rounded-lg border border-border/60 bg-muted/20 px-3 py-2">
                    <dt className="text-muted-foreground">Loại tệp</dt>
                    <dd className="font-medium capitalize text-foreground">{file.type}</dd>
                  </div>
                  <div className="flex justify-between rounded-lg border border-border/60 bg-muted/20 px-3 py-2">
                    <dt className="text-muted-foreground">Kích thước</dt>
                    <dd className="font-medium text-foreground">{file.size}</dd>
                  </div>
                  <div className="flex justify-between rounded-lg border border-border/60 bg-muted/20 px-3 py-2">
                    <dt className="text-muted-foreground">Cập nhật</dt>
                    <dd className="font-medium text-foreground">
                      {file.modifiedAtUtc ? dateFormatter.format(new Date(file.modifiedAtUtc)) : file.modified}
                    </dd>
                  </div>
                  <div className="flex justify-between rounded-lg border border-border/60 bg-muted/20 px-3 py-2">
                    <dt className="text-muted-foreground">Tạo lúc</dt>
                    <dd className="font-medium text-foreground">
                      {file.createdAtUtc ? dateFormatter.format(new Date(file.createdAtUtc)) : "--"}
                    </dd>
                  </div>
                </dl>
                <div className="space-y-2">
                  <p className="text-sm font-medium text-foreground">Thẻ gợi ý</p>
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

            <Card>
              <CardHeader>
                <CardTitle>Hoạt động gần đây</CardTitle>
                <CardDescription>Lịch sử tương tác trong 24 giờ qua (dữ liệu mẫu).</CardDescription>
              </CardHeader>
              <CardContent>
                <div className="space-y-4">
                  {file.activity.map((entry) => (
                    <div key={entry.id} className="flex items-start gap-3">
                      <div className="mt-1 h-2 w-2 rounded-full bg-primary" aria-hidden />
                      <div className="space-y-1 text-sm">
                        <p className="font-medium text-foreground">
                          {entry.actor} <span className="text-muted-foreground">{entry.action}</span>
                        </p>
                        <p className="text-muted-foreground">{entry.description}</p>
                        <p className="text-xs text-muted-foreground/80">
                          {dateFormatter.format(new Date(entry.timestamp))}
                        </p>
                      </div>
                    </div>
                  ))}
                </div>
              </CardContent>
            </Card>
          </div>
        </div>
      </div>
    </div>
  )
}
