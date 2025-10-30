"use client"

import { useState, useEffect, useRef, useCallback, useMemo } from "react"
import Uppy from "@uppy/core"
import type { UploadResult as UppyUploadResult, UppyFile } from "@uppy/core"
import { Dashboard } from "@uppy/react"
import XHRUpload from "@uppy/xhr-upload"
import "@uppy/core/dist/style.css"
import "@uppy/dashboard/dist/style.css"
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { Upload, CheckCircle2, AlertTriangle, ChevronDown, ChevronRight } from "lucide-react"
import { fetchFlows, fetchTags, checkLogin } from "@/lib/api"
import type { DocumentBatchResponse } from "@/lib/api"
import type { Flow, SelectedTag, TagNode, UploadMetadata, User } from "@/lib/types"
import { cn } from "@/lib/utils"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Input } from "@/components/ui/input"
import { ScrollArea } from "@/components/ui/scroll-area"

const DEFAULT_TAG_ICON = "📁"

type CoreMeta = Record<string, unknown>
type CoreResponse = Record<string, unknown>

type ManagedUploadResult = UppyUploadResult<CoreMeta, CoreResponse>
type ManagedUppyFile = UppyFile<CoreMeta, CoreResponse>
type ManagedUppy = Uppy<CoreMeta, CoreResponse>

const createDefaultMetadata = (): UploadMetadata => ({
  title: "",
  docType: "General",
  status: "Draft",
  sensitivity: "Internal",
  description: "",
  notes: "",
})

type UploadDialogProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
  onUploadComplete?: () => void
}

type UploadResultSummary = {
  successCount: number
  failureMessages: string[]
}

function isDocumentBatchResponse(value: unknown): value is DocumentBatchResponse {
  if (!value || typeof value !== "object") {
    return false
  }

  const candidate = value as Partial<DocumentBatchResponse>
  return Array.isArray(candidate.documents)
}

function formatFailureMessage(fileName: string, message?: string): string {
  const normalizedName = fileName?.trim() ? fileName : "Unknown file"
  const normalizedMessage = message?.trim() ? message : "Upload failed"
  return `${normalizedName}: ${normalizedMessage}`
}

export function UploadDialog({ open, onOpenChange, onUploadComplete }: UploadDialogProps) {
  const [flows, setFlows] = useState<Flow[]>([])
  const [tags, setTags] = useState<TagNode[]>([])
  const [selectedFlow, setSelectedFlow] = useState<string | null>(null)
  const [selectedTags, setSelectedTags] = useState<SelectedTag[]>([])
  const [metadata, setMetadata] = useState<UploadMetadata>(() => createDefaultMetadata())
  const [isUploading, setIsUploading] = useState(false)
  const [uploadResult, setUploadResult] = useState<UploadResultSummary | null>(null)
  const [selectedFileCount, setSelectedFileCount] = useState(0)
  const [currentUser, setCurrentUser] = useState<User | null>(null)
  const [expandedTags, setExpandedTags] = useState<Record<string, boolean>>({})
  const autoCloseTimeoutRef = useRef<number | null>(null)
  const dashboardRootRef = useRef<HTMLDivElement | null>(null)

  const uppy = useMemo<ManagedUppy>(() => {
    const instance = new Uppy<CoreMeta, CoreResponse>({
      autoProceed: false,
      allowMultipleUploads: true,
      restrictions: {
        maxNumberOfFiles: 20,
      },
    })

    instance.use(XHRUpload, {
      endpoint: "/api/documents/batch",
      method: "POST",
      fieldName: "Files",
      formData: true,
      bundle: true,
      withCredentials: true,
      responseType: "json",
      limit: 3,
    })

    return instance
  }, [])

  const clearAutoCloseTimeout = useCallback(() => {
    if (autoCloseTimeoutRef.current !== null) {
      window.clearTimeout(autoCloseTimeoutRef.current)
      autoCloseTimeoutRef.current = null
    }
  }, [])

  const resetUploaderState = useCallback(() => {
    clearAutoCloseTimeout()
    setUploadResult(null)
    setSelectedFileCount(0)
    uppy.cancelAll()
  }, [clearAutoCloseTimeout, uppy])

  const handleClose = useCallback(() => {
    resetUploaderState()
    setSelectedFlow(null)
    setSelectedTags([])
    setMetadata(createDefaultMetadata())
    setIsUploading(false)
    onOpenChange(false)
  }, [onOpenChange, resetUploaderState])

  const handleDialogChange = (nextOpen: boolean) => {
    if (!nextOpen) {
      handleClose()
    }
  }

  useEffect(() => {
    if (open) {
      setUploadResult(null)
      fetchFlows("default").then(setFlows).catch((error) => {
        console.error("[ui] Failed to load flows:", error)
      })
      fetchTags().then(setTags).catch((error) => {
        console.error("[ui] Failed to load tags:", error)
      })
      checkLogin()
        .then((result) => {
          if (result.user) {
            setCurrentUser(result.user)
          }
        })
        .catch((error) => {
          console.error("[ui] Unable to resolve current user before upload:", error)
        })
    } else {
      clearAutoCloseTimeout()
      uppy.cancelAll()
      setSelectedFileCount(0)
    }
  }, [open, uppy, clearAutoCloseTimeout])

  const openFileDialog = useCallback(() => {
    const dashboardRoot = dashboardRootRef.current

    if (!dashboardRoot) {
      return
    }

    const hiddenInput = dashboardRoot.querySelector<HTMLInputElement>(
      ".uppy-Dashboard-input",
    )

    if (hiddenInput) {
      hiddenInput.click()
      return
    }

    const browseButton = dashboardRoot.querySelector<HTMLButtonElement>(
      ".uppy-Dashboard-browse",
    )

    browseButton?.click()
  }, [])

  useEffect(() => {
    if (!open) {
      return
    }

    const dashboardRoot = dashboardRootRef.current
    if (!dashboardRoot) {
      return
    }

    const resolveAddFilesContainer = (target: EventTarget | null) => {
      if (!(target instanceof HTMLElement)) {
        return null
      }

      return target.closest<HTMLDivElement>(".uppy-Dashboard-AddFiles")
    }

    const shouldIgnoreEvent = (target: EventTarget | null, container: HTMLDivElement) => {
      if (!(target instanceof HTMLElement)) {
        return false
      }

      const interactiveAncestor = target.closest(
        "button, a, input, label, [role='button'], [data-uppy-super-focusable]",
      )

      if (!interactiveAncestor) {
        return false
      }

      if (interactiveAncestor === container) {
        return false
      }

      return container.contains(interactiveAncestor)
    }

    const handleClick = (event: MouseEvent) => {
      const container = resolveAddFilesContainer(event.target)

      if (!container || shouldIgnoreEvent(event.target, container)) {
        return
      }

      event.preventDefault()
      openFileDialog()
    }

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key !== "Enter" && event.key !== " ") {
        return
      }

      const container = resolveAddFilesContainer(event.target)

      if (!container || shouldIgnoreEvent(event.target, container)) {
        return
      }

      event.preventDefault()
      openFileDialog()
    }

    dashboardRoot.addEventListener("click", handleClick)
    dashboardRoot.addEventListener("keydown", handleKeyDown)

    let cleanupAttributes: (() => void) | undefined

    const ensureAccessibilityAttributes = () => {
      const addFilesElement = dashboardRoot.querySelector<HTMLDivElement>(
        ".uppy-Dashboard-AddFiles",
      )

      if (!addFilesElement) {
        return false
      }

      const previousTabIndex = addFilesElement.getAttribute("tabindex")
      const previousRole = addFilesElement.getAttribute("role")

      if (previousTabIndex === null) {
        addFilesElement.setAttribute("tabindex", "0")
      }

      if (previousRole === null) {
        addFilesElement.setAttribute("role", "button")
      }

      cleanupAttributes = () => {
        if (previousTabIndex === null) {
          addFilesElement.removeAttribute("tabindex")
        }

        if (previousRole === null) {
          addFilesElement.removeAttribute("role")
        } else {
          addFilesElement.setAttribute("role", previousRole)
        }
      }

      return true
    }

    const observer = new MutationObserver(() => {
      if (ensureAccessibilityAttributes()) {
        observer.disconnect()
      }
    })

    if (!ensureAccessibilityAttributes()) {
      observer.observe(dashboardRoot, { childList: true, subtree: true })
    }

    return () => {
      dashboardRoot.removeEventListener("click", handleClick)
      dashboardRoot.removeEventListener("keydown", handleKeyDown)
      observer.disconnect()
      cleanupAttributes?.()
    }
  }, [open, openFileDialog])

  useEffect(() => {
    return () => {
      clearAutoCloseTimeout()
      uppy.cancelAll()
      uppy.destroy()
    }
  }, [uppy, clearAutoCloseTimeout])

  const buildUploadSummary = useCallback(
    (result: ManagedUploadResult): UploadResultSummary => {
      const failureMessages: string[] = []
      let successCount = 0

      const successfulFiles = Array.isArray(result.successful) ? result.successful : []
      const failedFiles = Array.isArray(result.failed) ? result.failed : []

      const firstResponse = successfulFiles[0]?.response?.body as
        | DocumentBatchResponse
        | Record<string, unknown>
        | undefined

      if (isDocumentBatchResponse(firstResponse)) {
        successCount = firstResponse.documents.length
        if (Array.isArray(firstResponse.failures)) {
          for (const failure of firstResponse.failures) {
            failureMessages.push(formatFailureMessage(failure.fileName, failure.message))
          }
        }
      } else if (firstResponse) {
        successCount = 1
      } else {
        successCount = successfulFiles.length
      }

      const resolveErrorMessage = (error: unknown): string => {
        if (typeof error === "string") {
          return error
        }

        if (error && typeof error === "object" && "message" in error) {
          const candidate = (error as { message?: unknown }).message
          if (typeof candidate === "string") {
            return candidate
          }
        }

        return "Upload failed"
      }

      for (const failed of failedFiles) {
        const message = resolveErrorMessage(failed.error)
        const fileName = failed.name ?? "Unknown file"
        failureMessages.push(formatFailureMessage(fileName, message))
      }

      return { successCount, failureMessages }
    },
    [],
  )

  useEffect(() => {
    const handleFileAdded = (file: ManagedUppyFile) => {
      setSelectedFileCount(uppy.getFiles().length)
      setUploadResult(null)
      setMetadata((prev) => {
        if (prev.title.trim()) {
          return prev
        }

        const rawName = file.name ?? ""
        const nameWithoutExtension = rawName.replace(/\.[^/.]+$/, "")
        return { ...prev, title: nameWithoutExtension }
      })
    }

    const handleFileRemoved = () => {
      setSelectedFileCount(uppy.getFiles().length)
    }

    const handleUploadStarted = () => {
      clearAutoCloseTimeout()
      setIsUploading(true)
      setUploadResult(null)
    }

    const handleUploadComplete = (result: ManagedUploadResult) => {
      setIsUploading(false)
      const summary = buildUploadSummary(result)
      setUploadResult(summary)

      if (summary.successCount > 0) {
        onUploadComplete?.()
      }

      if (summary.successCount > 0 && summary.failureMessages.length === 0) {
        autoCloseTimeoutRef.current = window.setTimeout(() => {
          handleClose()
        }, 1500)
      }
    }

    const handleUploadError = (file: ManagedUppyFile | undefined, error: Error) => {
      setIsUploading(false)
      const fileName = file?.name ?? "Upload"
      setUploadResult({ successCount: 0, failureMessages: [formatFailureMessage(fileName, error.message)] })
    }

    const handleError = (error: Error) => {
      setIsUploading(false)
      setUploadResult({ successCount: 0, failureMessages: [formatFailureMessage("Upload", error.message)] })
    }

    uppy.on("file-added", handleFileAdded)
    uppy.on("file-removed", handleFileRemoved)
    uppy.on("cancel-all", handleFileRemoved)
    uppy.on("upload", handleUploadStarted)
    uppy.on("complete", handleUploadComplete)
    uppy.on("upload-error", handleUploadError)
    uppy.on("error", handleError)

    return () => {
      uppy.off("file-added", handleFileAdded)
      uppy.off("file-removed", handleFileRemoved)
      uppy.off("cancel-all", handleFileRemoved)
      uppy.off("upload", handleUploadStarted)
      uppy.off("complete", handleUploadComplete)
      uppy.off("upload-error", handleUploadError)
      uppy.off("error", handleError)
    }
  }, [uppy, buildUploadSummary, onUploadComplete, handleClose, clearAutoCloseTimeout])

  useEffect(() => {
    const tagIds = selectedTags.map((tag) => tag.id)

    uppy.setMeta({
      Title: metadata.title?.trim() || "",
      DocType: metadata.docType?.trim() || "General",
      Status: metadata.status?.trim() || "Draft",
      Sensitivity: metadata.sensitivity?.trim() || "Internal",
      Description: metadata.description?.trim() || "",
      Notes: metadata.notes?.trim() || "",
      FlowDefinition: selectedFlow ?? "",
      OwnerId: currentUser?.id ?? "",
      CreatedBy: currentUser?.id ?? "",
      Tags: JSON.stringify(tagIds),
    })
  }, [metadata, selectedFlow, selectedTags, currentUser, uppy])

  useEffect(() => {
    const buildExpandedMap = (nodes: TagNode[], initial: Record<string, boolean>) => {
      for (const node of nodes) {
        if (!(node.id in initial)) {
          initial[node.id] = true
        }
        if (node.children?.length) {
          buildExpandedMap(node.children, initial)
        }
      }
      return initial
    }

    setExpandedTags((previous) => buildExpandedMap(tags, { ...previous }))
  }, [tags])

  const toggleTag = (tag: TagNode) => {
    setSelectedTags((prev) => {
      const isSelected = prev.some((selected) => selected.id === tag.id)
      return isSelected
        ? prev.filter((selected) => selected.id !== tag.id)
        : [...prev, { id: tag.id, name: tag.name, namespaceId: tag.namespaceId }]
    })
  }

  const isSelectableTag = (tag: TagNode) =>
    !tag.kind || tag.kind === "label" || tag.kind === "namespace"

  const toggleTagExpansion = (tagId: string) => {
    setExpandedTags((prev) => ({ ...prev, [tagId]: !prev[tagId] }))
  }

  const renderTagTree = (nodes: TagNode[], level = 0) => {
    return nodes.map((tag) => {
      const hasChildren = Boolean(tag.children && tag.children.length > 0)
      const isExpanded = expandedTags[tag.id] ?? true
      const isSelected = selectedTags.some((selected) => selected.id === tag.id)
      const canSelect = isSelectableTag(tag)
      const displayIcon = tag.iconKey && tag.iconKey.trim() !== "" ? tag.iconKey : DEFAULT_TAG_ICON
      const backgroundStyle = tag.color ? { backgroundColor: tag.color } : undefined

      return (
        <div key={tag.id} className="space-y-2">
          <div
            className={cn(
              "flex items-center gap-1 rounded-md text-sm transition-colors group",
              isSelected
                ? "bg-primary/10 text-primary border border-primary/30"
                : "hover:bg-muted/60 border border-transparent text-muted-foreground",
            )}
            style={{ paddingLeft: `${level * 12 + 8}px` }}
          >
            {hasChildren ? (
              <button
                type="button"
                onClick={(event) => {
                  event.stopPropagation()
                  toggleTagExpansion(tag.id)
                }}
                className="p-0.5 rounded hover:bg-muted/80 text-muted-foreground"
              >
                {isExpanded ? (
                  <ChevronDown className="h-3 w-3" />
                ) : (
                  <ChevronRight className="h-3 w-3" />
                )}
              </button>
            ) : (
              <span className="w-3" />
            )}

            <button
              type="button"
              onClick={() => {
                if (canSelect) {
                  toggleTag(tag)
                }
              }}
              disabled={!canSelect}
              className={cn(
                "flex items-center gap-3 flex-1 min-w-0 rounded-md px-3 py-2 text-left transition",
                !tag.color ? "bg-muted/60" : "",
                canSelect ? "text-foreground" : "text-muted-foreground cursor-default opacity-80",
                isSelected ? "ring-1 ring-primary" : "",
              )}
              style={backgroundStyle}
            >
              <span className="text-sm flex-shrink-0">{displayIcon}</span>
              <span className="truncate">{tag.name}</span>
            </button>

            {isSelected ? (
              <CheckCircle2 className="mr-2 h-4 w-4 text-primary" />
            ) : null}
          </div>

          {hasChildren && isExpanded ? (
            <div className="space-y-2">{renderTagTree(tag.children!, level + 1)}</div>
          ) : null}
        </div>
      )
    })
  }

  const handleStartUpload = () => {
    clearAutoCloseTimeout()
    setUploadResult(null)
    uppy
      .upload()
      .catch((error) => {
        console.error("[ui] Failed to upload documents via Uppy:", error)
        setIsUploading(false)
        setUploadResult({ successCount: 0, failureMessages: [formatFailureMessage("Upload", error.message)] })
      })
  }

  const handleUploadMore = () => {
    resetUploaderState()
  }

  const isUploadDisabled = selectedFileCount === 0 || isUploading || !currentUser

  return (
    <Dialog open={open} onOpenChange={handleDialogChange}>
      <DialogContent className="w-[95vw] max-w-[1280px] sm:w-[90vw] lg:w-[80vw] xl:w-[70vw] h-[90vh] sm:h-[90vh] lg:h-[90vh] flex flex-col gap-6">
        <DialogHeader>
          <DialogTitle>Upload Files</DialogTitle>
        </DialogHeader>

        {uploadResult ? (
          <div className="flex flex-col items-center justify-center flex-1 gap-4 text-center">
            {uploadResult.successCount > 0 ? (
              <CheckCircle2 className="h-16 w-16 text-green-500" />
            ) : (
              <AlertTriangle className="h-16 w-16 text-amber-500" />
            )}
            <div className="space-y-2 max-w-2xl">
              <p className="text-lg font-medium">
                {uploadResult.successCount > 0
                  ? `Uploaded ${uploadResult.successCount} file${uploadResult.successCount > 1 ? "s" : ""} successfully.`
                  : "No files were uploaded."}
              </p>
              {uploadResult.failureMessages.length > 0 && (
                <div className="text-left">
                  <div className="flex items-center gap-2 text-sm text-amber-600">
                    <AlertTriangle className="h-4 w-4" />
                    <span>Some files could not be processed:</span>
                  </div>
                  <ul className="mt-2 space-y-1 text-sm text-muted-foreground max-h-48 overflow-y-auto pr-2">
                    {uploadResult.failureMessages.map((message, index) => (
                      <li key={`${message}-${index}`}>{message}</li>
                    ))}
                  </ul>
                </div>
              )}
              {uploadResult.successCount > 0 && uploadResult.failureMessages.length === 0 && (
                <p className="text-sm text-muted-foreground">This dialog will close automatically.</p>
              )}
            </div>

            <div className="flex flex-wrap items-center justify-center gap-3 mt-4">
              {uploadResult.failureMessages.length > 0 && (
                <Button onClick={handleUploadMore} disabled={isUploading}>
                  Upload more files
                </Button>
              )}
              <Button variant="outline" onClick={handleClose}>
                Close
              </Button>
            </div>
          </div>
        ) : (
          <div className="flex flex-col flex-1 overflow-hidden gap-6">
            <div className="flex flex-col gap-3" ref={dashboardRootRef}>
              <div className="flex justify-end">
                <Button type="button" variant="outline" onClick={openFileDialog}>
                  Browse files
                </Button>
              </div>
              <Dashboard
                uppy={uppy}
                width="100%"
                height={340}
                hideUploadButton
                proudlyDisplayPoweredByUppy={false}
                showProgressDetails
                locale={{ strings: { dropPasteImportBoth: "Drop files here or browse files" } }}
                note={
                  currentUser
                    ? "You can add up to 20 files per upload."
                    : "You must be signed in to upload documents."
                }
              />
              <div className="flex items-center justify-between text-sm text-muted-foreground mt-2">
                <span>{selectedFileCount} file{selectedFileCount === 1 ? "" : "s"} selected</span>
                {isUploading && <span>Uploading...</span>}
              </div>
            </div>

            <Tabs defaultValue="tags" className="flex-1 flex flex-col overflow-hidden gap-4">
              <TabsList className="grid w-full grid-cols-3">
                <TabsTrigger value="tags">Tags</TabsTrigger>
                <TabsTrigger value="flow">Flow</TabsTrigger>
                <TabsTrigger value="metadata">Metadata</TabsTrigger>
              </TabsList>

              <TabsContent value="tags" className="flex-1 overflow-y-auto mt-4">
                <div className="space-y-4">
                  <div className="text-sm text-muted-foreground">
                    Selected tags:{" "}
                    {selectedTags.length > 0 ? (
                      selectedTags.map((tag) => tag.name).join(", ")
                    ) : (
                      <span className="italic text-muted-foreground/80">No tags selected</span>
                    )}
                  </div>
                  <div className="flex items-center justify-between">
                    <Label>Select Tags</Label>
                    <span className="text-xs text-muted-foreground">
                      {selectedTags.length > 0
                        ? `${selectedTags.length} tag${selectedTags.length > 1 ? "s" : ""} selected`
                        : "Choose tags to classify your files"}
                    </span>
                  </div>
                  <div className="border rounded-lg bg-muted/30">
                    <ScrollArea className="h-[360px] sm:h-[420px] md:h-[480px]">
                      <div className="p-3 space-y-2">
                        {tags.length > 0 ? (
                          renderTagTree(tags)
                        ) : (
                          <p className="text-sm text-muted-foreground px-2 py-6 text-center">
                            No tags available.
                          </p>
                        )}
                      </div>
                    </ScrollArea>
                  </div>
                </div>
              </TabsContent>

              <TabsContent value="flow" className="flex-1 overflow-y-auto mt-4">
                <div className="space-y-4">
                  <div className="space-y-2">
                    <Label>Select workflow</Label>
                    <Select
                      value={selectedFlow ?? undefined}
                      onValueChange={(value) => setSelectedFlow(value === "__none__" ? null : value)}
                    >
                      <SelectTrigger>
                        <SelectValue placeholder="Choose a workflow" />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="__none__">No workflow</SelectItem>
                        {flows.map((flow) => (
                          <SelectItem key={flow.id} value={flow.id}>
                            {flow.name}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </div>
                  {selectedFlow && (
                    <p className="text-sm text-muted-foreground">
                      Selected flow will be applied to the files after upload.
                    </p>
                  )}
                </div>
              </TabsContent>

              <TabsContent value="metadata" className="flex-1 overflow-y-auto mt-4">
                <div className="space-y-6">
                  <div className="space-y-2">
                    <Label htmlFor="title">Title</Label>
                    <Input
                      id="title"
                      placeholder="Enter document title"
                      value={metadata.title}
                      onChange={(e) => setMetadata((prev) => ({ ...prev, title: e.target.value }))}
                    />
                  </div>

                  <div className="grid gap-4 md:grid-cols-3">
                    <div className="space-y-2">
                      <Label>Document type</Label>
                      <Select
                        value={metadata.docType}
                        onValueChange={(value) => setMetadata((prev) => ({ ...prev, docType: value }))}
                      >
                        <SelectTrigger>
                          <SelectValue placeholder="Select document type" />
                        </SelectTrigger>
                        <SelectContent>
                          <SelectItem value="General">General</SelectItem>
                          <SelectItem value="Contract">Contract</SelectItem>
                          <SelectItem value="Policy">Policy</SelectItem>
                          <SelectItem value="Report">Report</SelectItem>
                        </SelectContent>
                      </Select>
                    </div>

                    <div className="space-y-2">
                      <Label>Status</Label>
                      <Select
                        value={metadata.status}
                        onValueChange={(value) => setMetadata((prev) => ({ ...prev, status: value }))}
                      >
                        <SelectTrigger>
                          <SelectValue placeholder="Select status" />
                        </SelectTrigger>
                        <SelectContent>
                          <SelectItem value="Draft">Draft</SelectItem>
                          <SelectItem value="InReview">In review</SelectItem>
                          <SelectItem value="Published">Published</SelectItem>
                          <SelectItem value="Archived">Archived</SelectItem>
                        </SelectContent>
                      </Select>
                    </div>

                    <div className="space-y-2">
                      <Label>Sensitivity</Label>
                      <Select
                        value={metadata.sensitivity}
                        onValueChange={(value) => setMetadata((prev) => ({ ...prev, sensitivity: value }))}
                      >
                        <SelectTrigger>
                          <SelectValue placeholder="Select sensitivity" />
                        </SelectTrigger>
                        <SelectContent>
                          <SelectItem value="Public">Public</SelectItem>
                          <SelectItem value="Internal">Internal</SelectItem>
                          <SelectItem value="Confidential">Confidential</SelectItem>
                          <SelectItem value="Restricted">Restricted</SelectItem>
                        </SelectContent>
                      </Select>
                    </div>
                  </div>

                  <div className="space-y-2">
                    <Label htmlFor="description">Description</Label>
                    <Textarea
                      id="description"
                      placeholder="Enter file description..."
                      value={metadata.description}
                      onChange={(e) => setMetadata((prev) => ({ ...prev, description: e.target.value }))}
                      rows={3}
                    />
                  </div>

                  <div className="space-y-2">
                    <Label htmlFor="notes">Notes</Label>
                    <Textarea
                      id="notes"
                      placeholder="Additional notes..."
                      value={metadata.notes}
                      onChange={(e) => setMetadata((prev) => ({ ...prev, notes: e.target.value }))}
                      rows={3}
                    />
                  </div>
                </div>
              </TabsContent>
            </Tabs>

            <div className="flex justify-end gap-2 pt-4 border-t mt-4">
              <Button variant="outline" onClick={handleClose} disabled={isUploading}>
                Cancel
              </Button>
              <Button onClick={handleStartUpload} disabled={isUploadDisabled} className="gap-2">
                <Upload className="h-4 w-4" />
                {isUploading ? "Uploading..." : "Upload Files"}
              </Button>
            </div>
          </div>
        )}
      </DialogContent>
    </Dialog>
  )
}
