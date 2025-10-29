"use client"

import { FormEvent, useEffect, useMemo, useState } from "react"
import {
  fetchShareInterstitial,
  requestShareDownloadLink,
  verifySharePassword,
} from "@/lib/api"
import type { ShareInterstitial } from "@/lib/types"

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

type SharePageProps = {
  params: {
    code: string
  }
}

export default function ShareDownloadPage({ params }: SharePageProps) {
  const { code } = params
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

    loadShare()

    return () => {
      cancelled = true
    }
  }, [code])

  async function refreshShare(withPassword?: string) {
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
    if (!share) {
      return
    }

    setDownloadError(null)
    setDownloading(true)

    try {
      const pass = share.requiresPassword ? accessPassword : undefined
      const link = await requestShareDownloadLink(code, pass)

      if (typeof window !== "undefined") {
        window.location.href = link.url
      }
    } catch (err) {
      console.error("[ui] Failed to request download link", err)
      setDownloadError("Không thể tạo link tải xuống. Vui lòng thử lại.")
    } finally {
      setDownloading(false)
    }
  }

  if (loading && !share) {
    return (
      <main className="mx-auto flex min-h-screen max-w-3xl flex-col items-center justify-center gap-4 px-6 text-center">
        <h1 className="text-2xl font-semibold">Đang tải thông tin chia sẻ...</h1>
        <p className="text-muted-foreground">Vui lòng chờ trong giây lát.</p>
      </main>
    )
  }

  if (error) {
    return (
      <main className="mx-auto flex min-h-screen max-w-3xl flex-col items-center justify-center gap-4 px-6 text-center">
        <h1 className="text-2xl font-semibold text-destructive">Không thể truy cập liên kết</h1>
        <p className="text-muted-foreground">{error}</p>
      </main>
    )
  }

  if (!share) {
    return null
  }

  const file = share.file
  const quota = share.quota
  const remainingViews = quota.maxViews ? Math.max(quota.maxViews - quota.viewsUsed, 0) : undefined
  const remainingDownloads = quota.maxDownloads ? Math.max(quota.maxDownloads - quota.downloadsUsed, 0) : undefined

  return (
    <main className="mx-auto flex min-h-screen max-w-3xl flex-col gap-6 px-6 py-10">
      <header className="rounded-lg border bg-card p-6 shadow-sm">
        <h1 className="text-3xl font-semibold">Tải xuống tài liệu được chia sẻ</h1>
        <p className="mt-2 text-sm text-muted-foreground">
          Liên kết được chia sẻ với mã <span className="font-mono text-primary">{code}</span>.
        </p>
        <div className="mt-4 space-y-2">
          <div>
            <span className="text-sm text-muted-foreground">Tên file</span>
            <p className="text-lg font-medium">{file.name}</p>
          </div>
          <div className="grid gap-4 sm:grid-cols-2">
            <div>
              <span className="text-sm text-muted-foreground">Kích thước</span>
              <p className="font-medium">{formatFileSize(file.sizeBytes)}</p>
            </div>
            <div>
              <span className="text-sm text-muted-foreground">Loại</span>
              <p className="font-medium">{file.contentType}</p>
            </div>
            <div>
              <span className="text-sm text-muted-foreground">Ngày tạo</span>
              <p className="font-medium">
                {file.createdAtUtc ? dateFormatter.format(new Date(file.createdAtUtc)) : "--"}
              </p>
            </div>
            <div>
              <span className="text-sm text-muted-foreground">Trạng thái</span>
              <p className="font-medium">{translateStatus(share.status)}</p>
            </div>
          </div>
        </div>
      </header>

      {share.requiresPassword && !share.passwordValid ? (
        <section className="rounded-lg border bg-card p-6 shadow-sm">
          <h2 className="text-xl font-semibold">Liên kết này được bảo vệ bằng mật khẩu</h2>
          <p className="mt-2 text-sm text-muted-foreground">
            Vui lòng nhập mật khẩu do người chia sẻ cung cấp để tiếp tục tải file.
          </p>
          <form onSubmit={handleVerifyPassword} className="mt-4 flex flex-col gap-4 sm:flex-row sm:items-end">
            <div className="flex-1">
              <label htmlFor="share-password" className="text-sm font-medium">
                Mật khẩu
              </label>
              <input
                id="share-password"
                type="password"
                value={password}
                onChange={(event) => setPassword(event.target.value)}
                className="mt-1 w-full rounded-md border px-3 py-2 shadow-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary"
                placeholder="Nhập mật khẩu"
                required
              />
              {passwordError ? <p className="mt-2 text-sm text-destructive">{passwordError}</p> : null}
            </div>
            <button
              type="submit"
              className="inline-flex h-10 items-center justify-center rounded-md bg-primary px-6 text-sm font-semibold text-primary-foreground shadow hover:bg-primary/90 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-offset-2 focus-visible:ring-primary"
              disabled={verifying || password.length === 0}
            >
              {verifying ? "Đang kiểm tra..." : "Xác nhận"}
            </button>
          </form>
        </section>
      ) : null}

      <section className="rounded-lg border bg-card p-6 shadow-sm">
        <h2 className="text-xl font-semibold">Chi tiết truy cập</h2>
        <div className="mt-4 grid gap-4 sm:grid-cols-2">
          <div>
            <span className="text-sm text-muted-foreground">Số lượt xem đã dùng</span>
            <p className="font-medium">
              {quota.viewsUsed}
              {typeof remainingViews === "number" ? ` / ${quota.maxViews} (còn ${remainingViews})` : ""}
            </p>
          </div>
          <div>
            <span className="text-sm text-muted-foreground">Số lượt tải xuống đã dùng</span>
            <p className="font-medium">
              {quota.downloadsUsed}
              {typeof remainingDownloads === "number" ? ` / ${quota.maxDownloads} (còn ${remainingDownloads})` : ""}
            </p>
          </div>
        </div>
        {downloadError ? <p className="mt-4 text-sm text-destructive">{downloadError}</p> : null}
        <div className="mt-6 flex flex-col gap-3 sm:flex-row sm:items-center">
          <button
            type="button"
            onClick={handleDownload}
            disabled={!canDownload || downloading}
            className="inline-flex h-11 items-center justify-center rounded-md bg-primary px-8 text-sm font-semibold text-primary-foreground shadow hover:bg-primary/90 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-offset-2 focus-visible:ring-primary disabled:cursor-not-allowed disabled:bg-muted"
          >
            {downloading ? "Đang chuẩn bị..." : "Tải xuống"}
          </button>
          {!canDownload ? (
            <p className="text-sm text-muted-foreground">
              Không thể tải xuống ở thời điểm hiện tại. Vui lòng kiểm tra trạng thái liên kết hoặc quota.
            </p>
          ) : null}
        </div>
      </section>
    </main>
  )
}

function translateStatus(status: ShareInterstitial["status"]): string {
  switch (status) {
    case "Active":
      return "Đang hoạt động"
    case "Draft":
      return "Chưa hiệu lực"
    case "Expired":
      return "Đã hết hạn"
    case "Revoked":
      return "Đã thu hồi"
    default:
      return status
  }
}
