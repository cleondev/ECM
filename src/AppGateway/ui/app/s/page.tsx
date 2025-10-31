"use client"

import {
  FormEvent,
  Suspense,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react"
import { usePathname, useSearchParams, type ReadonlyURLSearchParams } from "next/navigation"

import { Button } from "@/components/ui/button"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Badge } from "@/components/ui/badge"
import { DownloadCloud, Eye, Lock } from "lucide-react"

import {
  fetchShareInterstitial,
  requestShareDownloadLink,
  verifySharePassword,
} from "@/lib/api"
import type { ShareInterstitial } from "@/lib/types"

export type ShareDownloadPageProps = {
  initialCode?: string
  initialPassword?: string
}

const dateFormatter = new Intl.DateTimeFormat("vi-VN", {
  dateStyle: "medium",
  timeStyle: "short",
})

function formatFileSize(bytes?: number | null): string {
  if (!bytes || bytes <= 0) {
    return "--"
  }

  const units = ["KB", "MB", "GB", "TB"]
  let value = bytes / 1024
  let index = 0

  while (value >= 1024 && index < units.length - 1) {
    value /= 1024
    index += 1
  }

  return `${value.toFixed(1)} ${units[index]}`
}

export default function ShareDownloadPage(props: ShareDownloadPageProps = {}) {
  return (
    <Suspense fallback={<ShareDownloadSuspenseFallback />}>
      <ShareDownloadPageContent {...props} />
    </Suspense>
  )
}

function ShareDownloadPageContent({
  initialCode,
  initialPassword,
}: ShareDownloadPageProps) {
  const pathname = usePathname()
  const searchParams = useSearchParams()

  const code = useMemo(() => {
    return (
      initialCode ??
      extractShareCodeFromRouter(pathname, searchParams) ??
      extractShareCodeFromWindowLocation()
    )
  }, [initialCode, pathname, searchParams])
  const passwordFromUrl = useMemo(
    () => initialPassword ?? searchParams?.get("password") ?? undefined,
    [initialPassword, searchParams],
  )
  const [share, setShare] = useState<ShareInterstitial | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [password, setPassword] = useState("")
  const [accessPassword, setAccessPassword] = useState<string | undefined>()
  const [passwordError, setPasswordError] = useState<string | null>(null)
  const [verifying, setVerifying] = useState(false)
  const [downloading, setDownloading] = useState(false)
  const [downloadError, setDownloadError] = useState<string | null>(null)
  const [downloadUrl, setDownloadUrl] = useState<string | null>(null)

  const canDownload = useMemo(() => {
    if (!share) {
      return false
    }

    if (!share.canDownload) {
      return false
    }

    if (share.requiresPassword && !share.passwordValid) {
      return false
    }

    if (share.status === "Revoked" || share.status === "Expired") {
      return false
    }

    return true
  }, [share])

  useEffect(() => {
    let cancelled = false

    async function loadShare(initialPassword?: string) {
      if (!code) {
        setShare(null)
        setError("Liên kết chia sẻ không hợp lệ.")
        setLoading(false)
        setAccessPassword(undefined)
        return
      }

      try {
        setLoading(true)
        const data = await fetchShareInterstitial(code, initialPassword)
        if (cancelled) {
          return
        }

        setShare(data)
        setError(null)

        if (data.requiresPassword) {
          if (data.passwordValid) {
            setAccessPassword(initialPassword)
          } else {
            setAccessPassword(undefined)
          }
        } else {
          setAccessPassword(undefined)
        }
      } catch (err) {
        if (!cancelled) {
          console.error("[ui] Failed to load shared file", err)
          setError("Không thể tải thông tin chia sẻ. Link có thể đã hết hạn hoặc bị thu hồi.")
          setShare(null)
        }
      } finally {
        if (!cancelled) {
          setLoading(false)
        }
      }
    }

    setPassword("")
    setPasswordError(null)
    setDownloadError(null)
    setDownloadUrl(null)
    loadShare(passwordFromUrl)

    return () => {
      cancelled = true
    }
  }, [code, passwordFromUrl])

  async function refreshShare(withPassword?: string) {
    if (!code) {
      return
    }

    try {
      const data = await fetchShareInterstitial(code, withPassword)
      setShare(data)
      setError(null)

      if (data.requiresPassword) {
        setAccessPassword(data.passwordValid ? withPassword : undefined)
      } else {
        setAccessPassword(undefined)
      }
    } catch (err) {
      console.error("[ui] Failed to refresh shared file", err)
      setError("Không thể cập nhật thông tin chia sẻ.")
    }
  }

  async function handleVerifyPassword(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setPasswordError(null)

    if (!code) {
      setPasswordError("Liên kết chia sẻ không hợp lệ.")
      return
    }

    setVerifying(true)

    try {
      const ok = await verifySharePassword(code, password)
      if (!ok) {
        setPasswordError("Mật khẩu không đúng. Vui lòng thử lại.")
        return
      }

      await refreshShare(password)
      setPassword("")
    } catch (err) {
      console.error("[ui] Failed to verify share password", err)
      setPasswordError("Không thể xác thực mật khẩu. Vui lòng thử lại sau.")
    } finally {
      setVerifying(false)
    }
  }

  async function handleDownload() {
    if (!share || !code) {
      return
    }

    setDownloadError(null)
    setDownloadUrl(null)
    setDownloading(true)

    try {
      const pass = share.requiresPassword ? accessPassword : undefined
      const download = await requestShareDownloadLink(code, pass)

      if (!download?.url) {
        throw new Error("Missing download URL from gateway response")
      }

      setDownloadUrl(download.url)
      window.location.href = download.url
    } catch (err) {
      console.error("[ui] Failed to download shared file", err)
      setDownloadError("Không thể tải xuống tệp. Vui lòng thử lại sau.")
    } finally {
      setDownloading(false)
    }
  }

  if (loading && !share) {
    return <ShareDownloadLoadingState />
  }

  if (!code || !share) {
    return <ShareDownloadErrorState message={error} />
  }

  const file = share.file
  const quota = share.quota

  return (
    <div className="min-h-screen bg-gradient-to-b from-slate-50 via-white to-slate-100 px-4 py-12 dark:from-slate-950 dark:via-slate-900 dark:to-slate-950">
      <div className="mx-auto flex max-w-5xl flex-col gap-8">
        <Card className="shadow-lg shadow-slate-200/60 dark:shadow-slate-950/40">
          <CardHeader className="gap-6 sm:flex-row sm:items-start sm:justify-between">
            <div className="space-y-2">
              <CardTitle className="text-3xl font-semibold tracking-tight text-foreground">{file.name}</CardTitle>
              <CardDescription>
                Truy cập nhanh vào tệp được chia sẻ và xem các chỉ số quan trọng trước khi tải xuống.
              </CardDescription>
            </div>
            <Badge
              variant="outline"
              className="self-start border-dashed border-primary/40 bg-primary/5 font-mono text-xs uppercase tracking-widest text-muted-foreground dark:border-primary/50 dark:bg-primary/10"
            >
              Mã chia sẻ
              <span className="ml-2 text-foreground">{share.code}</span>
            </Badge>
          </CardHeader>
          <CardContent className="space-y-6">
            <div className="grid gap-4 sm:grid-cols-2">
              <DetailItem label="Dung lượng" value={formatFileSize(file.sizeBytes)} />
              <DetailItem
                label="Ngày tạo"
                value={file.createdAtUtc ? dateFormatter.format(new Date(file.createdAtUtc)) : "--"}
              />
              <DetailItem
                label="Lượt xem"
                value={
                  <span>
                    {quota.viewsUsed}
                    {quota.maxViews ? ` / ${quota.maxViews}` : ""}
                  </span>
                }
              />
              <DetailItem
                label="Lượt tải"
                value={
                  <span>
                    {quota.downloadsUsed}
                    {quota.maxDownloads ? ` / ${quota.maxDownloads}` : ""}
                  </span>
                }
              />
            </div>

            <div className="flex flex-wrap items-center gap-3 rounded-lg border border-dashed border-muted/50 bg-muted/20 p-4 dark:bg-muted/30">
              <span className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
                Trạng thái hiện tại
              </span>
              <Badge variant="secondary" className="text-sm font-medium text-foreground">
                {translateStatus(share.status)}
              </Badge>
            </div>
          </CardContent>
        </Card>

        {share.requiresPassword && !share.passwordValid ? (
          <Card className="border-amber-200 bg-amber-50/90 shadow-md dark:border-amber-500/60 dark:bg-amber-950/30">
            <CardHeader className="space-y-2">
              <CardTitle className="flex items-center gap-2 text-base font-semibold text-amber-900 dark:text-amber-200">
                <Lock className="h-4 w-4" aria-hidden />
                Liên kết được bảo vệ bằng mật khẩu
              </CardTitle>
              <CardDescription className="text-amber-800 dark:text-amber-100/80">
                Chủ sở hữu đã yêu cầu mật khẩu để truy cập tệp này. Vui lòng nhập mật khẩu được cung cấp cùng liên kết chia sẻ.
              </CardDescription>
            </CardHeader>
            <CardContent>
              <form onSubmit={handleVerifyPassword} className="space-y-3">
                <div className="space-y-2">
                  <Label htmlFor="share-password">Mật khẩu truy cập</Label>
                  <Input
                    id="share-password"
                    type="password"
                    value={password}
                    onChange={(event) => setPassword(event.target.value)}
                    placeholder="Nhập mật khẩu"
                    autoComplete="current-password"
                    required
                  />
                </div>
                {passwordError ? <p className="text-sm text-destructive">{passwordError}</p> : null}
                <Button type="submit" disabled={verifying} className="w-full sm:w-auto">
                  {verifying ? "Đang kiểm tra…" : "Xác nhận mật khẩu"}
                </Button>
              </form>
            </CardContent>
          </Card>
        ) : null}

        <Card className="shadow-lg shadow-slate-200/60 dark:shadow-slate-950/40">
          <CardHeader className="space-y-2">
            <CardTitle>Nội dung chia sẻ</CardTitle>
            <CardDescription>
              Thông tin xem trước giúp bạn đánh giá nhanh tệp chia sẻ. Tính năng xem trực tuyến sẽ sớm ra mắt.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <div className="grid gap-6 lg:grid-cols-[3fr,2fr]">
              <div className="flex flex-col gap-4">
                <div className="flex aspect-video w-full items-center justify-center rounded-lg border border-dashed border-muted-foreground/40 bg-muted/20 p-6 text-muted-foreground dark:bg-muted/30">
                  <div className="flex flex-col items-center gap-3 text-center text-sm">
                    <Eye className="h-5 w-5" aria-hidden />
                    <span>Khu vực xem trước sẽ hiển thị nội dung file ngay khi tính năng sẵn sàng.</span>
                  </div>
                </div>
                {downloadUrl ? (
                  <div className="rounded-lg border border-primary/30 bg-primary/5 p-4 text-sm text-muted-foreground dark:border-primary/40 dark:bg-primary/10">
                    <span className="font-medium text-foreground">Link tải xuống:</span>{" "}
                    <a
                      href={downloadUrl}
                      className="text-primary underline-offset-2 hover:underline"
                    >
                      {downloadUrl}
                    </a>
                  </div>
                ) : null}
                {downloadError ? <p className="text-sm text-destructive">{downloadError}</p> : null}
              </div>
              <div className="flex flex-col gap-4 rounded-lg border border-border bg-background/80 p-5 shadow-inner dark:bg-slate-950/40">
                <div className="space-y-2">
                  <h3 className="text-lg font-semibold text-foreground">Hành động nhanh</h3>
                  <p className="text-sm text-muted-foreground">
                    Liên kết này có thể hết hạn hoặc bị thu hồi bất cứ lúc nào bởi người chia sẻ.
                  </p>
                </div>
                <div className="flex flex-col gap-3">
                  <Button
                    type="button"
                    onClick={handleDownload}
                    disabled={!canDownload || downloading}
                    className="w-full justify-center"
                  >
                    <DownloadCloud className="h-4 w-4" aria-hidden />
                    {downloading ? "Đang chuẩn bị…" : "Tải xuống ngay"}
                  </Button>
                  <Button type="button" variant="outline" disabled className="w-full justify-center">
                    <Eye className="h-4 w-4" aria-hidden />
                    Xem trực tuyến (sắp ra mắt)
                  </Button>
                </div>
                {!canDownload ? (
                  <p className="text-sm text-amber-600 dark:text-amber-400">
                    Liên kết này hiện không khả dụng để tải xuống. Vui lòng kiểm tra lại sau hoặc liên hệ chủ sở hữu.
                  </p>
                ) : null}
              </div>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  )
}

function translateStatus(status: ShareInterstitial["status"]): string {
  switch (status) {
    case "Active":
      return "Đang hoạt động"
    case "Expired":
      return "Đã hết hạn"
    case "Revoked":
      return "Đã thu hồi"
    case "Draft":
      return "Đang chuẩn bị"
    default:
      return status
  }
}

function ShareDownloadSuspenseFallback(): JSX.Element {
  return <ShareDownloadLoadingState />
}

function ShareDownloadLoadingState(): JSX.Element {
  return (
    <div className="flex min-h-screen items-center justify-center bg-gradient-to-b from-slate-50 via-white to-slate-100 p-6 dark:from-slate-950 dark:via-slate-900 dark:to-slate-950">
      <div className="w-full max-w-md space-y-4 rounded-xl border border-border bg-background/80 p-6 text-center shadow-sm backdrop-blur">
        <h1 className="text-2xl font-semibold text-foreground">Đang tải thông tin chia sẻ…</h1>
        <p className="text-muted-foreground">Vui lòng chờ trong giây lát.</p>
      </div>
    </div>
  )
}

function ShareDownloadErrorState({ message }: { message: string | null }): JSX.Element {
  return (
    <div className="flex min-h-screen items-center justify-center bg-gradient-to-b from-slate-50 via-white to-slate-100 p-6 dark:from-slate-950 dark:via-slate-900 dark:to-slate-950">
      <div className="w-full max-w-md space-y-4 rounded-xl border border-border bg-background/80 p-6 text-center shadow-sm backdrop-blur">
        <h1 className="text-2xl font-semibold text-foreground">Không thể mở liên kết chia sẻ</h1>
        <p className="text-muted-foreground">
          {message ?? "Liên kết chia sẻ không hợp lệ hoặc đã hết hạn. Vui lòng kiểm tra lại."}
        </p>
      </div>
    </div>
  )
}

type DetailItemProps = { label: string; value: ReactNode }

function DetailItem({ label, value }: DetailItemProps): JSX.Element {
  return (
    <div className="rounded-lg border border-border bg-muted/20 p-4 dark:bg-muted/30">
      <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">{label}</p>
      <div className="mt-2 text-lg font-semibold text-foreground">{value}</div>
    </div>
  )
}

function extractShareCodeFromRouter(
  pathname: string | null,
  searchParams: ReadonlyURLSearchParams | null,
): string | null {
  if (!pathname) {
    return null
  }

  const queryCode = searchParams?.get("code")
  if (queryCode) {
    return safelyDecodeShareCode(queryCode)
  }

  return extractShareCodeFromPath(pathname)
}

function safelyDecodeShareCode(raw: string): string {
  try {
    return decodeURIComponent(raw)
  } catch (error) {
    console.warn("[ui] Failed to decode share code", error)
    return raw
  }
}

function extractShareCodeFromPath(path: string): string | null {
  const segments = path.split("/").filter(Boolean)
  if (segments.length >= 2 && segments[0] === "s") {
    return safelyDecodeShareCode(segments[1])
  }

  return null
}

function extractShareCodeFromWindowLocation(): string | null {
  if (typeof window === "undefined") {
    return null
  }

  return extractShareCodeFromPath(window.location.pathname)
}
