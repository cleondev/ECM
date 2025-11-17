import type { FileDetail, FileItem, FilePreview } from "@/lib/types"

export type ViewerCategory = "video" | "image" | "code" | "pdf" | "unsupported"
export type OfficeViewerKind = "word" | "excel" | "powerpoint"

export type ViewerPreference = {
  category?: ViewerCategory
  officeKind?: OfficeViewerKind
}

export type ViewerConfig = {
  category: ViewerCategory
  officeKind?: OfficeViewerKind
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

const OFFICE_EXTENSION_MAP: Record<string, OfficeViewerKind> = {
  doc: "word",
  docx: "word",
  dot: "word",
  dotx: "word",
  rtf: "word",
  xls: "excel",
  xlsx: "excel",
  xlsm: "excel",
  xltx: "excel",
  csv: "excel",
  ppt: "powerpoint",
  pptx: "powerpoint",
  pps: "powerpoint",
  potx: "powerpoint",
}

const OFFICE_MIME_MAP: Record<string, OfficeViewerKind> = {
  "application/msword": "word",
  "application/vnd.openxmlformats-officedocument.wordprocessingml.document": "word",
  "application/rtf": "word",
  "application/vnd.ms-excel": "excel",
  "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet": "excel",
  "text/csv": "excel",
  "application/vnd.ms-powerpoint": "powerpoint",
  "application/vnd.openxmlformats-officedocument.presentationml.presentation": "powerpoint",
}

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

function detectOfficeViewerKind(meta: {
  latestVersionMimeType?: string
  name?: string
}): OfficeViewerKind | undefined {
  const mime = meta.latestVersionMimeType?.toLowerCase()
  if (mime && OFFICE_MIME_MAP[mime]) {
    return OFFICE_MIME_MAP[mime]
  }

  const ext = getExtension(meta.name)
  if (ext && OFFICE_EXTENSION_MAP[ext]) {
    return OFFICE_EXTENSION_MAP[ext]
  }

  return undefined
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

  if (mime.startsWith("video/")) {
    return "video"
  }

  if (mime.startsWith("image/")) {
    return "image"
  }

  if (mime === "application/pdf") {
    return "pdf"
  }

  if (mime.includes("json") || mime.includes("javascript")) {
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

function normalizeViewerCategory(input?: string | null): ViewerCategory | undefined {
  if (!input) {
    return undefined
  }

  switch (input) {
    case "video":
    case "image":
    case "code":
    case "pdf":
    case "unsupported":
      return input
    default:
      return undefined
  }
}

function normalizeOfficeKind(input?: string | null): OfficeViewerKind | undefined {
  if (!input) {
    return undefined
  }

  switch (input) {
    case "word":
    case "excel":
    case "powerpoint":
      return input
    default:
      return undefined
  }
}

export function parseViewerPreference(
  categoryParam?: string | null,
  officeParam?: string | null,
): ViewerPreference {
  const category = normalizeViewerCategory(categoryParam)
  const officeKind = normalizeOfficeKind(officeParam)

  return category || officeKind ? { category, officeKind } : {}
}

function buildDetectionResult(
  category?: ViewerCategory,
  officeKind?: OfficeViewerKind,
): ViewerConfig {
  const safeCategory: ViewerCategory = category ?? "unsupported"

  if (officeKind && safeCategory !== "pdf") {
    return { category: safeCategory }
  }

  return officeKind ? { category: "pdf", officeKind } : { category: safeCategory }
}

export function inferViewerConfigFromFileItem(file: Pick<FileItem, "type" | "name" | "latestVersionMimeType">): ViewerConfig {
  const officeKind = detectOfficeViewerKind(file)
  const category =
    detectCategoryFromMime(file.latestVersionMimeType) ??
    detectCategoryFromExtension(file.name) ??
    detectCategoryFromType(file.type)

  return buildDetectionResult(category, officeKind)
}

export function resolveViewerConfig(
  file: FileDetail,
  preference?: ViewerPreference,
): ViewerConfig {
  const officeKind = preference?.officeKind ?? detectOfficeViewerKind(file)
  const previewCategory = detectCategoryFromPreview(file.preview)

  const detectedCategory =
    preference?.category ??
    previewCategory ??
    detectCategoryFromMime(file.latestVersionMimeType) ??
    detectCategoryFromExtension(file.name) ??
    detectCategoryFromType(file.type)

  return buildDetectionResult(detectedCategory, officeKind)
}
