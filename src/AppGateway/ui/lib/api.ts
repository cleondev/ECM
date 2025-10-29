import type {
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
  NotificationItem,
} from "./types"
import {
  mockFiles,
  mockTagTree,
  mockFlowsByFile,
  mockSystemTags,
  mockNotifications,
} from "./mock-data"
import { normalizeRedirectTarget, slugify } from "./utils"
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
  department?: string | null
  isActive?: boolean
  createdAtUtc?: string
  roles?: RoleSummaryResponse[]
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
  namespaceSlug: string
  slug: string
  path: string
  isActive: boolean
  displayName: string
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
  department?: string | null
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
  expiresAtUtc: string
  isPublic: boolean
}

type TagLabelResponse = {
  id: string
  namespaceSlug: string
  slug: string
  path: string
  isActive: boolean
  createdBy?: string | null
  createdAtUtc: string
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
  "bg-blue-200 dark:bg-blue-900",
  "bg-purple-200 dark:bg-purple-900",
  "bg-green-200 dark:bg-green-900",
  "bg-red-200 dark:bg-red-900",
  "bg-yellow-200 dark:bg-yellow-900",
  "bg-pink-200 dark:bg-pink-900",
  "bg-orange-200 dark:bg-orange-900",
  "bg-teal-200 dark:bg-teal-900",
  "bg-cyan-200 dark:bg-cyan-900",
  "bg-indigo-200 dark:bg-indigo-900",
  "bg-violet-200 dark:bg-violet-900",
  "bg-fuchsia-200 dark:bg-fuchsia-900",
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

function formatNamespaceName(slug: string): string {
  if (!slug) {
    return "Tags"
  }

  return slug
    .split(/[\s_-]+/)
    .filter(Boolean)
    .map((segment) => segment.charAt(0).toUpperCase() + segment.slice(1))
    .join(" ")
}

function buildTagTree(labels: TagLabelResponse[]): TagNode[] {
  if (!labels?.length) {
    return []
  }

  const namespaces = new Map<string, TagNode>()

  for (const label of labels) {
    const namespaceSlug = label.namespaceSlug || "user"
    if (!namespaces.has(namespaceSlug)) {
      namespaces.set(namespaceSlug, {
        id: `ns:${namespaceSlug}`,
        name: formatNamespaceName(namespaceSlug),
        color: colorForKey(namespaceSlug),
        kind: "namespace",
        namespaceSlug,
        children: [],
      })
    }

    const namespaceNode = namespaces.get(namespaceSlug)!
    const name = (label.path || label.slug || label.id || "").split("/").filter(Boolean).pop() || label.slug || label.id

    namespaceNode.children?.push({
      id: label.id,
      name,
      color: colorForKey(label.slug || label.path || label.id),
      kind: "label",
      namespaceSlug: label.namespaceSlug,
      slug: label.slug,
      path: label.path,
      isActive: label.isActive,
    })
  }

  const result = Array.from(namespaces.values()).map((namespace) => ({
    ...namespace,
    children: namespace.children
      ? [...namespace.children].sort((a, b) => a.name.localeCompare(b.name))
      : undefined,
  }))

  result.sort((a, b) => a.name.localeCompare(b.name))
  return result
}

function normalizeMockTagTree(nodes: TagNode[]): TagNode[] {
  return nodes.map((node) => {
    const children = node.children ? normalizeMockTagTree(node.children) : undefined
    const normalizedChildren = children && children.length > 0 ? children : undefined

    return {
      ...node,
      color: node.color || colorForKey(node.slug || node.path || node.name),
      kind: node.kind || (normalizedChildren ? "namespace" : "label"),
      children: normalizedChildren,
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
  const tagNames = document.tags?.map((tag) => {
    if (tag.displayName) {
      return tag.displayName
    }

    if (tag.path) {
      const segments = tag.path.split("/").filter(Boolean)
      if (segments.length > 0) {
        return segments[segments.length - 1]
      }
      return tag.path
    }

    return tag.slug || tag.id
  }) ?? []
  const updatedAtUtc = document.updatedAtUtc ?? document.createdAtUtc
  const displayModified = document.updatedAtFormatted || formatDocumentTimestamp(updatedAtUtc)
  return {
    id: document.id,
    name: document.title || "Untitled document",
    type: "document",
    size: formatFileSize(latestVersion?.bytes ?? null),
    modified: displayModified,
    modifiedAtUtc: updatedAtUtc,
    tags: tagNames,
    folder: "All Files",
    owner: document.ownerId,
    description: "",
    status: document.status?.toLowerCase() === "draft" ? "draft" : "in-progress",
    latestVersionId: latestVersion?.id,
    latestVersionNumber: latestVersion?.versionNo,
    latestVersionStorageKey: latestVersion?.storageKey,
    sizeBytes: latestVersion?.bytes,
  }
}

function mapUserSummaryToUser(profile: UserSummaryResponse): User {
  return {
    id: profile.id,
    displayName: profile.displayName,
    email: profile.email,
    department: profile.department ?? null,
    roles: profile.roles?.map((role) => role.name) ?? [],
    isActive: profile.isActive,
    createdAtUtc: profile.createdAtUtc,
  }
}

export async function fetchCurrentUserProfile(): Promise<User | null> {
  try {
    const response = await gatewayFetch("/api/iam/profile")

    if ([401, 403, 404].includes(response.status)) {
      clearCachedAuthSnapshot()
      return null
    }

    if (!response.ok) {
      throw new Error(`Failed to fetch current user profile (${response.status})`)
    }

    const data = (await response.json()) as UserSummaryResponse
    return mapUserSummaryToUser(data)
  } catch (error) {
    clearCachedAuthSnapshot()
    console.error("[ui] Không lấy được hồ sơ người dùng hiện tại:", error)
    throw error
  }
}

export type UpdateUserProfileInput = {
  displayName: string
  department?: string | null
}

export async function updateCurrentUserProfile({
  displayName,
  department,
}: UpdateUserProfileInput): Promise<User> {
  const trimmedName = displayName.trim()
  if (!trimmedName) {
    throw new Error("Display name is required")
  }

  const payload = {
    displayName: trimmedName,
    department: department?.trim() ? department.trim() : null,
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

export type CheckLoginResult = {
  isAuthenticated: boolean
  redirectPath: string
  loginUrl: string | null
  user: User | null
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
  const redirectPath = normalizeRedirectTarget(data.redirectPath, normalizedRedirect)

  const result = {
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
        redirectPath,
        user: result.user,
      })
    } else {
      clearCachedAuthSnapshot()
    }
  }

  return result
}

function filterAndPaginate(files: FileItem[], params?: FileQueryParams): PaginatedResponse<FileItem> {
  let filtered = [...files]

  if (params?.search) {
    const searchLower = params.search.toLowerCase()
    filtered = filtered.filter(
      (file) =>
        file.name.toLowerCase().includes(searchLower) ||
        file.tags.some((tag) => tag.toLowerCase().includes(searchLower)),
    )
  }

  if (params?.tagLabel) {
    const normalizedTag = params.tagLabel.toLowerCase()
    filtered = filtered.filter((file) =>
      file.tags.some((tag) => tag.toLowerCase() === normalizedTag),
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

export async function createTag(data: TagUpdateData, parent?: TagNode | string): Promise<TagNode> {
  const parentValue = typeof parent === "string" ? parent : parent?.id

  try {
    const normalizedName = data.name.trim()
    if (!normalizedName) {
      throw new Error("Tag name is required")
    }

    const namespaceSlug = parentValue?.startsWith("ns:")
      ? parentValue.slice(3)
      : parentValue && parentValue.length > 0
        ? parentValue
        : typeof parent === "object" && parent?.namespaceSlug
          ? parent.namespaceSlug
          : "user"

    const slug = slugify(normalizedName)
    if (!slug) {
      throw new Error("Tag name must include alphanumeric characters")
    }
    const payload = {
      NamespaceSlug: namespaceSlug,
      Slug: slug,
      Path: slug,
      CreatedBy: null as string | null,
    }

    const response = await gatewayRequest<TagLabelResponse>("/api/tags", {
      method: "POST",
      body: JSON.stringify(payload),
    })

    const mapped: TagNode = {
      id: response.id,
      name: normalizedName,
      color: colorForKey(response.path || response.slug),
      kind: "label",
      namespaceSlug: response.namespaceSlug,
      slug: response.slug,
      path: response.path,
      isActive: response.isActive,
      icon: data.icon,
    }

    return mapped
  } catch (error) {
    console.warn("[ui] Failed to create tag via gateway, using mock response:", error)
    return {
      id: Date.now().toString(),
      name: data.name,
      color: data.color,
      icon: data.icon,
      kind: "label",
      namespaceSlug:
        typeof parent === "object"
          ? parent.namespaceSlug
          : parentValue?.startsWith("ns:")
            ? parentValue.slice(3)
            : undefined,
      slug: slugify(data.name),
      path: data.name,
      isActive: true,
    }
  }
}

export async function updateTag(tag: TagNode, data: TagUpdateData): Promise<TagNode> {
  try {
    const normalizedName = data.name.trim()
    if (!normalizedName) {
      throw new Error("Tag name is required")
    }

    const slug = slugify(normalizedName)
    if (!slug) {
      throw new Error("Tag name must include alphanumeric characters")
    }

    const namespaceSlug = tag.namespaceSlug ?? "user"
    const payload = {
      NamespaceSlug: namespaceSlug,
      Slug: slug,
      Path: slug,
      UpdatedBy: null as string | null,
    }

    const response = await gatewayRequest<TagLabelResponse>(`/api/tags/${tag.id}`, {
      method: "PUT",
      body: JSON.stringify(payload),
    })

    return {
      id: response.id,
      name: normalizedName,
      color: colorForKey(response.path || response.slug || response.id),
      kind: "label",
      namespaceSlug: response.namespaceSlug,
      slug: response.slug,
      path: response.path,
      isActive: response.isActive,
      icon: data.icon ?? tag.icon,
    }
  } catch (error) {
    console.warn("[ui] Failed to update tag via gateway, using mock response:", error)
    return {
      ...tag,
      name: data.name,
      color: data.color,
      icon: data.icon,
      slug: slugify(data.name),
      path: data.name,
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

export async function updateFile(fileId: string, data: Partial<FileItem>): Promise<FileItem> {
  try {
    const response = await gatewayRequest<DocumentResponse>(`/api/documents/${fileId}`, {
      method: "PUT",
      body: JSON.stringify({ title: data.name }),
    })
    return mapDocumentToFileItem(response)
  } catch (error) {
    console.warn("[ui] Failed to update document via gateway, falling back to mock data:", error)
    const file = mockFiles.find((f) => f.id === fileId)
    return { ...file!, ...data }
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

export async function createShareLink(versionId: string, options: ShareOptions): Promise<ShareLink> {
  try {
    const normalizedMinutes = Number.isFinite(options.expiresInMinutes)
      ? Math.min(10080, Math.max(1, Math.round(options.expiresInMinutes)))
      : 1440

    const payload = {
      isPublic: options.isPublic,
      expiresInMinutes: normalizedMinutes,
    }

    const response = await gatewayRequest<ShareLinkResponse>(`/api/documents/files/share/${versionId}`, {
      method: "POST",
      body: JSON.stringify(payload),
    })

    return {
      url: response.url,
      expiresAtUtc: response.expiresAtUtc,
      isPublic: response.isPublic,
    }
  } catch (error) {
    console.error(`[ui] Failed to create share link for version ${versionId}:`, error)
    throw error
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
