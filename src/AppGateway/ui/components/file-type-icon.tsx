import { cn } from "@/lib/utils"
import type { FileItem } from "@/lib/types"
import squareOIcons from "file-icon-vectors/dist/icons/square-o/catalog.json"

type FileSummary = Pick<FileItem, "name" | "type">

type FileTypeIconProps = {
  file?: FileSummary | null
  className?: string
  size?: "sm" | "md" | "lg" | "xl"
}

const fallbackIcons: Record<FileItem["type"], string> = {
  design: "ai",
  document: "doc",
  image: "image",
  video: "mp4",
  code: "js",
}

const availableIcons = new Set((squareOIcons as string[]).map((icon) => icon.toLowerCase()))

const sizeClassMap: Record<NonNullable<FileTypeIconProps["size"]>, string> = {
  sm: "text-[1.25rem]",
  md: "text-[2rem]",
  lg: "text-[3rem]",
  xl: "text-[3.75rem]",
}

export function getFileIconName(file?: FileSummary | null) {
  if (!file) {
    return "blank"
  }

  const extension = extractExtension(file.name)
  if (extension && availableIcons.has(extension)) {
    return extension
  }

  return fallbackIcons[file.type] ?? "blank"
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

export function FileTypeIcon({ file, className, size = "md" }: FileTypeIconProps) {
  const iconName = getFileIconName(file)
  const sizeClass = sizeClassMap[size]

  return (
    <span
      aria-hidden="true"
      className={cn("fiv-sqo", `fiv-icon-${iconName}`, sizeClass, className)}
    />
  )
}
