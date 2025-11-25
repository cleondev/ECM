"use client"

import { Label } from "@/components/ui/label"
import { cn } from "@/lib/utils"
import type { FileItem } from "@/lib/types"
import {
  AtSign,
  Calendar,
  ChevronDown,
  ChevronRight,
  Clock,
  FileText,
  FolderOpen,
  GitBranch,
  MessageCircle,
  Paperclip,
  Send,
  Smile,
  Tag,
  User,
  X,
} from "lucide-react"
import type React from "react"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Separator } from "@/components/ui/separator"
import { Tabs, TabsContent } from "@/components/ui/tabs"
import { useState, useEffect, useCallback, useRef } from "react"
import { fetchFlows, fetchSystemTags, fetchTags, updateFile, type UpdateFileRequest } from "@/lib/api"
import type { Flow, SystemTag, TagNode } from "@/lib/types"
import { FileTypeIcon } from "./file-type-icon"

type ActiveTab = "info" | "flow" | "form" | "chat"

type RightSidebarProps = {
  selectedFile: FileItem | null
  activeTab: ActiveTab
  onTabChange: (tab: ActiveTab) => void
  onClose: () => void
  onFileUpdate?: (file: FileItem) => void
}

type ChatMessage = {
  id: string
  author: string
  content: string
  timestamp: string
  mentions?: string[]
  attachments?: string[]
}

const statusColors: Record<NonNullable<FileItem['status']>, string> = {
  "in-progress": "bg-yellow-500/10 text-yellow-700 dark:text-yellow-400 border-yellow-500/20",
  completed: "bg-green-500/10 text-green-700 dark:text-green-400 border-green-500/20",
  draft: "bg-gray-500/10 text-gray-700 dark:text-gray-400 border-gray-500/20",
}

type EditableFileState = {
  name: string
  description: string
  owner: string
  folder: string
  tags: string
  status: NonNullable<FileItem['status']>
}

const DEFAULT_FILE_STATUS: NonNullable<FileItem['status']> = "draft"

function createEditableState(file: FileItem | null): EditableFileState {
  return {
    name: file?.name ?? "",
    description: file?.description ?? "",
    owner: file?.owner ?? "",
    folder: file?.folder ?? "",
    tags: file ? file.tags.map((tag) => tag.name).join(", ") : "",
    status: file?.status ?? DEFAULT_FILE_STATUS,
  }
}

export function RightSidebar({ selectedFile, activeTab, onTabChange, onClose, onFileUpdate }: RightSidebarProps) {
  const [editValues, setEditValues] = useState<EditableFileState>(() => createEditableState(selectedFile))

  const [flows, setFlows] = useState<Flow[]>([])
  const [collapsedFlows, setCollapsedFlows] = useState<Set<string>>(new Set())
  const [systemTags, setSystemTags] = useState<SystemTag[]>([])
  const [tagTree, setTagTree] = useState<TagNode[]>([])
  const [chatMessages, setChatMessages] = useState<ChatMessage[]>([])
  const [chatInput, setChatInput] = useState("")
  const [chatAttachments, setChatAttachments] = useState<File[]>([])
  const fileInputRef = useRef<HTMLInputElement>(null)

  const [collapsedSections, setCollapsedSections] = useState<Set<string>>(() => {
    if (typeof window !== "undefined") {
      const saved = localStorage.getItem("collapsedSections")
      return saved ? new Set(JSON.parse(saved)) : new Set()
    }
    return new Set()
  })

  useEffect(() => {
    setEditValues(createEditableState(selectedFile))
  }, [selectedFile])

  useEffect(() => {
    const ownerHandle = selectedFile?.owner ? `@${selectedFile.owner}` : "@owner"
    const starterMessages: ChatMessage[] = [
      {
        id: "intro",
        author: "System",
        content: `Discuss updates to ${selectedFile?.name ?? "this file"} here.`,
        timestamp: "Just now",
      },
      {
        id: "owner",
        author: selectedFile?.owner ?? "Owner",
        content: `Let's keep ${ownerHandle} in the loop as we make changes.`,
        timestamp: "3h ago",
        mentions: [ownerHandle],
      },
      {
        id: "review",
        author: "Reviewer",
        content: "Please attach the latest changes when ready.",
        timestamp: "1d ago",
        attachments: [selectedFile?.name ? `${selectedFile.name}-v1.pdf` : "design-spec.pdf"],
      },
    ]

    setChatMessages(starterMessages)
    setChatInput("")
    setChatAttachments([])
    if (fileInputRef.current) {
      fileInputRef.current.value = ""
    }
  }, [selectedFile])

  useEffect(() => {
    fetchTags().then(setTagTree)
  }, [])

  useEffect(() => {
    if (!selectedFile?.id) {
      setFlows([])
      setSystemTags([])
      setCollapsedFlows(new Set())
      return
    }

    fetchFlows(selectedFile.id).then((data) => {
      const sortedFlows = data.sort((a, b) => {
        const timeA = parseTimeAgo(a.lastUpdated)
        const timeB = parseTimeAgo(b.lastUpdated)
        return timeA - timeB
      })
      setFlows(sortedFlows)
      setCollapsedFlows(new Set(sortedFlows.map((f) => f.id)))
    })
    fetchSystemTags(selectedFile.id).then(setSystemTags)
  }, [selectedFile?.id])

  const parseTimeAgo = (timeStr: string): number => {
    const match = timeStr.match(/(\d+)\s+(hour|day|week|month)/)
    if (!match) return 0
    const value = Number.parseInt(match[1])
    const unit = match[2]
    const multipliers: Record<string, number> = { hour: 1, day: 24, week: 168, month: 720 }
    return value * (multipliers[unit] || 1)
  }

  const toggleFlowCollapse = (flowId: string) => {
    setCollapsedFlows((prev) => {
      const newSet = new Set(prev)
      if (newSet.has(flowId)) {
        newSet.delete(flowId)
      } else {
        newSet.add(flowId)
      }
      return newSet
    })
  }

  const toggleSection = (sectionId: string) => {
    setCollapsedSections((prev) => {
      const newSet = new Set(prev)
      if (newSet.has(sectionId)) {
        newSet.delete(sectionId)
      } else {
        newSet.add(sectionId)
      }
      localStorage.setItem("collapsedSections", JSON.stringify(Array.from(newSet)))
      return newSet
    })
  }

  const getIconComponent = (iconName: string) => {
    const icons: Record<string, any> = {
      Clock,
      User,
      FileText,
      GitBranch,
      FolderOpen,
    }
    return icons[iconName] || FileText
  }

  const getStatusBadge = (status: Flow["status"]) => {
    switch (status) {
      case "active":
        return "bg-blue-500/10 text-blue-700 dark:text-blue-400 border-blue-500/20"
      case "pending":
        return "bg-orange-500/10 text-orange-700 dark:text-orange-400 border-orange-500/20"
      case "completed":
        return "bg-green-500/10 text-green-700 dark:text-green-400 border-green-500/20"
    }
  }

  const getTagColor = (tagName: string): string | null => {
    const findTag = (tags: TagNode[]): string | null => {
      for (const tag of tags) {
        if ((!tag.kind || tag.kind === "label") && tag.name === tagName && tag.color) {
          return tag.color
        }
        if (tag.children) {
          const found = findTag(tag.children)
          if (found) return found
        }
      }
      return null
    }
    return findTag(tagTree)
  }

  const renderMessageContent = (message: ChatMessage) => {
    const parts = message.content.split(/(@[\w.-]+)/g)

    return (
      <p className="text-sm text-sidebar-foreground/90 leading-relaxed">
        {parts.map((part, index) => {
          if (part.startsWith("@")) {
            return (
              <span key={`${message.id}-mention-${index}`} className="font-semibold text-primary">
                {part}
              </span>
            )
          }

          return <span key={`${message.id}-text-${index}`}>{part}</span>
        })}
      </p>
    )
  }

  const handleInsertMention = () => {
    const mention = selectedFile?.owner ? `@${selectedFile.owner}` : "@mention"
    setChatInput((previous) => `${previous}${previous && !previous.endsWith(" ") ? " " : ""}${mention} `)
  }

  const handleAddEmoji = () => {
    setChatInput((previous) => `${previous}${previous && !previous.endsWith(" ") ? " " : ""}😊 `)
  }

  const handleAttachmentClick = () => {
    fileInputRef.current?.click()
  }

  const handleAttachmentChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    const files = Array.from(event.target.files ?? [])
    if (!files.length) return

    setChatAttachments((previous) => [...previous, ...files])
  }

  const handleRemoveAttachment = (name: string) => {
    setChatAttachments((previous) => previous.filter((file) => file.name !== name))
  }

  const handleSendMessage = () => {
    const trimmed = chatInput.trim()
    if (!trimmed && chatAttachments.length === 0) return

    const newMessage: ChatMessage = {
      id: `${Date.now()}`,
      author: "You",
      content: trimmed || "Shared attachments",
      timestamp: "Just now",
      mentions: trimmed ? trimmed.match(/@[\w.-]+/g) ?? undefined : undefined,
      attachments: chatAttachments.length ? chatAttachments.map((file) => file.name) : undefined,
    }

    setChatMessages((previous) => [newMessage, ...previous])
    setChatInput("")
    setChatAttachments([])
    if (fileInputRef.current) {
      fileInputRef.current.value = ""
    }
  }

  if (!selectedFile) {
    return (
      <div className="w-full h-full border-l border-sidebar-border bg-sidebar text-sidebar-foreground flex items-center justify-center p-6">
        <div className="text-center">
          <FileTypeIcon size="lg" className="block mx-auto mb-3" />
          <p className="text-sm font-medium text-muted-foreground">No file selected</p>
          <p className="text-xs text-muted-foreground mt-1">Select a file to view details</p>
        </div>
      </div>
    )
  }

  const handleBlur = useCallback(
    async (field: keyof EditableFileState, rawValue: string) => {
      if (!selectedFile) {
        return
      }

      const updateData: UpdateFileRequest = {}
      const trimmedValue = field === "description" ? rawValue : rawValue.trim()

      switch (field) {
        case "name": {
          if (trimmedValue === selectedFile.name) {
            return
          }
          updateData.name = trimmedValue
          break
        }
        case "description": {
          const currentDescription = selectedFile.description ?? ""
          if (rawValue === currentDescription) {
            return
          }
          updateData.description = rawValue
          break
        }
        case "owner": {
          if (trimmedValue === selectedFile.owner) {
            return
          }
          updateData.owner = trimmedValue
          break
        }
        case "folder": {
          if (trimmedValue === selectedFile.folder) {
            return
          }
          updateData.folder = trimmedValue
          break
        }
        case "tags": {
          const nextTagNames = trimmedValue
            .split(",")
            .map((tag) => tag.trim())
            .filter((tag) => tag.length > 0)
          const currentTagNames = selectedFile.tags.map((tag) => tag.name.trim()).filter((tag) => tag.length > 0)

          const normalizedNext = Array.from(new Set(nextTagNames)).sort()
          const normalizedCurrent = Array.from(new Set(currentTagNames)).sort()

          const hasChanged =
            normalizedNext.length !== normalizedCurrent.length ||
            normalizedNext.some((tag, index) => tag !== normalizedCurrent[index])

          if (!hasChanged) {
            return
          }

          updateData.tagNames = normalizedNext
          break
        }
        case "status": {
          const normalizedStatus = (trimmedValue || DEFAULT_FILE_STATUS) as NonNullable<FileItem["status"]>
          const currentStatus = selectedFile.status ?? DEFAULT_FILE_STATUS
          if (normalizedStatus === currentStatus) {
            return
          }
          updateData.status = normalizedStatus
          break
        }
        default:
          return
      }

      if (Object.keys(updateData).length === 0) {
        return
      }

      try {
        console.log("[v0] Auto-save field:", field, rawValue)
        const updatedFile = await updateFile(selectedFile.id, updateData)
        setEditValues(createEditableState(updatedFile))
        onFileUpdate?.(updatedFile)
      } catch (error) {
        console.error(`[v0] Failed to auto-save field ${field}:`, error)
      }
    },
    [selectedFile, onFileUpdate],
  )

  const tabLabels: Record<ActiveTab, string> = {
    info: "Info",
    flow: "Flow",
    form: "Form",
    chat: "Chat",
  }

  const tabBadgeStyles: Record<ActiveTab, string> = {
    info: "bg-sky-500/10 text-sky-700 dark:text-sky-300 border-sky-500/20",
    flow: "bg-emerald-500/10 text-emerald-700 dark:text-emerald-300 border-emerald-500/20",
    form: "bg-violet-500/10 text-violet-700 dark:text-violet-300 border-violet-500/20",
    chat: "bg-amber-500/10 text-amber-700 dark:text-amber-300 border-amber-500/20",
  }

  return (
    <div className="w-full h-full border-l border-sidebar-border bg-sidebar text-sidebar-foreground flex flex-col relative z-20">
      <div className="p-4 border-b border-sidebar-border/80 flex items-center justify-between flex-shrink-0">
        <div>
          <h2 className="font-semibold text-sidebar-foreground">File Details</h2>
          <Badge variant="outline" className={cn("text-[10px] mt-2", tabBadgeStyles[activeTab])}>
            {tabLabels[activeTab]}
          </Badge>
        </div>
        <Button variant="ghost" size="icon" className="h-8 w-8" onClick={onClose}>
          <X className="h-4 w-4" />
        </Button>
      </div>

      <Tabs
        value={activeTab}
        onValueChange={(value) => {
          if (!value) return
          onTabChange((value as ActiveTab) || "info")
        }}
        className="flex-1 flex flex-col min-h-0"
      >
        <TabsContent value="info" className="flex-1 overflow-y-auto mt-0">
          <div className="p-4">
            <div className="aspect-video w-full rounded-lg bg-muted flex items-center justify-center mb-4">
              <FileTypeIcon file={selectedFile} size="lg" />
            </div>

            <h3 className="font-semibold text-lg mb-1 text-sidebar-foreground text-pretty">{selectedFile.name}</h3>
            {selectedFile.description && (
              <p className="text-sm text-muted-foreground mb-4 text-pretty">{selectedFile.description}</p>
            )}

            {selectedFile.status && (
              <Badge className={cn("mb-4", statusColors[selectedFile.status])}>{selectedFile.status}</Badge>
            )}

            <Separator className="my-4" />

            <div className="space-y-4">
              <div>
                <button
                  onClick={() => toggleSection("information")}
                  className="w-full flex items-center justify-between mb-2 hover:bg-muted/50 rounded p-1 transition-colors"
                >
                  <h4 className="text-xs font-semibold text-muted-foreground uppercase tracking-wider">Information</h4>
                  {collapsedSections.has("information") ? (
                    <ChevronRight className="h-4 w-4 text-muted-foreground" />
                  ) : (
                    <ChevronDown className="h-4 w-4 text-muted-foreground" />
                  )}
                </button>
                {!collapsedSections.has("information") && (
                  <div className="space-y-3">
                    <div className="flex items-start gap-3">
                      <FileTypeIcon file={selectedFile} size="sm" className="mt-0.5" />
                      <div className="flex-1 min-w-0">
                        <p className="text-xs text-muted-foreground">Type</p>
                        <p className="text-sm font-medium text-sidebar-foreground/90 capitalize">{selectedFile.type}</p>
                      </div>
                    </div>

                    <div className="flex items-start gap-3">
                      <FileText className="h-4 w-4 text-muted-foreground mt-0.5" />
                      <div className="flex-1 min-w-0">
                        <p className="text-xs text-muted-foreground">Size</p>
                        <p className="text-sm font-medium text-sidebar-foreground/90">{selectedFile.size}</p>
                      </div>
                    </div>

                    <div className="flex items-start gap-3">
                      <Calendar className="h-4 w-4 text-muted-foreground mt-0.5" />
                      <div className="flex-1 min-w-0">
                        <p className="text-xs text-muted-foreground">Modified</p>
                        <p className="text-sm font-medium text-sidebar-foreground/90">{selectedFile.modified}</p>
                      </div>
                    </div>

                    <div className="flex items-start gap-3">
                      <User className="h-4 w-4 text-muted-foreground mt-0.5" />
                      <div className="flex-1 min-w-0">
                        <p className="text-xs text-muted-foreground">Owner</p>
                        <p className="text-sm font-medium text-sidebar-foreground/90">{selectedFile.owner}</p>
                      </div>
                    </div>

                    <div className="flex items-start gap-3">
                      <FolderOpen className="h-4 w-4 text-muted-foreground mt-0.5" />
                      <div className="flex-1 min-w-0">
                        <p className="text-xs text-muted-foreground">Folder</p>
                        <p className="text-sm font-medium text-sidebar-foreground/90">{selectedFile.folder}</p>
                      </div>
                    </div>
                  </div>
                )}
              </div>

              <Separator />

              <div>
                <button
                  onClick={() => toggleSection("tags")}
                  className="w-full flex items-center justify-between mb-2 hover:bg-muted/50 rounded p-1 transition-colors"
                >
                  <h4 className="text-xs font-semibold text-muted-foreground uppercase tracking-wider flex items-center gap-2">
                    <Tag className="h-3 w-3" />
                    Tags
                  </h4>
                  {collapsedSections.has("tags") ? (
                    <ChevronRight className="h-4 w-4 text-muted-foreground" />
                  ) : (
                    <ChevronDown className="h-4 w-4 text-muted-foreground" />
                  )}
                </button>
                {!collapsedSections.has("tags") && (
                  <div className="space-y-3">
                    {/* System Tags - displayed as regular tags */}
                    {systemTags.length > 0 && (
                      <>
                        <div className="space-y-2">
                          <p className="text-[10px] font-medium text-muted-foreground uppercase tracking-wider px-1">
                            System
                          </p>
                          <div className="flex flex-wrap gap-1.5 px-1">
                            {systemTags.map((tag) => (
                              <Badge key={tag.name} variant="outline" className="text-xs">
                                {tag.name}: {tag.value}
                              </Badge>
                            ))}
                          </div>
                        </div>

                        <div className="relative py-2">
                          <div className="absolute inset-0 flex items-center">
                            <div className="w-full border-t border-dashed border-border" />
                          </div>
                        </div>
                      </>
                    )}

                    {/* User Tags - with colors from tag tree */}
                    <div className="space-y-2">
                      <p className="text-[10px] font-medium text-muted-foreground uppercase tracking-wider px-1">
                        User Defined
                      </p>
                      <div className="flex flex-wrap gap-1.5 px-1">
                        {selectedFile.tags.map((tag) => {
                          const color = tag.color ?? getTagColor(tag.name)
                          const style = color
                            ? {
                                backgroundColor: color,
                                borderColor: color,
                              }
                            : undefined
                          return (
                            <Badge
                              key={tag.id}
                              className="text-xs"
                              style={style}
                              variant={color ? "secondary" : "outline"}
                            >
                              {tag.name}
                            </Badge>
                          )
                        })}
                      </div>
                    </div>
                  </div>
                )}
              </div>
            </div>
          </div>
        </TabsContent>

        <TabsContent value="flow" className="flex-1 overflow-y-auto mt-0">
          <div className="p-4">
            <h4 className="text-xs font-semibold text-muted-foreground uppercase tracking-wider mb-4">
              Activity Flows
            </h4>

            <div className="space-y-3">
              {flows.map((flow) => {
                const isCollapsed = collapsedFlows.has(flow.id)

                return (
                  <div key={flow.id} className="border border-border rounded-lg overflow-hidden">
                    <div className="grid grid-cols-[auto_1fr] gap-3 items-start p-3 hover:bg-muted/50 transition-colors">
                      <button
                        onClick={() => toggleFlowCollapse(flow.id)}
                        className="flex-shrink-0 hover:bg-muted rounded p-1 transition-colors mt-0.5"
                      >
                        {isCollapsed ? (
                          <ChevronRight className="h-4 w-4 text-muted-foreground" />
                        ) : (
                          <ChevronDown className="h-4 w-4 text-muted-foreground" />
                        )}
                      </button>
                      <div className="min-w-0">
                        <div className="flex items-center gap-2 mb-1 flex-wrap">
                          <h5 className="text-sm font-semibold text-sidebar-foreground">{flow.name}</h5>
                          <Badge className={cn("text-xs", getStatusBadge(flow.status))}>{flow.status}</Badge>
                        </div>
                        {isCollapsed && (
                          <div className="text-xs text-muted-foreground">
                            <p className="truncate">{flow.lastStep}</p>
                            <p className="text-[10px] mt-0.5">{flow.lastUpdated}</p>
                          </div>
                        )}
                      </div>
                    </div>

                    {!isCollapsed && (
                      <div className="px-3 pb-3 space-y-3 border-t border-border pt-3">
                        {flow.steps.map((step, index) => {
                          const StepIcon = getIconComponent(step.icon)
                          const isLast = index === flow.steps.length - 1

                          return (
                            <div key={step.id} className="flex gap-3">
                              <div className="flex flex-col items-center">
                                <div className="h-8 w-8 rounded-full bg-muted flex items-center justify-center flex-shrink-0">
                                  <StepIcon className={cn("h-4 w-4", step.iconColor)} />
                                </div>
                                {!isLast && <div className="w-px h-full bg-border mt-2" />}
                              </div>
                              <div className={cn("flex-1", !isLast && "pb-3")}>
                                <p className="text-sm font-medium text-sidebar-foreground/90">{step.title}</p>
                                <p className="text-xs text-muted-foreground mt-1">{step.description}</p>
                                <p className="text-xs text-muted-foreground mt-1">{step.timestamp}</p>
                                <p className="text-xs text-muted-foreground">by {step.user}</p>
                              </div>
                            </div>
                          )
                        })}
                      </div>
                    )}
                  </div>
                )
              })}
            </div>
          </div>
        </TabsContent>

        <TabsContent value="form" className="flex-1 overflow-y-auto mt-0">
          <div className="p-4">
            <h4 className="text-xs font-semibold text-muted-foreground uppercase tracking-wider mb-4">Edit Metadata</h4>
            <div className="space-y-4">
              <div className="space-y-2">
                <Label htmlFor="file-name" className="text-xs text-muted-foreground">
                  File Name
                </Label>
                <input
                  id="file-name"
                  value={editValues.name}
                  onChange={(e) => setEditValues({ ...editValues, name: e.target.value })}
                  onBlur={(e) => handleBlur("name", e.target.value)}
                  className="w-full h-9 rounded-md border border-input bg-background px-3 py-1 text-sm ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring transition-all"
                  placeholder="Enter file name..."
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="description" className="text-xs text-muted-foreground">
                  Description
                </Label>
                <textarea
                  id="description"
                  value={editValues.description}
                  onChange={(e) => setEditValues({ ...editValues, description: e.target.value })}
                  onBlur={(e) => handleBlur("description", e.target.value)}
                  className="w-full min-h-[80px] rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring resize-none transition-all"
                  placeholder="Add description..."
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="owner" className="text-xs text-muted-foreground">
                  Owner
                </Label>
                <input
                  id="owner"
                  value={editValues.owner}
                  onChange={(e) => setEditValues({ ...editValues, owner: e.target.value })}
                  onBlur={(e) => handleBlur("owner", e.target.value)}
                  className="w-full h-9 rounded-md border border-input bg-background px-3 py-1 text-sm ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring transition-all"
                  placeholder="Enter owner name..."
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="folder" className="text-xs text-muted-foreground">
                  Folder
                </Label>
                <input
                  id="folder"
                  value={editValues.folder}
                  onChange={(e) => setEditValues({ ...editValues, folder: e.target.value })}
                  onBlur={(e) => handleBlur("folder", e.target.value)}
                  className="w-full h-9 rounded-md border border-input bg-background px-3 py-1 text-sm ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring transition-all"
                  placeholder="Enter folder name..."
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="tags" className="text-xs text-muted-foreground">
                  Tags
                </Label>
                <input
                  id="tags"
                  value={editValues.tags}
                  onChange={(e) => setEditValues({ ...editValues, tags: e.target.value })}
                  onBlur={(e) => handleBlur("tags", e.target.value)}
                  placeholder="Separate tags with commas"
                  className="w-full h-9 rounded-md border border-input bg-background px-3 py-1 text-sm ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring transition-all"
                />
                <div className="flex flex-wrap gap-1 mt-2">
                  {editValues.tags
                    .split(",")
                    .filter((t) => t.trim())
                    .map((tag, idx) => (
                      <Badge key={idx} variant="secondary" className="text-xs">
                        {tag.trim()}
                      </Badge>
                    ))}
                </div>
              </div>

              <div className="space-y-2">
                <Label htmlFor="status" className="text-xs text-muted-foreground">
                  Status
                </Label>
                <select
                  id="status"
                  value={editValues.status}
                  onChange={(e) => {
                    const nextStatus = e.target.value as NonNullable<FileItem['status']>
                    setEditValues({ ...editValues, status: nextStatus })
                    handleBlur("status", nextStatus)
                  }}
                  className="flex h-9 w-full rounded-md border border-input bg-background px-3 py-1 text-sm ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring transition-all"
                >
                  <option value="draft">Draft</option>
                  <option value="in-progress">In Progress</option>
                  <option value="completed">Completed</option>
                </select>
              </div>
            </div>
          </div>
        </TabsContent>

        <TabsContent value="chat" className="flex-1 overflow-y-auto mt-0">
          <div className="p-4 flex flex-col gap-4 h-full">
            <div className="flex items-start justify-between gap-2">
              <div>
                <h4 className="text-xs font-semibold text-muted-foreground uppercase tracking-wider">Team chat</h4>
                <p className="text-sm text-muted-foreground mt-1">
                  Mention teammates, react with emoji, or drop files for a quick review.
                </p>
              </div>
              <Badge variant="outline" className="text-[10px]">Live</Badge>
            </div>

            <div className="space-y-3">
              {chatMessages.map((message) => (
                <div key={message.id} className="rounded-lg border border-border bg-background/40 p-3 space-y-2">
                  <div className="flex items-center justify-between gap-3">
                    <div className="flex items-center gap-2 min-w-0">
                      <div className="h-9 w-9 rounded-full bg-muted flex items-center justify-center flex-shrink-0">
                        <MessageCircle className="h-4 w-4 text-muted-foreground" />
                      </div>
                      <div className="min-w-0">
                        <p className="text-sm font-semibold text-sidebar-foreground truncate">{message.author}</p>
                        <p className="text-xs text-muted-foreground">{message.timestamp}</p>
                      </div>
                    </div>

                    {message.mentions?.length ? (
                      <Badge variant="outline" className="text-[10px] uppercase tracking-wide">
                        {message.mentions.length} mention{message.mentions.length > 1 ? "s" : ""}
                      </Badge>
                    ) : null}
                  </div>

                  {renderMessageContent(message)}

                  {message.attachments?.length ? (
                    <div className="flex flex-wrap gap-2 text-xs text-muted-foreground">
                      {message.attachments.map((file) => (
                        <div
                          key={`${message.id}-${file}`}
                          className="flex items-center gap-2 rounded-md border border-border px-2 py-1 bg-muted/50"
                        >
                          <Paperclip className="h-3.5 w-3.5" />
                          <span className="truncate max-w-[200px]" title={file}>
                            {file}
                          </span>
                        </div>
                      ))}
                    </div>
                  ) : null}
                </div>
              ))}
            </div>

            <div className="mt-auto space-y-3">
              <div className="flex flex-wrap gap-2">
                <Button variant="outline" size="sm" className="gap-2" onClick={handleInsertMention}>
                  <AtSign className="h-4 w-4" />
                  Mention
                </Button>
                <Button variant="outline" size="sm" className="gap-2" onClick={handleAddEmoji}>
                  <Smile className="h-4 w-4" />
                  Emoji
                </Button>
                <Button variant="outline" size="sm" className="gap-2" onClick={handleAttachmentClick}>
                  <Paperclip className="h-4 w-4" />
                  File
                </Button>
                <input
                  ref={fileInputRef}
                  type="file"
                  multiple
                  className="hidden"
                  onChange={handleAttachmentChange}
                />
              </div>

              {chatAttachments.length > 0 && (
                <div className="flex flex-wrap gap-2">
                  {chatAttachments.map((file) => (
                    <div
                      key={`${file.name}-${file.lastModified}`}
                      className="flex items-center gap-2 rounded-md border border-border px-2 py-1 bg-muted/60"
                    >
                      <Paperclip className="h-3.5 w-3.5 text-muted-foreground" />
                      <span className="text-xs truncate max-w-[160px]" title={file.name}>
                        {file.name}
                      </span>
                      <button
                        type="button"
                        onClick={() => handleRemoveAttachment(file.name)}
                        className="text-muted-foreground hover:text-foreground"
                        aria-label={`Remove ${file.name}`}
                      >
                        <X className="h-3 w-3" />
                      </button>
                    </div>
                  ))}
                </div>
              )}

              <div className="space-y-2">
                <textarea
                  value={chatInput}
                  onChange={(event) => setChatInput(event.target.value)}
                  placeholder="Write a message with mentions, emoji, or file notes..."
                  className="w-full min-h-[90px] rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring resize-none transition-all"
                />
                <div className="flex items-center justify-between gap-3">
                  <p className="text-xs text-muted-foreground">
                    Tip: use @mentions, emoji, or attach files for richer context.
                  </p>
                  <Button
                    className="gap-2"
                    onClick={handleSendMessage}
                    disabled={!chatInput.trim() && chatAttachments.length === 0}
                  >
                    <Send className="h-4 w-4" />
                    <span>Send</span>
                  </Button>
                </div>
              </div>
            </div>
          </div>
        </TabsContent>
      </Tabs>

    </div>
  )
}
