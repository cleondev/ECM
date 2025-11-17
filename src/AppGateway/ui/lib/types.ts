export type DocumentTag = {
  id: string
  namespaceId: string
  namespaceDisplayName?: string | null
  namespaceScope?: TagScope
  parentId?: string | null
  name: string
  color?: string | null
  iconKey?: string | null
  sortOrder?: number | null
  pathIds?: string[]
  isActive?: boolean
  isSystem?: boolean
}

export type FileItem = {
  id: string
  name: string
  type: "design" | "document" | "image" | "video" | "code"
  size: string
  modified: string
  createdAtUtc?: string
  modifiedAtUtc?: string
  tags: DocumentTag[]
  folder: string
  thumbnail?: string
  status?: "in-progress" | "completed" | "draft"
  owner: string
  description?: string
  latestVersionId?: string
  latestVersionNumber?: number
  latestVersionStorageKey?: string
  latestVersionMimeType?: string
  latestVersionCreatedAtUtc?: string
  sizeBytes?: number
}

export type FileVersion = {
  id: string
  label: string
  size: string
  createdAt: string
  author: string
  notes?: string
}

export type FileActivity = {
  id: string
  action: string
  actor: string
  timestamp: string
  description?: string
}

export type FileComment = {
  id: string
  author: string
  avatar?: string
  message: string
  createdAt: string
  role?: string
}

export type FileDocumentPreviewPage = {
  number: number
  excerpt: string
  thumbnail?: string
}

export type FilePreview =
  | { kind: "image" | "design"; url: string; alt?: string }
  | { kind: "video"; url: string; poster?: string }
  | { kind: "document"; pages: FileDocumentPreviewPage[]; summary?: string }
  | { kind: "code"; language: string; content: string }

export type FileDetail = FileItem & {
  ownerAvatar?: string
  preview: FilePreview
  versions: FileVersion[]
  activity: FileActivity[]
  comments: FileComment[]
}

export type TagScope = "user" | "group" | "global"

export type TagNode = {
  id: string
  namespaceId: string
  name: string
  color?: string | null
  iconKey?: string | null
  sortOrder?: number | null
  parentId?: string | null
  pathIds?: string[]
  isActive?: boolean
  isSystem?: boolean
  children?: TagNode[]
  kind?: "namespace" | "label"
  namespaceLabel?: string
  namespaceScope?: TagScope
}

export type TagUpdateData = {
  name: string
  color?: string | null
  iconKey?: string | null
  applyColorToChildren?: boolean
}

export type FlowStep = {
  id: string
  title: string
  description: string
  timestamp: string
  user: string
  icon: string
  iconColor: string
}

export type Flow = {
  id: string
  name: string
  status: "active" | "pending" | "completed"
  lastUpdated: string
  lastStep: string
  steps: FlowStep[]
}

export type SystemTag = {
  name: string
  value: string
  editable: boolean
}

export type User = {
  id: string
  displayName: string
  email: string
  avatar?: string
  roles: string[]
  isActive?: boolean
  createdAtUtc?: string
  primaryGroupId?: string | null
  groupIds?: string[]
  hasPassword?: boolean
}

export type Group = {
  id: string
  name: string
  description?: string | null
}

export type NotificationItem = {
  id: string
  title: string
  description?: string
  createdAt: string
  type: "system" | "event" | "reminder" | "task" | "alert"
  isRead?: boolean
  actionUrl?: string
}

export type UploadMetadata = {
  title: string
  docType: string
  status: string
  sensitivity: string
  description?: string
  notes?: string
}

export type SelectedTag = {
  id: string
  name: string
  namespaceId?: string
}

export type FileQueryParams = {
  search?: string
  tagId?: string
  tagLabel?: string
  folder?: string
  sortBy?: "name" | "modified" | "size"
  sortOrder?: "asc" | "desc"
  page?: number
  limit?: number
}

export type PaginatedResponse<T> = {
  data: T[]
  total: number
  page: number
  limit: number
  hasMore: boolean
}

export type ShareOptions = {
  subjectType: ShareSubjectType
  subjectId: string | null
  expiresInMinutes: number
  password?: string | null
}

export type ShareLink = {
  url: string
  shortUrl: string
  expiresAtUtc: string
  subjectType: ShareSubjectType
  subjectId: string | null
  isPublic: boolean
  requiresPassword: boolean
}

export type ShareSubjectType = "public" | "user" | "group"

export type ShareLinkStatus = "Draft" | "Active" | "Expired" | "Revoked"

export type ShareQuota = {
  maxViews?: number | null
  maxDownloads?: number | null
  viewsUsed: number
  downloadsUsed: number
}

export type ShareFileDescriptor = {
  name: string
  extension?: string | null
  contentType: string
  sizeBytes: number
  createdAtUtc?: string | null
}

export type ShareInterstitial = {
  shareId: string
  code: string
  subjectType: ShareSubjectType
  status: ShareLinkStatus
  requiresPassword: boolean
  passwordValid: boolean
  canDownload: boolean
  file: ShareFileDescriptor
  quota: ShareQuota
}
