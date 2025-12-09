import type { FileDetail, FileItem, FilePreview } from "@/lib/types"
import type { ViewerDescriptor, ViewerType } from "@/lib/viewer-types"

export type ViewerCategory = ViewerType | "code"

export type ViewerPreference = {
  category?: ViewerCategory
}

export type ViewerConfig = {
  category: ViewerCategory
  viewerType: ViewerType
}

const CODE_EXTENSIONS = new Set([
  "ts",
  "tsx",
  "js",
  "jsx",
  "json",
  "css",
  "scss",
  "java",
  "kt",
  "py",
  "rb",
  "go",
  "rs",
  "swift",
  "c",
  "cpp",
  "cs",
  "sql",
  "md",
])

const WORD_MIME_TYPES = new Set([
  "application/msword",
  "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
  "application/rtf",
])

const EXCEL_MIME_TYPES = new Set([
  "application/vnd.ms-excel",
  "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
  "text/csv",
])

const WORD_EXTENSIONS = new Set(["doc", "docx", "dot", "dotx", "rtf"])
const EXCEL_EXTENSIONS = new Set(["xls", "xlsx", "xlsm", "xltx", "csv"])

function getExtension(name?: string): string | undefined {
  if (!name) {
    return undefined
  }

  const lastDot = name.lastIndexOf(".")
  if (lastDot === -1 || lastDot === name.length - 1) {
    return undefined
  }

  return name.slice(lastDot + 1).toLowerCase()
}

function normalizeViewerType(input?: string | ViewerType | null): ViewerType | undefined {
  if (!input) {
    return undefined
  }

  const normalized = typeof input === "string" ? input.trim().toLowerCase() : input

  switch (normalized) {
    case "video":
    case "image":
    case "pdf":
    case "code":
    case "word":
    case "excel":
    case "unsupported":
      return normalized
    default:
      return undefined
  }
}

function normalizeViewerCategory(input?: string | ViewerCategory | null): ViewerCategory | undefined {
  if (!input) {
    return undefined
  }

  const normalized = typeof input === "string" ? input.trim().toLowerCase() : input

  switch (normalized) {
    case "video":
    case "image":
    case "pdf":
    case "word":
    case "excel":
    case "unsupported":
    case "code":
      return normalized as ViewerCategory
    default:
      return undefined
  }
}

function viewerTypeToCategory(viewerType?: ViewerType): ViewerCategory | undefined {
  if (!viewerType) {
    return undefined
  }

  return viewerType
}

function detectCategoryFromPreview(preview?: FilePreview): ViewerCategory | undefined {
  if (!preview) {
    return undefined
  }

  switch (preview.kind) {
    case "video":
      return "video"
    case "image":
    case "design":
      return "image"
    case "code":
      return "code"
    case "document":
      return "pdf"
    default:
      return undefined
  }
}

function detectCategoryFromMime(mime?: string): ViewerCategory | undefined {
  if (!mime) {
    return undefined
  }

  const normalized = mime.toLowerCase()

  if (normalized.startsWith("video/")) {
    return "video"
  }

  if (normalized.startsWith("image/")) {
    return "image"
  }

  if (normalized === "application/pdf") {
    return "pdf"
  }

  if (WORD_MIME_TYPES.has(normalized)) {
    return "word"
  }

  if (EXCEL_MIME_TYPES.has(normalized)) {
    return "excel"
  }

  if (normalized.includes("json") || normalized.includes("javascript")) {
    return "code"
  }

  return undefined
}

function detectCategoryFromExtension(name?: string): ViewerCategory | undefined {
  const ext = getExtension(name)

  if (!ext) {
    return undefined
  }

  if (CODE_EXTENSIONS.has(ext)) {
    return "code"
  }

  if (ext === "pdf") {
    return "pdf"
  }

  if (WORD_EXTENSIONS.has(ext)) {
    return "word"
  }

  if (EXCEL_EXTENSIONS.has(ext)) {
    return "excel"
  }

  return undefined
}

function detectCategoryFromType(type?: FileItem["type"]): ViewerCategory | undefined {
  switch (type) {
    case "video":
      return "video"
    case "image":
    case "design":
      return "image"
    case "code":
      return "code"
    case "document":
      return "pdf"
    default:
      return undefined
  }
}

export function parseViewerPreference(
  categoryParam?: string | null,
  _officeParam?: string | null,
): ViewerPreference {
  const category = normalizeViewerCategory(categoryParam)

  return category ? { category } : {}
}

function buildDetectionResult(category?: ViewerCategory, viewerType?: ViewerType): ViewerConfig {
  if (viewerType === "unsupported") {
    return { category: "unsupported", viewerType }
  }

  const resolvedCategory = category ?? viewerTypeToCategory(viewerType) ?? "unsupported"
  const resolvedViewerType: ViewerType = viewerType ?? (resolvedCategory as ViewerType)

  return { category: resolvedCategory, viewerType: resolvedViewerType }
}

export function inferViewerConfigFromFileItem(
  file: Pick<FileItem, "type" | "name" | "latestVersionMimeType">,
): ViewerConfig {
  const category =
    detectCategoryFromMime(file.latestVersionMimeType) ??
    detectCategoryFromExtension(file.name) ??
    detectCategoryFromType(file.type)

  return buildDetectionResult(category)
}

export function resolveViewerConfig(
  file: FileDetail,
  descriptor?: ViewerDescriptor,
  preference?: ViewerPreference,
): ViewerConfig {
  const descriptorViewerType = normalizeViewerType(descriptor?.viewerType)
  const descriptorCategory = viewerTypeToCategory(descriptorViewerType)
  const preferredCategory = normalizeViewerCategory(preference?.category)

  const detectedCategory =
    preferredCategory ??
    descriptorCategory ??
    detectCategoryFromPreview(file.preview) ??
    detectCategoryFromMime(file.latestVersionMimeType) ??
    detectCategoryFromExtension(file.name) ??
    detectCategoryFromType(file.type)

  return buildDetectionResult(detectedCategory, descriptorViewerType)
}
