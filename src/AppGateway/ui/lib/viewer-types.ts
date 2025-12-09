import type { DocumentVersion } from "./types"

export type ViewerType = "word" | "excel" | "image" | "video" | "pdf" | "code" | "unsupported"

export type ViewerView = {
  url?: string | null
}

export type ViewerDescriptorDto = {
  version: DocumentVersion
  viewerType?: string | null
  view?: ViewerView | null
  previewUrl?: string | null
  downloadUrl?: string | null
  thumbnailUrl?: string | null
  sfdtUrl?: string | null
  excelJsonUrl?: string | null
}

export type ViewerDescriptor = {
  version: DocumentVersion
  viewerType: ViewerType
  view?: ViewerView | null
  previewUrl?: string | null
  downloadUrl?: string | null
  thumbnailUrl?: string | null
  sfdtUrl?: string | null
  excelJsonUrl?: string | null
}

export function normalizeViewerType(value?: string | null): ViewerType | undefined {
  if (!value) {
    return undefined
  }

  const normalized = value.trim().toLowerCase()

  switch (normalized) {
    case "word":
    case "excel":
    case "image":
    case "video":
    case "pdf":
    case "code":
    case "unsupported":
      return normalized
    default:
      return undefined
  }
}

export function toViewerDescriptor(dto: ViewerDescriptorDto): ViewerDescriptor {
  const viewerType = normalizeViewerType(dto.viewerType) ?? "unsupported"
  const viewUrl = dto.view?.url ?? dto.previewUrl ?? null
  const view = dto.view ?? (viewUrl ? { url: viewUrl } : null)

  return {
    version: dto.version,
    viewerType,
    view,
    previewUrl: dto.previewUrl ?? viewUrl ?? undefined,
    downloadUrl: dto.downloadUrl ?? undefined,
    thumbnailUrl: dto.thumbnailUrl ?? undefined,
    sfdtUrl: dto.sfdtUrl ?? null,
    excelJsonUrl: dto.excelJsonUrl ?? null,
  }
}
