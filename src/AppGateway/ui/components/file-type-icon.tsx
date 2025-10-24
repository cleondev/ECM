import { cn } from "@/lib/utils"
import type { FileItem } from "@/lib/types"
import type { CSSProperties } from "react"
import type { LucideIcon } from "lucide-react"
import {
  Clapperboard,
  File as FileIcon,
  FileCode,
  FileText,
  Image as ImageIcon,
  Palette,
} from "lucide-react"

type FileSummary = Pick<FileItem, "name" | "type">

type FileTypeIconProps = {
  file?: FileSummary | null
  className?: string
  size?: "sm" | "md" | "lg" | "xl"
}

type FileVisualType = FileItem["type"] | "other"

const sizeStyles: Record<NonNullable<FileTypeIconProps["size"]>, { wrapper: string; icon: string }> = {
  sm: { wrapper: "h-8 w-8 rounded-md text-[0.625rem]", icon: "h-4 w-4" },
  md: { wrapper: "h-10 w-10 rounded-lg text-sm", icon: "h-5 w-5" },
  lg: { wrapper: "h-14 w-14 rounded-xl text-base", icon: "h-7 w-7" },
  xl: { wrapper: "h-16 w-16 rounded-2xl text-lg", icon: "h-9 w-9" },
}

const typeIcons: Record<FileVisualType, LucideIcon> = {
  design: Palette,
  document: FileText,
  image: ImageIcon,
  video: Clapperboard,
  code: FileCode,
  other: FileIcon,
}

const typeColorStyles: Record<FileVisualType, CSSProperties> = {
  design: {
    backgroundColor: "color-mix(in srgb, var(--color-chart-5) 16%, transparent)",
    color: "var(--color-chart-5)",
    borderColor: "color-mix(in srgb, var(--color-chart-5) 35%, transparent)",
  },
  document: {
    backgroundColor: "color-mix(in srgb, var(--color-chart-1) 16%, transparent)",
    color: "var(--color-chart-1)",
    borderColor: "color-mix(in srgb, var(--color-chart-1) 35%, transparent)",
  },
  image: {
    backgroundColor: "color-mix(in srgb, var(--color-chart-3) 18%, transparent)",
    color: "var(--color-chart-3)",
    borderColor: "color-mix(in srgb, var(--color-chart-3) 38%, transparent)",
  },
  video: {
    backgroundColor: "color-mix(in srgb, var(--color-chart-4) 18%, transparent)",
    color: "var(--color-chart-4)",
    borderColor: "color-mix(in srgb, var(--color-chart-4) 38%, transparent)",
  },
  code: {
    backgroundColor: "color-mix(in srgb, var(--color-chart-2) 18%, transparent)",
    color: "var(--color-chart-2)",
    borderColor: "color-mix(in srgb, var(--color-chart-2) 38%, transparent)",
  },
  other: {
    backgroundColor: "var(--color-muted)",
    color: "var(--color-muted-foreground)",
    borderColor: "var(--color-border)",
  },
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

export function FileTypeIcon({ file, className, size = "md" }: FileTypeIconProps) {
  const { type, extension } = resolveVisualType(file)
  const { wrapper, icon } = sizeStyles[size]
  const IconComponent = typeIcons[type]
  const colorStyle = typeColorStyles[type]
  const shouldShowExtension = type === "other" && extension

  return (
    <span
      aria-hidden="true"
      className={cn(
        "inline-flex items-center justify-center border font-semibold uppercase leading-none tracking-tight",
        wrapper,
        className,
      )}
      style={colorStyle}
    >
      {shouldShowExtension ? (
        <span className="truncate">{extension?.slice(0, 4)}</span>
      ) : (
        <IconComponent className={cn("shrink-0", icon)} strokeWidth={1.75} />
      )}
    </span>
  )
}
