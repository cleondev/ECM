"use client"

import { FileWarning } from "lucide-react"

import type { FileDetail } from "@/lib/types"
import { getExtension } from "@/components/shared/sidebar-tabs"

type UnsupportedViewerProps = {
  file: FileDetail
  message?: string
}

export function UnsupportedViewer({ file, message }: UnsupportedViewerProps) {
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
