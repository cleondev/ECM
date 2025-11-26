"use client"
import {
  Avatar,
  AvatarFallback,
  AvatarImage,
} from "@/components/ui/avatar"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Separator } from "@/components/ui/separator"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { Textarea } from "@/components/ui/textarea"
import type {
  DocumentTag,
  FileActivity,
  FileComment,
  FileDetail,
  FileVersion,
  Flow,
  SystemTag,
} from "@/lib/types"
import { useEffect, useRef } from "react"
import { Clock3, FileText, Folder, GitBranch, HardDrive, Info, ListChecks, MessageSquare, NotebookPen, Tag, UserRound } from "lucide-react"

export type SidebarFileLike = Pick<
  FileDetail,
  | "name"
  | "size"
  | "sizeBytes"
  | "createdAtUtc"
  | "modifiedAtUtc"
  | "modified"
  | "tags"
  | "versions"
  | "activity"
  | "latestVersionNumber"
  | "latestVersionId"
  | "owner"
  | "ownerAvatar"
  | "description"
  | "folder"
  | "status"
  | "type"
  | "id"
  | "docType"
>

export type SidebarComment = FileComment & { attachments?: string[] }

export type SidebarFormValues = {
  name: string
  owner?: string
  description?: string | null
  docType?: string
  tags?: string
  latestVersionLabel?: string
  folder?: string
  fileId?: string
  type?: string
  createdAt?: string
  modifiedAt?: string
  status?: string
  sizeLabel?: string
}

type BaseSidebarProps = {
  tabs: {
    info?: boolean
    flow?: boolean
    form?: boolean
    chat?: boolean
  }
  activeTab: string
  onTabChange: (value: string) => void
  headerBadge?: string
}

type InfoTabProps = {
  file: SidebarFileLike
  extraSections?: React.ReactNode
  systemTags?: SystemTag[]
}

type FlowTabProps = { flows: Flow[]; loading: boolean }

type FormTabProps = {
  file: SidebarFileLike
  values?: SidebarFormValues
  editable?: boolean
  onChange?: (field: keyof SidebarFormValues, value: string) => void
  onBlur?: (field: keyof SidebarFormValues, value: string) => void
  actionsSlot?: React.ReactNode
}

type ChatTabProps = {
  comments: SidebarComment[]
  draftMessage: string
  onDraftChange: (value: string) => void
  onSubmit: () => void
  composerExtras?: React.ReactNode
  canSubmit?: boolean
}

export function formatBytes(bytes?: number, fallback?: string) {
  if (!bytes) {
    return fallback ?? "—"
  }

  const units = ["B", "KB", "MB", "GB", "TB"]
  let value = bytes
  let unitIndex = 0

  while (value >= 1024 && unitIndex < units.length - 1) {
    value /= 1024
    unitIndex += 1
  }

  return `${value.toFixed(value >= 10 || value % 1 === 0 ? 0 : 1)} ${units[unitIndex]}`
}

export function formatDate(input?: string, fallback = "Không xác định") {
  if (!input) {
    return fallback
  }

  const date = new Date(input)
  if (Number.isNaN(date.getTime())) {
    return fallback
  }

  return date.toLocaleString("vi-VN", {
    year: "numeric",
    month: "short",
    day: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  })
}

export function getExtension(name?: string) {
  if (!name) {
    return undefined
  }

  const lastDot = name.lastIndexOf(".")
  if (lastDot <= 0 || lastDot === name.length - 1) {
    return undefined
  }

  return name.slice(lastDot + 1).toLowerCase()
}

export function getTagColor(tagColor?: string | null) {
  const palette: Record<string, string> = {
    blue: "bg-blue-100 text-blue-700",
    cyan: "bg-cyan-100 text-cyan-700",
    green: "bg-green-100 text-green-700",
    yellow: "bg-yellow-100 text-yellow-700",
    purple: "bg-purple-100 text-purple-700",
    pink: "bg-pink-100 text-pink-700",
    red: "bg-red-100 text-red-700",
    orange: "bg-orange-100 text-orange-700",
  }

  return palette[tagColor ?? ""] ?? "bg-secondary text-secondary-foreground"
}

export function SidebarShell({
  children,
  activeTab,
  onTabChange,
  tabs,
  headerBadge,
}: React.PropsWithChildren<BaseSidebarProps>) {
  return (
    <Tabs value={activeTab} onValueChange={onTabChange} className="flex h-full flex-col">
      <div className="flex items-center justify-between px-4 pt-4">
        <div className="flex items-center gap-2 text-sm font-semibold">
          <Tag className="h-4 w-4 text-primary" />
          Bảng điều khiển
        </div>
        {headerBadge ? (
          <Badge variant="outline" className="text-[11px]">
            {headerBadge}
          </Badge>
        ) : null}
      </div>
      <TabsList className="grid grid-cols-4 gap-2 px-4 pb-3 pt-2">
        {tabs.info ? (
          <TabsTrigger value="info" className="flex items-center gap-1 text-xs">
            <Info className="h-3.5 w-3.5" />
            Info
          </TabsTrigger>
        ) : null}
        {tabs.flow ? (
          <TabsTrigger value="flow" className="flex items-center gap-1 text-xs">
            <GitBranch className="h-3.5 w-3.5" />
            Flow
          </TabsTrigger>
        ) : null}
        {tabs.form ? (
          <TabsTrigger value="form" className="flex items-center gap-1 text-xs">
            <NotebookPen className="h-3.5 w-3.5" />
            Form
          </TabsTrigger>
        ) : null}
        {tabs.chat ? (
          <TabsTrigger value="chat" className="flex items-center gap-1 text-xs">
            <MessageSquare className="h-3.5 w-3.5" />
            Chat
          </TabsTrigger>
        ) : null}
      </TabsList>
      <Separator />
      <div className="flex-1 overflow-y-auto px-4 pb-4 pt-3 text-sm">{children}</div>
    </Tabs>
  )
}

export function SidebarInfoTab({ file, extraSections, systemTags }: InfoTabProps) {
  const tags: DocumentTag[] = file.tags ?? []

  const statusLabel: Record<NonNullable<FileDetail["status"]>, string> = {
    draft: "Draft",
    "in-progress": "In Progress",
    completed: "Completed",
  }

  const infoItems = [
    {
      label: "Type",
      value: file.type ? file.type.charAt(0).toUpperCase() + file.type.slice(1) : "Unknown",
      icon: FileText,
    },
    {
      label: "Document Type",
      value: file.docType ?? "Document",
      icon: NotebookPen,
    },
    {
      label: "Size",
      value: formatBytes(file.sizeBytes, file.size),
      icon: HardDrive,
    },
    {
      label: "Modified",
      value: formatDate(file.modifiedAtUtc ?? file.modified),
      icon: Clock3,
    },
    {
      label: "Owner",
      value: file.owner || "Unknown",
      icon: UserRound,
    },
    {
      label: "Folder",
      value: file.folder || "All Files",
      icon: Folder,
    },
  ]

  return (
    <div className="space-y-6">
      <section className="space-y-3">
        <div className="flex items-center justify-between text-xs text-muted-foreground">
          <span>Information</span>
          {file.status ? (
            <Badge variant="outline" className="text-[11px]">
              {statusLabel[file.status] ?? file.status}
            </Badge>
          ) : null}
        </div>
        <div className="grid grid-cols-2 gap-3">
          {infoItems.map(({ label, value, icon: Icon }) => (
            <div key={label} className="space-y-2 rounded-lg border border-border/60 bg-background/60 p-3 shadow-sm">
              <div className="flex items-center gap-2 text-xs text-muted-foreground">
                <Icon className="h-3.5 w-3.5" />
                <span>{label}</span>
              </div>
              <p className="text-sm font-semibold text-foreground">{value}</p>
            </div>
          ))}
        </div>
      </section>

      <section className="space-y-2">
        <div className="flex items-center justify-between text-xs text-muted-foreground">
          <span>Tags</span>
        </div>
        {tags.length > 0 ? (
          <div className="flex flex-wrap gap-2">
            {tags.map((tag) => (
              <Badge key={tag.id} className={`text-[11px] ${getTagColor(tag.color)}`}>
                {tag.name}
              </Badge>
            ))}
          </div>
        ) : (
          <p className="text-xs text-muted-foreground">No tags added</p>
        )}
      </section>

      {systemTags?.length ? (
        <section className="space-y-2">
          <div className="flex items-center justify-between text-xs text-muted-foreground">
            <span>SYSTEM</span>
          </div>
          <div className="grid gap-2">
            {systemTags.map((tag) => (
              <div key={tag.name} className="rounded-lg border border-border/70 bg-muted/30 p-3">
                <div className="flex items-center justify-between text-xs text-muted-foreground">
                  <span className="font-semibold text-foreground">{tag.name}</span>
                  <Badge variant="outline" className="text-[10px]">
                    {tag.editable ? "Editable" : "Read only"}
                  </Badge>
                </div>
                <div className="mt-1 text-sm font-semibold text-foreground">{tag.value}</div>
              </div>
            ))}
          </div>
        </section>
      ) : null}

      {extraSections}
    </div>
  )
}

function FlowStatusBadge({ status }: { status: Flow["status"] }) {
  const tone: Record<Flow["status"], string> = {
    active: "bg-emerald-100 text-emerald-700",
    pending: "bg-amber-100 text-amber-700",
    completed: "bg-blue-100 text-blue-700",
  }

  const labels: Record<Flow["status"], string> = {
    active: "Đang chạy",
    pending: "Đang chờ",
    completed: "Hoàn tất",
  }

  return <span className={`rounded-full px-2 py-1 text-[11px] font-semibold ${tone[status]}`}>{labels[status]}</span>
}

export function SidebarFlowTab({ flows, loading }: FlowTabProps) {
  if (loading) {
    return <p className="text-sm text-muted-foreground">Đang tải luồng công việc…</p>
  }

  if (!flows.length) {
    return <p className="text-sm text-muted-foreground">Chưa có luồng công việc nào gắn với tệp này.</p>
  }

  return (
    <div className="space-y-4">
      {flows.map((flow) => (
        <div key={flow.id} className="space-y-3 rounded-xl border border-border/70 bg-muted/30 p-3">
          <div className="flex items-start justify-between gap-3">
            <div className="flex items-center gap-2">
              <GitBranch className="h-4 w-4 text-primary" />
              <div>
                <p className="text-sm font-semibold text-foreground">{flow.name}</p>
                <p className="text-xs text-muted-foreground">Cập nhật {flow.lastUpdated}</p>
              </div>
            </div>
            <div className="text-right">
              <FlowStatusBadge status={flow.status} />
              <p className="mt-1 text-[11px] text-muted-foreground">{flow.lastStep}</p>
            </div>
          </div>

          <div className="space-y-2">
            {flow.steps.map((step) => (
              <div key={step.id} className="flex gap-3 rounded-lg border border-border/60 bg-background/60 p-3">
                <ListChecks className={`h-4 w-4 ${step.iconColor}`} />
                <div className="flex-1">
                  <div className="flex items-center justify-between text-sm font-semibold text-foreground">
                    <span>{step.title}</span>
                    <span className="text-[11px] text-muted-foreground">{step.timestamp}</span>
                  </div>
                  <p className="text-sm text-muted-foreground">{step.description}</p>
                  <p className="text-[11px] text-muted-foreground">Bởi {step.user}</p>
                </div>
              </div>
            ))}
          </div>
        </div>
      ))}
    </div>
  )
}

export function SidebarFormTab({ file, values, editable = false, onChange, onBlur, actionsSlot }: FormTabProps) {
  const mergedValues: SidebarFormValues = {
    name: values?.name ?? file.name,
    owner: values?.owner ?? file.owner,
    description: values?.description ?? file.description ?? "",
    docType: values?.docType ?? file.docType ?? "Document",
    tags: values?.tags ?? file.tags?.map((tag) => tag.name).join(", ") ?? "",
    latestVersionLabel: values?.latestVersionLabel ?? file.latestVersionNumber ?? file.latestVersionId ?? "N/A",
    folder: values?.folder ?? file.folder,
    fileId: values?.fileId ?? file.id,
    type: values?.type ?? file.type,
    createdAt: values?.createdAt ?? formatDate(file.createdAtUtc),
    modifiedAt: values?.modifiedAt ?? formatDate(file.modifiedAtUtc ?? file.modified),
    status: values?.status ?? file.status ?? "Draft",
    sizeLabel: values?.sizeLabel ?? formatBytes(file.sizeBytes, file.size),
  }

  return (
    <div className="space-y-4">
      <div className="space-y-1 text-xs text-muted-foreground">
        <p>Adjust document metadata. Changes are saved for the current session.</p>
      </div>
      <div className="space-y-3">
        <div className="space-y-1">
          <label className="text-xs font-medium text-muted-foreground" htmlFor="file-name">
            File Name
          </label>
          <Input
            id="file-name"
            value={mergedValues.name}
            readOnly={!editable}
            className="bg-muted/40"
            onChange={(event) => onChange?.("name", event.target.value)}
            onBlur={(event) => onBlur?.("name", event.target.value)}
          />
        </div>
        <div className="space-y-1">
          <label className="text-xs font-medium text-muted-foreground" htmlFor="file-description">
            Description
          </label>
          <Textarea
            id="file-description"
            value={mergedValues.description ?? ""}
            readOnly={!editable}
            className="min-h-[100px] bg-muted/40"
            onChange={(event) => onChange?.("description", event.target.value)}
            onBlur={(event) => onBlur?.("description", event.target.value)}
          />
        </div>
        <div className="grid grid-cols-2 gap-3">
          <div className="space-y-1">
            <label className="text-xs font-medium text-muted-foreground" htmlFor="file-owner">
              Owner
            </label>
            <Input
              id="file-owner"
              value={mergedValues.owner ?? ""}
              readOnly={!editable}
              className="bg-muted/40"
              onChange={(event) => onChange?.("owner", event.target.value)}
              onBlur={(event) => onBlur?.("owner", event.target.value)}
            />
          </div>
          <div className="space-y-1">
            <label className="text-xs font-medium text-muted-foreground" htmlFor="file-folder">
              Folder
            </label>
            <Input
              id="file-folder"
              value={mergedValues.folder ?? ""}
              readOnly={!editable}
              className="bg-muted/40"
              onChange={(event) => onChange?.("folder", event.target.value)}
              onBlur={(event) => onBlur?.("folder", event.target.value)}
            />
          </div>
        </div>
        <div className="space-y-1">
          <label className="text-xs font-medium text-muted-foreground" htmlFor="file-doc-type">
            Document Type
          </label>
          <Input id="file-doc-type" value={mergedValues.docType ?? "Document"} readOnly className="bg-muted/40" />
        </div>
        <div className="space-y-1">
          <Label htmlFor="file-tags" className="text-xs text-muted-foreground">
            Tags
          </Label>
          <Input
            id="file-tags"
            value={mergedValues.tags ?? ""}
            readOnly={!editable}
            placeholder="Separate tags with commas"
            className="bg-muted/40"
            onChange={(event) => onChange?.("tags", event.target.value)}
            onBlur={(event) => onBlur?.("tags", event.target.value)}
          />
        </div>
        <div className="space-y-1">
          <label className="text-xs font-medium text-muted-foreground" htmlFor="file-status">
            Status
          </label>
          <select
            id="file-status"
            value={mergedValues.status}
            onChange={(event) => onChange?.("status", event.target.value)}
            onBlur={(event) => onBlur?.("status", event.target.value)}
            className="flex h-9 w-full rounded-md border border-input bg-background px-3 py-1 text-sm ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring transition-all"
          >
            <option value="draft">Draft</option>
            <option value="in-progress">In Progress</option>
            <option value="completed">Completed</option>
          </select>
        </div>
      </div>
      {actionsSlot ?? (
        <Button variant="secondary" type="button" className="w-full" disabled>
          Save changes (demo)
        </Button>
      )}
    </div>
  )
}

export function SidebarChatTab({
  comments,
  draftMessage,
  onDraftChange,
  onSubmit,
  composerExtras,
  canSubmit,
}: ChatTabProps) {
  const renderMessage = (message: string) =>
    message.split(/(@[\w.-]+)/g).map((part, index) =>
      part.startsWith("@") ? (
        <span key={`${part}-${index}`} className="font-semibold text-primary">
          {part}
        </span>
      ) : (
        <span key={`${part}-${index}`}>{part}</span>
      ),
    )

  const sendDisabled = canSubmit === undefined ? !draftMessage.trim() : !canSubmit

  const listRef = useRef<HTMLDivElement | null>(null)

  useEffect(() => {
    if (!listRef.current) return

    listRef.current.scrollTop = listRef.current.scrollHeight
  }, [comments.length])

  return (
    <div className="flex h-full flex-col gap-4">
      <div ref={listRef} className="flex-1 min-h-0 space-y-2 overflow-y-auto pr-1">
        {comments.map((comment) => (
          <div key={comment.id} className="rounded-lg border border-border/70 bg-muted/30 p-3 text-xs">
            <div className="flex items-center gap-2 text-sm font-semibold text-foreground">
              <Avatar className="h-7 w-7">
                <AvatarImage src={comment.avatar} alt={comment.author} />
                <AvatarFallback>{comment.author.slice(0, 2).toUpperCase()}</AvatarFallback>
              </Avatar>
              <div className="min-w-0">
                <div className="truncate">{comment.author}</div>
                <div className="flex items-center gap-2 text-[11px] text-muted-foreground">
                  <MessageSquare className="h-3 w-3" />
                  <span>{comment.role ?? "Bình luận"}</span>
                  <Separator orientation="vertical" className="h-3" />
                  <span>{comment.createdAt}</span>
                </div>
              </div>
            </div>
            <p className="mt-2 text-[13px] leading-relaxed text-foreground">{renderMessage(comment.message)}</p>
            {comment.attachments?.length ? (
              <div className="mt-2 space-y-1 text-[11px] text-muted-foreground">
                {comment.attachments.map((attachment) => (
                  <div key={`${comment.id}-${attachment}`} className="rounded bg-background/80 px-2 py-1">
                    {attachment}
                  </div>
                ))}
              </div>
            ) : null}
          </div>
        ))}
        {comments.length === 0 ? (
          <p className="text-sm text-muted-foreground">Chưa có trao đổi nào cho tệp này.</p>
        ) : null}
      </div>

      <div className="space-y-2 rounded-lg border border-border/60 bg-background/70 p-3">
        <Textarea
          placeholder="Thêm bình luận mới"
          value={draftMessage}
          onChange={(event) => onDraftChange(event.target.value)}
          className="min-h-[100px]"
        />
        {composerExtras}
        <div className="flex items-center justify-between text-xs text-muted-foreground">
          <span>{draftMessage.trim().length} ký tự</span>
          <Button size="sm" onClick={onSubmit} disabled={sendDisabled}>
            Gửi bình luận
          </Button>
        </div>
      </div>
    </div>
  )
}

export function SidebarTabsSkeleton() {
  return (
    <div className="space-y-4 text-sm text-muted-foreground">
      <div className="h-6 w-32 rounded bg-muted/70" />
      <div className="h-40 rounded-lg border border-dashed border-border/60 bg-muted/40" />
      <div className="h-32 rounded-lg border border-dashed border-border/60 bg-muted/30" />
    </div>
  )
}
