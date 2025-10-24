"use client"

import { useState, useEffect } from "react"
import { LeftSidebar } from "./left-sidebar"
import { FileGrid } from "./file-grid"
import { RightSidebar } from "./right-sidebar"
import { AppHeader } from "./app-header"
import { FileToolbar } from "./file-toolbar"
import { UploadDialog } from "./upload-dialog"
import { ResizableHandle } from "./resizable-handle"
import { ShareDialog } from "./share-dialog"
import type { FileItem, FileQueryParams, SelectedTag, ShareLink, ShareOptions } from "@/lib/types"
import { buildDocumentDownloadUrl, createShareLink, fetchFiles } from "@/lib/api"

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
  const [activeRightTab, setActiveRightTab] = useState<"property" | "flow" | "form">("property")
  const [isLeftSidebarCollapsed, setIsLeftSidebarCollapsed] = useState(false)
  const [isRightSidebarOpen, setIsRightSidebarOpen] = useState(false)
  const [selectedFiles, setSelectedFiles] = useState<Set<string>>(new Set())
  const [page, setPage] = useState(1)
  const [hasMore, setHasMore] = useState(true)
  const [isLoading, setIsLoading] = useState(false)
  const [uploadDialogOpen, setUploadDialogOpen] = useState(false)
  const [shareDialogOpen, setShareDialogOpen] = useState(false)
  const [shareResult, setShareResult] = useState<ShareLink | null>(null)
  const [shareError, setShareError] = useState<string | null>(null)
  const [isGeneratingShare, setIsGeneratingShare] = useState(false)
  const [sortBy, setSortBy] = useState<"name" | "modified" | "size">("modified")
  const [sortOrder, setSortOrder] = useState<"asc" | "desc">("desc")

  const isSingleSelection =
    selectedFile !== null && selectedFiles.size === 1 && selectedFiles.has(selectedFile.id)
  const disableDetailsPanels = !isSingleSelection

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

  useEffect(() => {
    setShareResult(null)
    setShareError(null)
  }, [selectedFile?.id])

  const handleDownloadClick = () => {
    if (!selectedFile?.latestVersionId) {
      console.warn("[ui] Unable to download: selected file does not have a version identifier.")
      return
    }

    const downloadUrl = buildDocumentDownloadUrl(selectedFile.latestVersionId)
    window.open(downloadUrl, "_blank", "noopener,noreferrer")
  }

  const handleShareConfirm = async (options: ShareOptions) => {
    if (!selectedFile?.latestVersionId) {
      setShareError("Please select a file that has at least one version before sharing.")
      return
    }

    setIsGeneratingShare(true)
    setShareError(null)

    try {
      const link = await createShareLink(selectedFile.latestVersionId, options)
      setShareResult(link)
    } catch (error) {
      console.error("[ui] Failed to create share link:", error)
      setShareError("Could not generate a share link right now. Please try again in a moment.")
    } finally {
      setIsGeneratingShare(false)
    }
  }

  const resetShareState = () => {
    setShareResult(null)
    setShareError(null)
    setIsGeneratingShare(false)
  }

  useEffect(() => {
    if (selectedFiles.size === 0) {
      if (selectedFile !== null) {
        setSelectedFile(null)
      }
      return
    }

    if (!selectedFile || !selectedFiles.has(selectedFile.id)) {
      const firstSelectedId = selectedFiles.values().next().value as string | undefined
      if (!firstSelectedId) {
        if (selectedFile !== null) {
          setSelectedFile(null)
        }
        return
      }

      const fallbackFile = files.find((file) => file.id === firstSelectedId) || null
      if ((fallbackFile?.id || null) !== (selectedFile?.id || null)) {
        setSelectedFile(fallbackFile)
      }
    }
  }, [files, selectedFile, selectedFiles])

  useEffect(() => {
    if (disableDetailsPanels && isRightSidebarOpen) {
      setIsRightSidebarOpen(false)
    }
  }, [disableDetailsPanels, isRightSidebarOpen])

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
            onDownloadClick={handleDownloadClick}
            onShareClick={() => {
              resetShareState()
              setShareDialogOpen(true)
            }}
            sortBy={sortBy}
            sortOrder={sortOrder}
            onSortChange={(nextSortBy, nextSortOrder) => {
              setSortBy(nextSortBy)
              setSortOrder(nextSortOrder)
            }}
            disableFileActions={!selectedFile?.latestVersionId || !isSingleSelection}
            isRightSidebarOpen={isRightSidebarOpen}
            onToggleRightSidebar={() => setIsRightSidebarOpen((prev) => !prev)}
            activeRightTab={activeRightTab}
            onRightTabChange={setActiveRightTab}
            disableRightSidebarTabs={disableDetailsPanels}
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

        {isRightSidebarOpen && (
          <>
            <ResizableHandle
              onResize={(delta) => {
                setRightSidebarWidth((prev) => Math.max(280, Math.min(600, prev - delta)))
              }}
            />

            <div style={{ width: rightSidebarWidth }} className="flex-shrink-0">
              <RightSidebar
                selectedFile={selectedFile}
                activeTab={activeRightTab}
                onTabChange={setActiveRightTab}
                onClose={() => setIsRightSidebarOpen(false)}
              />
            </div>
          </>
        )}
      </div>

      <UploadDialog
        open={uploadDialogOpen}
        onOpenChange={setUploadDialogOpen}
        onUploadComplete={handleUploadComplete}
      />

      <ShareDialog
        open={shareDialogOpen}
        onOpenChange={setShareDialogOpen}
        file={selectedFile}
        onConfirm={handleShareConfirm}
        isLoading={isGeneratingShare}
        result={shareResult}
        error={shareError}
        onReset={resetShareState}
      />
    </div>
  )
}
