"use client"

import type { FileDetail } from "@/lib/types"
import { UnsupportedViewer } from "./UnsupportedViewer"

type VideoViewerProps = {
  file: FileDetail
  src?: string | null
  poster?: string | null
}

export function VideoViewer({ file, src, poster }: VideoViewerProps) {
  if (!src) {
    return <UnsupportedViewer file={file} message="Không có nội dung video để hiển thị." />
  }

  return (
    <div className="overflow-hidden rounded-lg border border-border bg-background">
      <video controls className="h-[75vh] w-full bg-black" poster={poster ?? undefined} src={src} />
    </div>
  )
}
