"use client"

import { useMemo } from "react"
import {
  Avatar,
  AvatarFallback,
  AvatarImage,
} from "@/components/ui/avatar"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
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
} from "@/lib/types"
import {
  Calendar,
  Clock3,
  FileText,
  GitBranch,
  HardDrive,
  Info,
  ListChecks,
  MessageSquare,
  NotebookPen,
  Tag,
} from "lucide-react"

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
>

export type SidebarComment = FileComment & { attachments?: string[] }

export type SidebarFormValues = {
  name: string
  owner?: string
  description?: string | null
  latestVersionLabel?: string
  folder?: string
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

export function SidebarInfoTab({ file, extraSections }: InfoTabProps) {
  const extension = useMemo(() => getExtension(file.name), [file.name])
  const versions: FileVersion[] = file.versions ?? []
  const activity: FileActivity[] = file.activity ?? []
  const tags: DocumentTag[] = file.tags ?? []

  return (
    <div className="space-y-6">
      <section className="space-y-3">
        <div className="flex items-center justify-between text-xs text-muted-foreground">
          <span>Thông tin</span>
        </div>
        <div className="space-y-2 rounded-xl border border-border/70 bg-muted/40 p-3">
          <div className="flex items-center gap-2 text-xs text-muted-foreground">
            <HardDrive className="h-3.5 w-3.5" />
            <span>Kích thước</span>
          </div>
          <div className="text-sm font-semibold text-foreground">{formatBytes(file.sizeBytes, file.size)}</div>
          <div className="grid grid-cols-2 gap-3 text-xs text-muted-foreground">
            <div className="flex items-center gap-2">
              <Calendar className="h-3.5 w-3.5" />
              <span>{formatDate(file.createdAtUtc)}</span>
            </div>
            <div className="flex items-center gap-2">
              <Clock3 className="h-3.5 w-3.5" />
              <span>{formatDate(file.modifiedAtUtc ?? file.modified)}</span>
            </div>
          </div>
          <div className="flex items-center gap-2 text-xs text-muted-foreground">
            <FileText className="h-3.5 w-3.5" />
            <span>{extension ? `.${extension}` : "Định dạng chưa xác định"}</span>
          </div>
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
          <p className="text-xs text-muted-foreground">Chưa có tag nào</p>
        )}
      </section>

      {extraSections}

      <Separator />

      <section className="space-y-3">
        <div className="text-xs font-semibold text-muted-foreground">Phiên bản</div>
        <div className="space-y-2">
          {versions.map((version) => (
            <div key={version.id} className="rounded-lg border border-border/70 bg-muted/30 p-3 text-xs text-muted-foreground">
              <div className="flex items-center justify-between text-sm text-foreground">
                <span>{version.label}</span>
                <span className="text-[11px] text-muted-foreground">{version.size}</span>
              </div>
              <div className="mt-1 flex items-center justify-between">
                <span>{version.author}</span>
                <span>{version.createdAt}</span>
              </div>
              {version.notes ? <p className="mt-1 text-[11px] italic">{version.notes}</p> : null}
            </div>
          ))}
          {versions.length === 0 ? (
            <p className="text-xs text-muted-foreground">Chưa có phiên bản nào.</p>
          ) : null}
        </div>
      </section>

      <Separator />

      <section className="space-y-3">
        <div className="text-xs font-semibold text-muted-foreground">Hoạt động</div>
        <div className="space-y-2">
          {activity.map((item) => (
            <div key={item.id} className="rounded-lg border border-border/70 bg-muted/30 p-3 text-xs">
              <div className="flex items-center justify-between text-sm font-semibold text-foreground">
                <span>{item.action}</span>
                <span className="text-[11px] text-muted-foreground">{item.timestamp}</span>
              </div>
              <p className="text-muted-foreground">{item.actor}</p>
              {item.description ? <p className="mt-1 text-[11px] text-muted-foreground">{item.description}</p> : null}
            </div>
          ))}
          {activity.length === 0 ? (
            <p className="text-xs text-muted-foreground">Chưa có hoạt động nào.</p>
          ) : null}
        </div>
      </section>
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
    description: values?.description ?? file.description ?? "Chưa có mô tả",
    latestVersionLabel: values?.latestVersionLabel ?? file.latestVersionNumber ?? file.latestVersionId ?? "N/A",
    folder: values?.folder ?? file.folder,
  }

  return (
    <div className="space-y-4">
      <div className="space-y-1 text-xs text-muted-foreground">
        <p>Điều chỉnh nhanh thông tin tài liệu. Thay đổi sẽ được đồng bộ trong phiên hiện tại.</p>
      </div>
      <div className="space-y-3">
        <div className="space-y-1">
          <label className="text-xs font-medium text-muted-foreground" htmlFor="file-name">
            Tên tài liệu
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
          <label className="text-xs font-medium text-muted-foreground" htmlFor="file-owner">
            Chủ sở hữu
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
          <label className="text-xs font-medium text-muted-foreground" htmlFor="file-description">
            Mô tả
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
            <label className="text-xs font-medium text-muted-foreground" htmlFor="file-version">
              Phiên bản mới nhất
            </label>
            <Input
              id="file-version"
              value={mergedValues.latestVersionLabel ?? "N/A"}
              readOnly
              className="bg-muted/40"
            />
          </div>
          <div className="space-y-1">
            <label className="text-xs font-medium text-muted-foreground" htmlFor="file-folder">
              Thư mục
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
      </div>
      {actionsSlot ?? (
        <Button variant="secondary" type="button" className="w-full" disabled>
          Lưu thay đổi (demo)
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
}: ChatTabProps) {
  return (
    <div className="space-y-4">
      <div className="space-y-2">
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
            <p className="mt-2 text-[13px] leading-relaxed text-foreground">{comment.message}</p>
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
          <Button size="sm" onClick={onSubmit} disabled={!draftMessage.trim()}>
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
