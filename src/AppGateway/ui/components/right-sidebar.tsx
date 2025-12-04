"use client"

import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { cn } from "@/lib/utils"
import type { DocumentTag, DocumentType, FileItem, TagNode, User } from "@/lib/types"
import {
  AtSign,
  ChevronDown,
  ChevronRight,
  Clock,
  FileText,
  FolderOpen,
  Info,
  GitBranch,
  MessageSquare,
  NotebookPen,
  HardDrive,
  Paperclip,
  Smile,
  Tag,
  User as UserIcon,
  X,
} from "lucide-react"
import type React from "react"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { TabsContent } from "@/components/ui/tabs"
import { Separator } from "@/components/ui/separator"
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover"
import { useState, useEffect, useCallback, useRef, useMemo } from "react"
import {
  fetchDocumentTypes,
  fetchFlows,
  fetchTags,
  fetchUserById,
  searchUsers,
  updateFile,
  type UpdateFileRequest,
} from "@/lib/api"
import type { Flow } from "@/lib/types"
import { FileTypeIcon } from "./file-type-icon"
import {
  SidebarChatTab,
  SidebarShell,
  type SidebarComment,
} from "./shared/sidebar-tabs"
import { UserIdentity } from "@/components/user/user-identity"

type ActiveTab = "info" | "flow" | "form" | "chat"

type RightSidebarProps = {
  selectedFile: FileItem | null
  activeTab: ActiveTab
  onTabChange: (tab: ActiveTab) => void
  onClose: () => void
  onFileUpdate?: (file: FileItem) => void
  showTabShortcuts?: boolean
}

type ChatMessage = {
  id: string
  author: string
  content: string
  timestamp: string
  mentions?: string[]
  attachments?: string[]
}

const tabBadgeStyles: Record<ActiveTab, string> = {
  info: "border-sky-500/30 text-sky-600 bg-sky-500/10 dark:text-sky-200",
  flow: "border-emerald-500/30 text-emerald-600 bg-emerald-500/10 dark:text-emerald-200",
  form: "border-violet-500/30 text-violet-600 bg-violet-500/10 dark:text-violet-200",
  chat: "border-amber-500/30 text-amber-600 bg-amber-500/10 dark:text-amber-200",
}

type EditableFileState = {
  name: string
  description: string
  owner: string
  folder: string
  tags: string
  status: NonNullable<FileItem['status']>
  documentTypeId: string | null
}

const DEFAULT_FILE_STATUS: NonNullable<FileItem['status']> = "draft"

const statusColors: Record<NonNullable<FileItem['status']>, string> = {
  "in-progress": "bg-yellow-500/10 text-yellow-700 dark:text-yellow-400 border-yellow-500/20",
  completed: "bg-green-500/10 text-green-700 dark:text-green-400 border-green-500/20",
  draft: "bg-gray-500/10 text-gray-700 dark:text-gray-400 border-gray-500/20",
}

function createEditableState(file: FileItem | null): EditableFileState {
  return {
    name: file?.name ?? "",
    description: file?.description ?? "",
    owner: file?.ownerName ?? file?.owner ?? "",
    folder: file?.folder ?? "",
    tags: file ? file.tags.map((tag) => tag.name).join(", ") : "",
    status: file?.status ?? DEFAULT_FILE_STATUS,
    documentTypeId: file?.documentTypeId ?? null,
  }
}

export function RightSidebar({
  selectedFile,
  activeTab,
  onTabChange,
  onClose,
  onFileUpdate,
  showTabShortcuts = false,
}: RightSidebarProps) {
  const [editValues, setEditValues] = useState<EditableFileState>(() => createEditableState(selectedFile))

  const [flows, setFlows] = useState<Flow[]>([])
  const [flowsLoading, setFlowsLoading] = useState(false)
  const [tagTree, setTagTree] = useState<TagNode[]>([])
  const [collapsedFlows, setCollapsedFlows] = useState<Set<string>>(new Set())
  const [documentTypes, setDocumentTypes] = useState<DocumentType[]>([])
  const [collapsedSections, setCollapsedSections] = useState<Set<string>>(() => {
    if (typeof window !== "undefined") {
      const saved = localStorage.getItem("collapsedSections")
      return saved ? new Set(JSON.parse(saved)) : new Set()
    }
    return new Set()
  })
  const [chatMessages, setChatMessages] = useState<ChatMessage[]>([])
  const [chatInput, setChatInput] = useState("")
  const [chatAttachments, setChatAttachments] = useState<File[]>([])
  const [mentionSuggestions, setMentionSuggestions] = useState<User[]>([])
  const [mentionQuery, setMentionQuery] = useState("")
  const [mentionOpen, setMentionOpen] = useState(false)
  const [mentionLoading, setMentionLoading] = useState(false)
  const [emojiOpen, setEmojiOpen] = useState(false)
  const [ownerProfile, setOwnerProfile] = useState<User | null>(null)
  const [isUpdatingDocumentType, setIsUpdatingDocumentType] = useState(false)
  const fileInputRef = useRef<HTMLInputElement>(null)
  const emojiOptions = ["😀", "😁", "😍", "😎", "🤔", "🙏", "🚀", "👍", "🎉", "🔥", "📄", "✅"]
  const DOCUMENT_TYPE_NONE_VALUE = "__none"
  const documentTypeSelectValue = editValues.documentTypeId ?? DOCUMENT_TYPE_NONE_VALUE
  useEffect(() => {
    setEditValues(createEditableState(selectedFile))
  }, [selectedFile])

  useEffect(() => {
    setOwnerProfile(null)

    const ownerLookup = selectedFile?.ownerId ?? selectedFile?.owner
    if (!ownerLookup) {
      return
    }

    let cancelled = false

    fetchUserById(ownerLookup)
      .then((user) => {
        if (cancelled) return

        if (user) {
          setOwnerProfile(user)
          return
        }

        return searchUsers(ownerLookup)
          .then((users) => {
            if (cancelled) return
            const matched = users.find((candidate) =>
              candidate.id === ownerLookup || candidate.email === ownerLookup,
            )
            setOwnerProfile(matched ?? users[0] ?? null)
          })
          .catch((error) => console.warn("[ui] Failed to resolve owner profile:", error))
      })
      .catch((error) => console.warn("[ui] Failed to resolve owner profile:", error))

    return () => {
      cancelled = true
    }
  }, [selectedFile?.owner, selectedFile?.ownerId])

  
  const ownerDisplayName = ownerProfile?.displayName ?? selectedFile?.ownerName ?? selectedFile?.owner ?? "Owner"
  const ownerEmail = ownerProfile?.email ?? selectedFile?.ownerEmail ?? selectedFile?.owner ?? ""
  const ownerDisplayValue = ownerProfile?.displayName ?? ownerEmail ?? selectedFile?.owner ?? ""
  const ownerLookup = selectedFile?.ownerId ?? selectedFile?.owner ?? ""

  useEffect(() => {
    if (!ownerDisplayValue || !selectedFile) {
      return
    }

    setEditValues((previous) => {
      if (previous.owner === selectedFile.owner || !previous.owner) {
        return { ...previous, owner: ownerDisplayValue }
      }
      return previous
    })
  }, [ownerDisplayValue, selectedFile])

  useEffect(() => {
    fetchTags().then(setTagTree)
  }, [])

  useEffect(() => {
    fetchDocumentTypes()
      .then(setDocumentTypes)
      .catch((error) => {
        console.warn("[ui] Failed to load document types:", error)
        setDocumentTypes([])
      })
  }, [])

  useEffect(() => {
    const ownerName = ownerProfile?.displayName ?? selectedFile?.owner ?? "Owner"
    const ownerHandle = `@${(ownerProfile?.displayName ?? selectedFile?.owner ?? "owner").replace(/\s+/g, "")}`
    const starterMessages: ChatMessage[] = [
      {
        id: "intro",
        author: "System",
        content: `Discuss updates to ${selectedFile?.name ?? "this file"} here.`,
        timestamp: "Just now",
      },
      {
        id: "owner",
        author: ownerName,
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
  }, [selectedFile, ownerProfile])

  useEffect(() => {
    if (!selectedFile?.id) {
      setFlows([])
      return
    }

    setFlowsLoading(true)
    fetchFlows(selectedFile.id)
      .then((data) => {
        const sortedFlows = data.sort((a, b) => {
          const timeA = parseTimeAgo(a.lastUpdated)
          const timeB = parseTimeAgo(b.lastUpdated)
          return timeA - timeB
        })
        setFlows(sortedFlows)
        setCollapsedFlows(new Set(sortedFlows.map((flow) => flow.id)))
      })
      .finally(() => setFlowsLoading(false))
  }, [selectedFile?.id])

  useEffect(() => {
    setMentionQuery("")
    setMentionSuggestions([])
    setMentionOpen(false)
    setEmojiOpen(false)
  }, [selectedFile?.id])

  useEffect(() => {
    if (!mentionOpen) {
      return
    }

    setMentionLoading(true)
    searchUsers(mentionQuery)
      .then((users) => setMentionSuggestions(users))
      .catch((error) => console.warn("[ui] Failed to load mention suggestions:", error))
      .finally(() => setMentionLoading(false))
  }, [mentionOpen, mentionQuery])

  const parseTimeAgo = (timeStr: string): number => {
    const match = timeStr.match(/(\d+)\s+(hour|day|week|month)/)
    if (!match) return 0
    const value = Number.parseInt(match[1])
    const unit = match[2]
    const multipliers: Record<string, number> = { hour: 1, day: 24, week: 168, month: 720 }
    return value * (multipliers[unit] || 1)
  }


  const selectedDocumentType = useMemo(() => {
    const currentId = editValues.documentTypeId ?? selectedFile?.documentTypeId ?? null
    return currentId ? documentTypes.find((type) => type.id === currentId) : undefined
  }, [documentTypes, editValues.documentTypeId, selectedFile?.documentTypeId])

  const appliedUserTags = useMemo(() => {
    if (!selectedFile?.tags?.length) {
      return []
    }

    const groups = selectedFile.tags
      .filter((tag) => !tag.isSystem)
      .reduce<Record<string, DocumentTag[]>>((accumulator, tag) => {
        const appliedByValues = (tag.appliedBy ?? "")
          .split(",")
          .map((value) => value.trim())
          .filter(Boolean)

        const appliers = appliedByValues.length > 0 ? appliedByValues : ["Unknown"]

        for (const applier of appliers) {
          accumulator[applier] = accumulator[applier] ? [...accumulator[applier], tag] : [tag]
        }

        return accumulator
      }, {})

    return Object.entries(groups)
  }, [selectedFile?.tags])

  const appendToken = (token: string) => {
    setChatInput((previous) => `${previous}${previous && !previous.endsWith(" ") ? " " : ""}${token} `)
  }

  const formatMentionHandle = (user?: Pick<User, "displayName" | "email"> | null) => {
    const base = user?.displayName || user?.email || ownerDisplayName
    return `@${base.replace(/\s+/g, "")}`
  }

  const handleInsertMention = (user?: User) => {
    const mentionHandle = formatMentionHandle(user ?? ownerProfile)
    appendToken(mentionHandle)
    setMentionOpen(false)
  }

  const handleInsertEmoji = (emoji: string) => {
    appendToken(emoji)
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

    setChatMessages((previous) => [...previous, newMessage])
    setChatInput("")
    setChatAttachments([])
    if (fileInputRef.current) {
      fileInputRef.current.value = ""
    }
  }

  const handleChatInputChange = (value: string) => {
    setChatInput(value)

    const mentionMatch = value.match(/@([\w.-]*)$/)
    if (mentionMatch) {
      setMentionOpen(true)
      setMentionQuery(mentionMatch[1])
    } else if (mentionOpen) {
      setMentionOpen(false)
      setMentionQuery("")
    }
  }

  const toggleFlowCollapse = (flowId: string) => {
    setCollapsedFlows((previous) => {
      const next = new Set(previous)
      if (next.has(flowId)) {
        next.delete(flowId)
      } else {
        next.add(flowId)
      }
      return next
    })
  }

  const toggleSection = (sectionId: string) => {
    setCollapsedSections((previous) => {
      const next = new Set(previous)
      if (next.has(sectionId)) {
        next.delete(sectionId)
      } else {
        next.add(sectionId)
      }
      localStorage.setItem("collapsedSections", JSON.stringify(Array.from(next)))
      return next
    })
  }

  const getIconComponent = (iconName: string) => {
    const icons: Record<string, any> = {
      Clock: Clock,
      UserIcon,
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
          const currentOwner = selectedFile.ownerName ?? selectedFile.owner
          if (trimmedValue === currentOwner) {
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

  const handleDocumentTypeChange = useCallback(
    async (nextTypeId: string | null) => {
      if (!selectedFile) {
        return
      }

      const normalized = nextTypeId && nextTypeId.trim() ? nextTypeId : null
      if ((selectedFile.documentTypeId ?? null) === normalized) {
        setEditValues((previous) => ({ ...previous, documentTypeId: normalized }))
        return
      }

      setEditValues((previous) => ({ ...previous, documentTypeId: normalized }))
      setIsUpdatingDocumentType(true)
      try {
        const updatedFile = await updateFile(selectedFile.id, { documentTypeId: normalized })
        setEditValues(createEditableState(updatedFile))
        onFileUpdate?.(updatedFile)
      } catch (error) {
        console.error("[ui] Failed to update document type:", error)
        setEditValues((previous) => ({ ...previous, documentTypeId: selectedFile.documentTypeId ?? null }))
      } finally {
        setIsUpdatingDocumentType(false)
      }
    },
    [onFileUpdate, selectedFile],
  )

  const tabLabels: Record<ActiveTab, string> = {
    info: "Info",
    flow: "Flow",
    form: "Form",
    chat: "Chat",
  }

  const tabIcons: Record<ActiveTab, React.ComponentType<{ className?: string }>> = {
    info: Info,
    flow: GitBranch,
    form: NotebookPen,
    chat: MessageSquare,
  }

  const sidebarComments: SidebarComment[] = useMemo(
    () =>
      chatMessages.map((message) => ({
        id: message.id,
        author: message.author,
        message: message.content,
        createdAt: message.timestamp,
        attachments: message.attachments,
      })),
    [chatMessages],
  )

  const composerExtras = (
    <div className="space-y-2 text-xs text-muted-foreground">
      <div className="flex items-center gap-2">
        <Popover open={mentionOpen} onOpenChange={setMentionOpen}>
          <PopoverTrigger asChild>
            <Button variant="ghost" size="icon" className="h-8 w-8">
              <AtSign className="h-4 w-4" />
            </Button>
          </PopoverTrigger>
          <PopoverContent className="w-64 space-y-2 p-3" align="start">
            <div className="text-xs font-semibold text-foreground">Mention someone</div>
            <Input
              value={mentionQuery}
              onChange={(event) => setMentionQuery(event.target.value)}
              placeholder="Search by name or email"
              className="h-8"
            />
            <div className="max-h-48 space-y-1 overflow-y-auto">
              {mentionLoading ? (
                <p className="text-[11px] text-muted-foreground">Loading mentions…</p>
              ) : mentionSuggestions.length ? (
                mentionSuggestions.map((user) => (
                  <Button
                    key={user.id}
                    variant="ghost"
                    className="h-auto w-full justify-start px-2 py-1.5"
                    onClick={() => handleInsertMention(user)}
                  >
                    <div className="flex flex-col text-left">
                      <span className="text-sm font-medium text-foreground">{user.displayName}</span>
                      <span className="text-[11px] text-muted-foreground">{user.email}</span>
                    </div>
                  </Button>
                ))
              ) : (
                <p className="text-[11px] text-muted-foreground">No matches found</p>
              )}
            </div>
            <Button variant="outline" size="sm" className="w-full" onClick={() => handleInsertMention()}>
              Mention owner {ownerDisplayName ? `(@${formatMentionHandle(ownerProfile)})` : ""}
            </Button>
          </PopoverContent>
        </Popover>
        <Popover open={emojiOpen} onOpenChange={setEmojiOpen}>
          <PopoverTrigger asChild>
            <Button variant="ghost" size="icon" className="h-8 w-8">
              <Smile className="h-4 w-4" />
            </Button>
          </PopoverTrigger>
          <PopoverContent className="w-56 space-y-2 p-3" align="start">
            <div className="grid grid-cols-6 gap-2">
              {emojiOptions.map((emoji) => (
                <button
                  key={emoji}
                  type="button"
                  className="flex h-8 w-8 items-center justify-center rounded-md border border-border/50 bg-background text-lg hover:bg-muted"
                  onClick={() => handleInsertEmoji(emoji)}
                >
                  {emoji}
                </button>
              ))}
            </div>
            <p className="text-[11px] text-muted-foreground">Pick multiple emojis to add to your comment.</p>
          </PopoverContent>
        </Popover>
        <Button variant="ghost" size="icon" className="h-8 w-8" onClick={handleAttachmentClick}>
          <Paperclip className="h-4 w-4" />
        </Button>
        <input
          ref={fileInputRef}
          type="file"
          multiple
          className="hidden"
          onChange={handleAttachmentChange}
          aria-label="Attach files"
        />
      </div>
      {chatAttachments.length ? (
        <div className="space-y-1">
          {chatAttachments.map((file) => (
            <div
              key={file.name}
              className="flex items-center justify-between rounded border border-border/60 bg-background/70 px-2 py-1"
            >
              <span className="truncate text-xs">{file.name}</span>
              <Button variant="ghost" size="icon" className="h-6 w-6" onClick={() => handleRemoveAttachment(file.name)}>
                <X className="h-3 w-3" />
              </Button>
            </div>
          ))}
        </div>
      ) : null}
    </div>
  )

  return (
    <div className="w-full h-full min-h-0 border-l border-sidebar-border bg-sidebar text-sidebar-foreground flex flex-col relative z-20">
      <div className="p-4 border-b border-sidebar-border/80 flex items-center justify-between flex-shrink-0">
        <div className="space-y-1">
          <h2 className="font-semibold text-sidebar-foreground">File Details</h2>
          <div className="flex flex-wrap items-center gap-2">
            <Badge variant="outline" className={cn("text-[10px]", tabBadgeStyles[activeTab])}>
              {tabLabels[activeTab]}
            </Badge>
            {selectedFile?.latestVersionNumber || selectedFile?.latestVersionId ? (
              <span className="text-[11px] text-muted-foreground">
                Version {selectedFile.latestVersionNumber ?? selectedFile.latestVersionId}
              </span>
            ) : null}
          </div>
        </div>
        <Button variant="ghost" size="icon" className="h-8 w-8" onClick={onClose}>
          <X className="h-4 w-4" />
        </Button>
      </div>

      {showTabShortcuts ? (
        <div className="flex flex-wrap gap-2 border-b border-sidebar-border/80 px-4 py-2">
          {(Object.keys(tabLabels) as ActiveTab[]).map((tab) => {
            const Icon = tabIcons[tab]
            const isActive = activeTab === tab
            return (
              <Button
                key={tab}
                variant={isActive ? "secondary" : "ghost"}
                size="sm"
                className={cn(
                  "h-8 gap-2 rounded-full px-3 text-xs",
                  isActive
                    ? "bg-primary/10 text-primary shadow-[0_0_0_1px] shadow-primary/40"
                    : "text-muted-foreground hover:text-foreground",
                )}
                onClick={() => onTabChange(tab)}
              >
                <Icon className="h-4 w-4" />
                {tabLabels[tab]}
              </Button>
            )
          })}
        </div>
      ) : null}

      <div className="flex-1 min-h-0 overflow-hidden">
        <SidebarShell
          tabs={{ info: true, flow: true, form: true, chat: true }}
          activeTab={activeTab}
          onTabChange={(value) => onTabChange((value as ActiveTab) || "info")}
          headerBadge={`Version ${selectedFile?.latestVersionNumber ?? selectedFile?.latestVersionId ?? "N/A"}`}
          showTabsList={false}
        >
        <TabsContent value="info" className="mt-0 h-full">
          <div className="space-y-4">
            {selectedFile ? (
              <>
                <div className="aspect-video w-full rounded-lg bg-muted flex items-center justify-center">
                  <FileTypeIcon file={selectedFile} size="lg" />
                </div>

                <h3 className="font-semibold text-lg mb-1 text-sidebar-foreground text-pretty">{selectedFile.name}</h3>
                {selectedFile.description ? (
                  <p className="text-sm text-muted-foreground mb-4 text-pretty">{selectedFile.description}</p>
                ) : null}

                {selectedFile.status ? (
                  <Badge className={cn("mb-4", statusColors[selectedFile.status])}>{selectedFile.status}</Badge>
                ) : null}

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
                        {[
                          {
                            label: "Document Type",
                            value: selectedDocumentType?.typeName ?? selectedFile.docType ?? "Document",
                            icon: FileText,
                          },
                          {
                            label: "Size",
                            value: selectedFile.size,
                            icon: HardDrive,
                          },
                          {
                            label: "Modified",
                            value: selectedFile.modified,
                            icon: Clock,
                          },
                          {
                            label: "Owner",
                            value: ownerLookup ? (
                              <UserIdentity
                                userId={ownerLookup}
                                size="sm"
                                density="compact"
                                className="px-0 py-0"
                              />
                            ) : (
                              "Unknown"
                            ),
                            icon: UserIcon,
                          },
                          {
                            label: "Folder",
                            value: selectedFile.folder,
                            icon: FolderOpen,
                          },
                        ].map(({ label, value, icon: Icon }) => (
                          <div key={label} className="flex items-start gap-3">
                            <Icon className="h-4 w-4 text-muted-foreground mt-0.5" />
                            <div className="flex-1 min-w-0">
                              <p className="text-xs text-muted-foreground">{label}</p>
                              <div className="text-sm font-medium text-sidebar-foreground/90">{value}</div>
                            </div>
                          </div>
                        ))}
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
                        <div className="space-y-2">
                          {appliedUserTags.length ? (
                            <div className="space-y-3">
                              {appliedUserTags.map(([applier, tags]) => (
                                <div key={applier} className="space-y-1 px-1">
                                  <div className="text-[11px] font-medium text-muted-foreground uppercase tracking-wider flex items-center gap-2">
                                    <span>Applied by</span>
                                    {applier === "Unknown" ? (
                                      <span className="text-sidebar-foreground/80">Unknown</span>
                                    ) : (
                                      <UserIdentity
                                        userId={applier}
                                        size="sm"
                                        density="compact"
                                        interactive={false}
                                        className="px-0 py-0"
                                      />
                                    )}
                                  </div>
                                  <div className="flex flex-wrap gap-1.5">
                                    {tags.map((tag) => {
                                      const color = tag.color ?? getTagColor(tag.name)
                                      const style = color
                                        ? {
                                            backgroundColor: color,
                                            borderColor: color,
                                          }
                                        : undefined

                                      return (
                                        <Badge
                                          key={`${applier}-${tag.id}`}
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
                              ))}
                            </div>
                          ) : (
                            <p className="text-xs text-muted-foreground px-1">No user-defined tags</p>
                          )}
                        </div>
                      </div>
                    )}
                  </div>
                </div>
              </>
            ) : (
              <div className="text-center text-muted-foreground">No file selected</div>
            )}
          </div>
        </TabsContent>

        <TabsContent value="flow" className="mt-0 h-full">
          <div className="space-y-3">
            {flowsLoading ? (
              <p className="text-sm text-muted-foreground">Loading flows…</p>
            ) : !selectedFile ? (
              <p className="text-sm text-muted-foreground">Select a file to view flows.</p>
            ) : (
              flows.map((flow) => {
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
              })
            )}
          </div>
        </TabsContent>

        <TabsContent value="form" className="mt-0 h-full">
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
                disabled={!selectedFile}
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
                disabled={!selectedFile}
              />
            </div>

            <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor="owner" className="text-xs text-muted-foreground">
                  Owner (Username)
                </Label>
                <input
                  id="owner"
                  value={editValues.owner}
                  onChange={(e) => setEditValues({ ...editValues, owner: e.target.value })}
                  onBlur={(e) => handleBlur("owner", e.target.value)}
                  className="w-full h-9 rounded-md border border-input bg-background px-3 py-1 text-sm ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring transition-all"
                  placeholder="Enter owner email..."
                  disabled={!selectedFile}
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
                  disabled={!selectedFile}
                />
              </div>
            </div>

            <div className="space-y-2">
              <Label htmlFor="docType" className="text-xs text-muted-foreground">
                Document Type
              </Label>
              <Select
                value={documentTypeSelectValue}
                onValueChange={(value) =>
                  handleDocumentTypeChange(
                    value === DOCUMENT_TYPE_NONE_VALUE ? null : value,
                  )
                }
                disabled={!selectedFile || isUpdatingDocumentType}
              >
                <SelectTrigger className="h-9 w-full">
                  <SelectValue
                    placeholder={
                      selectedDocumentType?.typeName ??
                      selectedFile?.docType ??
                      "Select a document type"
                    }
                  />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value={DOCUMENT_TYPE_NONE_VALUE}>
                    No document type
                  </SelectItem>
                  {documentTypes.map((type) => (
                    <SelectItem key={type.id} value={type.id}>
                      {type.typeName}{" "}
                      <span className="text-xs text-muted-foreground">
                        ({type.typeKey})
                      </span>
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
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
                disabled={!selectedFile}
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
                disabled={!selectedFile}
              >
                <option value="draft">Draft</option>
                <option value="in-progress">In Progress</option>
                <option value="completed">Completed</option>
              </select>
            </div>

            {!selectedFile ? (
              <div className="rounded-md border border-dashed border-border/60 bg-muted/30 p-3 text-xs text-muted-foreground">
                Select a file to edit its metadata.
              </div>
            ) : (
              <div className="rounded-md border border-dashed border-border/60 bg-muted/30 p-3 text-xs text-muted-foreground">
                Changes are auto-saved when you leave a field.
              </div>
            )}
          </div>
        </TabsContent>

        <TabsContent value="chat" className="mt-0 h-full">
          <div className="space-y-4">
            <SidebarChatTab
              comments={sidebarComments}
              draftMessage={chatInput}
              onDraftChange={handleChatInputChange}
              onSubmit={handleSendMessage}
              composerExtras={composerExtras}
              canSubmit={chatInput.trim().length > 0 || chatAttachments.length > 0}
            />
          </div>
        </TabsContent>
      </SidebarShell>

      </div>

    </div>
  )
}
