"use client"

import type React from "react"

import type { FileItem } from "./file-manager"
import { MoreVertical } from "lucide-react"
import { cn } from "@/lib/utils"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { FileTypeIcon } from "./file-type-icon"

type FileCardProps = {
  file: FileItem
  isSelected: boolean
  isMultiSelected?: boolean
  onSelect: (e: React.MouseEvent) => void
}

const statusColors = {
  "in-progress": "bg-yellow-500/10 text-yellow-700 dark:text-yellow-400",
  completed: "bg-green-500/10 text-green-700 dark:text-green-400",
  draft: "bg-gray-500/10 text-gray-700 dark:text-gray-400",
}

export function FileCard({ file, isSelected, isMultiSelected, onSelect }: FileCardProps) {
  return (
    <button
      onClick={onSelect}
      className={cn(
        "group relative flex flex-col rounded-lg border bg-card text-left transition-all hover:shadow-md",
        "flex-[1_0_160px] sm:flex-[1_0_200px] lg:flex-[1_0_220px] xl:flex-[1_0_240px]",
        "max-w-[200px] sm:max-w-[240px] lg:max-w-[260px] xl:max-w-[280px]",
        isSelected ? "border-primary ring-2 ring-primary/20" : "border-border hover:border-primary/50",
        isMultiSelected && !isSelected && "border-primary/50 bg-primary/5",
      )}
    >
      <div className="aspect-video w-full rounded-t-lg bg-muted flex items-center justify-center relative overflow-hidden">
        <FileTypeIcon file={file} size="lg" />
        {file.status && (
          <Badge className={cn("absolute top-2 right-2 text-xs", statusColors[file.status])}>{file.status}</Badge>
        )}
      </div>

      <div className="px-3 py-2.5 sm:p-3 flex-1 flex flex-col">
        <div className="flex items-start justify-between gap-2 mb-1.5 sm:mb-2">
          <h3 className="font-medium text-[13px] sm:text-sm leading-5 line-clamp-2 text-card-foreground">{file.name}</h3>
          <Button
            variant="ghost"
            size="icon"
            className="h-6 w-6 opacity-0 group-hover:opacity-100 transition-opacity"
            onClick={(e) => {
              e.stopPropagation()
            }}
          >
            <MoreVertical className="h-3 w-3" />
          </Button>
        </div>

        <div className="flex items-center gap-2 text-[11px] sm:text-xs text-muted-foreground mb-1.5 sm:mb-2">
          <span>{file.size}</span>
          <span>•</span>
          <span>{file.modified}</span>
        </div>

        <div className="flex flex-wrap gap-1 mt-auto">
          {file.tags.slice(0, 2).map((tag) => (
            <Badge key={tag} variant="secondary" className="text-xs">
              {tag}
            </Badge>
          ))}
          {file.tags.length > 2 && (
            <Badge variant="secondary" className="text-xs">
              +{file.tags.length - 2}
            </Badge>
          )}
        </div>
      </div>
    </button>
  )
}
