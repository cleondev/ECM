"use client"

import { FileCode } from "lucide-react"

import type { FileDetail } from "@/lib/types"
import { CodeHighlightViewer } from "./CodeHighlightViewer"
import { UnsupportedViewer } from "./UnsupportedViewer"

type CodeViewerProps = {
  file: FileDetail
}

export function CodeViewer({ file }: CodeViewerProps) {
  if (file.preview.kind !== "code") {
    return <UnsupportedViewer file={file} message="Không có nội dung mã nguồn để hiển thị." />
  }

  const { language, content } = file.preview

  if (!content?.trim()) {
    return <UnsupportedViewer file={file} message="Không có nội dung mã nguồn để hiển thị." />
  }

  return (
    <div className="space-y-3">
      <div className="flex items-center gap-2 rounded-lg border border-border bg-slate-900 px-4 py-2 text-xs uppercase tracking-wide text-slate-200">
        <FileCode className="h-4 w-4" />
        <span>{language?.toUpperCase() ?? "CODE"}</span>
      </div>
      <div className="overflow-hidden rounded-lg border border-border bg-slate-900">
        <CodeHighlightViewer code={content} language={language} />
      </div>
    </div>
  )
}
