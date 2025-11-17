"use client"

import { useState, useEffect, useRef } from "react"
import { useRouter } from "next/navigation"
import { LeftSidebar } from "./left-sidebar"
import { FileGrid } from "./file-grid"
import { RightSidebar } from "./right-sidebar"
import { AppHeader } from "./app-header"
import { FileToolbar } from "./file-toolbar"
import { UploadDialog } from "./upload-dialog"
import { ResizableHandle } from "./resizable-handle"
import { ShareDialog } from "./share-dialog"
import { TagAssignmentDialog } from "./tag-assignment-dialog"
import type {
  DocumentTag,
  FileItem,
  FileQueryParams,
  SelectedTag,
  ShareLink,
  ShareOptions,
} from "@/lib/types"
import { buildDocumentDownloadUrl, createShareLink, deleteFiles, fetchFiles } from "@/lib/api"
import { useIsMobile } from "@/components/ui/use-mobile"
import {
  Drawer,
  DrawerClose,
  DrawerContent,
  DrawerHeader,
  DrawerTitle,
} from "@/components/ui/drawer"
import { Button } from "@/components/ui/button"
import { Loader2, X } from "lucide-react"
import { useToast } from "@/hooks/use-toast"
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog"

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
  const [isLeftSidebarCollapsed, setIsLeftSidebarCollapsed] = useState(true)
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
  const [isTagDialogOpen, setIsTagDialogOpen] = useState(false)
  const [tagDialogFile, setTagDialogFile] = useState<FileItem | null>(null)
  const [filesPendingDelete, setFilesPendingDelete] = useState<FileItem[]>([])
  const [isDeletingFiles, setIsDeletingFiles] = useState(false)
  const [sortBy, setSortBy] = useState<"name" | "modified" | "size">("modified")
  const [sortOrder, setSortOrder] = useState<"asc" | "desc">("desc")
  const [isLeftDrawerOpen, setIsLeftDrawerOpen] = useState(false)
  const [isRightDrawerOpen, setIsRightDrawerOpen] = useState(false)

  const router = useRouter()
  const isMobile = useIsMobile()
  const isMobileDevice = isMobile ?? false
  const hasSyncedDesktopSidebar = useRef(false)
  const { toast } = useToast()

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

  const handleFileUpdated = (updatedFile: FileItem) => {
    setFiles((prev) => prev.map((file) => (file.id === updatedFile.id ? { ...file, ...updatedFile } : file)))
    setSelectedFile(updatedFile)
  }

  useEffect(() => {
    setShareResult(null)
    setShareError(null)
  }, [selectedFile?.id])

  const ensureSingleSelection = (file: FileItem) => {
    setSelectedFiles(new Set([file.id]))
    setSelectedFile(file)
  }

  const handleDownloadClick = (file?: FileItem) => {
    const targetFile = file ?? selectedFile

    if (!targetFile?.latestVersionId) {
      console.warn("[ui] Unable to download: selected file does not have a version identifier.")
      return
    }

    if (file) {
      ensureSingleSelection(targetFile)
    }

    const downloadUrl = buildDocumentDownloadUrl(targetFile.latestVersionId)
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
      const link = await createShareLink(selectedFile, options)
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

  const handleShareClick = (file?: FileItem) => {
    const targetFile = file ?? selectedFile

    if (!targetFile) {
      return
    }

    if (file) {
      ensureSingleSelection(targetFile)
    }

    resetShareState()
    setShareDialogOpen(true)
  }

  const handleViewFile = (file?: FileItem) => {
    const targetFile = file ?? selectedFile

    if (!targetFile) {
      return
    }

    ensureSingleSelection(targetFile)
    router.push(`/app/files/${targetFile.id}`)
  }

  const handleAssignTagsClick = (file?: FileItem) => {
    const targetFile = file ?? selectedFile

    if (!targetFile) {
      return
    }

    if (file) {
      ensureSingleSelection(targetFile)
    }

    setTagDialogFile(targetFile)
    setIsTagDialogOpen(true)
  }

  const handleDeleteFileRequest = (file: FileItem) => {
    ensureSingleSelection(file)
    setFilesPendingDelete([file])
  }

  const handleDeleteDialogOpenChange = (open: boolean) => {
    if (!open && !isDeletingFiles) {
      setFilesPendingDelete([])
    }
  }

  const handleDeleteSelectionRequest = (fileIds: Set<string>) => {
    if (fileIds.size === 0) {
      return
    }

    const targetIds = new Set(fileIds)
    const pending = files.filter((file) => targetIds.has(file.id))

    if (pending.length === 0) {
      return
    }

    if (pending.length === 1) {
      ensureSingleSelection(pending[0])
    }

    setFilesPendingDelete(pending)
  }

  const handleDeleteFilesConfirm = async () => {
    if (filesPendingDelete.length === 0) {
      return
    }

    const pendingFiles = filesPendingDelete
    const deletedFileIds = pendingFiles.map((file) => file.id)
    const fileLookup = new Map(pendingFiles.map((file) => [file.id, file]))

    setIsDeletingFiles(true)

    try {
      const { deletedIds, failedIds } = await deleteFiles(deletedFileIds)

      if (deletedIds.length > 0) {
        const deletedIdSet = new Set(deletedIds)
        const remainingFiles = files.filter((file) => !deletedIdSet.has(file.id))
        const previousSelection = Array.from(selectedFiles)
        const remainingSelectedIds = previousSelection.filter((id) => !deletedIdSet.has(id))
        const fallbackFile =
          remainingSelectedIds
            .map((id) => remainingFiles.find((file) => file.id === id) ?? null)
            .find((file): file is FileItem => Boolean(file)) ?? null

        setFiles(remainingFiles)
        setSelectedFiles(new Set(remainingSelectedIds))
        setSelectedFile((previous) => {
          if (previous && !deletedIdSet.has(previous.id)) {
            return previous
          }
          return fallbackFile
        })

        if (shareDialogOpen && selectedFile && deletedIdSet.has(selectedFile.id)) {
          setShareDialogOpen(false)
          resetShareState()
        }

        setTagDialogFile((previous) => {
          if (!previous || !deletedIdSet.has(previous.id)) {
            return previous
          }
          setIsTagDialogOpen(false)
          return null
        })

        const deletedFiles = deletedIds
          .map((id) => fileLookup.get(id))
          .filter((file): file is FileItem => Boolean(file))

        if (deletedFiles.length > 0) {
          const title = deletedFiles.length === 1 ? "Đã xóa tệp" : "Đã xóa các tệp"
          const description =
            deletedFiles.length === 1
              ? `"${deletedFiles[0]!.name}" đã được xóa khỏi hệ thống.`
              : `${deletedFiles.length} tệp đã được xóa khỏi hệ thống.`

          toast({
            title,
            description,
          })
        }
      }

      if (failedIds.length > 0) {
        const failedFiles = failedIds
          .map((id) => fileLookup.get(id))
          .filter((file): file is FileItem => Boolean(file))

        setFilesPendingDelete(failedFiles)

        toast({
          title: "Không thể xóa một số tệp",
          description: "Vui lòng thử lại sau.",
          variant: "destructive",
        })

        return
      }

      setFilesPendingDelete([])
    } catch (error) {
      console.error(`[ui] Failed to delete files '${deletedFileIds.join(",")}'`, error)
      toast({
        title: "Không thể xóa tệp",
        description: "Vui lòng thử lại sau.",
        variant: "destructive",
      })
    } finally {
      setIsDeletingFiles(false)
    }
  }

  const handleTagsAssigned = (fileId: string, updatedTags: DocumentTag[]) => {
    setFiles((previous) =>
      previous.map((file) => (file.id === fileId ? { ...file, tags: updatedTags } : file)),
    )

    setSelectedFile((previous) => {
      if (!previous || previous.id !== fileId) {
        return previous
      }
      return { ...previous, tags: updatedTags }
    })

    setTagDialogFile((previous) => {
      if (!previous || previous.id !== fileId) {
        return previous
      }
      return { ...previous, tags: updatedTags }
    })
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
    if (isMobile === undefined) {
      return
    }

    if (isMobile) {
      setIsLeftSidebarCollapsed(true)
      setIsLeftDrawerOpen(false)
      hasSyncedDesktopSidebar.current = false
      return
    }

    if (!hasSyncedDesktopSidebar.current) {
      setIsLeftSidebarCollapsed(false)
      hasSyncedDesktopSidebar.current = true
    }
  }, [isMobile])

  useEffect(() => {
    if (disableDetailsPanels && isRightSidebarOpen) {
      setIsRightSidebarOpen(false)
    }
  }, [disableDetailsPanels, isRightSidebarOpen])

  useEffect(() => {
    if (!isMobileDevice) {
      setIsLeftDrawerOpen(false)
      setIsRightDrawerOpen(false)
      return
    }

    setIsRightDrawerOpen(isRightSidebarOpen && !disableDetailsPanels)
  }, [disableDetailsPanels, isMobileDevice, isRightSidebarOpen])

  const handleToggleLeftSidebar = () => {
    if (isMobileDevice) {
      setIsLeftDrawerOpen(true)
      return
    }

    setIsLeftSidebarCollapsed((prev) => !prev)
  }

  const handleToggleRightSidebar = () => {
    if (disableDetailsPanels) {
      return
    }

    if (isMobileDevice) {
      setIsRightSidebarOpen((prev) => {
        const next = !prev
        setIsRightDrawerOpen(next && !disableDetailsPanels)
        return next
      })
      return
    }

    setIsRightSidebarOpen((prev) => !prev)
  }

  const handleRightTabChange = (tab: "property" | "flow" | "form") => {
    setActiveRightTab(tab)
  }

  const handleOpenDetailsPanel = (tab: "property" | "flow" | "form", file?: FileItem) => {
    const targetFile = file ?? selectedFile

    if (!targetFile) {
      return
    }

    ensureSingleSelection(targetFile)
    setActiveRightTab(tab)

    if (isMobileDevice) {
      setIsRightSidebarOpen(true)
      setIsRightDrawerOpen(true)
    } else {
      setIsRightSidebarOpen(true)
    }
  }

  const detailsPanelOpen = isMobileDevice ? isRightDrawerOpen : isRightSidebarOpen
  const hasSelectedFiles = selectedFiles.size > 0

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
        onToggleLeftSidebar={handleToggleLeftSidebar}
        isMobile={isMobileDevice}
      />

      <div className="flex flex-1 min-h-0 overflow-hidden">
        {!isMobileDevice && !isLeftSidebarCollapsed && (
          <>
            <div style={{ width: leftSidebarWidth }} className="flex-shrink-0">
              <LeftSidebar
                selectedFolder={selectedFolder}
                onFolderSelect={setSelectedFolder}
                selectedTag={selectedTag}
                onTagClick={handleTagClick}
              />
            </div>

            <ResizableHandle
              onResize={(delta) => {
                setLeftSidebarWidth((prev) => Math.max(200, Math.min(500, prev + delta)))
              }}
            />
          </>
        )}

        <div className="flex-1 flex flex-col min-w-0 overflow-hidden p-3 sm:p-6">
          <div className="flex-1 min-h-0 flex flex-col overflow-hidden rounded-2xl border border-border bg-card shadow-sm">
            <FileToolbar
              viewMode={viewMode}
              onViewModeChange={setViewMode}
              onUploadClick={() => setUploadDialogOpen(true)}
              onDownloadClick={handleDownloadClick}
              onShareClick={handleShareClick}
              onDeleteClick={() => handleDeleteSelectionRequest(selectedFiles)}
              sortBy={sortBy}
              sortOrder={sortOrder}
              onSortChange={(nextSortBy, nextSortOrder) => {
                setSortBy(nextSortBy)
                setSortOrder(nextSortOrder)
              }}
              disableFileActions={!selectedFile?.latestVersionId || !isSingleSelection}
              disableDeleteAction={!hasSelectedFiles || isDeletingFiles}
              isRightSidebarOpen={detailsPanelOpen}
              onToggleRightSidebar={handleToggleRightSidebar}
              activeRightTab={activeRightTab}
              onRightTabChange={handleRightTabChange}
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
              onViewFile={handleViewFile}
              onDownloadFile={handleDownloadClick}
              onShareFile={handleShareClick}
              onAssignTags={handleAssignTagsClick}
              onDeleteFile={handleDeleteFileRequest}
              onDeleteSelection={handleDeleteSelectionRequest}
              onOpenDetailsTab={handleOpenDetailsPanel}
            />
          </div>
        </div>

        {!isMobileDevice && isRightSidebarOpen && (
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
                onFileUpdate={handleFileUpdated}
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

      <TagAssignmentDialog
        open={isTagDialogOpen}
        onOpenChange={(open) => {
          setIsTagDialogOpen(open)
          if (!open) {
            setTagDialogFile(null)
          }
        }}
        file={tagDialogFile}
        onTagsAssigned={handleTagsAssigned}
      />

      <AlertDialog
        open={filesPendingDelete.length > 0}
        onOpenChange={handleDeleteDialogOpenChange}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>
              {filesPendingDelete.length > 1 ? "Xóa các tệp" : "Xóa tệp"}
            </AlertDialogTitle>
            <AlertDialogDescription>
              {filesPendingDelete.length > 1 ? (
                <>
                  Bạn có chắc chắn muốn xóa {filesPendingDelete.length} tệp đã chọn? Hành động này không thể hoàn tác.
                  <ul className="mt-2 space-y-1 text-foreground">
                    {filesPendingDelete.slice(0, 3).map((file) => (
                      <li key={file.id} className="truncate">
                        • {file.name}
                      </li>
                    ))}
                    {filesPendingDelete.length > 3 && (
                      <li className="text-muted-foreground">
                        +{filesPendingDelete.length - 3} tệp khác
                      </li>
                    )}
                  </ul>
                </>
              ) : (
                <>
                  Bạn có chắc chắn muốn xóa {" "}
                  <span className="font-medium text-foreground">
                    {filesPendingDelete[0]?.name}
                  </span>
                  ? Hành động này không thể hoàn tác.
                </>
              )}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel disabled={isDeletingFiles}>Hủy</AlertDialogCancel>
            <AlertDialogAction
              onClick={handleDeleteFilesConfirm}
              disabled={isDeletingFiles}
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
            >
              {isDeletingFiles && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              Xóa
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>

      {isMobileDevice && (
        <>
          <Drawer open={isLeftDrawerOpen} onOpenChange={setIsLeftDrawerOpen} direction="left">
            <DrawerContent className="h-full max-h-full w-full sm:max-w-sm">
              <DrawerHeader className="flex flex-row items-center justify-between gap-2 pb-2">
                <DrawerTitle>Danh mục &amp; thẻ</DrawerTitle>
                <DrawerClose asChild>
                  <Button variant="ghost" size="icon" className="h-8 w-8">
                    <X className="h-4 w-4" />
                  </Button>
                </DrawerClose>
              </DrawerHeader>
              <div className="flex-1 overflow-y-auto px-2 pb-4">
                <LeftSidebar
                  selectedFolder={selectedFolder}
                  onFolderSelect={(folder) => {
                    setSelectedFolder(folder)
                    setIsLeftDrawerOpen(false)
                  }}
                  selectedTag={selectedTag}
                  onTagClick={(tag) => {
                    handleTagClick(tag)
                    setIsLeftDrawerOpen(false)
                  }}
                />
              </div>
            </DrawerContent>
          </Drawer>

          <Drawer
            open={isRightDrawerOpen}
            onOpenChange={(open) => {
              setIsRightDrawerOpen(open)
              if (!open) {
                setIsRightSidebarOpen(false)
              } else if (!disableDetailsPanels) {
                setIsRightSidebarOpen(true)
              }
            }}
            direction="right"
          >
            <DrawerContent className="h-full max-h-full w-full sm:max-w-md">
              <RightSidebar
                selectedFile={selectedFile}
                activeTab={activeRightTab}
                onTabChange={(tab) => {
                  handleRightTabChange(tab)
                  setIsRightSidebarOpen(true)
                }}
                onClose={() => {
                  setIsRightDrawerOpen(false)
                  setIsRightSidebarOpen(false)
                }}
                onFileUpdate={handleFileUpdated}
              />
            </DrawerContent>
          </Drawer>
        </>
      )}
    </div>
  )
}
