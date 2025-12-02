import type {
  DocumentTag,
  DocumentType,
  FileItem,
  FileDetail,
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
  ShareSubjectType,
  TagScope,
  WorkflowDefinition,
  WorkflowInstance,
  WorkflowInstanceStep,
} from "./types"
import {
  mockFiles,
  mockTagTree,
  mockFlowsByFile,
  mockSystemTags,
  mockGroups,
  mockUsers,
  mockFileDetails,
  createMockDetailFromFile,
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
  silentLoginUrl?: string | null
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
  namespaceScope?: string | null
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

export type DeleteFilesResult = {
  deletedIds: string[]
  failedIds: string[]
}

type ShareLinkResponse = {
  url: string
  shortUrl: string
  expiresAtUtc: string
  isPublic?: boolean
  subjectType?: unknown
  subjectId?: string | null
  requiresPassword?: boolean
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
  namespaceScope?: string | null
  scope?: string | null
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
  kind?: string | null
  role?: string | null
  parentGroupId?: string | null
}

type GroupMutationRequest = {
  name: string
  kind?: string | null
  parentGroupId?: string | null
}

type WorkflowDefinitionResponse = {
  id: string
  name: string
  spec?: string
  isActive?: boolean
  createdAtUtc?: string
  updatedAtUtc?: string
}

type WorkflowInstanceStepResponse = {
  id: string
  name: string
  assignee?: string | null
  status?: string
  createdAtUtc?: string
  updatedAtUtc?: string
  completedAtUtc?: string
  notes?: string | null
}

type WorkflowInstanceResponse = {
  id: string
  definitionId: string
  definitionName?: string
  documentId: string
  state: string
  startedAtUtc?: string
  updatedAtUtc?: string
  variables?: Record<string, unknown>
  steps?: WorkflowInstanceStepResponse[]
}

const API_BASE_URL = (process.env.NEXT_PUBLIC_GATEWAY_API_URL ?? "").replace(/\/$/, "")

function createGatewayUrl(path: string): string {
  const normalizedPath = path.startsWith("/") ? path : `/${path}`

  if (!API_BASE_URL) {
    return normalizedPath
  }

  return `${API_BASE_URL}${normalizedPath}`
}

function ensureTrailingSlash(value: string): string {
  if (!value) {
    return value
  }

  return value.endsWith("/") ? value : `${value}/`
}

function isAbsoluteUrl(value: string): boolean {
  return /^https?:\/\//i.test(value)
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

const scopePriority: Record<TagScope, number> = {
  user: 0,
  group: 1,
  global: 2,
}

function normalizeScope(value?: string | null): TagScope {
  const normalized = value?.trim().toLowerCase()

  if (!normalized) {
    return "user"
  }

  if (normalized === "global" || normalized.includes("global") || normalized.includes("tenant")) {
    return "global"
  }

  if (
    normalized === "group" ||
    normalized === "groups" ||
    normalized.includes("group") ||
    normalized.includes("team") ||
    normalized.includes("shared")
  ) {
    return "group"
  }

  return "user"
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
    namespaceScope?: string | null,
  ): TagNode => {
    const existing = namespaceNodes.get(namespaceId)
    const normalizedLabel = namespaceDisplayName?.trim()
    const normalizedScope = normalizeScope(namespaceScope)

    if (existing) {
      if (normalizedLabel && normalizedLabel !== existing.namespaceLabel) {
        existing.name = normalizedLabel
        existing.namespaceLabel = normalizedLabel
      }
      if (!existing.namespaceScope) {
        existing.namespaceScope = normalizedScope
      } else {
        const currentScope = existing.namespaceScope ?? "user"
        const currentPriority = scopePriority[currentScope]
        const nextPriority = scopePriority[normalizedScope]
        if (nextPriority > currentPriority) {
          existing.namespaceScope = normalizedScope
        }
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
      namespaceScope: normalizedScope,
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
      namespaceScope: normalizeScope(label.namespaceScope ?? label.scope),
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
      const namespaceNode = ensureNamespace(
        label.namespaceId,
        label.namespaceDisplayName,
        label.namespaceScope ?? label.scope,
      )
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
  const descriptors = [
    data.role?.trim() ? `Role: ${data.role}` : null,
    data.kind?.trim() ? `Kind: ${data.kind}` : null,
  ].filter(Boolean)

  return {
    id: data.id,
    name: data.name,
    description: descriptors.length > 0 ? descriptors.join(" • ") : null,
  }
}

function normalizeMockTagTree(nodes: TagNode[], parentScope?: TagScope): TagNode[] {
  return nodes.map((node) => {
    const scope = node.namespaceScope ?? parentScope ?? "user"
    const children = node.children ? normalizeMockTagTree(node.children, scope) : []
    return {
      ...node,
      color: node.color ?? colorForKey(node.id),
      kind: node.kind ?? (children.length > 0 ? "namespace" : "label"),
      namespaceScope: scope,
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
      namespaceScope: tag.namespaceScope ? normalizeScope(tag.namespaceScope) : undefined,
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
  const ownerId = document.ownerId
  return {
    id: document.id,
    name: document.title || "Untitled document",
    type: "document",
    docType: document.docType,
    size: formatFileSize(latestVersion?.bytes ?? null),
    modified: displayModified,
    modifiedAtUtc: updatedAtUtc,
    tags,
    folder: "All Files",
    owner: ownerId,
    ownerId,
    description: "",
    status: document.status?.toLowerCase() === "draft" ? "draft" : "in-progress",
    latestVersionId: latestVersion?.id,
    latestVersionNumber: latestVersion?.versionNo,
    latestVersionStorageKey: latestVersion?.storageKey,
    latestVersionMimeType: latestVersion?.mimeType,
    latestVersionCreatedAtUtc: latestVersion?.createdAtUtc,
    documentTypeId: document.documentTypeId ?? null,
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
      "[ui] Starting /api/iam/profile request to load current user profile (pageLocation=%s).",
      locationSnapshot,
    )
    const response = await gatewayFetch("/api/iam/profile")

    console.debug(
      "[ui] Received response from /api/iam/profile with status:",
      response.status,
    )

    if ([401, 403, 404].includes(response.status)) {
      console.warn(
        "[ui] Current user profile returned sign-in required/not found status (status = %d).",
        response.status,
      )
      clearCachedAuthSnapshot()
      return null
    }

    if (!response.ok) {
      console.error(
        "[ui] Request to /api/iam/profile failed with unexpected status:",
        response.status,
      )
      throw new Error(`Failed to fetch current user profile (${response.status})`)
    }

    const data = (await response.json()) as UserSummaryResponse
    console.debug("[ui] Current user profile loaded successfully with id:", data.id)
    return mapUserSummaryToUser(data)
  } catch (error) {
    clearCachedAuthSnapshot()
    console.error("[ui] Failed to fetch current user profile:", error)
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
  silentLoginUrl: string | null
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
    silentLoginUrl: data.silentLoginUrl ?? null,
    user: data.profile ? mapUserSummaryToUser(data.profile) : null,
  }

  if (result.isAuthenticated && !result.user) {
    console.warn(
      "[auth] Missing user profile in check-login response; treating as unauthenticated.",
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

function mapWorkflowInstance(response: WorkflowInstanceResponse): WorkflowInstance {
  return {
    id: response.id,
    definitionId: response.definitionId,
    definitionName: response.definitionName,
    documentId: response.documentId,
    state: response.state,
    startedAtUtc: response.startedAtUtc,
    updatedAtUtc: response.updatedAtUtc,
    variables: response.variables,
    steps: response.steps?.map((step) => ({
      id: step.id,
      name: step.name,
      assignee: step.assignee ?? null,
      status: step.status as WorkflowInstanceStep["status"],
      createdAtUtc: step.createdAtUtc,
      updatedAtUtc: step.updatedAtUtc,
      completedAtUtc: step.completedAtUtc,
      notes: step.notes,
    })),
  }
}

function normalizeWorkflowState(state?: string): Flow["status"] {
  const normalized = state?.toLowerCase()
  if (normalized === "completed" || normalized === "done" || normalized === "finished") {
    return "completed"
  }

  if (normalized === "active" || normalized === "running" || normalized === "in-progress") {
    return "active"
  }

  return "pending"
}

function toFlowStep(instance: WorkflowInstance, step: WorkflowInstanceStep, index: number) {
  const stepTimestamp = step.updatedAtUtc ?? step.completedAtUtc ?? step.createdAtUtc ?? instance.updatedAtUtc
  const iconColor =
    step.status === "completed"
      ? "text-emerald-500"
      : step.status === "in-progress"
        ? "text-primary"
        : "text-muted-foreground"

  return {
    id: step.id || `${instance.id}-step-${index + 1}`,
    title: step.name,
    description: step.notes ?? step.status ?? "",
    timestamp: formatDocumentTimestamp(stepTimestamp ?? instance.startedAtUtc ?? new Date().toISOString()),
    user: step.assignee || "System",
    icon: "ListChecks",
    iconColor,
  }
}

function mapWorkflowInstanceToFlow(instance: WorkflowInstance): Flow {
  const steps = instance.steps?.map((step, index) => toFlowStep(instance, step, index)) ?? []
  const lastStep = steps[0]?.title || instance.steps?.[0]?.name || ""
  const lastUpdated = instance.updatedAtUtc ?? instance.startedAtUtc ?? new Date().toISOString()
  return {
    id: instance.id,
    name: instance.definitionName || "Workflow",
    status: normalizeWorkflowState(instance.state),
    lastUpdated: formatDocumentTimestamp(lastUpdated),
    lastStep: lastStep || "Chưa có bước",
    steps,
  }
}

export async function fetchWorkflowDefinitions(params?: {
  page?: number
  pageSize?: number
  active?: boolean
}): Promise<WorkflowDefinition[]> {
  const query = new URLSearchParams()
  if (params?.page) query.set("page", params.page.toString())
  if (params?.pageSize) query.set("pageSize", params.pageSize.toString())
  if (params?.active !== undefined) query.set("active", String(params.active))

  try {
    const path = `/api/workflows/definitions${query.toString() ? `?${query}` : ""}`
    const response = await gatewayRequest<{ items?: WorkflowDefinitionResponse[]; data?: WorkflowDefinitionResponse[] }>(path)
    const definitions = response.items ?? response.data ?? []
    return definitions.map((item) => ({
      id: item.id,
      name: item.name,
      spec: item.spec,
      isActive: item.isActive,
      createdAtUtc: item.createdAtUtc,
      updatedAtUtc: item.updatedAtUtc,
    }))
  } catch (error) {
    console.warn("[ui] Failed to fetch workflow definitions:", error)
    return []
  }
}

export async function fetchWorkflowInstances(params?: {
  documentId?: string
  state?: string
  page?: number
  pageSize?: number
}): Promise<WorkflowInstance[]> {
  const query = new URLSearchParams()
  if (params?.documentId) query.set("documentId", params.documentId)
  if (params?.state) query.set("state", params.state)
  if (params?.page) query.set("page", params.page.toString())
  if (params?.pageSize) query.set("pageSize", params.pageSize.toString())

  try {
    const path = `/api/workflows/instances${query.toString() ? `?${query}` : ""}`
    const response = await gatewayRequest<{ items?: WorkflowInstanceResponse[]; data?: WorkflowInstanceResponse[] }>(path)
    const instances = response.items ?? response.data ?? []
    return instances.map(mapWorkflowInstance)
  } catch (error) {
    console.warn("[ui] Failed to fetch workflow instances:", error)
    return []
  }
}

export async function fetchWorkflowInstance(instanceId: string): Promise<WorkflowInstance | null> {
  if (!instanceId) {
    return null
  }

  try {
    const response = await gatewayRequest<WorkflowInstanceResponse>(`/api/workflows/instances/${instanceId}`)
    return mapWorkflowInstance(response)
  } catch (error) {
    console.warn(`[ui] Failed to fetch workflow instance '${instanceId}':`, error)
    return null
  }
}

export async function createWorkflowInstance(request: {
  documentId: string
  definitionId: string
  variables?: Record<string, unknown>
}): Promise<WorkflowInstance | null> {
  try {
    const response = await gatewayRequest<WorkflowInstanceResponse>(`/api/workflows/instances`, {
      method: "POST",
      body: JSON.stringify({
        documentId: request.documentId,
        definitionId: request.definitionId,
        variables: request.variables,
      }),
    })

    return mapWorkflowInstance(response)
  } catch (error) {
    console.warn("[ui] Failed to create workflow instance:", error)
    return null
  }
}

export async function cancelWorkflowInstance(instanceId: string, reason?: string): Promise<void> {
  if (!instanceId) {
    return
  }

  try {
    await gatewayRequest(`/api/workflows/instances/${instanceId}/cancel`, {
      method: "POST",
      body: JSON.stringify({ reason }),
    })
  } catch (error) {
    console.warn(`[ui] Failed to cancel workflow instance '${instanceId}':`, error)
  }
}

export async function checkLogin(returnUrl?: string): Promise<CheckLoginResult> {
  const normalizedRedirect = normalizeRedirectTarget(returnUrl, "/app/")
  const params = new URLSearchParams()

  if (normalizedRedirect) {
    params.set("returnUrl", normalizedRedirect)
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
  returnUrl?: string
}

export async function passwordLogin({
  email,
  password,
  returnUrl,
}: PasswordLoginRequest): Promise<CheckLoginResult> {
  const normalizedRedirect = normalizeRedirectTarget(returnUrl, "/app/")
  const payload = {
    email,
    password,
    returnUrl: normalizedRedirect,
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

export async function fetchFileDetails(fileId: string): Promise<FileDetail> {
  if (!fileId) {
    throw new Error("File identifier is required")
  }

  try {
    const response = await gatewayRequest<DocumentResponse>(`/api/documents/${fileId}`)
    const mapped = mapDocumentToFileItem(response)
    const existing = mockFileDetails.get(fileId)
    const detail = existing
      ? {
          ...existing,
          ...mapped,
          preview: existing.preview,
          versions: existing.versions,
          activity: existing.activity,
          comments: existing.comments,
        }
      : createMockDetailFromFile(mapped)

    mockFileDetails.set(fileId, detail)
    return detail
  } catch (error) {
    console.warn(`[ui] Failed to fetch file details for '${fileId}', falling back to mock data:`, error)

    const fallback = mockFileDetails.get(fileId)
    if (fallback) {
      return fallback
    }

    const file = mockFiles.find((item) => item.id === fileId)
    if (file) {
      const generated = createMockDetailFromFile(file)
      mockFileDetails.set(fileId, generated)
      return generated
    }

    const placeholder = createMockDetailFromFile({
      id: fileId,
      name: "Tệp được chia sẻ",
      type: "document",
      size: "0 KB",
      modified: "Vừa cập nhật",
      tags: [],
      folder: "Chia sẻ",
      owner: "Người dùng ẩn danh",
      description: "Tệp được mở từ liên kết trực tiếp khi máy chủ không trả về thông tin chi tiết.",
      latestVersionMimeType: "application/pdf",
    })
    mockFileDetails.set(fileId, placeholder)
    return placeholder

    throw (error instanceof Error ? error : new Error("Không thể tải chi tiết tệp."))
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

type DocumentTypeResponseDto = {
  id: string
  typeKey: string
  typeName: string
  isActive: boolean
  createdAtUtc: string
}

export async function fetchDocumentTypes(): Promise<DocumentType[]> {
  try {
    const response = await gatewayRequest<DocumentTypeResponseDto[]>("/api/document-types")
    if (!response?.length) {
      return []
    }

    return response
      .filter((type) => type.isActive)
      .map((type) => ({
        id: type.id,
        typeKey: type.typeKey,
        typeName: type.typeName,
        isActive: type.isActive,
        createdAtUtc: type.createdAtUtc,
      }))
      .sort((a, b) => a.typeName.localeCompare(b.typeName, undefined, { sensitivity: "base" }))
  } catch (error) {
    console.warn("[ui] Failed to fetch document types via gateway, returning empty list:", error)
    return []
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

export async function createGroup(request: GroupMutationRequest): Promise<Group> {
  const payload: GroupMutationRequest = {
    name: request.name.trim(),
    kind: request.kind?.trim() ?? null,
    parentGroupId: request.parentGroupId?.trim() || null,
  }

  try {
    const response = await gatewayRequest<GroupSummaryResponse>("/api/iam/groups", {
      method: "POST",
      body: JSON.stringify(payload),
    })

    return mapGroupSummaryToGroup(response)
  } catch (error) {
    console.warn("[ui] Failed to create group via gateway, using mock response:", error)
    const fallback: Group = {
      id: crypto.randomUUID(),
      name: payload.name || "Nhóm mới",
      description: payload.kind ? `Kind: ${payload.kind}` : null,
    }
    mockGroups.push(fallback)
    return fallback
  }
}

export async function updateGroup(id: string, request: GroupMutationRequest): Promise<Group | null> {
  const trimmedId = id.trim()
  if (!trimmedId) {
    return null
  }

  const payload: GroupMutationRequest = {
    name: request.name.trim(),
    kind: request.kind?.trim() ?? null,
    parentGroupId: request.parentGroupId?.trim() || null,
  }

  try {
    const response = await gatewayRequest<GroupSummaryResponse>(`/api/iam/groups/${trimmedId}`, {
      method: "PUT",
      body: JSON.stringify(payload),
    })

    return mapGroupSummaryToGroup(response)
  } catch (error) {
    console.warn("[ui] Failed to update group via gateway, using mock data:", error)
    const existing = mockGroups.find((group) => group.id === trimmedId)
    if (!existing) {
      return null
    }

    existing.name = payload.name || existing.name
    existing.description = payload.kind ? `Kind: ${payload.kind}` : existing.description
    return { ...existing }
  }
}

export async function deleteGroup(id: string): Promise<boolean> {
  const trimmedId = id.trim()
  if (!trimmedId) {
    return false
  }

  try {
    await gatewayRequest(`/api/iam/groups/${trimmedId}`, { method: "DELETE" })
    return true
  } catch (error) {
    console.warn("[ui] Failed to delete group via gateway, updating mock list:", error)
    const index = mockGroups.findIndex((group) => group.id === trimmedId)
    if (index >= 0) {
      mockGroups.splice(index, 1)
    }
    return false
  }
}

export async function fetchUsers(): Promise<User[]> {
  try {
    const response = await gatewayRequest<UserSummaryResponse[]>("/api/iam/users")
    if (!response?.length) {
      return mockUsers
    }

    return response.map(mapUserSummaryToUser).sort((a, b) =>
      a.displayName.localeCompare(b.displayName, undefined, { sensitivity: "base" }),
    )
  } catch (error) {
    console.warn("[ui] Failed to fetch users from gateway, using mock data:", error)
    await delay(120)
    return mockUsers
  }
}

export async function fetchUserById(userId: string): Promise<User | null> {
  const trimmed = userId.trim()
  if (!trimmed) {
    return null
  }

  try {
    const response = await gatewayRequest<UserSummaryResponse>(`/api/iam/users/${trimmed}`)
    return response ? mapUserSummaryToUser(response) : null
  } catch (error) {
    console.warn("[ui] Failed to fetch user profile via gateway, checking mock data:", error)
    return mockUsers.find((user) => user.id === trimmed) ?? null
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
      namespaceScope: normalizeScope(response.namespaceScope ?? response.scope),
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
      namespaceScope: parent?.namespaceScope ?? "user",
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
      namespaceScope: normalizeScope(response.namespaceScope ?? response.scope),
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
  documentTypeId?: string | null
}

const statusToApiStatus: Record<NonNullable<FileItem["status"]>, string> = {
  draft: "Draft",
  "in-progress": "InProgress",
  completed: "Completed",
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

  if (data.documentTypeId !== undefined) {
    payloadEntries.push(["documentTypeId", data.documentTypeId])
  }

  if (data.tags !== undefined) {
    payloadEntries.push(["tagIds", data.tags.map((tag) => tag.id)])
  }

  if (data.tagNames !== undefined) {
    payloadEntries.push(["tagNames", data.tagNames])
  }

  const payload = Object.fromEntries(payloadEntries)

  const response = await gatewayRequest<DocumentResponse>(`/api/documents/${fileId}`, {
    method: "PUT",
    body: JSON.stringify(payload),
  })

  return mapDocumentToFileItem(response)
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
    mockFileDetails.delete(fileId)
  }
}

export async function deleteFiles(fileIds: string[]): Promise<DeleteFilesResult> {
  const uniqueIds = Array.from(new Set(fileIds.filter((id) => id)))

  if (uniqueIds.length === 0) {
    return { deletedIds: [], failedIds: [] }
  }

  const deletedIds: string[] = []
  const failedIds: string[] = []

  for (const fileId of uniqueIds) {
    try {
      await deleteFile(fileId)
      deletedIds.push(fileId)
    } catch (error) {
      console.warn(`[ui] Failed to delete file '${fileId}' during batch delete:`, error)
      failedIds.push(fileId)
    }
  }

  return { deletedIds, failedIds }
}

export async function fetchFlows(fileId: string): Promise<Flow[]> {
  try {
    const workflowInstances = await fetchWorkflowInstances({ documentId: fileId })
    if (workflowInstances.length) {
      return workflowInstances.map(mapWorkflowInstanceToFlow)
    }
  } catch (error) {
    console.warn("[ui] Unable to fetch workflow instances, falling back to gateway flows:", error)
  }

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
    console.warn("[ui] Failed to fetch notifications via gateway, returning empty list:", error)
    await delay(150)
    return []
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
    console.warn("[ui] Unable to fetch user profile via /api/iam/profile:", error)
  }

  try {
    const { user } = await checkLogin()
    return user
  } catch (error) {
    console.error("[ui] Failed to retrieve user information:", error)
    throw error
  }
}

export async function searchUsers(query: string): Promise<User[]> {
  const normalized = query.trim()
  const params = new URLSearchParams()

  if (normalized) {
    params.set("search", normalized)
  }

  try {
    const response = await gatewayRequest<{ items?: User[]; data?: User[] }>(
      `/api/iam/users${params.toString() ? `?${params}` : ""}`,
    )
    return response.items ?? response.data ?? []
  } catch (error) {
    console.warn("[ui] Failed to search users via gateway, using mock users:", error)
    return mockUsers.filter((user) => {
      if (!normalized) return true
      const haystack = `${user.displayName} ${user.email}`.toLowerCase()
      return haystack.includes(normalized.toLowerCase())
    })
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

export async function signOut(returnUrl?: string): Promise<void> {
  const search = returnUrl ? `?returnUrl=${encodeURIComponent(returnUrl)}` : ""

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
      if (returnUrl) {
        window.location.href = returnUrl
      }
      return
    }

    const location = response.headers.get("Location")

    if (location) {
      window.location.href = resolveRedirectLocation(location)
      return
    }

    if (response.ok) {
      if (returnUrl) {
        window.location.href = returnUrl
      } else {
        window.location.reload()
      }
      return
    }

    throw new Error(`Failed to sign out (${response.status})`)
  } catch (error) {
    console.error("[ui] Sign-out via fetch failed; trying direct redirect:", error)

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

    const subjectType = normalizeShareSubjectType(options.subjectType)
    const normalizedSubjectId =
      subjectType === "public" ? null : normalizeShareSubjectId(options.subjectId)

    const normalizedPassword =
      typeof options.password === "string" && options.password.trim().length > 0
        ? options.password.trim()
        : undefined

    const payload = {
      documentId: file.id,
      versionId: file.latestVersionId,
      fileName: file.name,
      fileExtension: extractFileExtension(file.name),
      fileContentType: file.latestVersionMimeType ?? "application/octet-stream",
      fileSizeBytes: file.sizeBytes ?? 0,
      fileCreatedAtUtc: file.latestVersionCreatedAtUtc ?? null,
      subjectType,
      subjectId: normalizedSubjectId,
      expiresInMinutes: normalizedMinutes,
      ...(normalizedPassword ? { password: normalizedPassword } : {}),
    }

    const response = await gatewayRequest<ShareLinkResponse>(
      `/api/documents/files/share/${file.latestVersionId}`,
      {
        method: "POST",
        body: JSON.stringify(payload),
      },
    )

    const responseSubjectType = normalizeShareSubjectType(response.subjectType)
    const responseSubjectId =
      responseSubjectType === "public" ? null : normalizeShareSubjectId(response.subjectId)
    const isPublic =
      responseSubjectType === "public" ||
      (response.subjectType === undefined && Boolean(response.isPublic))

    return {
      url: normalizeShareLinkUrl(response.url),
      shortUrl: normalizeShareShortUrl(response.shortUrl),
      expiresAtUtc: response.expiresAtUtc,
      subjectType: responseSubjectType,
      subjectId: responseSubjectId,
      isPublic,
      requiresPassword: Boolean(response.requiresPassword),
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
    const parseBase = API_BASE_URL || (typeof window === "undefined" ? "http://localhost" : window.location.origin)
    const parsed = new URL(url, ensureTrailingSlash(parseBase))
    const code = extractShareCode(parsed)

    if (!code) {
      return url
    }

    const shortPath = `/s/${encodeURIComponent(code)}`

    if (API_BASE_URL) {
      return `${API_BASE_URL}${shortPath}`
    }

    if (isAbsoluteUrl(url)) {
      parsed.pathname = shortPath
      parsed.search = ""
      parsed.hash = ""
      return parsed.toString()
    }

    if (typeof window !== "undefined") {
      return `${window.location.origin}${shortPath}`
    }

    return shortPath
  } catch (error) {
    console.warn("[ui] Failed to normalize short share link URL", error)

    const fallbackCode = extractShareCodeFromString(url)
    if (fallbackCode) {
      const shortPath = `/s/${encodeURIComponent(fallbackCode)}`

      if (API_BASE_URL) {
        return `${API_BASE_URL}${shortPath}`
      }

      if (typeof window !== "undefined") {
        return `${window.location.origin}${shortPath}`
      }

      return shortPath
    }

    return url
  }
}

function normalizeShareSubjectType(raw: unknown): ShareSubjectType {
  if (typeof raw === "string") {
    const normalized = raw.trim().toLowerCase()
    if (normalized === "user") {
      return "user"
    }
    if (normalized === "group") {
      return "group"
    }
    return "public"
  }

  if (typeof raw === "number") {
    switch (raw) {
      case 0:
        return "user"
      case 1:
        return "group"
      default:
        return "public"
    }
  }

  return "public"
}

function normalizeShareSubjectId(raw: unknown): string | null {
  if (typeof raw !== "string") {
    return null
  }

  const trimmed = raw.trim()
  return trimmed.length > 0 ? trimmed : null
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
  const dto = await gatewayRequest<ShareInterstitialResponseDto>(
    `/api/share-links/${encodeShareCode(code)}${query}`,
  )
  return normalizeShareInterstitial(dto)
}

export async function verifySharePassword(code: string, password: string): Promise<boolean> {
  const response = await gatewayFetch(`/api/share-links/${encodeShareCode(code)}/password`, {
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
  const response = await gatewayRequest<SharePresignResponse>(
    `/api/share-links/${encodeShareCode(code)}/presign`,
    {
      method: "POST",
      body: JSON.stringify(payload),
    },
  )

  return response
}

export function redirectToShareDownload(code: string, password?: string): void {
  const query = password ? `?password=${encodeURIComponent(password)}` : ""
  const url = createGatewayUrl(`/api/share-links/${encodeShareCode(code)}/download${query}`)

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
