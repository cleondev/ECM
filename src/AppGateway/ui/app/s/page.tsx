"use client"

import {
  FormEvent,
  Suspense,
  useEffect,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from "react"
import { usePathname, useSearchParams, type ReadonlyURLSearchParams } from "next/navigation"

import { Button } from "@/components/ui/button"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Badge } from "@/components/ui/badge"
import { DownloadCloud, Eye, Fingerprint, Lock, Mail } from "lucide-react"

import {
  checkLogin,
  fetchShareInterstitial,
  requestShareDownloadLink,
  verifySharePassword,
} from "@/lib/api"
import type { ShareInterstitial, User } from "@/lib/types"
import { attemptSilentLogin, resolveGatewayUrl } from "@/lib/auth"
import { createSignInRedirectPath } from "@/lib/utils"
import { UserIdentity } from "@/components/user/user-identity"

export type ShareDownloadPageProps = {
  initialCode?: string
  initialPassword?: string
}

type AuthState = {
  loading: boolean
  isAuthenticated: boolean
  loginUrl: string | null
  silentLoginUrl: string | null
  user: User | null
  error: string | null
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
  const [authState, setAuthState] = useState<AuthState>({
    loading: true,
    isAuthenticated: false,
    loginUrl: null,
    silentLoginUrl: null,
    user: null,
    error: null,
  })
  const [signInPath, setSignInPath] = useState("/signin/")
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

  const silentLoginAttempted = useRef(false)

  useEffect(() => {
    if (typeof window === "undefined") {
      return
    }

    let cancelled = false
    const redirectTarget = `${window.location.pathname}${window.location.search}` || "/s/"
    setSignInPath(createSignInRedirectPath(redirectTarget, "/s/"))

    async function ensureAuthenticated() {
      try {
        setAuthState((previous) => ({
          ...previous,
          loading: true,
          error: null,
        }))

        const initial = await checkLogin(redirectTarget)
        if (cancelled) {
          return
        }

        if (!initial.isAuthenticated && initial.silentLoginUrl && !silentLoginAttempted.current) {
          silentLoginAttempted.current = true
          await attemptSilentLogin(initial.silentLoginUrl)

          if (cancelled) {
            return
          }

          const followUp = await checkLogin(redirectTarget)
          if (cancelled) {
            return
          }

          setAuthState({
            loading: false,
            isAuthenticated: followUp.isAuthenticated,
            loginUrl: followUp.loginUrl,
            silentLoginUrl: followUp.silentLoginUrl,
            user: followUp.user,
            error: null,
          })
          return
        }

        setAuthState({
          loading: false,
          isAuthenticated: initial.isAuthenticated,
          loginUrl: initial.loginUrl,
          silentLoginUrl: initial.silentLoginUrl,
          user: initial.user,
          error: null,
        })
      } catch (err) {
        console.error("[ui] Unable to verify sign-in status:", err)
        if (!cancelled) {
          setAuthState({
            loading: false,
            isAuthenticated: false,
            loginUrl: null,
            silentLoginUrl: null,
            user: null,
            error: "Unable to verify sign-in status. Please try again.",
          })
        }
      }
    }

    ensureAuthenticated()

    return () => {
      cancelled = true
    }
  }, [])

  useEffect(() => {
    let cancelled = false

    async function loadShare(initialPassword?: string) {
      if (!code) {
        setShare(null)
        setError("Invalid share link.")
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
          setError("Unable to load share details. The link may have expired or been revoked.")
          setShare(null)
        }
      } finally {
        if (!cancelled) {
          setLoading(false)
        }
      }
    }

    if (authState.loading) {
      return () => {
        cancelled = true
      }
    }

    if (!authState.isAuthenticated) {
      setShare(null)
      setLoading(false)
      setError(null)
      setAccessPassword(undefined)
      return () => {
        cancelled = true
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
  }, [authState.isAuthenticated, authState.loading, code, passwordFromUrl])

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
      setError("Unable to refresh share information.")
    }
  }

  async function handleVerifyPassword(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setPasswordError(null)

    if (!code) {
      setPasswordError("Invalid share link.")
      return
    }

    setVerifying(true)

    try {
      const ok = await verifySharePassword(code, password)
      if (!ok) {
        setPasswordError("Incorrect password. Please try again.")
        return
      }

      await refreshShare(password)
      setPassword("")
    } catch (err) {
      console.error("[ui] Failed to verify share password", err)
      setPasswordError("Unable to validate the password. Please try again later.")
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
      setDownloadError("Unable to download the file. Please try again later.")
    } finally {
      setDownloading(false)
    }
  }

  if (authState.loading) {
    return <ShareDownloadLoadingState message="Checking sign-in status…" />
  }

  if (authState.error) {
    return <ShareDownloadAuthErrorState message={authState.error} />
  }

  if (!authState.isAuthenticated) {
    return (
      <ShareDownloadLoginRequired
        loginUrl={authState.loginUrl}
        fallbackUrl={signInPath}
      />
    )
  }

  if (loading && !share) {
    return <ShareDownloadLoadingState />
  }

  if (!code || !share) {
    return <ShareDownloadErrorState message={error} />
  }

  const file = share.file
  const quota = share.quota
  const currentUser = authState.user

  return (
    <div className="min-h-screen bg-gradient-to-b from-slate-50 via-white to-slate-100 px-4 py-12 dark:from-slate-950 dark:via-slate-900 dark:to-slate-950">
      <div className="mx-auto flex max-w-5xl flex-col gap-8">
        {currentUser ? (
          <Card className="border-primary/30 bg-primary/5 shadow-sm shadow-primary/10 dark:border-primary/40 dark:bg-primary/10">
            <CardHeader className="space-y-3">
              <div className="flex flex-col gap-1 sm:flex-row sm:items-center sm:justify-between">
                <div>
                  <CardTitle className="text-lg font-semibold text-primary dark:text-primary-200">Signed in</CardTitle>
                  <CardDescription>
                    You are using the account {currentUser.displayName} to view this shared content.
                  </CardDescription>
                </div>
                <UserIdentity
                  size="sm"
                  profileHref="/me"
                  lazy
                  className="px-0 py-0 hover:bg-transparent"
                />
              </div>
            </CardHeader>
            <CardContent className="space-y-3">
              <div className="flex flex-col gap-3 rounded-md border border-primary/10 bg-background/80 p-3 sm:flex-row sm:items-center sm:justify-between dark:bg-slate-950/40">
                <div className="flex items-center gap-3">
                  <Mail className="h-4 w-4 text-muted-foreground" />
                  <span className="text-sm text-foreground break-all">{currentUser.email}</span>
                </div>
                <div className="flex items-center gap-3">
                  <Fingerprint className="h-4 w-4 text-muted-foreground" />
                  <span className="font-mono text-xs text-foreground break-all">{currentUser.id}</span>
                </div>
              </div>
              {currentUser.roles?.length ? (
                <div className="flex flex-col gap-2 rounded-md border border-primary/10 bg-background/80 p-3 text-sm text-foreground dark:bg-slate-950/40">
                  <span className="text-muted-foreground">Roles</span>
                  <span>{currentUser.roles.join(", ")}</span>
                </div>
              ) : null}
            </CardContent>
          </Card>
        ) : null}
        <Card className="shadow-lg shadow-slate-200/60 dark:shadow-slate-950/40">
          <CardHeader className="gap-6 sm:flex-row sm:items-start sm:justify-between">
            <div className="space-y-2">
              <CardTitle className="text-3xl font-semibold tracking-tight text-foreground">{file.name}</CardTitle>
              <CardDescription>
                Quickly access the shared file and view important metrics before downloading.
              </CardDescription>
            </div>
            <Badge
              variant="outline"
              className="self-start border-dashed border-primary/40 bg-primary/5 font-mono text-xs uppercase tracking-widest text-muted-foreground dark:border-primary/50 dark:bg-primary/10"
            >
              Share code
              <span className="ml-2 text-foreground">{share.code}</span>
            </Badge>
          </CardHeader>
          <CardContent className="space-y-6">
            <div className="grid gap-4 sm:grid-cols-2">
              <DetailItem label="File size" value={formatFileSize(file.sizeBytes)} />
              <DetailItem
                label="Created"
                value={file.createdAtUtc ? dateFormatter.format(new Date(file.createdAtUtc)) : "--"}
              />
              <DetailItem
                label="Views"
                value={
                  <span>
                    {quota.viewsUsed}
                    {quota.maxViews ? ` / ${quota.maxViews}` : ""}
                  </span>
                }
              />
              <DetailItem
                label="Downloads"
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
                Current status
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
                  Password-protected link
                </CardTitle>
                <CardDescription className="text-amber-800 dark:text-amber-100/80">
                  The owner requires a password to access this file. Enter the password that was provided with the shared link.
                </CardDescription>
              </CardHeader>
              <CardContent>
                <form onSubmit={handleVerifyPassword} className="space-y-3">
                  <div className="space-y-2">
                    <Label htmlFor="share-password">Access password</Label>
                    <Input
                      id="share-password"
                      type="password"
                      value={password}
                      onChange={(event) => setPassword(event.target.value)}
                      placeholder="Enter password"
                      autoComplete="current-password"
                      required
                    />
                  </div>
                  {passwordError ? <p className="text-sm text-destructive">{passwordError}</p> : null}
                  <Button type="submit" disabled={verifying} className="w-full sm:w-auto">
                    {verifying ? "Verifying…" : "Confirm password"}
                  </Button>
                </form>
              </CardContent>
            </Card>
        ) : null}

        <Card className="shadow-lg shadow-slate-200/60 dark:shadow-slate-950/40">
          <CardHeader className="space-y-2">
            <CardTitle>Shared content</CardTitle>
            <CardDescription>
              Preview information helps you evaluate the shared file quickly. Inline viewing is coming soon.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <div className="grid gap-6 lg:grid-cols-[3fr,2fr]">
              <div className="flex flex-col gap-4">
                <div className="flex aspect-video w-full items-center justify-center rounded-lg border border-dashed border-muted-foreground/40 bg-muted/20 p-6 text-muted-foreground dark:bg-muted/30">
                <div className="flex flex-col items-center gap-3 text-center text-sm">
                  <Eye className="h-5 w-5" aria-hidden />
                  <span>The preview area will display the file once the feature is ready.</span>
                </div>
              </div>
              {downloadUrl ? (
                <div className="rounded-lg border border-primary/30 bg-primary/5 p-4 text-sm text-muted-foreground dark:border-primary/40 dark:bg-primary/10">
                  <span className="font-medium text-foreground">Download link:</span>{" "}
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
                  <h3 className="text-lg font-semibold text-foreground">Quick actions</h3>
                  <p className="text-sm text-muted-foreground">
                    This link can expire or be revoked by the sharer at any time.
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
                    {downloading ? "Preparing…" : "Download now"}
                  </Button>
                  <Button type="button" variant="outline" disabled className="w-full justify-center">
                    <Eye className="h-4 w-4" aria-hidden />
                    View online (coming soon)
                  </Button>
                </div>
                {!canDownload ? (
                  <p className="text-sm text-amber-600 dark:text-amber-400">
                    This link is not currently available for download. Please try again later or contact the owner.
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
      return "Active"
    case "Expired":
      return "Expired"
    case "Revoked":
      return "Revoked"
    case "Draft":
      return "Preparing"
    default:
      return status
  }
}

function ShareDownloadSuspenseFallback(): JSX.Element {
  return <ShareDownloadLoadingState />
}

function ShareDownloadLoadingState({ message = "Loading share details…" }: { message?: string } = {}): JSX.Element {
  return (
    <div className="flex min-h-screen items-center justify-center bg-gradient-to-b from-slate-50 via-white to-slate-100 p-6 dark:from-slate-950 dark:via-slate-900 dark:to-slate-950">
      <div className="w-full max-w-md space-y-4 rounded-xl border border-border bg-background/80 p-6 text-center shadow-sm backdrop-blur">
        <h1 className="text-2xl font-semibold text-foreground">{message}</h1>
        <p className="text-muted-foreground">Please wait a moment.</p>
      </div>
    </div>
  )
}

function ShareDownloadErrorState({ message }: { message: string | null }): JSX.Element {
  return (
    <div className="flex min-h-screen items-center justify-center bg-gradient-to-b from-slate-50 via-white to-slate-100 p-6 dark:from-slate-950 dark:via-slate-900 dark:to-slate-950">
      <div className="w-full max-w-md space-y-4 rounded-xl border border-border bg-background/80 p-6 text-center shadow-sm backdrop-blur">
        <h1 className="text-2xl font-semibold text-foreground">Unable to open shared link</h1>
        <p className="text-muted-foreground">
          {message ?? "The shared link is invalid or has expired. Please check again."}
        </p>
      </div>
    </div>
  )
}

type ShareDownloadLoginRequiredProps = {
  loginUrl: string | null
  fallbackUrl: string
}

function ShareDownloadLoginRequired({ loginUrl, fallbackUrl }: ShareDownloadLoginRequiredProps): JSX.Element {
  const resolvedLoginUrl = loginUrl ? resolveGatewayUrl(loginUrl) : fallbackUrl

  return (
    <div className="flex min-h-screen items-center justify-center bg-gradient-to-b from-slate-50 via-white to-slate-100 p-6 dark:from-slate-950 dark:via-slate-900 dark:to-slate-950">
      <div className="w-full max-w-md space-y-4 rounded-xl border border-border bg-background/80 p-6 text-center shadow-sm backdrop-blur">
        <h1 className="text-2xl font-semibold text-foreground">Sign-in required</h1>
        <p className="text-muted-foreground">
          You need to sign in to access this shared link. Please log in to continue.
        </p>
        <Button asChild className="w-full">
          <a href={resolvedLoginUrl}>Sign in to continue</a>
        </Button>
      </div>
    </div>
  )
}

function ShareDownloadAuthErrorState({ message }: { message: string }): JSX.Element {
  return (
    <div className="flex min-h-screen items-center justify-center bg-gradient-to-b from-slate-50 via-white to-slate-100 p-6 dark:from-slate-950 dark:via-slate-900 dark:to-slate-950">
      <div className="w-full max-w-md space-y-4 rounded-xl border border-border bg-background/80 p-6 text-center shadow-sm backdrop-blur">
        <h1 className="text-2xl font-semibold text-foreground">Could not verify sign-in</h1>
        <p className="text-muted-foreground">{message}</p>
        <Button type="button" onClick={() => window.location.reload()} className="w-full">
          Try again
        </Button>
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
