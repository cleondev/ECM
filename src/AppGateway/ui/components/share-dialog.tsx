"use client"

import { useCallback, useEffect, useMemo, useState } from "react"
import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Label } from "@/components/ui/label"
import { Switch } from "@/components/ui/switch"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Input } from "@/components/ui/input"
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert"
import { Badge } from "@/components/ui/badge"
import { cn } from "@/lib/utils"
import { Check, Copy, ExternalLink, Link2, Loader2, Sparkles } from "lucide-react"
import type { FileItem, ShareLink, ShareOptions } from "@/lib/types"

const SHARE_DURATION_OPTIONS = [
  { label: "15 minutes", value: 15 },
  { label: "1 hour", value: 60 },
  { label: "8 hours", value: 480 },
  { label: "24 hours", value: 1440 },
  { label: "7 days", value: 10080 },
] as const

const DEFAULT_DURATION = SHARE_DURATION_OPTIONS[3].value

type ShareDialogProps = {
  open: boolean
  file: FileItem | null
  onOpenChange: (open: boolean) => void
  onConfirm: (options: ShareOptions) => Promise<void>
  isLoading: boolean
  result: ShareLink | null
  error?: string | null
  onReset: () => void
}

export function ShareDialog({
  open,
  file,
  onOpenChange,
  onConfirm,
  isLoading,
  result,
  error,
  onReset,
}: ShareDialogProps) {
  const [isPublic, setIsPublic] = useState(true)
  const [expiresInMinutes, setExpiresInMinutes] = useState<number>(DEFAULT_DURATION)
  const [copiedShort, setCopiedShort] = useState(false)
  const [copiedFull, setCopiedFull] = useState(false)

  const resetCopyState = useCallback(() => {
    setCopiedShort(false)
    setCopiedFull(false)
  }, [])

  useEffect(() => {
    if (!open) {
      resetCopyState()
      return
    }

    resetCopyState()
    setIsPublic(true)
    setExpiresInMinutes(DEFAULT_DURATION)
  }, [open, file?.id, resetCopyState])

  useEffect(() => {
    if (!result) {
      resetCopyState()
    }
  }, [resetCopyState, result?.shortUrl, result?.url])

  const formattedExpiry = useMemo(() => {
    if (!result?.expiresAtUtc) {
      return null
    }

    try {
      return new Date(result.expiresAtUtc).toLocaleString()
    } catch (err) {
      console.warn("[ui] Failed to format share link expiration:", err)
      return result.expiresAtUtc
    }
  }, [result?.expiresAtUtc])

  const handleOpenChange = (nextOpen: boolean) => {
    onOpenChange(nextOpen)
    if (!nextOpen) {
      onReset()
    }
  }

  const handleSubmit = async () => {
    if (!file || isLoading) {
      return
    }

    resetCopyState()
    await onConfirm({ isPublic, expiresInMinutes })
  }

  const handleCopy = async (value: string | null | undefined, type: "short" | "full") => {
    if (!value) {
      return
    }

    try {
      await navigator.clipboard.writeText(value)
      setCopiedShort(type === "short")
      setCopiedFull(type === "full")
    } catch (err) {
      console.error("[ui] Failed to copy share link to clipboard:", err)
      resetCopyState()
    }
  }

  const hasResult = Boolean(result)

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent className="max-w-3xl sm:max-w-4xl">
        <DialogHeader>
          <DialogTitle>Share file</DialogTitle>
          <DialogDescription>
            Generate a temporary link to share “{file?.name ?? "No file selected"}”. The link will expire automatically.
          </DialogDescription>
        </DialogHeader>

        <div
          className={cn(
            "space-y-6",
            hasResult &&
              "lg:grid lg:grid-cols-[minmax(0,1fr)_360px] lg:items-start lg:gap-6 lg:space-y-0",
          )}
        >
          <div className="space-y-6">
            <div className="rounded-xl border border-dashed border-primary/40 bg-primary/5 p-4">
              <div className="flex flex-wrap items-center justify-between gap-3">
                <div className="space-y-1">
                  <p className="text-sm font-medium text-card-foreground">{file?.name ?? "Select a file"}</p>
                  <p className="text-xs text-muted-foreground">
                    {file?.size ?? ""}
                    {file?.latestVersionNumber ? ` • Version ${file.latestVersionNumber}` : ""}
                  </p>
                </div>
                <Badge variant="outline" className="gap-1 border-primary/50 text-primary">
                  <Link2 className="h-3.5 w-3.5" />
                  Short link ready
                </Badge>
              </div>
              <p className="mt-3 text-xs text-muted-foreground">
                Generate a short link to share quickly in chat apps. The full link remains available for detailed audit trails.
              </p>
            </div>

            {!hasResult ? (
              <Alert className="border-primary/30 bg-primary/5 text-primary">
                <Sparkles className="h-4 w-4" />
                <AlertTitle>Shortened links are now built-in</AlertTitle>
                <AlertDescription>
                  Choose the duration and access level, then create the link. You will receive both the short and full URLs side
                  by side for copying.
                </AlertDescription>
              </Alert>
            ) : null}

            <div className="space-y-4 rounded-xl border bg-card p-4 shadow-sm">
              <div className="flex flex-wrap items-center justify-between gap-3">
                <div>
                  <Label htmlFor="share-public" className="font-semibold">
                    Public access
                  </Label>
                  <p className="text-xs text-muted-foreground">
                    Anyone with the link can download this file until it expires.
                  </p>
                </div>
                <Switch id="share-public" checked={isPublic} onCheckedChange={setIsPublic} />
              </div>

              <div className="space-y-2">
                <Label htmlFor="share-duration" className="font-semibold">
                  Share duration
                </Label>
                <Select
                  value={expiresInMinutes.toString()}
                  onValueChange={(value) => setExpiresInMinutes(Number.parseInt(value, 10))}
                >
                  <SelectTrigger id="share-duration" className="w-full">
                    <SelectValue placeholder="Select how long the link should stay active" />
                  </SelectTrigger>
                  <SelectContent>
                    {SHARE_DURATION_OPTIONS.map((option) => (
                      <SelectItem key={option.value} value={option.value.toString()}>
                        {option.label}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            </div>

            {error ? (
              <Alert variant="destructive">
                <AlertDescription>{error}</AlertDescription>
              </Alert>
            ) : null}
          </div>

          {hasResult ? (
            <div className="space-y-4 rounded-xl border border-primary/40 bg-primary/5 p-4 lg:sticky lg:top-6">
              <div className="space-y-3">
                <div className="flex flex-wrap items-start justify-between gap-3">
                  <div>
                    <p className="text-sm font-semibold text-primary">Short link</p>
                    <p className="text-xs text-muted-foreground">
                      Use this link for quick sharing in chat or email.
                    </p>
                  </div>
                  <Button asChild variant="ghost" size="sm" className="gap-1">
                    <a href={result.shortUrl} target="_blank" rel="noreferrer">
                      Open
                      <ExternalLink className="h-3.5 w-3.5" />
                    </a>
                  </Button>
                </div>
                <div className="flex flex-col gap-2 sm:flex-row sm:items-center">
                  <Input value={result.shortUrl} readOnly className="sm:flex-1" />
                  <Button
                    variant="outline"
                    size="icon"
                    onClick={() => handleCopy(result.shortUrl, "short")}
                    className={cn(
                      "transition-colors",
                      copiedShort ? "border-primary text-primary" : undefined,
                    )}
                  >
                    {copiedShort ? <Check className="h-4 w-4" /> : <Copy className="h-4 w-4" />}
                    <span className="sr-only">Copy short link</span>
                  </Button>
                </div>
              </div>

              <div className="space-y-3 rounded-lg border border-border bg-background/60 p-3">
                <div className="flex flex-wrap items-start justify-between gap-3">
                  <div>
                    <p className="text-sm font-medium text-muted-foreground">Full link</p>
                    <p className="text-xs text-muted-foreground">
                      Includes tracking parameters for auditing access.
                    </p>
                  </div>
                  <Button asChild variant="ghost" size="sm" className="gap-1">
                    <a href={result.url} target="_blank" rel="noreferrer">
                      Open
                      <ExternalLink className="h-3.5 w-3.5" />
                    </a>
                  </Button>
                </div>
                <div className="flex flex-col gap-2 sm:flex-row sm:items-center">
                  <Input value={result.url} readOnly className="sm:flex-1" />
                  <Button
                    variant="outline"
                    size="icon"
                    onClick={() => handleCopy(result.url, "full")}
                    className={cn(
                      "transition-colors",
                      copiedFull ? "border-primary text-primary" : undefined,
                    )}
                  >
                    {copiedFull ? <Check className="h-4 w-4" /> : <Copy className="h-4 w-4" />}
                    <span className="sr-only">Copy full link</span>
                  </Button>
                </div>
              </div>

              {formattedExpiry ? (
                <p className="text-xs text-muted-foreground">
                  Links expire on {formattedExpiry}. Share recipients will no longer have access afterwards.
                </p>
              ) : null}
            </div>
          ) : null}
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={() => handleOpenChange(false)}>
            Cancel
          </Button>
          <Button onClick={handleSubmit} disabled={!file || isLoading} className="gap-2">
            {isLoading ? <Loader2 className="h-4 w-4 animate-spin" /> : null}
            Create link
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
