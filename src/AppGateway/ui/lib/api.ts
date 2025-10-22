import type {
  FileItem,
  TagNode,
  TagUpdateData,
  FileQueryParams,
  PaginatedResponse,
  Flow,
  SystemTag,
  User,
  UploadFileData,
  SelectedTag,
} from "./types"
import { mockFiles, mockTagTree, mockFlowsByFile, mockSystemTags, mockUser } from "./mock-data"

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
  roles?: RoleSummaryResponse[]
}

type CheckLoginResponse = {
  isAuthenticated: boolean
  loginUrl?: string | null
  profile?: (UserSummaryResponse & {
    isActive?: boolean
    createdAtUtc?: string
  }) | null
}

type DocumentVersionResponse = {
  id: string
  versionNo: number
  storageKey: string
  bytes: number
  mimeType: string
  sha256: string
  createdBy: string
  createdAtUtc: string
}

type DocumentResponse = {
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
  documentTypeId?: string | null
  latestVersion?: DocumentVersionResponse | null
}

type DocumentListResponse = {
  page: number
  pageSize: number
  totalItems: number
  totalPages: number
  items: DocumentResponse[]
}

const API_BASE_URL = (process.env.NEXT_PUBLIC_GATEWAY_API_URL ?? "").replace(/\/$/, "")

const jsonHeaders = { Accept: "application/json" }

async function gatewayFetch(path: string, init?: RequestInit) {
  const url = `${API_BASE_URL}${path.startsWith("/") ? path : `/${path}`}`
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

function mapDocumentToFileItem(document: DocumentResponse): FileItem {
  const latestVersion = document.latestVersion
  return {
    id: document.id,
    name: document.title || "Untitled document",
    type: "document",
    size: formatFileSize(latestVersion?.bytes ?? null),
    modified: new Date(document.updatedAtUtc ?? document.createdAtUtc).toISOString(),
    tags: [],
    folder: "All Files",
    owner: document.ownerId,
    description: "",
    status: document.status?.toLowerCase() === "draft" ? "draft" : "in-progress",
  }
}

function mapUserSummaryToUser(profile: UserSummaryResponse): User {
  return {
    id: profile.id,
    displayName: profile.displayName,
    email: profile.email,
    department: profile.department ?? null,
    roles: profile.roles?.map((role) => role.name) ?? [],
  }
}

export type CheckLoginResult = {
  isAuthenticated: boolean
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
  const params = new URLSearchParams()

  if (redirectUri) {
    params.set("redirectUri", redirectUri)
  }

  const search = params.toString()
  const response = await gatewayFetch(
    `/api/iam/check-login${search ? `?${search}` : ""}`,
  )

  if (!response.ok) {
    throw new Error(`Failed to check login status (${response.status})`)
  }

  const data = (await response.json()) as CheckLoginResponse

  return {
    isAuthenticated: Boolean(data.isAuthenticated),
    loginUrl: data.loginUrl ?? null,
    user: data.profile ? mapUserSummaryToUser(data.profile) : null,
  }
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

  if (params?.tag) {
    filtered = filtered.filter((file) => file.tags.includes(params.tag!))
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
          comparison = new Date(a.modified).getTime() - new Date(b.modified).getTime()
          break
        case "size": {
          const sizeA = Number.parseFloat(a.size)
          const sizeB = Number.parseFloat(b.size)
          const normalizedA = Number.isFinite(sizeA) ? sizeA : 0
          const normalizedB = Number.isFinite(sizeB) ? sizeB : 0
          comparison = normalizedA - normalizedB
          break
        }
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

export async function fetchFiles(params?: FileQueryParams): Promise<PaginatedResponse<FileItem>> {
  try {
    const searchParams = new URLSearchParams()

    if (params?.page && params.page > 0) {
      searchParams.set("page", params.page.toString())
    }

    if (params?.limit && params.limit > 0) {
      searchParams.set("pageSize", params.limit.toString())
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

export async function fetchTags(): Promise<TagNode[]> {
  try {
    const response = await gatewayRequest<TagNode[]>("/api/tags")
    return response
  } catch (error) {
    console.warn("[ui] Failed to fetch tags from gateway, using mock data:", error)
    return mockTagTree
  }
}

export async function createTag(data: TagUpdateData, parentId?: string): Promise<TagNode> {
  try {
    const payload = parentId
      ? { ...data, parentId }
      : data
    return await gatewayRequest<TagNode>("/api/tags", {
      method: "POST",
      body: JSON.stringify(payload),
    })
  } catch (error) {
    console.warn("[ui] Failed to create tag via gateway, using mock response:", error)
    return {
      id: Date.now().toString(),
      name: data.name,
      color: data.color,
      icon: data.icon,
    }
  }
}

export async function updateTag(tagId: string, data: TagUpdateData): Promise<TagNode> {
  try {
    return await gatewayRequest<TagNode>(`/api/tags/${tagId}`, {
      method: "PUT",
      body: JSON.stringify(data),
    })
  } catch (error) {
    console.warn("[ui] Failed to update tag via gateway, using mock response:", error)
    return {
      id: tagId,
      name: data.name,
      color: data.color,
      icon: data.icon,
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
  await delay(150)
  try {
    const { user } = await checkLogin()
    return user
  } catch (error) {
    console.error("[ui] Không lấy được thông tin người dùng:", error)
    throw error
  }
}

export async function uploadFile(data: UploadFileData): Promise<FileItem> {
  try {
    const { user } = await checkLogin()
    if (!user) {
      throw new Error("User must be authenticated before uploading a document")
    }

    const { metadata } = data
    const title = (metadata.title?.trim() || data.file.name || "Untitled document").slice(0, 256)
    const formData = new FormData()
    formData.append("Title", title)
    formData.append("DocType", metadata.docType?.trim() || "General")
    formData.append("Status", metadata.status?.trim() || "Draft")
    formData.append("OwnerId", user.id)
    formData.append("CreatedBy", user.id)

    if (metadata.department?.trim()) {
      formData.append("Department", metadata.department.trim())
    }

    formData.append("Sensitivity", metadata.sensitivity?.trim() || "Internal")
    formData.append("File", data.file, data.file.name)

    const document = await gatewayRequest<DocumentResponse>("/api/documents", {
      method: "POST",
      body: formData,
    })

    await assignTagsToDocument(document.id, data.tags, user.id)
    await startWorkflowForDocument(document.id, data.flowDefinition)

    return mapDocumentToFileItem(document)
  } catch (error) {
    console.error("[ui] Failed to upload document via gateway, falling back to mock behaviour:", error)

    await delay(SIMULATED_DELAY)

    const newFile: FileItem = {
      id: Date.now().toString(),
      name: data.file.name,
      type: "document",
      size: `${(data.file.size / 1024 / 1024).toFixed(1)} MB`,
      modified: "Just now",
      tags: data.tags.map((tag) => tag.name),
      folder: "All Files",
      owner: mockUser.displayName,
      description: data.metadata.description || "",
    }

    return newFile
  }
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
