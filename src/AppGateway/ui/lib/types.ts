export type FileItem = {
  id: string
  name: string
  type: "design" | "document" | "image" | "video" | "code"
  size: string
  modified: string
  tags: string[]
  folder: string
  thumbnail?: string
  status?: "in-progress" | "completed" | "draft"
  owner: string
  description?: string
}

export type TagNode = {
  id: string
  name: string
  color: string
  icon?: string
  children?: TagNode[]
}

export type TagUpdateData = {
  name: string
  color: string
  icon?: string
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
  department?: string | null
  roles: string[]
}

export type UploadFileData = {
  file: File
  flowId?: string
  metadata: Record<string, string>
  tags: string[]
}

export type FileQueryParams = {
  search?: string
  tag?: string
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
