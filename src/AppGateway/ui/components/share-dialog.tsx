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
import { Alert, AlertDescription } from "@/components/ui/alert"
import { Badge } from "@/components/ui/badge"
import { Check, Copy, ExternalLink, Link2, Loader2 } from "lucide-react"
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

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent className="max-w-3xl sm:max-w-4xl">
        <DialogHeader>
          <DialogTitle>Share file</DialogTitle>
          <DialogDescription>
            Generate a temporary link to share “{file?.name ?? "No file selected"}”. The link will expire automatically.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-6">
          <div className="flex items-center justify-between rounded-md border border-dashed border-border bg-muted/30 px-4 py-3">
            <div>
              <p className="text-sm font-medium text-card-foreground">{file?.name ?? "Select a file"}</p>
              <p className="text-xs text-muted-foreground">
                {file?.size ?? ""}
                {file?.latestVersionNumber ? ` • Version ${file.latestVersionNumber}` : ""}
              </p>
            </div>
            <Badge variant="outline" className="gap-1">
              <Link2 className="h-3.5 w-3.5" />
              Share link
            </Badge>
          </div>

          <div className="space-y-4">
            <div className="flex items-center justify-between rounded-md border border-border px-4 py-3">
              <div>
                <Label htmlFor="share-public" className="font-medium">
                  Public access
                </Label>
                <p className="text-xs text-muted-foreground">
                  Anyone with the link can download this file until it expires.
                </p>
              </div>
              <Switch id="share-public" checked={isPublic} onCheckedChange={setIsPublic} />
            </div>

            <div className="space-y-2">
              <Label htmlFor="share-duration" className="font-medium">
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

          {result ? (
            <div className="space-y-4">
              <div className="rounded-lg border border-primary/40 bg-primary/5 p-4">
                <div className="flex flex-wrap items-center justify-between gap-2">
                  <div className="space-y-1">
                    <p className="text-sm font-semibold text-primary">Short link</p>
                    <p className="text-xs text-muted-foreground">
                      Use the shortened link for quick sharing in chats or emails.
                    </p>
                  </div>
                  <Button asChild variant="ghost" size="sm" className="gap-1">
                    <a href={result.shortUrl} target="_blank" rel="noreferrer">
                      Open link
                      <ExternalLink className="h-3.5 w-3.5" />
                    </a>
                  </Button>
                </div>
                <div className="mt-3 flex flex-col gap-2 sm:flex-row sm:items-center">
                  <Input value={result.shortUrl} readOnly className="sm:flex-1" />
                  <div className="flex gap-2">
                    <Button
                      variant="outline"
                      size="icon"
                      onClick={() => handleCopy(result.shortUrl, "short")}
                    >
                      {copiedShort ? <Check className="h-4 w-4" /> : <Copy className="h-4 w-4" />}
                      <span className="sr-only">Copy short link</span>
                    </Button>
                  </div>
                </div>
              </div>

              <div className="rounded-lg border bg-muted/20 p-4">
                <div className="flex flex-wrap items-center justify-between gap-2">
                  <div className="space-y-1">
                    <p className="text-sm font-medium text-muted-foreground">Full link</p>
                    <p className="text-xs text-muted-foreground">
                      The full link includes the complete URL with tracking parameters.
                    </p>
                  </div>
                  <Button asChild variant="ghost" size="sm" className="gap-1">
                    <a href={result.url} target="_blank" rel="noreferrer">
                      Open link
                      <ExternalLink className="h-3.5 w-3.5" />
                    </a>
                  </Button>
                </div>
                <div className="mt-3 flex flex-col gap-2 sm:flex-row sm:items-center">
                  <Input value={result.url} readOnly className="sm:flex-1" />
                  <div className="flex gap-2">
                    <Button
                      variant="outline"
                      size="icon"
                      onClick={() => handleCopy(result.url, "full")}
                    >
                      {copiedFull ? <Check className="h-4 w-4" /> : <Copy className="h-4 w-4" />}
                      <span className="sr-only">Copy full link</span>
                    </Button>
                  </div>
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
