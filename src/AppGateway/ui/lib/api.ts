import type {
  DocumentTag,
  FileItem,
  TagNode,
  TagUpdateData,
  FileQueryParams,
  PaginatedResponse,
  Flow,
  SystemTag,
  User,
  SelectedTag,
  ShareOptions,
  ShareLink,
  ShareInterstitial,
  NotificationItem,
  Group,
} from "./types"
import {
  mockFiles,
  mockTagTree,
  mockFlowsByFile,
  mockSystemTags,
  mockGroups,
  mockNotifications,
} from "./mock-data"
import { normalizeRedirectTarget } from "./utils"
import { clearCachedAuthSnapshot, getCachedAuthSnapshot, updateCachedAuthSnapshot } from "./auth-state"

const SIMULATED_DELAY = 800 // milliseconds

const delay = (ms: number) => new Promise((resolve) => setTimeout(resolve, ms))

type RoleSummaryResponse = {
  id: string
  name: string
}

type UserSummaryResponse = {
  id: string
  email: string
  displayName: string
  isActive?: boolean
  createdAtUtc?: string
  roles?: RoleSummaryResponse[]
  primaryGroupId?: string | null
  groupIds?: string[]
  hasPassword?: boolean
}

type CheckLoginResponse = {
  isAuthenticated: boolean
  redirectPath: string
  loginUrl?: string | null
  profile?: (UserSummaryResponse & {
    isActive?: boolean
    createdAtUtc?: string
  }) | null
}

export type DocumentVersionResponse = {
  id: string
  versionNo: number
  storageKey: string
  bytes: number
  mimeType: string
  sha256: string
  createdBy: string
  createdAtUtc: string
}

export type DocumentTagResponse = {
  id: string
  namespaceId: string
  namespaceDisplayName?: string | null
  parentId?: string | null
  name: string
  pathIds: string[]
  sortOrder: number
  color?: string | null
  iconKey?: string | null
  isActive: boolean
  isSystem: boolean
  appliedBy?: string | null
  appliedAtUtc: string
}

export type DocumentResponse = {
  id: string
  title: string
  docType: string
  status: string
  sensitivity: string
  ownerId: string
  createdBy: string
  groupId?: string | null
  createdAtUtc: string
  updatedAtUtc: string
  createdAtFormatted?: string
  updatedAtFormatted?: string
  documentTypeId?: string | null
  latestVersion?: DocumentVersionResponse | null
  tags: DocumentTagResponse[]
}

type DocumentListResponse = {
  page: number
  pageSize: number
  totalItems: number
  totalPages: number
  items: DocumentResponse[]
}

export type DocumentUploadFailure = {
  fileName: string
  message: string
}

export type DocumentBatchResponse = {
  documents: DocumentResponse[]
  failures: DocumentUploadFailure[]
}

type ShareLinkResponse = {
  url: string
  shortUrl: string
  expiresAtUtc: string
  isPublic: boolean
}

type ShareInterstitialResponseDto = ShareInterstitial

type SharePresignResponse = {
  url: string
  expiresAtUtc: string
}

type TagLabelResponse = {
  id: string
  namespaceId: string
  namespaceDisplayName?: string | null
  parentId?: string | null
  name: string
  pathIds: string[]
  sortOrder: number
  color?: string | null
  iconKey?: string | null
  isActive: boolean
  isSystem: boolean
  createdBy?: string | null
  createdAtUtc: string
}

type GroupSummaryResponse = {
  id: string
  name: string
  description?: string | null
}

const API_BASE_URL = (process.env.NEXT_PUBLIC_GATEWAY_API_URL ?? "").replace(/\/$/, "")

function createGatewayUrl(path: string): string {
  const normalizedPath = path.startsWith("/") ? path : `/${path}`

  if (!API_BASE_URL) {
    return normalizedPath
  }

  return `${API_BASE_URL}${normalizedPath}`
}

const jsonHeaders = { Accept: "application/json" }

const TAG_COLOR_PALETTE = [
  "#60A5FA",
  "#A78BFA",
  "#34D399",
  "#F87171",
  "#FBBF24",
  "#F472B6",
  "#FB923C",
  "#2DD4BF",
  "#22D3EE",
  "#6366F1",
  "#8B5CF6",
  "#EC4899",
]

function colorForKey(key: string): string {
  if (!key) {
    return TAG_COLOR_PALETTE[0]
  }

  let hash = 0
  for (let index = 0; index < key.length; index += 1) {
    hash = (hash << 5) - hash + key.charCodeAt(index)
    hash |= 0
  }

  const paletteIndex = Math.abs(hash) % TAG_COLOR_PALETTE.length
  return TAG_COLOR_PALETTE[paletteIndex]
}

function buildTagTree(labels: TagLabelResponse[]): TagNode[] {
  if (!labels?.length) {
    return []
  }

  const labelNodes = new Map<string, TagNode>()
  const namespaceNodes = new Map<string, TagNode>()
  const namespaceOrder = new Map<string, number>()

  const sortedLabels = [...labels].sort((a, b) => {
    const depthA = a.pathIds?.length ?? 0
    const depthB = b.pathIds?.length ?? 0
    if (depthA !== depthB) {
      return depthA - depthB
    }

    const sortOrderA = Number.isFinite(a.sortOrder) ? a.sortOrder : 0
    const sortOrderB = Number.isFinite(b.sortOrder) ? b.sortOrder : 0
    if (sortOrderA !== sortOrderB) {
      return sortOrderA - sortOrderB
    }

    return a.name.localeCompare(b.name, undefined, { sensitivity: "base" })
  })

  const ensureNamespace = (
    namespaceId: string,
    namespaceDisplayName?: string | null,
  ): TagNode => {
    const existing = namespaceNodes.get(namespaceId)
    const normalizedLabel = namespaceDisplayName?.trim()

    if (existing) {
      if (normalizedLabel && normalizedLabel !== existing.namespaceLabel) {
        existing.name = normalizedLabel
        existing.namespaceLabel = normalizedLabel
      }
      return existing
    }

    const index = namespaceOrder.size + 1
    const label = normalizedLabel || `Namespace ${index}`
    const namespaceNode: TagNode = {
      id: `ns:${namespaceId}`,
      namespaceId,
      name: label,
      namespaceLabel: label,
      kind: "namespace",
      color: colorForKey(namespaceId),
      isActive: true,
      isSystem: false,
      sortOrder: index,
      children: [],
    }

    namespaceOrder.set(namespaceId, index)
    namespaceNodes.set(namespaceId, namespaceNode)
    return namespaceNode
  }

  sortedLabels.forEach((label) => {
    const node: TagNode = {
      id: label.id,
      namespaceId: label.namespaceId,
      parentId: label.parentId ?? null,
      name: label.name,
      color: label.color ?? null,
      iconKey: label.iconKey ?? null,
      sortOrder: Number.isFinite(label.sortOrder) ? label.sortOrder : 0,
      pathIds: label.pathIds ?? [],
      isActive: label.isActive,
      isSystem: label.isSystem,
      kind: "label",
      children: [],
    }

    labelNodes.set(label.id, node)
  })

  const roots: TagNode[] = []

  sortedLabels.forEach((label) => {
    const node = labelNodes.get(label.id)
    if (!node) {
      return
    }

    const parentId = label.parentId
    if (parentId && labelNodes.has(parentId)) {
      const parentNode = labelNodes.get(parentId)!
      if (!parentNode.children) {
        parentNode.children = []
      }
      parentNode.children.push(node)
    } else {
      const namespaceNode = ensureNamespace(label.namespaceId, label.namespaceDisplayName)
      if (!namespaceNode.children) {
        namespaceNode.children = []
      }
      namespaceNode.children.push(node)
      if (!roots.includes(namespaceNode)) {
        roots.push(namespaceNode)
      }
    }
  })

  const sortChildren = (nodes: TagNode[]) => {
    nodes.forEach((node) => {
      if (node.children && node.children.length > 0) {
        node.children.sort((a, b) => {
          const orderA = Number.isFinite(a.sortOrder) ? a.sortOrder ?? 0 : 0
          const orderB = Number.isFinite(b.sortOrder) ? b.sortOrder ?? 0 : 0
          if (orderA !== orderB) {
            return orderA - orderB
          }
          return a.name.localeCompare(b.name, undefined, { sensitivity: "base" })
        })
        sortChildren(node.children)
      } else if (node.children) {
        node.children = []
      }
    })
  }

  sortChildren(roots)

  roots.sort((a, b) => {
    const orderA = namespaceOrder.get(a.namespaceId) ?? 0
    const orderB = namespaceOrder.get(b.namespaceId) ?? 0
    if (orderA !== orderB) {
      return orderA - orderB
    }
    return a.name.localeCompare(b.name, undefined, { sensitivity: "base" })
  })

  return roots
}

function mapGroupSummaryToGroup(data: GroupSummaryResponse): Group {
  return {
    id: data.id,
    name: data.name,
    description: data.description ?? null,
  }
}

function normalizeMockTagTree(nodes: TagNode[]): TagNode[] {
  return nodes.map((node) => {
    const children = node.children ? normalizeMockTagTree(node.children) : []
    return {
      ...node,
      color: node.color ?? colorForKey(node.id),
      kind: node.kind ?? (children.length > 0 ? "namespace" : "label"),
      children,
    }
  })
}

async function gatewayFetch(path: string, init?: RequestInit) {
  const url = createGatewayUrl(path)
  const headers = new Headers(init?.headers ?? jsonHeaders)

  if (!headers.has("Accept")) {
    headers.set("Accept", "application/json")
  }

  const body = init?.body
  if (body && !(body instanceof FormData) && !headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json")
  }

  return fetch(url, {
    ...init,
    headers,
    credentials: "include",
    cache: "no-store",
  })
}

async function gatewayRequest<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await gatewayFetch(path, init)

  if (!response.ok) {
    throw new Error(`Gateway request failed with status ${response.status}`)
  }

  if (response.status === 204) {
    return undefined as T
  }

  return (await response.json()) as T
}

function encodeShareCode(code: string): string {
  return encodeURIComponent(code.trim())
}

function formatFileSize(bytes?: number | null): string {
  if (!bytes || bytes <= 0) {
    return "--"
  }

  const kiloBytes = bytes / 1024
  if (kiloBytes < 1024) {
    return `${kiloBytes.toFixed(1)} KB`
  }

  const megaBytes = kiloBytes / 1024
  if (megaBytes < 1024) {
    return `${megaBytes.toFixed(1)} MB`
  }

  const gigaBytes = megaBytes / 1024
  return `${gigaBytes.toFixed(1)} GB`
}

function extractFileExtension(fileName: string): string | null {
  if (!fileName) {
    return null
  }

  const trimmed = fileName.trim()
  if (!trimmed) {
    return null
  }

  const lastDot = trimmed.lastIndexOf(".")
  if (lastDot === -1 || lastDot === trimmed.length - 1) {
    return null
  }

  const extension = trimmed.slice(lastDot + 1).toLowerCase()
  return extension.length > 0 ? extension : null
}

const displayDateFormatter = new Intl.DateTimeFormat("vi-VN", {
  day: "2-digit",
  month: "2-digit",
  year: "numeric",
})

const displayTimeFormatter = new Intl.DateTimeFormat("vi-VN", {
  hour: "2-digit",
  minute: "2-digit",
})

function formatDocumentTimestamp(value?: string | null): string {
  if (!value) {
    return "--"
  }

  const date = new Date(value)
  if (Number.isNaN(date.getTime())) {
    return value
  }

  const datePart = displayDateFormatter.format(date)
  const timePart = displayTimeFormatter.format(date)
  return `${datePart} ${timePart}`
}

function mapDocumentToFileItem(document: DocumentResponse): FileItem {
  const latestVersion = document.latestVersion
  const tags =
    document.tags?.map((tag) => ({
      id: tag.id,
      namespaceId: tag.namespaceId,
      namespaceDisplayName: tag.namespaceDisplayName ?? null,
      parentId: tag.parentId ?? null,
      name: tag.name,
      color: tag.color ?? null,
      iconKey: tag.iconKey ?? null,
      sortOrder: tag.sortOrder,
      pathIds: tag.pathIds ?? [],
      isActive: tag.isActive,
      isSystem: tag.isSystem,
    })) ?? []
  const updatedAtUtc = document.updatedAtUtc ?? document.createdAtUtc
  const displayModified = document.updatedAtFormatted || formatDocumentTimestamp(updatedAtUtc)
  return {
    id: document.id,
    name: document.title || "Untitled document",
    type: "document",
    size: formatFileSize(latestVersion?.bytes ?? null),
    modified: displayModified,
    modifiedAtUtc: updatedAtUtc,
    tags,
    folder: "All Files",
    owner: document.ownerId,
    description: "",
    status: document.status?.toLowerCase() === "draft" ? "draft" : "in-progress",
    latestVersionId: latestVersion?.id,
    latestVersionNumber: latestVersion?.versionNo,
    latestVersionStorageKey: latestVersion?.storageKey,
    latestVersionMimeType: latestVersion?.mimeType,
    latestVersionCreatedAtUtc: latestVersion?.createdAtUtc,
    sizeBytes: latestVersion?.bytes,
  }
}

function mapUserSummaryToUser(profile: UserSummaryResponse): User {
  const uniqueGroupIds = Array.from(new Set(profile.groupIds ?? []))
    .map((value) => value?.trim())
    .filter((value): value is string => Boolean(value))
  const primaryGroupId = profile.primaryGroupId?.trim() || uniqueGroupIds[0] || null

  if (primaryGroupId && !uniqueGroupIds.includes(primaryGroupId)) {
    uniqueGroupIds.unshift(primaryGroupId)
  }

  return {
    id: profile.id,
    displayName: profile.displayName,
    email: profile.email,
    roles: profile.roles?.map((role) => role.name) ?? [],
    isActive: profile.isActive,
    createdAtUtc: profile.createdAtUtc,
    primaryGroupId,
    groupIds: uniqueGroupIds,
    hasPassword: Boolean(profile.hasPassword),
  }
}

export async function fetchCurrentUserProfile(): Promise<User | null> {
  try {
    const locationSnapshot =
      typeof window === "undefined"
        ? "(window unavailable)"
        : `${window.location.pathname}${window.location.search}${window.location.hash}`

    console.debug(
      "[ui] Bắt đầu gọi /api/iam/profile để lấy hồ sơ người dùng hiện tại (pageLocation=%s).",
      locationSnapshot,
    )
    const response = await gatewayFetch("/api/iam/profile")

    console.debug(
      "[ui] Nhận phản hồi từ /api/iam/profile với mã trạng thái:",
      response.status,
    )

    if ([401, 403, 404].includes(response.status)) {
      console.warn(
        "[ui] Hồ sơ người dùng trả về trạng thái yêu cầu đăng nhập/không tìm thấy (status = %d).",
        response.status,
      )
      clearCachedAuthSnapshot()
      return null
    }

    if (!response.ok) {
      console.error(
        "[ui] Yêu cầu /api/iam/profile thất bại với trạng thái bất thường:",
        response.status,
      )
      throw new Error(`Failed to fetch current user profile (${response.status})`)
    }

    const data = (await response.json()) as UserSummaryResponse
    console.debug("[ui] Hồ sơ người dùng hiện tại đã được tải thành công với id:", data.id)
    return mapUserSummaryToUser(data)
  } catch (error) {
    clearCachedAuthSnapshot()
    console.error("[ui] Không lấy được hồ sơ người dùng hiện tại:", error)
    throw error
  }
}

export type UpdateUserProfileInput = {
  displayName: string
  primaryGroupId?: string | null
  groupIds?: string[]
}

export type UpdateUserPasswordInput = {
  currentPassword?: string | null
  newPassword: string
}

export async function updateCurrentUserProfile({
  displayName,
  primaryGroupId,
  groupIds,
}: UpdateUserProfileInput): Promise<User> {
  const trimmedName = displayName.trim()
  if (!trimmedName) {
    throw new Error("Display name is required")
  }

  const normalizedGroupIds = Array.from(new Set(groupIds ?? []))
    .map((value) => value?.trim())
    .filter((value): value is string => Boolean(value))
  const normalizedPrimaryGroupId = primaryGroupId?.trim() || normalizedGroupIds[0] || null

  if (normalizedPrimaryGroupId && !normalizedGroupIds.includes(normalizedPrimaryGroupId)) {
    normalizedGroupIds.unshift(normalizedPrimaryGroupId)
  }

  const payload = {
    displayName: trimmedName,
    primaryGroupId: normalizedPrimaryGroupId,
    groupIds: normalizedGroupIds,
  }

  const response = await gatewayFetch("/api/iam/profile", {
    method: "PUT",
    body: JSON.stringify(payload),
  })

  if (!response.ok) {
    throw new Error(`Failed to update profile (${response.status})`)
  }

  const data = (await response.json()) as UserSummaryResponse
  return mapUserSummaryToUser(data)
}

export async function updateCurrentUserPassword({
  currentPassword,
  newPassword,
}: UpdateUserPasswordInput): Promise<void> {
  const payload: Record<string, string> = { newPassword }
  if (currentPassword) {
    payload.currentPassword = currentPassword
  }

  const response = await gatewayFetch("/api/iam/profile/password", {
    method: "PUT",
    body: JSON.stringify(payload),
  })

  if (response.status === 204) {
    return
  }

  if (response.status === 404) {
    throw new Error("Không tìm thấy người dùng hiện tại.")
  }

  if (response.status === 400) {
    try {
      const problem = (await response.json()) as {
        errors?: Record<string, string[]>
        detail?: string
        title?: string
      }
      const messages = problem?.errors ? Object.values(problem.errors).flat() : []
      const message = messages[0] ?? problem?.detail ?? problem?.title
      if (message) {
        throw new Error(message)
      }
    } catch (error) {
      if (error instanceof Error && error.message) {
        throw error
      }
      throw new Error("Không thể cập nhật mật khẩu. Vui lòng thử lại.")
    }
  }

  if (!response.ok) {
    throw new Error(`Failed to update password (${response.status})`)
  }

  throw new Error("Không thể cập nhật mật khẩu. Vui lòng thử lại.")
}

export type CheckLoginResult = {
  isAuthenticated: boolean
  redirectPath: string
  loginUrl: string | null
  user: User | null
}

function mapCheckLoginResponse(
  data: CheckLoginResponse,
  normalizedRedirect: string,
): CheckLoginResult {
  const redirectPath = normalizeRedirectTarget(data.redirectPath, normalizedRedirect)

  const result: CheckLoginResult = {
    isAuthenticated: Boolean(data.isAuthenticated),
    redirectPath,
    loginUrl: data.loginUrl ?? null,
    user: data.profile ? mapUserSummaryToUser(data.profile) : null,
  }

  if (result.isAuthenticated && !result.user) {
    console.warn(
      "[auth] Thiếu hồ sơ người dùng trong phản hồi check-login, coi như chưa xác thực.",
    )
    result.isAuthenticated = false
    result.redirectPath = "/"
  }

  if (typeof window !== "undefined") {
    if (result.isAuthenticated) {
      updateCachedAuthSnapshot({
        isAuthenticated: true,
        redirectPath: result.redirectPath,
        user: result.user,
      })
    } else {
      clearCachedAuthSnapshot()
    }
  }

  return result
}

async function assignTagsToDocument(documentId: string, tags: SelectedTag[], userId: string) {
  if (!tags.length) {
    return
  }

  await Promise.all(
    tags.map((tag) =>
      gatewayRequest(`/api/documents/${documentId}/tags`, {
        method: "POST",
        body: JSON.stringify({ tagId: tag.id, appliedBy: userId }),
      }).catch((error) => {
        console.error(
          `[ui] Failed to assign tag '${tag.id}' to document '${documentId}':`,
          error,
        )
      }),
    ),
  )
}

async function removeTagsFromDocument(documentId: string, tagIds: string[]) {
  if (!tagIds.length) {
    return
  }

  await Promise.all(
    tagIds.map((tagId) =>
      gatewayRequest(`/api/documents/${documentId}/tags/${tagId}`, {
        method: "DELETE",
      }).catch((error) => {
        console.error(
          `[ui] Failed to remove tag '${tagId}' from document '${documentId}':`,
          error,
        )
      }),
    ),
  )
}

async function startWorkflowForDocument(documentId: string, flowDefinition?: string) {
  if (!flowDefinition) {
    return
  }

  try {
    await gatewayRequest(`/api/workflows/instances`, {
      method: "POST",
      body: JSON.stringify({ documentId, definition: flowDefinition }),
    })
  } catch (error) {
    console.error(
      `[ui] Failed to start workflow '${flowDefinition}' for document '${documentId}':`,
      error,
    )
  }
}

export async function checkLogin(redirectUri?: string): Promise<CheckLoginResult> {
  const normalizedRedirect = normalizeRedirectTarget(redirectUri, "/app/")
  const params = new URLSearchParams()

  if (normalizedRedirect) {
    params.set("redirectUri", normalizedRedirect)
  }

  const search = params.toString()
  const response = await gatewayFetch(
    `/api/iam/check-login${search ? `?${search}` : ""}`,
  )

  if (!response.ok) {
    throw new Error(`Failed to check login status (${response.status})`)
  }

  const data = (await response.json()) as CheckLoginResponse
  return mapCheckLoginResponse(data, normalizedRedirect)
}

export class PasswordLoginError extends Error {
  readonly reason: "invalid" | "unavailable" | "validation" | "unknown"
  readonly status?: number

  constructor(
    message: string,
    reason: "invalid" | "unavailable" | "validation" | "unknown",
    status?: number,
  ) {
    super(message)
    this.name = "PasswordLoginError"
    this.reason = reason
    this.status = status
  }
}

type PasswordLoginRequest = {
  email: string
  password: string
  redirectUri?: string
}

export async function passwordLogin({
  email,
  password,
  redirectUri,
}: PasswordLoginRequest): Promise<CheckLoginResult> {
  const normalizedRedirect = normalizeRedirectTarget(redirectUri, "/app/")
  const payload = {
    email,
    password,
    redirectUri: normalizedRedirect,
  }

  const response = await gatewayFetch(`/api/iam/password-login`, {
    method: "POST",
    body: JSON.stringify(payload),
  })

  if (response.status === 401) {
    throw new PasswordLoginError("Invalid email or password.", "invalid", response.status)
  }

  if (response.status === 502) {
    throw new PasswordLoginError(
      "Login service is temporarily unavailable.",
      "unavailable",
      response.status,
    )
  }

  if (response.status === 400) {
    const problem = (await response.json().catch(() => null)) as {
      errors?: Record<string, string[]>
      title?: string
      detail?: string
    } | null

    const message =
      problem?.errors && Object.values(problem.errors).length > 0
        ? Object.values(problem.errors)
            .flat()
            .join(" \n")
        : problem?.detail || problem?.title || "Invalid login request."

    throw new PasswordLoginError(message, "validation", response.status)
  }

  if (!response.ok) {
    throw new PasswordLoginError(
      `Password login request failed (${response.status})`,
      "unknown",
      response.status,
    )
  }

  const data = (await response.json()) as CheckLoginResponse
  return mapCheckLoginResponse(data, normalizedRedirect)
}

function filterAndPaginate(files: FileItem[], params?: FileQueryParams): PaginatedResponse<FileItem> {
  let filtered = [...files]

  if (params?.search) {
    const searchLower = params.search.toLowerCase()
    filtered = filtered.filter(
      (file) =>
        file.name.toLowerCase().includes(searchLower) ||
        file.tags.some((tag) => tag.name.toLowerCase().includes(searchLower)),
    )
  }

  if (params?.tagLabel) {
    const normalizedTag = params.tagLabel.toLowerCase()
    filtered = filtered.filter((file) =>
      file.tags.some((tag) => tag.name.toLowerCase() === normalizedTag),
    )
  }

  if (params?.folder && params.folder !== "All Files") {
    filtered = filtered.filter((file) => file.folder === params.folder)
  }

  if (params?.sortBy) {
    filtered.sort((a, b) => {
      let comparison = 0
      switch (params.sortBy) {
        case "name":
          comparison = a.name.localeCompare(b.name)
          break
        case "modified":
          comparison =
            getModifiedSortValue(a.modifiedAtUtc ?? a.modified) -
            getModifiedSortValue(b.modifiedAtUtc ?? b.modified)
          break
        case "size":
          comparison = getNumericSize(a.size) - getNumericSize(b.size)
          break
      }
      return params.sortOrder === "desc" ? -comparison : comparison
    })
  }

  const page = params?.page || 1
  const limit = params?.limit || 20
  const start = (page - 1) * limit
  const end = start + limit
  const paginatedData = filtered.slice(start, end)

  return {
    data: paginatedData,
    total: filtered.length,
    page,
    limit,
    hasMore: end < filtered.length,
  }
}

const RELATIVE_TIME_UNIT_IN_MS: Record<string, number> = {
  minute: 60 * 1000,
  hour: 60 * 60 * 1000,
  day: 24 * 60 * 60 * 1000,
  week: 7 * 24 * 60 * 60 * 1000,
  month: 30 * 24 * 60 * 60 * 1000,
  year: 365 * 24 * 60 * 60 * 1000,
}

function getNumericSize(value: string): number {
  const parsed = Number.parseFloat(value)
  return Number.isFinite(parsed) ? parsed : 0
}

function getModifiedSortValue(value?: string): number {
  if (!value) {
    return Number.NEGATIVE_INFINITY
  }
  const timestamp = Date.parse(value)
  if (!Number.isNaN(timestamp)) {
    return timestamp
  }

  const relativeMatch = value.match(/^(\d+)\s+(minute|hour|day|week|month|year)s?\s+ago$/i)
  if (!relativeMatch) {
    return Number.NEGATIVE_INFINITY
  }

  const quantity = Number.parseInt(relativeMatch[1], 10)
  if (!Number.isFinite(quantity) || quantity < 0) {
    return Number.NEGATIVE_INFINITY
  }

  const unit = relativeMatch[2].toLowerCase()
  const multiplier = RELATIVE_TIME_UNIT_IN_MS[unit]
  if (!multiplier) {
    return Number.NEGATIVE_INFINITY
  }

  return Date.now() - quantity * multiplier
}

export async function fetchFiles(params?: FileQueryParams): Promise<PaginatedResponse<FileItem>> {
  try {
    const searchParams = new URLSearchParams()

    if (params?.page && params.page > 0) {
      searchParams.set("page", params.page.toString())
    }

    if (params?.limit && params.limit > 0) {
      searchParams.set("pageSize", params.limit.toString())
    }

    if (params?.search) {
      const trimmed = params.search.trim()
      if (trimmed) {
        searchParams.set("q", trimmed)
      }
    }

    if (params?.tagId) {
      searchParams.append("tags[]", params.tagId)
    }

    if (params?.sortBy) {
      const sortField = resolveSortField(params.sortBy)
      const sortOrder = params.sortOrder === "asc" ? "asc" : "desc"
      searchParams.set("sort", `${sortField}:${sortOrder}`)
    }

    const query = searchParams.toString()
    const path = query ? `/api/documents?${query}` : "/api/documents"
    const response = await gatewayRequest<DocumentListResponse>(path)
    const mapped = response.items.map(mapDocumentToFileItem)

    return {
      data: mapped,
      total: response.totalItems,
      page: response.page,
      limit: response.pageSize,
      hasMore: response.page < response.totalPages,
    }
  } catch (error) {
    console.error("[ui] Failed to fetch documents from gateway, falling back to mock data:", error)
    return filterAndPaginate(mockFiles, params)
  }
}

function resolveSortField(sortBy: NonNullable<FileQueryParams["sortBy"]>): string {
  switch (sortBy) {
    case "name":
      return "name"
    case "size":
      return "size"
    case "modified":
    default:
      return "modified"
  }
}

export async function fetchTags(): Promise<TagNode[]> {
  try {
    const response = await gatewayRequest<TagLabelResponse[]>("/api/tags")
    return buildTagTree(response)
  } catch (error) {
    console.warn("[ui] Failed to fetch tags from gateway, using mock data:", error)
    return normalizeMockTagTree(mockTagTree)
  }
}

export async function fetchGroups(): Promise<Group[]> {
  try {
    const response = await gatewayRequest<GroupSummaryResponse[]>("/api/iam/groups")
    if (!response?.length) {
      return mockGroups
    }

    return response.map(mapGroupSummaryToGroup)
  } catch (error) {
    console.warn("[ui] Failed to fetch groups from gateway, using mock data:", error)
    await delay(120)
    return mockGroups
  }
}

export async function createTag(data: TagUpdateData, parent?: TagNode): Promise<TagNode> {
  try {
    const normalizedName = data.name.trim()
    if (!normalizedName) {
      throw new Error("Tag name is required")
    }

    const namespaceId = parent?.namespaceId
    if (!namespaceId) {
      throw new Error("A target namespace is required to create a tag")
    }

    const parentId = parent?.kind === "label" ? parent.id : null

    const payload = {
      NamespaceId: namespaceId,
      ParentId: parentId,
      Name: normalizedName,
      SortOrder: null as number | null,
      Color: data.color ?? null,
      IconKey: data.iconKey?.trim() ? data.iconKey.trim() : null,
      CreatedBy: null as string | null,
      IsSystem: false,
    }

    const response = await gatewayRequest<TagLabelResponse>("/api/tags", {
      method: "POST",
      body: JSON.stringify(payload),
    })

    const mapped: TagNode = {
      id: response.id,
      namespaceId: response.namespaceId,
      parentId: response.parentId ?? null,
      name: response.name,
      color: response.color ?? colorForKey(response.id),
      iconKey: response.iconKey ?? null,
      sortOrder: response.sortOrder,
      pathIds: response.pathIds ?? [],
      isActive: response.isActive,
      isSystem: response.isSystem,
      kind: "label",
      children: [],
    }

    return mapped
  } catch (error) {
    console.warn("[ui] Failed to create tag via gateway, using mock response:", error)
    return {
      id: Date.now().toString(),
      namespaceId: parent?.namespaceId ?? "",
      parentId: parent?.kind === "label" ? parent.id : null,
      name: data.name,
      color: data.color ?? colorForKey(data.name),
      iconKey: data.iconKey ?? null,
      sortOrder: null,
      pathIds: parent?.pathIds ? [...parent.pathIds, Date.now().toString()] : [],
      isActive: true,
      isSystem: false,
      kind: "label",
      children: [],
    }
  }
}

export async function updateTag(tag: TagNode, data: TagUpdateData): Promise<TagNode> {
  try {
    const normalizedName = data.name.trim()
    if (!normalizedName) {
      throw new Error("Tag name is required")
    }

    const payload = {
      NamespaceId: tag.namespaceId,
      ParentId: tag.parentId ?? null,
      Name: normalizedName,
      SortOrder: tag.sortOrder ?? null,
      Color: data.color ?? null,
      IconKey: data.iconKey?.trim() ? data.iconKey.trim() : null,
      IsActive: tag.isActive ?? true,
      UpdatedBy: null as string | null,
    }

    const response = await gatewayRequest<TagLabelResponse>(`/api/tags/${tag.id}`, {
      method: "PUT",
      body: JSON.stringify(payload),
    })

    return {
      id: response.id,
      namespaceId: response.namespaceId,
      parentId: response.parentId ?? null,
      name: response.name,
      color: response.color ?? colorForKey(response.id),
      iconKey: response.iconKey ?? null,
      sortOrder: response.sortOrder,
      pathIds: response.pathIds ?? [],
      isActive: response.isActive,
      isSystem: response.isSystem,
      kind: "label",
      children: tag.children,
    }
  } catch (error) {
    console.warn("[ui] Failed to update tag via gateway, using mock response:", error)
    return {
      ...tag,
      name: data.name,
      color: data.color ?? tag.color,
      iconKey: data.iconKey ?? tag.iconKey,
    }
  }
}

export async function deleteTag(tagId: string): Promise<void> {
  try {
    await gatewayRequest(`/api/tags/${tagId}`, {
      method: "DELETE",
    })
  } catch (error) {
    console.warn("[ui] Failed to delete tag via gateway, ignoring:", error)
  }
}

export type UpdateFileRequest = {
  name?: string
  description?: string
  owner?: string
  folder?: string
  status?: NonNullable<FileItem["status"]>
  tags?: DocumentTag[]
  tagNames?: string[]
}

const statusToApiStatus: Record<NonNullable<FileItem["status"]>, string> = {
  draft: "Draft",
  "in-progress": "InProgress",
  completed: "Completed",
}

function createDocumentTagsFromNames(
  fileId: string,
  tagNames: string[],
  existingTags: DocumentTag[] = [],
): DocumentTag[] {
  const uniqueNames = Array.from(
    new Set(
      tagNames
        .map((tag) => tag.trim())
        .filter((tag): tag is string => tag.length > 0),
    ),
  )

  return uniqueNames.map((name, index) => {
    const normalized = name.toLowerCase()
    const match = existingTags.find((tag) => tag.name.toLowerCase() === normalized)
    if (match) {
      return { ...match, name }
    }

    const slug = normalized.replace(/[^a-z0-9]+/g, "-").replace(/^-+|-+$/g, "")
    return {
      id: `${fileId}-tag-${slug || index}`,
      namespaceId: "default",
      name,
      color: null,
      iconKey: null,
      sortOrder: null,
      pathIds: [],
      isActive: true,
      isSystem: false,
    }
  })
}

export async function updateFile(fileId: string, data: UpdateFileRequest): Promise<FileItem> {
  const payloadEntries: [string, unknown][] = []

  if (data.name !== undefined) {
    payloadEntries.push(["title", data.name])
  }

  if (data.description !== undefined) {
    payloadEntries.push(["description", data.description])
  }

  if (data.owner !== undefined) {
    payloadEntries.push(["ownerId", data.owner])
  }

  if (data.folder !== undefined) {
    payloadEntries.push(["folder", data.folder])
  }

  if (data.status !== undefined) {
    payloadEntries.push(["status", statusToApiStatus[data.status]])
  }

  if (data.tags !== undefined) {
    payloadEntries.push(["tagIds", data.tags.map((tag) => tag.id)])
  }

  if (data.tagNames !== undefined) {
    payloadEntries.push(["tagNames", data.tagNames])
  }

  const payload = Object.fromEntries(payloadEntries)

  if (payloadEntries.length > 0) {
    try {
      const response = await gatewayRequest<DocumentResponse>(`/api/documents/${fileId}`, {
        method: "PUT",
        body: JSON.stringify(payload),
      })
      return mapDocumentToFileItem(response)
    } catch (error) {
      console.warn("[ui] Failed to update document via gateway, falling back to mock data:", error)
    }
  }

export async function deleteFile(fileId: string): Promise<void> {
  try {
    await gatewayRequest(`/api/documents/${fileId}`, {
      method: "DELETE",
    })
  } catch (error) {
    console.warn("[ui] Failed to delete document via gateway, using mock data:", error)
    const index = mockFiles.findIndex((file) => file.id === fileId)
    if (index === -1) {
      throw (error instanceof Error ? error : new Error("Failed to delete file."))
    }
    mockFiles.splice(index, 1)
  }
}

export async function updateFile(fileId: string, data: Partial<FileItem>): Promise<FileItem> {
  try {
    const file = mockFiles.find((f) => f.id === fileId)
    if (!file) {
      throw new Error(`File with id ${fileId} not found in mock data`)
    }

    const nextTags =
      data.tags ?? (data.tagNames ? createDocumentTagsFromNames(fileId, data.tagNames, file.tags) : undefined)

    return {
      ...file,
      ...(data.name !== undefined ? { name: data.name } : {}),
      ...(data.description !== undefined ? { description: data.description } : {}),
      ...(data.owner !== undefined ? { owner: data.owner } : {}),
      ...(data.folder !== undefined ? { folder: data.folder } : {}),
      ...(data.status !== undefined ? { status: data.status } : {}),
      ...(nextTags !== undefined ? { tags: nextTags } : {}),
    }
  } catch (error) {
    console.warn("[ui] Failed to update mock document data:", error)
    throw error
  }
}

export async function fetchFlows(fileId: string): Promise<Flow[]> {
  try {
    return await gatewayRequest<Flow[]>(`/api/documents/${fileId}/flows`)
  } catch (error) {
    console.warn("[ui] Failed to fetch flows via gateway, using mock data:", error)
    return mockFlowsByFile[fileId] || mockFlowsByFile.default
  }
}

export async function fetchSystemTags(fileId: string): Promise<SystemTag[]> {
  try {
    return await gatewayRequest<SystemTag[]>(`/api/documents/${fileId}/system-tags`)
  } catch (error) {
    console.warn("[ui] Failed to fetch system tags via gateway, using mock data:", error)
    return mockSystemTags
  }
}

export async function fetchNotifications(): Promise<NotificationItem[]> {
  try {
    const notifications = await gatewayRequest<NotificationItem[]>("/api/notifications")
    return notifications
  } catch (error) {
    console.warn("[ui] Failed to fetch notifications via gateway, using mock data:", error)
    await delay(150)
    return mockNotifications
  }
}

export async function updateSystemTag(fileId: string, tagName: string, value: string): Promise<void> {
  try {
    await gatewayRequest(`/api/documents/${fileId}/system-tags/${encodeURIComponent(tagName)}`, {
      method: "PUT",
      body: JSON.stringify({ value }),
    })
  } catch (error) {
    console.warn("[ui] Failed to update system tag via gateway, ignoring:", error)
  }
}

export async function fetchUser(): Promise<User | null> {
  const cached = getCachedAuthSnapshot()
  if (cached?.isAuthenticated && cached.user) {
    return cached.user
  }

  await delay(150)
  try {
    const profile = await fetchCurrentUserProfile()
    if (profile) {
      return profile
    }
  } catch (error) {
    console.warn("[ui] Không thể lấy hồ sơ người dùng qua /api/iam/profile:", error)
  }

  try {
    const { user } = await checkLogin()
    return user
  } catch (error) {
    console.error("[ui] Không lấy được thông tin người dùng:", error)
    throw error
  }
}

export async function applyTagsToDocument(
  documentId: string,
  tagsToAdd: SelectedTag[],
  tagIdsToRemove: string[],
): Promise<void> {
  if (!tagsToAdd.length && !tagIdsToRemove.length) {
    return
  }

  const user = await fetchUser()
  const userId = user?.id?.trim()

  if (!userId) {
    throw new Error("You must be signed in to update document tags.")
  }

  await Promise.all([
    assignTagsToDocument(documentId, tagsToAdd, userId),
    removeTagsFromDocument(documentId, tagIdsToRemove),
  ])
}

function resolveRedirectLocation(location: string): string {
  if (/^https?:\/\//i.test(location)) {
    return location
  }

  if (location.startsWith("/")) {
    return location
  }

  return `/${location}`
}

export async function signOut(redirectUri?: string): Promise<void> {
  const search = redirectUri ? `?redirectUri=${encodeURIComponent(redirectUri)}` : ""

  if (typeof window === "undefined") {
    return
  }

  clearCachedAuthSnapshot()

  try {
    const response = await gatewayFetch(`/signout${search}`, {
      method: "POST",
      redirect: "manual",
    })

    if (response.type === "opaqueredirect") {
      if (redirectUri) {
        window.location.href = redirectUri
      }
      return
    }

    const location = response.headers.get("Location")

    if (location) {
      window.location.href = resolveRedirectLocation(location)
      return
    }

    if (response.ok) {
      if (redirectUri) {
        window.location.href = redirectUri
      } else {
        window.location.reload()
      }
      return
    }

    throw new Error(`Failed to sign out (${response.status})`)
  } catch (error) {
    console.error("[ui] Đăng xuất qua fetch thất bại, thử chuyển hướng trực tiếp:", error)

    const form = document.createElement("form")
    form.method = "POST"
    form.action = createGatewayUrl(`/signout${search}`)
    form.style.display = "none"
    document.body.appendChild(form)
    form.submit()
  }
}

export async function createShareLink(file: FileItem, options: ShareOptions): Promise<ShareLink> {
  try {
    if (!file.latestVersionId) {
      throw new Error("A latest file version is required to create a share link.")
    }

    const normalizedMinutes = Number.isFinite(options.expiresInMinutes)
      ? Math.min(10080, Math.max(1, Math.round(options.expiresInMinutes)))
      : 1440

    const payload = {
      documentId: file.id,
      versionId: file.latestVersionId,
      fileName: file.name,
      fileExtension: extractFileExtension(file.name),
      fileContentType: file.latestVersionMimeType ?? "application/octet-stream",
      fileSizeBytes: file.sizeBytes ?? 0,
      fileCreatedAtUtc: file.latestVersionCreatedAtUtc ?? null,
      isPublic: options.isPublic,
      expiresInMinutes: normalizedMinutes,
    }

    const response = await gatewayRequest<ShareLinkResponse>(
      `/api/documents/files/share/${file.latestVersionId}`,
      {
        method: "POST",
        body: JSON.stringify(payload),
      },
    )

    return {
      url: normalizeShareLinkUrl(response.url),
      shortUrl: normalizeShareShortUrl(response.shortUrl),
      expiresAtUtc: response.expiresAtUtc,
      isPublic: response.isPublic,
    }
  } catch (error) {
    console.error(
      `[ui] Failed to create share link for version ${file.latestVersionId ?? "(unknown)"}:`,
      error,
    )
    throw error
  }
}

function normalizeShareLinkUrl(url: string): string {
  if (!url) {
    return url
  }

  try {
    const origin = typeof window === "undefined" ? "http://localhost" : window.location.origin
    const parsed = new URL(url, origin)
    const code = extractShareCode(parsed)

    if (!code) {
      return url
    }

    parsed.pathname = "/s/"
    parsed.search = `code=${encodeURIComponent(code)}`
    parsed.hash = ""

    if (typeof window !== "undefined" && parsed.origin === window.location.origin) {
      return `${parsed.pathname}?${parsed.searchParams.toString()}`
    }

    return parsed.toString()
  } catch (error) {
    console.warn("[ui] Failed to normalize share link URL", error)

    const fallbackCode = extractShareCodeFromString(url)
    if (fallbackCode) {
      return `/s/?code=${encodeURIComponent(fallbackCode)}`
    }

    return url
  }
}

function normalizeShareShortUrl(url: string): string {
  if (!url) {
    return url
  }

  try {
    const origin = typeof window === "undefined" ? "http://localhost" : window.location.origin
    const parsed = new URL(url, origin)
    const code = extractShareCode(parsed)

    if (!code) {
      return url
    }

    parsed.pathname = `/s/${encodeURIComponent(code)}`
    parsed.search = ""
    parsed.hash = ""

    if (typeof window !== "undefined" && parsed.origin === window.location.origin) {
      return parsed.pathname
    }

    return parsed.toString()
  } catch (error) {
    console.warn("[ui] Failed to normalize short share link URL", error)

    const fallbackCode = extractShareCodeFromString(url)
    if (fallbackCode) {
      return `/s/${encodeURIComponent(fallbackCode)}`
    }

    return url
  }
}

function extractShareCode(parsed: URL): string | null {
  const queryCode = parsed.searchParams.get("code")
  if (queryCode) {
    return queryCode
  }

  const segments = parsed.pathname.split("/").filter(Boolean)
  if (segments.length >= 2 && segments[0] === "s") {
    return segments[1]
  }

  return null
}

function extractShareCodeFromString(raw: string): string | null {
  const pathMatch = raw.match(/\/s\/([^/?#]+)/)
  if (pathMatch?.[1]) {
    return decodeURIComponent(pathMatch[1])
  }

  const queryMatch = raw.match(/[?&]code=([^&#]+)/)
  if (queryMatch?.[1]) {
    return decodeURIComponent(queryMatch[1])
  }

  return null
}

function normalizeShareInterstitial(dto: ShareInterstitialResponseDto): ShareInterstitial {
  return {
    ...dto,
    file: {
      ...dto.file,
      extension: dto.file.extension ?? null,
      createdAtUtc: dto.file.createdAtUtc ?? null,
    },
    quota: {
      maxViews: dto.quota?.maxViews ?? null,
      maxDownloads: dto.quota?.maxDownloads ?? null,
      viewsUsed: dto.quota?.viewsUsed ?? 0,
      downloadsUsed: dto.quota?.downloadsUsed ?? 0,
    },
  }
}

export async function fetchShareInterstitial(code: string, password?: string): Promise<ShareInterstitial> {
  const query = password ? `?password=${encodeURIComponent(password)}` : ""
  const dto = await gatewayRequest<ShareInterstitialResponseDto>(`/s/${encodeShareCode(code)}${query}`)
  return normalizeShareInterstitial(dto)
}

export async function verifySharePassword(code: string, password: string): Promise<boolean> {
  const response = await gatewayFetch(`/s/${encodeShareCode(code)}/password`, {
    method: "POST",
    body: JSON.stringify({ password }),
  })

  if (response.ok) {
    return true
  }

  if (response.status === 400 || response.status === 403) {
    return false
  }

  if (response.status === 404) {
    throw new Error("Share link not found")
  }

  throw new Error(`Failed to verify password (status ${response.status})`)
}

export async function requestShareDownloadLink(code: string, password?: string): Promise<SharePresignResponse> {
  const payload = password ? { password } : {}
  const response = await gatewayRequest<SharePresignResponse>(`/s/${encodeShareCode(code)}/presign`, {
    method: "POST",
    body: JSON.stringify(payload),
  })

  return response
}

export function redirectToShareDownload(code: string, password?: string): void {
  const query = password ? `?password=${encodeURIComponent(password)}` : ""
  const url = createGatewayUrl(`/s/${encodeShareCode(code)}/download${query}`)

  if (typeof window !== "undefined") {
    window.location.href = url
  }
}

export function buildDocumentDownloadUrl(versionId: string): string {
  return createGatewayUrl(`/api/documents/files/download/${versionId}`)
}

export async function updateUserAvatar(file: File): Promise<string> {
  await delay(SIMULATED_DELAY)
  console.log("[v0] Upload avatar:", file.name)

  return URL.createObjectURL(file)
}

export async function signInWithAzure(): Promise<void> {
  await delay(SIMULATED_DELAY)
  console.log("[v0] Sign in with Azure AD")
}

export async function signInWithEmail(email: string, password: string): Promise<void> {
  await delay(SIMULATED_DELAY)
  console.log("[v0] Sign in with email:", email)
}

export async function signUpWithAzure(): Promise<void> {
  await delay(SIMULATED_DELAY)
  console.log("[v0] Sign up with Azure AD")
}

export async function signUpWithEmail(name: string, email: string, password: string): Promise<void> {
  await delay(SIMULATED_DELAY)
  console.log("[v0] Sign up with email:", name, email)
}
