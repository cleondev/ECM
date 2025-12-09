"use client"

import { PdfViewer } from "@/app/viewer/pdf-viewer"
import type { FileDetail } from "@/lib/types"
import type { ViewerCategory } from "@/lib/viewer-utils"
import type { ViewerDescriptor, ViewerType } from "@/lib/viewer-types"
import { ExcelViewer } from "./ExcelViewer"
import { ImageViewer } from "./ImageViewer"
import { UnsupportedViewer } from "./UnsupportedViewer"
import { VideoViewer } from "./VideoViewer"
import { WordViewer } from "./WordViewer"

type ViewerPanelProps = {
  file: FileDetail
  viewerCategory: ViewerCategory
  viewerType: ViewerType
  descriptor?: ViewerDescriptor | null
  previewUrl?: string
  thumbnailUrl?: string
  wordViewerUrl?: string | null
  excelViewerUrl?: string | null
}

export function ViewerPanel({
  file,
  viewerCategory,
  viewerType,
  descriptor,
  previewUrl,
  thumbnailUrl,
  wordViewerUrl,
  excelViewerUrl,
}: ViewerPanelProps) {
  const resolvedPreviewUrl = descriptor?.view?.url ?? previewUrl
  const resolvedThumbnail = descriptor?.thumbnailUrl ?? thumbnailUrl ?? file.thumbnail
  const resolvedWordUrl = descriptor?.sfdtUrl ?? wordViewerUrl ?? resolvedPreviewUrl
  const resolvedExcelUrl = descriptor?.excelJsonUrl ?? excelViewerUrl ?? resolvedPreviewUrl

  if (viewerType === "unsupported" || viewerCategory === "unsupported") {
    return <UnsupportedViewer file={file} />
  }

  switch (viewerCategory) {
    case "pdf":
      return resolvedPreviewUrl ? (
        <PdfViewer documentUrl={resolvedPreviewUrl} />
      ) : (
        <UnsupportedViewer file={file} message="Không tìm thấy file PDF" />
      )
    case "image": {
      const previewSource =
        resolvedPreviewUrl || (file.preview.kind === "image" || file.preview.kind === "design" ? file.preview.url : null)
      return <ImageViewer file={file} src={previewSource} fallbackSrc={resolvedThumbnail} />
    }
    case "video": {
      const source = file.preview.kind === "video" ? file.preview.url : resolvedPreviewUrl
      const poster = file.preview.kind === "video" ? file.preview.poster : resolvedThumbnail
      return <VideoViewer file={file} src={source} poster={poster ?? undefined} />
    }
    case "word":
      return <WordViewer file={file} sfdtUrl={resolvedWordUrl} />
    case "excel":
      return <ExcelViewer file={file} excelJsonUrl={resolvedExcelUrl} />
    default:
      return <UnsupportedViewer file={file} />
  }
}
