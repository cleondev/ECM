"use client"

import { useRef } from "react"
import type React from "react"

import type { FileItem } from "@/lib/types"
import { Download, Edit3, Eye, FileText, GitBranch, MoreVertical, Share2, Tag, Trash2 } from "lucide-react"
import { cn } from "@/lib/utils"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { FileTypeIcon } from "./file-type-icon"
import {
  ContextMenu,
  ContextMenuContent,
  ContextMenuItem,
  ContextMenuSeparator,
  ContextMenuTrigger,
} from "@/components/ui/context-menu"

type FileCardProps = {
  file: FileItem
  isSelected: boolean
  isMultiSelected?: boolean
  onSelect: (e: React.MouseEvent) => void
  onContextMenuOpen?: () => void
  onView?: () => void
  onDownload?: () => void
  onShare?: () => void
  onAssignTags?: () => void
  onOpenDetails?: (tab: "property" | "flow" | "form") => void
  onDelete?: () => void
  actionsDisabled?: boolean
}

const statusColors: Record<NonNullable<FileItem['status']>, string> = {
  "in-progress": "bg-yellow-500/10 text-yellow-700 dark:text-yellow-400",
  completed: "bg-green-500/10 text-green-700 dark:text-green-400",
  draft: "bg-gray-500/10 text-gray-700 dark:text-gray-400",
}

export function FileCard({
  file,
  isSelected,
  isMultiSelected,
  onSelect,
  onContextMenuOpen,
  onView,
  onDownload,
  onShare,
  onAssignTags,
  onOpenDetails,
  onDelete,
  actionsDisabled,
}: FileCardProps) {
  const cardRef = useRef<HTMLButtonElement>(null)

  return (
    <ContextMenu onOpenChange={(open) => open && onContextMenuOpen?.()}>
      <ContextMenuTrigger asChild>
        <button
          onClick={onSelect}
          ref={cardRef}
          className={cn(
            "group relative flex h-full w-full flex-col overflow-hidden rounded-xl border bg-card text-left transition-all hover:shadow-lg",
            isSelected ? "border-primary ring-2 ring-primary/20" : "border-border hover:border-primary/50",
            isMultiSelected && !isSelected && "border-primary/50 bg-primary/5",
          )}
        >
          <div className="aspect-[4/3] w-full bg-muted/70 flex items-center justify-center relative">
            <FileTypeIcon file={file} size="lg" />
            {file.status ? (
              <Badge className={cn("absolute top-2 right-2 text-xs", statusColors[file.status])}>
                {file.status}
              </Badge>
            ) : null}
          </div>

          <div className="px-4 py-3 sm:p-4 flex-1 flex flex-col gap-2">
            <div className="flex items-start justify-between gap-3">
              <h3 className="flex-1 min-w-0 text-sm sm:text-base font-medium leading-5 text-card-foreground break-words line-clamp-2">
                {file.name}
              </h3>
              <Button
                variant="ghost"
                size="icon"
                className="h-7 w-7 opacity-0 group-hover:opacity-100 transition-opacity"
                onClick={(e) => {
                  e.stopPropagation()
                  const event = new MouseEvent("contextmenu", {
                    bubbles: true,
                    clientX: e.clientX,
                    clientY: e.clientY,
                  })
                  cardRef.current?.dispatchEvent(event)
                }}
              >
                <MoreVertical className="h-4 w-4" />
              </Button>
            </div>

            <div className="flex flex-wrap items-center gap-x-3 gap-y-1 text-xs text-muted-foreground">
              <span>{file.size}</span>
              <span className="text-muted-foreground/70">•</span>
              <span className="whitespace-nowrap">{file.modified}</span>
            </div>

            <div className="flex flex-wrap gap-1.5 mt-auto">
              {file.tags.slice(0, 2).map((tag) => (
                <Badge
                  key={tag.id}
                  variant={tag.color ? "secondary" : "outline"}
                  className="text-xs max-w-full truncate"
                  style={tag.color ? { backgroundColor: tag.color, borderColor: tag.color } : undefined}
                >
                  {tag.name}
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
      </ContextMenuTrigger>

      <ContextMenuContent className="w-48">
        <ContextMenuItem onSelect={() => onView?.()} disabled={actionsDisabled}>
          <Eye className="h-4 w-4" />
          View file
        </ContextMenuItem>
        <ContextMenuItem onSelect={() => onDownload?.()} disabled={actionsDisabled || !file.latestVersionId}>
          <Download className="h-4 w-4" />
          Download
        </ContextMenuItem>
        <ContextMenuItem onSelect={() => onShare?.()} disabled={actionsDisabled || !file.latestVersionId}>
          <Share2 className="h-4 w-4" />
          Share
        </ContextMenuItem>
        <ContextMenuItem onSelect={() => onAssignTags?.()} disabled={actionsDisabled}>
          <Tag className="h-4 w-4" />
          Add Tags
        </ContextMenuItem>
        <ContextMenuSeparator />
        <ContextMenuItem onSelect={() => onOpenDetails?.("property")} disabled={actionsDisabled}>
          <FileText className="h-4 w-4" />
          Open Property
        </ContextMenuItem>
        <ContextMenuItem onSelect={() => onOpenDetails?.("flow")} disabled={actionsDisabled}>
          <GitBranch className="h-4 w-4" />
          Open Flow
        </ContextMenuItem>
        <ContextMenuItem onSelect={() => onOpenDetails?.("form")} disabled={actionsDisabled}>
          <Edit3 className="h-4 w-4" />
          Open Form
        </ContextMenuItem>
        <ContextMenuSeparator />
        <ContextMenuItem
          onSelect={() => onDelete?.()}
          disabled={actionsDisabled}
          className="text-destructive focus:text-destructive"
        >
          <Trash2 className="h-4 w-4" />
          Delete
        </ContextMenuItem>
      </ContextMenuContent>
    </ContextMenu>
  )
}
