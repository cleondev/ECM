"use client"

import type React from "react"
import type { FileItem } from "@/lib/types"
import { FileCard } from "./file-card"
import { FileListItem } from "./file-list-item"
import { useEffect, useRef } from "react"
import { Loader2 } from "lucide-react"
import { Skeleton } from "@/components/ui/skeleton"

type FileGridProps = {
  files: FileItem[]
  viewMode: "grid" | "list"
  selectedFile: FileItem | null
  onFileSelect: (file: FileItem) => void
  selectedFiles: Set<string>
  onSelectedFilesChange: (files: Set<string>) => void
  hasMore: boolean
  isLoading: boolean
  onLoadMore: () => void
  onViewFile: (file: FileItem) => void
  onDownloadFile: (file: FileItem) => void
  onShareFile: (file: FileItem) => void
  onOpenDetailsTab: (tab: "property" | "flow" | "form", file: FileItem) => void
  onAssignTags: (file: FileItem) => void
  onDeleteFile: (file: FileItem) => void
  onDeleteSelection: (fileIds: Set<string>) => void
}

function FileGridSkeleton({ viewMode }: { viewMode: "grid" | "list" }) {
  if (viewMode === "list") {
    return (
      <div className="space-y-1">
        {Array.from({ length: 8 }).map((_, i) => (
          <div key={i} className="flex items-center gap-3 p-3 rounded-lg border border-border">
            <Skeleton className="h-10 w-10 rounded" />
            <div className="flex-1 space-y-2">
              <Skeleton className="h-4 w-48" />
              <Skeleton className="h-3 w-32" />
            </div>
            <Skeleton className="h-3 w-16" />
          </div>
        ))}
      </div>
    )
  }

  return (
    <div className="grid gap-3 sm:gap-4 lg:gap-5 grid-cols-[repeat(auto-fill,minmax(200px,1fr))]">
      {Array.from({ length: 8 }).map((_, i) => (
        <div key={i} className="rounded-lg border border-border p-3 sm:p-4 space-y-3">
          <Skeleton className="aspect-video w-full rounded" />
          <Skeleton className="h-4 w-3/4" />
          <Skeleton className="h-3 w-1/2" />
        </div>
      ))}
    </div>
  )
}

export function FileGrid({
  files,
  viewMode,
  selectedFile,
  onFileSelect,
  selectedFiles,
  onSelectedFilesChange,
  hasMore,
  isLoading,
  onLoadMore,
  onViewFile,
  onDownloadFile,
  onShareFile,
  onOpenDetailsTab,
  onAssignTags,
  onDeleteFile,
  onDeleteSelection,
}: FileGridProps) {
  const containerRef = useRef<HTMLDivElement>(null)
  const lastSelectedIndexRef = useRef<number>(-1)
  const observerRef = useRef<IntersectionObserver | null>(null)
  const loadMoreRef = useRef<HTMLDivElement>(null)
  const disableSingleFileActions = selectedFiles.size > 1
  const disableTagActions = selectedFiles.size === 0

  useEffect(() => {
    if (!loadMoreRef.current || !hasMore || isLoading) return

    observerRef.current = new IntersectionObserver(
      (entries) => {
        if (entries[0].isIntersecting && hasMore && !isLoading) {
          onLoadMore()
        }
      },
      { threshold: 0.1 },
    )

    observerRef.current.observe(loadMoreRef.current)

    return () => {
      if (observerRef.current) {
        observerRef.current.disconnect()
      }
    }
  }, [hasMore, isLoading, onLoadMore])

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (!containerRef.current) return

      if (e.ctrlKey && e.key === "a") {
        e.preventDefault()
        if (selectedFiles.size === files.length && files.length > 0) {
          // If all selected, unselect all
          onSelectedFilesChange(new Set())
        } else {
          // Otherwise, select all
          const allFileIds = new Set(files.map((f) => f.id))
          onSelectedFilesChange(allFileIds)
        }
        return
      }

      if (e.key === "Delete" && selectedFiles.size > 0) {
        e.preventDefault()
        onDeleteSelection(new Set(selectedFiles))
        return
      }

      // Arrow key navigation
      const currentIndex = selectedFile ? files.findIndex((f) => f.id === selectedFile.id) : -1
      let newIndex = currentIndex

      if (e.key === "ArrowDown") {
        e.preventDefault()
        newIndex = Math.min(currentIndex + 1, files.length - 1)
      } else if (e.key === "ArrowUp") {
        e.preventDefault()
        newIndex = Math.max(currentIndex - 1, 0)
      } else if (e.key === "ArrowRight" && viewMode === "grid") {
        e.preventDefault()
        newIndex = Math.min(currentIndex + 1, files.length - 1)
      } else if (e.key === "ArrowLeft" && viewMode === "grid") {
        e.preventDefault()
        newIndex = Math.max(currentIndex - 1, 0)
      }

      if (newIndex !== currentIndex && newIndex >= 0) {
        onFileSelect(files[newIndex])
        lastSelectedIndexRef.current = newIndex
      }
    }

    const container = containerRef.current
    if (container) {
      container.addEventListener("keydown", handleKeyDown)
      return () => container.removeEventListener("keydown", handleKeyDown)
    }
  }, [
    files,
    selectedFile,
    viewMode,
    onFileSelect,
    onSelectedFilesChange,
    selectedFiles,
    onDeleteSelection,
  ])

  const handleFileClick = (file: FileItem, index: number, e: React.MouseEvent) => {
    if (e.ctrlKey || e.metaKey) {
      const newSelection = new Set(selectedFiles)
      if (newSelection.has(file.id)) {
        newSelection.delete(file.id)
      } else {
        newSelection.add(file.id)
      }
      onSelectedFilesChange(newSelection)
      onFileSelect(file)
      lastSelectedIndexRef.current = index
    } else if (e.shiftKey && lastSelectedIndexRef.current >= 0) {
      const start = Math.min(lastSelectedIndexRef.current, index)
      const end = Math.max(lastSelectedIndexRef.current, index)
      const rangeIds = new Set(files.slice(start, end + 1).map((f) => f.id))
      onSelectedFilesChange(rangeIds)
      onFileSelect(file)
    } else {
      onSelectedFilesChange(new Set([file.id]))
      onFileSelect(file)
      lastSelectedIndexRef.current = index
    }
  }

  const handleFileContextMenu = (file: FileItem, index: number) => {
    if (selectedFiles.size > 1 && selectedFiles.has(file.id)) {
      onFileSelect(file)
      lastSelectedIndexRef.current = index
      return
    }

    const newSelection = new Set([file.id])
    onSelectedFilesChange(newSelection)
    onFileSelect(file)
    lastSelectedIndexRef.current = index
  }

  if (files.length === 0 && isLoading) {
    return (
      <div
        ref={containerRef}
        className={viewMode === "list" ? "flex-1 overflow-y-auto p-4" : "flex-1 overflow-y-auto p-6"}
        tabIndex={0}
      >
        <FileGridSkeleton viewMode={viewMode} />
      </div>
    )
  }

  if (files.length === 0 && !isLoading) {
    return (
      <div className="flex-1 flex items-center justify-center">
        <div className="text-center">
          <p className="text-lg font-medium text-muted-foreground">No files found</p>
          <p className="text-sm text-muted-foreground mt-1">Try adjusting your filters</p>
        </div>
      </div>
    )
  }

  if (viewMode === "list") {
    return (
      <div ref={containerRef} className="flex-1 overflow-y-auto overflow-x-hidden p-4" tabIndex={0}>
        <div className="space-y-1">
          {files.map((file, index) => (
            <FileListItem
              key={file.id}
              file={file}
              isSelected={selectedFile?.id === file.id}
              isMultiSelected={selectedFiles.has(file.id)}
              onSelect={(e) => handleFileClick(file, index, e)}
              onContextMenuOpen={() => handleFileContextMenu(file, index)}
              onView={() => onViewFile(file)}
              onDoubleClick={() => onViewFile(file)}
              onDownload={() => onDownloadFile(file)}
              onShare={() => onShareFile(file)}
              onAssignTags={() => onAssignTags(file)}
              onOpenDetails={(tab) => onOpenDetailsTab(tab, file)}
              onDelete={() => onDeleteFile(file)}
              actionsDisabled={disableSingleFileActions}
              assignTagsDisabled={disableTagActions}
            />
          ))}
        </div>
        {hasMore && (
          <div ref={loadMoreRef} className="flex justify-center py-4">
            {isLoading && <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />}
          </div>
        )}
      </div>
    )
  }

  return (
    <div ref={containerRef} className="flex-1 overflow-y-auto overflow-x-hidden p-4 sm:p-6 lg:p-8" tabIndex={0}>
      <div className="grid gap-3 sm:gap-4 lg:gap-5 grid-cols-[repeat(auto-fill,minmax(200px,1fr))]">
        {files.map((file, index) => (
          <FileCard
            key={file.id}
            file={file}
            isSelected={selectedFile?.id === file.id}
            isMultiSelected={selectedFiles.has(file.id)}
            onSelect={(e) => handleFileClick(file, index, e)}
            onContextMenuOpen={() => handleFileContextMenu(file, index)}
            onView={() => onViewFile(file)}
            onDoubleClick={() => onViewFile(file)}
            onDownload={() => onDownloadFile(file)}
            onShare={() => onShareFile(file)}
            onAssignTags={() => onAssignTags(file)}
            onOpenDetails={(tab) => onOpenDetailsTab(tab, file)}
            onDelete={() => onDeleteFile(file)}
            actionsDisabled={disableSingleFileActions}
            assignTagsDisabled={disableTagActions}
          />
        ))}
      </div>
      {hasMore && (
        <div ref={loadMoreRef} className="flex justify-center py-8">
          {isLoading && <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />}
        </div>
      )}
    </div>
  )
}
