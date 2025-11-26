"use client"

import { Input } from "@/components/ui/input"
import { cn } from "@/lib/utils"
import type { FileItem, User } from "@/lib/types"
import { AtSign, Paperclip, Smile, X } from "lucide-react"
import type React from "react"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { TabsContent } from "@/components/ui/tabs"
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover"
import { useState, useEffect, useCallback, useRef, useMemo } from "react"
import { fetchFlows, fetchSystemTags, searchUsers, updateFile, type UpdateFileRequest } from "@/lib/api"
import type { Flow, SystemTag } from "@/lib/types"
import { FileTypeIcon } from "./file-type-icon"
import {
  SidebarChatTab,
  SidebarFlowTab,
  SidebarFormTab,
  SidebarInfoTab,
  SidebarShell,
  formatBytes,
  formatDate,
  type SidebarComment,
  type SidebarFormValues,
} from "./shared/sidebar-tabs"

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
  docType: string
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
    docType: file?.docType ?? "Document",
  }
}

export function RightSidebar({ selectedFile, activeTab, onTabChange, onClose, onFileUpdate }: RightSidebarProps) {
  const [editValues, setEditValues] = useState<EditableFileState>(() => createEditableState(selectedFile))

  const [flows, setFlows] = useState<Flow[]>([])
  const [flowsLoading, setFlowsLoading] = useState(false)
  const [systemTags, setSystemTags] = useState<SystemTag[]>([])
  const [chatMessages, setChatMessages] = useState<ChatMessage[]>([])
  const [chatInput, setChatInput] = useState("")
  const [chatAttachments, setChatAttachments] = useState<File[]>([])
  const [mentionSuggestions, setMentionSuggestions] = useState<User[]>([])
  const [mentionQuery, setMentionQuery] = useState("")
  const [mentionOpen, setMentionOpen] = useState(false)
  const [mentionLoading, setMentionLoading] = useState(false)
  const [emojiOpen, setEmojiOpen] = useState(false)
  const fileInputRef = useRef<HTMLInputElement>(null)
  const emojiOptions = ["😀", "😁", "😍", "😎", "🤔", "🙏", "🚀", "👍", "🎉", "🔥", "📄", "✅"]

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
    if (!selectedFile?.id) {
      setFlows([])
      setSystemTags([])
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
      })
      .finally(() => setFlowsLoading(false))
    fetchSystemTags(selectedFile.id).then(setSystemTags)
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

  const appendToken = (token: string) => {
    setChatInput((previous) => `${previous}${previous && !previous.endsWith(" ") ? " " : ""}${token} `)
  }

  const handleInsertMention = (user?: User) => {
    const mentionHandle = user
      ? `@${user.displayName.replace(/\s+/g, "")}`
      : selectedFile?.owner
        ? `@${selectedFile.owner}`
        : "@mention"
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

  const sidebarFile = useMemo(() => {
    if (!selectedFile) return null

    return {
      ...selectedFile,
      docType: selectedFile.docType ?? "Document",
      versions: [],
      activity: [],
    }
  }, [selectedFile])

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

  const formValues: SidebarFormValues | undefined = sidebarFile
    ? {
        name: editValues.name,
        owner: editValues.owner,
        description: editValues.description,
        docType: sidebarFile.docType ?? editValues.docType,
        tags: editValues.tags,
        folder: editValues.folder,
        latestVersionLabel: sidebarFile.latestVersionNumber ?? sidebarFile.latestVersionId ?? "N/A",
        fileId: sidebarFile.id,
        type: sidebarFile.type,
        createdAt: formatDate(sidebarFile.createdAtUtc),
        modifiedAt: formatDate(sidebarFile.modifiedAtUtc ?? sidebarFile.modified),
        status: sidebarFile.status ?? DEFAULT_FILE_STATUS,
        sizeLabel: formatBytes(sidebarFile.sizeBytes, sidebarFile.size),
      }
    : undefined

  const infoExtraSections = null

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
              Mention owner {selectedFile?.owner ? `(@${selectedFile.owner})` : ""}
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

      <SidebarShell
        tabs={{ info: true, flow: true, form: true, chat: true }}
        activeTab={activeTab}
        onTabChange={(value) => onTabChange((value as ActiveTab) || "info")}
        headerBadge={`Version ${selectedFile.latestVersionNumber ?? selectedFile.latestVersionId ?? "N/A"}`}
      >
        <TabsContent value="info" className="mt-0 h-full">
          <div className="p-4 space-y-4">
            <div className="aspect-video w-full rounded-lg bg-muted flex items-center justify-center">
              <FileTypeIcon file={selectedFile} size="lg" />
            </div>
            <div>
              <h3 className="font-semibold text-lg text-sidebar-foreground text-pretty">{selectedFile.name}</h3>
              {selectedFile.description ? (
                <p className="text-sm text-muted-foreground text-pretty">{selectedFile.description}</p>
              ) : null}
            </div>
            {sidebarFile ? (
              <SidebarInfoTab file={sidebarFile} systemTags={systemTags} extraSections={infoExtraSections} />
            ) : null}
          </div>
        </TabsContent>

        <TabsContent value="flow" className="mt-0 h-full">
          <div className="p-4">
            <SidebarFlowTab flows={flows} loading={flowsLoading} />
          </div>
        </TabsContent>

        <TabsContent value="form" className="mt-0 h-full">
          <div className="p-4 space-y-4">
            {sidebarFile ? (
              <SidebarFormTab
                file={sidebarFile}
                values={formValues}
                editable
                onChange={(field, value) =>
                  setEditValues((previous) => ({ ...previous, [field]: value ?? "" } as EditableFileState))
                }
                onBlur={(field, value) => handleBlur(field as keyof EditableFileState, value)}
                actionsSlot={
                  <div className="space-y-2 rounded-md border border-dashed border-border/60 bg-muted/30 p-3 text-xs text-muted-foreground">
                    <p>Changes are auto-saved when you leave a field.</p>
                    <p>Use the fields above to update tags or status.</p>
                  </div>
                }
              />
            ) : null}
          </div>
        </TabsContent>

        <TabsContent value="chat" className="mt-0 h-full">
          <div className="p-4">
            <SidebarChatTab
              comments={sidebarComments}
              draftMessage={chatInput}
              onDraftChange={setChatInput}
              onSubmit={handleSendMessage}
              composerExtras={composerExtras}
              canSubmit={chatInput.trim().length > 0 || chatAttachments.length > 0}
            />
          </div>
        </TabsContent>
      </SidebarShell>

    </div>
  )
}
