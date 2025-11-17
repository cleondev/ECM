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

type FileListItemProps = {
  file: FileItem
  isSelected: boolean
  isMultiSelected?: boolean
  onSelect: (e: React.MouseEvent) => void
  onContextMenuOpen?: () => void
  onView?: () => void
  onDoubleClick?: () => void
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

export function FileListItem({
  file,
  isSelected,
  isMultiSelected,
  onSelect,
  onContextMenuOpen,
  onView,
  onDoubleClick,
  onDownload,
  onShare,
  onAssignTags,
  onOpenDetails,
  onDelete,
  actionsDisabled,
}: FileListItemProps) {
  const itemRef = useRef<HTMLButtonElement>(null)

  return (
    <ContextMenu onOpenChange={(open) => open && onContextMenuOpen?.()}>
      <ContextMenuTrigger asChild>
        <button
          onClick={onSelect}
          onDoubleClick={(event) => {
            event.preventDefault()
            onDoubleClick?.()
          }}
          ref={itemRef}
          className={cn(
            "w-full flex items-center gap-3 p-3 rounded-lg border bg-card text-left transition-all hover:shadow-sm",
            isSelected ? "border-primary ring-2 ring-primary/20" : "border-border hover:border-primary/50",
            isMultiSelected && !isSelected && "border-primary/50 bg-primary/5",
          )}
        >
          <div className="h-10 w-10 rounded bg-muted flex items-center justify-center flex-shrink-0">
            <FileTypeIcon file={file} size="sm" />
          </div>

          <div className="flex-1 min-w-0">
            <div className="flex items-center gap-2 mb-1">
              <h3 className="font-medium text-sm truncate text-card-foreground">{file.name}</h3>
              {file.status ? (
                <Badge className={cn("text-xs", statusColors[file.status])}>{file.status}</Badge>
              ) : null}
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
                <Badge
                  key={tag.id}
                  variant={tag.color ? "secondary" : "outline"}
                  className="text-xs"
                  style={tag.color ? { backgroundColor: tag.color, borderColor: tag.color } : undefined}
                >
                  {tag.name}
                </Badge>
              ))}
            </div>
            <Button
              variant="ghost"
              size="icon"
              className="h-8 w-8"
              onClick={(e) => {
                e.stopPropagation()
                const event = new MouseEvent("contextmenu", {
                  bubbles: true,
                  clientX: e.clientX,
                  clientY: e.clientY,
                })
                itemRef.current?.dispatchEvent(event)
              }}
            >
              <MoreVertical className="h-4 w-4" />
            </Button>
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
