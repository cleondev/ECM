"use client"

import type { FileDetail } from "@/lib/types"
import { UnsupportedViewer } from "./UnsupportedViewer"

type ImageViewerProps = {
  file: FileDetail
  src?: string | null
  fallbackSrc?: string | null
}

export function ImageViewer({ file, src, fallbackSrc }: ImageViewerProps) {
  if (!src && !fallbackSrc) {
    return <UnsupportedViewer file={file} message="Không có nội dung hình ảnh để hiển thị." />
  }

  return (
    <div className="flex items-center justify-center rounded-lg border border-border bg-background p-4">
      <img
        src={src ?? fallbackSrc ?? undefined}
        alt={file.name}
        className="max-h-[70vh] w-auto max-w-full rounded-md shadow"
        loading="lazy"
      />
    </div>
  )
}
