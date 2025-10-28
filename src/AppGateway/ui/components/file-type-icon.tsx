import { cn } from "@/lib/utils"
import type { FileItem } from "@/lib/types"

type FileSummary = Pick<FileItem, "name" | "type">

type FileTypeIconProps = {
  file?: FileSummary | null
  className?: string
  size?: "sm" | "md" | "lg" | "xl"
}

type FileVisualType = FileItem["type"] | "other"

const sizeStyles: Record<NonNullable<FileTypeIconProps["size"]>, { wrapper: string; icon: string }> = {
  sm: { wrapper: "h-8 w-8", icon: "text-[2rem]" },
  md: { wrapper: "h-10 w-10", icon: "text-[2.5rem]" },
  lg: { wrapper: "h-14 w-14", icon: "text-[3.5rem]" },
  xl: { wrapper: "h-16 w-16", icon: "text-[4rem]" },
}

const typeFallbackIcons: Record<FileVisualType, string> = {
  design: "fiv-icon-ai",
  document: "fiv-icon-tex",
  image: "fiv-icon-image",
  video: "fiv-icon-mp4",
  code: "fiv-icon-js",
  other: "fiv-icon-blank",
}

const designExtensions = new Set([
  "ai",
  "ait",
  "psd",
  "psb",
  "sketch",
  "xd",
  "fig",
  "indd",
  "idml",
  "eps",
  "svg",
])

const documentExtensions = new Set([
  "pdf",
  "doc",
  "docx",
  "docm",
  "dot",
  "dotx",
  "dotm",
  "rtf",
  "txt",
  "md",
  "csv",
  "ppt",
  "pptx",
  "pps",
  "ppsx",
  "xls",
  "xlsx",
  "xlsm",
  "xlt",
  "xltx",
  "xltm",
  "tex",
])

const imageExtensions = new Set([
  "png",
  "jpg",
  "jpeg",
  "gif",
  "webp",
  "bmp",
  "tif",
  "tiff",
  "ico",
  "heic",
  "raw",
  "nef",
  "cr2",
  "dng",
])

const videoExtensions = new Set([
  "mp4",
  "mov",
  "m4v",
  "avi",
  "wmv",
  "flv",
  "webm",
  "mkv",
  "mpeg",
  "mpg",
])

const codeExtensions = new Set([
  "js",
  "jsx",
  "ts",
  "tsx",
  "json",
  "html",
  "css",
  "scss",
  "sass",
  "less",
  "mdx",
  "yml",
  "yaml",
  "xml",
  "c",
  "cpp",
  "h",
  "hpp",
  "cs",
  "java",
  "py",
  "rb",
  "php",
  "go",
  "rs",
  "kt",
  "swift",
  "sql",
  "sh",
  "bash",
  "zsh",
  "ps1",
  "bat",
  "ini",
  "cfg",
  "toml",
  "lock",
])

function mapExtensionToType(extension: string): FileItem["type"] | undefined {
  if (designExtensions.has(extension)) {
    return "design"
  }
  if (documentExtensions.has(extension)) {
    return "document"
  }
  if (imageExtensions.has(extension)) {
    return "image"
  }
  if (videoExtensions.has(extension)) {
    return "video"
  }
  if (codeExtensions.has(extension)) {
    return "code"
  }

  return undefined
}

function extractExtension(fileName: string | undefined) {
  if (!fileName) {
    return null
  }

  const trimmed = fileName.trim()
  const lastDotIndex = trimmed.lastIndexOf(".")

  if (lastDotIndex <= 0 || lastDotIndex === trimmed.length - 1) {
    return null
  }

  return trimmed.slice(lastDotIndex + 1).toLowerCase()
}

function resolveVisualType(file?: FileSummary | null): { type: FileVisualType; extension: string | null } {
  if (!file) {
    return { type: "other", extension: null }
  }

  const extension = extractExtension(file.name)
  const typeFromExtension = extension ? mapExtensionToType(extension) : undefined
  const type = typeFromExtension ?? file.type ?? "document"

  return { type: type as FileVisualType, extension }
}

function buildIconClass(extension: string | null, type: FileVisualType) {
  if (!extension) {
    return typeFallbackIcons[type]
  }

  if (!/^[a-z0-9_-]+$/i.test(extension)) {
    return typeFallbackIcons[type]
  }

  return `fiv-icon-${extension.toLowerCase()}`
}

export function FileTypeIcon({ file, className, size = "md" }: FileTypeIconProps) {
  const { type, extension } = resolveVisualType(file)
  const { wrapper, icon } = sizeStyles[size]
  const iconClass = buildIconClass(extension, type)

  return (
    <span
      aria-hidden="true"
      className={cn("inline-flex items-center justify-center", wrapper, className)}
    >
      <span className={cn("fiv-sqo fiv-icon-blank", icon, iconClass)} role="presentation" />
    </span>
  )
}
