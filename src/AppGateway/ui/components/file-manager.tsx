"use client"

import { useState, useEffect } from "react"
import { LeftSidebar } from "./left-sidebar"
import { FileGrid } from "./file-grid"
import { RightSidebar } from "./right-sidebar"
import { AppHeader } from "./app-header"
import { FileToolbar } from "./file-toolbar"
import { UploadDialog } from "./upload-dialog"
import { ResizableHandle } from "./resizable-handle"
import type { FileItem, FileQueryParams, SelectedTag } from "@/lib/types"
import { fetchFiles } from "@/lib/api"

const PAGE_SIZE = 20

export function FileManager() {
  const [files, setFiles] = useState<FileItem[]>([])
  const [selectedFile, setSelectedFile] = useState<FileItem | null>(null)
  const [selectedFolder, setSelectedFolder] = useState<string>("All Files")
  const [selectedTag, setSelectedTag] = useState<SelectedTag | null>(null)
  const [viewMode, setViewMode] = useState<"grid" | "list">("grid")
  const [searchQuery, setSearchQuery] = useState("")
  const [leftSidebarWidth, setLeftSidebarWidth] = useState(280)
  const [rightSidebarWidth, setRightSidebarWidth] = useState(320)
  const [activeRightTab, setActiveRightTab] = useState("property")
  const [isLeftSidebarCollapsed, setIsLeftSidebarCollapsed] = useState(false)
  const [selectedFiles, setSelectedFiles] = useState<Set<string>>(new Set())
  const [page, setPage] = useState(1)
  const [hasMore, setHasMore] = useState(true)
  const [isLoading, setIsLoading] = useState(false)
  const [uploadDialogOpen, setUploadDialogOpen] = useState(false)
  const [sortBy, setSortBy] = useState<"name" | "modified" | "size">("modified")
  const [sortOrder, setSortOrder] = useState<"asc" | "desc">("desc")

  useEffect(() => {
    loadFiles(true)
  }, [selectedFolder, selectedTag, searchQuery, sortBy, sortOrder])

  const loadFiles = async (reset = false) => {
    if (isLoading) return

    setIsLoading(true)
    const currentPage = reset ? 1 : page

    const params: FileQueryParams = {
      search: searchQuery.trim() || undefined,
      tagId: selectedTag?.id,
      tagLabel: selectedTag?.name,
      folder: selectedFolder !== "All Files" ? selectedFolder : undefined,
      page: currentPage,
      limit: PAGE_SIZE,
      sortBy,
      sortOrder,
    }

    try {
      const response = await fetchFiles(params)

      if (reset) {
        setFiles(response.data)
        setPage(1)
      } else {
        setFiles((prev) => [...prev, ...response.data])
      }

      setHasMore(response.hasMore)
      setPage(currentPage + 1)
    } catch (error) {
      console.error("[v0] Error loading files:", error)
    } finally {
      setIsLoading(false)
    }
  }

  const handleTagClick = (tag: SelectedTag) => {
    setSelectedTag((current) => (current?.id === tag.id ? null : tag))
  }

  const handleUploadComplete = () => {
    loadFiles(true)
  }

  return (
    <div className="flex flex-col h-screen bg-background">
      <AppHeader
        searchQuery={searchQuery}
        onSearchChange={setSearchQuery}
        selectedTag={selectedTag}
        onClearTag={() => {
          setSelectedTag(null)
        }}
        isLeftSidebarCollapsed={isLeftSidebarCollapsed}
        onToggleLeftSidebar={() => setIsLeftSidebarCollapsed(!isLeftSidebarCollapsed)}
      />

      <div className="flex flex-1 min-h-0">
        {!isLeftSidebarCollapsed && (
          <>
            <div style={{ width: leftSidebarWidth }} className="flex-shrink-0">
              <LeftSidebar
                selectedFolder={selectedFolder}
                onFolderSelect={setSelectedFolder}
                selectedTag={selectedTag}
                onTagClick={handleTagClick}
                onCollapse={() => setIsLeftSidebarCollapsed(true)}
              />
            </div>

            <ResizableHandle
              onResize={(delta) => {
                setLeftSidebarWidth((prev) => Math.max(200, Math.min(500, prev + delta)))
              }}
            />
          </>
        )}

        <div className="flex-1 flex flex-col min-w-0">
          <FileToolbar
            viewMode={viewMode}
            onViewModeChange={setViewMode}
            onUploadClick={() => setUploadDialogOpen(true)}
            sortBy={sortBy}
            sortOrder={sortOrder}
            onSortChange={(nextSortBy, nextSortOrder) => {
              setSortBy(nextSortBy)
              setSortOrder(nextSortOrder)
            }}
          />

          <FileGrid
            files={files}
            viewMode={viewMode}
            selectedFile={selectedFile}
            onFileSelect={setSelectedFile}
            selectedFiles={selectedFiles}
            onSelectedFilesChange={setSelectedFiles}
            hasMore={hasMore}
            isLoading={isLoading}
            onLoadMore={() => loadFiles(false)}
          />
        </div>

        <ResizableHandle
          onResize={(delta) => {
            setRightSidebarWidth((prev) => Math.max(280, Math.min(600, prev - delta)))
          }}
        />

        <div style={{ width: rightSidebarWidth }} className="flex-shrink-0">
          <RightSidebar selectedFile={selectedFile} activeTab={activeRightTab} onTabChange={setActiveRightTab} />
        </div>
      </div>

      <UploadDialog
        open={uploadDialogOpen}
        onOpenChange={setUploadDialogOpen}
        onUploadComplete={handleUploadComplete}
      />
    </div>
  )
}
