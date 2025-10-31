"use client"

import { FormEvent, Suspense, useEffect, useMemo, useState } from "react"
import { usePathname, useSearchParams, type ReadonlyURLSearchParams } from "next/navigation"

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

  const code = useMemo(
    () => initialCode ?? extractShareCodeFromRouter(pathname, searchParams),
    [initialCode, pathname, searchParams],
  )
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
    setDownloading(true)

    try {
      const pass = share.requiresPassword ? accessPassword : undefined
      const download = await requestShareDownloadLink(code, pass)

      if (!download?.url) {
        throw new Error("Missing download URL from gateway response")
      }

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
    <div className="min-h-screen bg-gradient-to-br from-slate-900 via-slate-800 to-slate-900 p-6 text-white">
      <div className="mx-auto flex max-w-3xl flex-col gap-6">
        <div className="rounded-2xl bg-black/30 p-6 backdrop-blur">
          <div className="space-y-4">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-slate-300">File được chia sẻ</p>
                <h1 className="text-2xl font-semibold text-white">{file.name}</h1>
              </div>
              <div className="rounded-full border border-white/20 bg-white/10 px-3 py-1 text-sm text-white">
                Mã chia sẻ: <span className="font-mono">{share.code}</span>
              </div>
            </div>

            <div className="grid gap-4 rounded-xl border border-white/10 bg-white/5 p-4 backdrop-blur-lg sm:grid-cols-2">
              <div>
                <p className="text-xs uppercase tracking-wider text-slate-300">Dung lượng</p>
                <p className="text-lg font-semibold text-white">{formatFileSize(file.sizeBytes)}</p>
              </div>
              <div>
                <p className="text-xs uppercase tracking-wider text-slate-300">Ngày tạo</p>
                <p className="text-lg font-semibold text-white">
                  {file.createdAtUtc ? dateFormatter.format(new Date(file.createdAtUtc)) : "--"}
                </p>
              </div>
              <div>
                <p className="text-xs uppercase tracking-wider text-slate-300">Lượt xem</p>
                <p className="text-lg font-semibold text-white">
                  {quota.viewsUsed}
                  {quota.maxViews ? ` / ${quota.maxViews}` : ""}
                </p>
              </div>
              <div>
                <p className="text-xs uppercase tracking-wider text-slate-300">Lượt tải</p>
                <p className="text-lg font-semibold text-white">
                  {quota.downloadsUsed}
                  {quota.maxDownloads ? ` / ${quota.maxDownloads}` : ""}
                </p>
              </div>
            </div>

            <div className="rounded-xl border border-white/10 bg-white/5 p-4 backdrop-blur-lg">
              <p className="text-xs uppercase tracking-wider text-slate-300">Trạng thái</p>
              <p className="font-medium text-white">{translateStatus(share.status)}</p>
            </div>
          </div>
        </div>

        {share.requiresPassword && !share.passwordValid ? (
          <div className="rounded-2xl border border-yellow-500/40 bg-yellow-500/10 p-6 text-yellow-100">
            <h2 className="text-xl font-semibold">Liên kết được bảo vệ bằng mật khẩu</h2>
            <p className="mt-2 text-sm">
              Chủ sở hữu đã yêu cầu mật khẩu để truy cập tệp này. Vui lòng nhập mật khẩu được cung cấp cùng liên
              kết chia sẻ.
            </p>

            <form onSubmit={handleVerifyPassword} className="mt-4 space-y-3">
              <label htmlFor="share-password" className="text-sm font-medium">
                Mật khẩu truy cập
              </label>
              <input
                id="share-password"
                type="password"
                value={password}
                onChange={(event) => setPassword(event.target.value)}
                className="w-full rounded-lg border border-white/20 bg-black/40 px-4 py-2 text-white focus:border-white/60 focus:outline-none"
                placeholder="Nhập mật khẩu"
                autoComplete="current-password"
                required
              />
              {passwordError ? <p className="text-sm text-red-300">{passwordError}</p> : null}
              <button
                type="submit"
                disabled={verifying}
                className="inline-flex items-center justify-center rounded-lg bg-white px-4 py-2 font-medium text-slate-900 hover:bg-slate-200 disabled:cursor-not-allowed disabled:opacity-70"
              >
                {verifying ? "Đang kiểm tra…" : "Xác nhận mật khẩu"}
              </button>
            </form>
          </div>
        ) : null}

        <div className="rounded-2xl border border-white/10 bg-black/40 p-6 backdrop-blur">
          <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
            <div>
              <h2 className="text-xl font-semibold text-white">Tải tệp chia sẻ</h2>
              <p className="text-sm text-slate-300">
                Liên kết này có thể hết hạn hoặc bị thu hồi bất cứ lúc nào bởi người chia sẻ.
              </p>
            </div>
            <button
              type="button"
              onClick={handleDownload}
              disabled={!canDownload || downloading}
              className="inline-flex items-center justify-center rounded-lg bg-white px-5 py-2 font-medium text-slate-900 hover:bg-slate-200 disabled:cursor-not-allowed disabled:opacity-70"
            >
              {downloading ? "Đang chuẩn bị…" : "Tải xuống ngay"}
            </button>
          </div>
          {!canDownload ? (
            <p className="mt-4 text-sm text-yellow-200">
              Liên kết này hiện không khả dụng để tải xuống. Vui lòng kiểm tra lại sau hoặc liên hệ chủ sở hữu.
            </p>
          ) : null}
          {downloadError ? <p className="mt-4 text-sm text-red-300">{downloadError}</p> : null}
        </div>
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
    <div className="flex min-h-screen items-center justify-center bg-background p-6">
      <div className="w-full max-w-md space-y-4 text-center">
        <h1 className="text-2xl font-semibold">Đang tải thông tin chia sẻ…</h1>
        <p className="text-muted-foreground">Vui lòng chờ trong giây lát.</p>
      </div>
    </div>
  )
}

function ShareDownloadErrorState({ message }: { message: string | null }): JSX.Element {
  return (
    <div className="flex min-h-screen items-center justify-center bg-background p-6">
      <div className="w-full max-w-md space-y-4 text-center">
        <h1 className="text-2xl font-semibold">Không thể mở liên kết chia sẻ</h1>
        <p className="text-muted-foreground">
          {message ?? "Liên kết chia sẻ không hợp lệ hoặc đã hết hạn. Vui lòng kiểm tra lại."}
        </p>
      </div>
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

  const segments = pathname.split("/").filter(Boolean)
  if (segments.length >= 2 && segments[0] === "s") {
    return safelyDecodeShareCode(segments[1])
  }

  return null
}

function safelyDecodeShareCode(raw: string): string {
  try {
    return decodeURIComponent(raw)
  } catch (error) {
    console.warn("[ui] Failed to decode share code", error)
    return raw
  }
}
