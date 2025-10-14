"use client"

import type React from "react"

import type { FileItem } from "./file-manager"
import { FileText, ImageIcon, Video, Code, LucideComponent as FileIconComponent, MoreVertical } from "lucide-react"
import { cn } from "@/lib/utils"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"

type FileListItemProps = {
  file: FileItem
  isSelected: boolean
  isMultiSelected?: boolean
  onSelect: (e: React.MouseEvent) => void
}

const fileIcons = {
  design: FileIconComponent,
  document: FileText,
  image: ImageIcon,
  video: Video,
  code: Code,
}

const statusColors = {
  "in-progress": "bg-yellow-500/10 text-yellow-700 dark:text-yellow-400",
  completed: "bg-green-500/10 text-green-700 dark:text-green-400",
  draft: "bg-gray-500/10 text-gray-700 dark:text-gray-400",
}

export function FileListItem({ file, isSelected, isMultiSelected, onSelect }: FileListItemProps) {
  const Icon = fileIcons[file.type]

  return (
    <button
      onClick={onSelect}
      className={cn(
        "w-full flex items-center gap-3 p-3 rounded-lg border bg-card text-left transition-all hover:shadow-sm",
        isSelected ? "border-primary ring-2 ring-primary/20" : "border-border hover:border-primary/50",
        isMultiSelected && !isSelected && "border-primary/50 bg-primary/5",
      )}
    >
      <div className="h-10 w-10 rounded bg-muted flex items-center justify-center flex-shrink-0">
        <Icon className="h-5 w-5 text-muted-foreground" />
      </div>

      <div className="flex-1 min-w-0">
        <div className="flex items-center gap-2 mb-1">
          <h3 className="font-medium text-sm truncate text-card-foreground">{file.name}</h3>
          {file.status && <Badge className={cn("text-xs", statusColors[file.status])}>{file.status}</Badge>}
        </div>
        <div className="flex items-center gap-2 text-xs text-muted-foreground">
          <span>{file.size}</span>
          <span>•</span>
          <span>{file.modified}</span>
          <span>•</span>
          <span>{file.owner}</span>
        </div>
      </div>

      <div className="flex items-center gap-2 flex-shrink-0">
        <div className="flex flex-wrap gap-1">
          {file.tags.slice(0, 3).map((tag) => (
            <Badge key={tag} variant="secondary" className="text-xs">
              {tag}
            </Badge>
          ))}
        </div>
        <Button variant="ghost" size="icon" className="h-8 w-8" onClick={(e) => e.stopPropagation()}>
          <MoreVertical className="h-4 w-4" />
        </Button>
      </div>
    </button>
  )
}
