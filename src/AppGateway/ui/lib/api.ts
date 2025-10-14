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
} from "./types"
import { mockFiles, mockTagTree, mockFlowsByFile, mockSystemTags, mockUser } from "./mock-data"

const SIMULATED_DELAY = 800 // milliseconds

const delay = (ms: number) => new Promise((resolve) => setTimeout(resolve, ms))

export async function fetchFiles(params?: FileQueryParams): Promise<PaginatedResponse<FileItem>> {
  await delay(SIMULATED_DELAY)

  let filtered = [...mockFiles]

  // Apply search filter
  if (params?.search) {
    const searchLower = params.search.toLowerCase()
    filtered = filtered.filter(
      (file) =>
        file.name.toLowerCase().includes(searchLower) ||
        file.tags.some((tag) => tag.toLowerCase().includes(searchLower)),
    )
  }

  // Apply tag filter
  if (params?.tag) {
    filtered = filtered.filter((file) => file.tags.includes(params.tag!))
  }

  // Apply folder filter
  if (params?.folder && params.folder !== "All Files") {
    filtered = filtered.filter((file) => file.folder === params.folder)
  }

  // Apply sorting
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
        case "size":
          comparison = Number.parseFloat(a.size) - Number.parseFloat(b.size)
          break
      }
      return params.sortOrder === "desc" ? -comparison : comparison
    })
  }

  // Apply pagination
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

export async function fetchTags(): Promise<TagNode[]> {
  await delay(SIMULATED_DELAY)
  return mockTagTree
}

export async function createTag(data: TagUpdateData, parentId?: string): Promise<TagNode> {
  await delay(SIMULATED_DELAY)
  console.log("[v0] Create tag:", data, "Parent:", parentId)
  return {
    id: Date.now().toString(),
    name: data.name,
    color: data.color,
    icon: data.icon,
  }
}

export async function updateTag(tagId: string, data: TagUpdateData): Promise<TagNode> {
  await delay(SIMULATED_DELAY)
  console.log("[v0] Update tag:", tagId, data)
  return {
    id: tagId,
    name: data.name,
    color: data.color,
    icon: data.icon,
  }
}

export async function deleteTag(tagId: string): Promise<void> {
  await delay(SIMULATED_DELAY)
  console.log("[v0] Delete tag:", tagId)
}

export async function updateFile(fileId: string, data: Partial<FileItem>): Promise<FileItem> {
  await delay(SIMULATED_DELAY)
  console.log("[v0] Update file:", fileId, data)
  const file = mockFiles.find((f) => f.id === fileId)
  return { ...file!, ...data }
}

export async function fetchFlows(fileId: string): Promise<Flow[]> {
  await delay(SIMULATED_DELAY)
  return mockFlowsByFile[fileId] || mockFlowsByFile.default
}

export async function fetchSystemTags(fileId: string): Promise<SystemTag[]> {
  await delay(SIMULATED_DELAY)
  return mockSystemTags
}

export async function updateSystemTag(fileId: string, tagName: string, value: string): Promise<void> {
  await delay(SIMULATED_DELAY)
  console.log("[v0] Update system tag:", fileId, tagName, value)
}

export async function fetchUser(): Promise<User> {
  await delay(SIMULATED_DELAY)
  return mockUser
}

export async function uploadFile(data: UploadFileData): Promise<FileItem> {
  await delay(SIMULATED_DELAY)
  console.log("[v0] Upload file:", data.file.name, "Flow:", data.flowId, "Metadata:", data.metadata)

  // Create a new file item
  const newFile: FileItem = {
    id: Date.now().toString(),
    name: data.file.name,
    type: "document", // You can determine this from file extension
    size: `${(data.file.size / 1024 / 1024).toFixed(1)} MB`,
    modified: "Just now",
    tags: data.tags,
    folder: "All Files",
    owner: mockUser.name,
    description: data.metadata.description || "",
  }

  return newFile
}

export async function updateUserAvatar(file: File): Promise<string> {
  await delay(SIMULATED_DELAY)
  console.log("[v0] Upload avatar:", file.name)

  // In a real app, upload to blob storage and return URL
  // For now, create a local preview URL
  return URL.createObjectURL(file)
}

export async function signInWithAzure(): Promise<void> {
  await delay(SIMULATED_DELAY)
  console.log("[v0] Sign in with Azure AD")
  // In a real app, redirect to Azure AD OAuth flow
  // window.location.href = '/api/auth/azure'
}

export async function signInWithEmail(email: string, password: string): Promise<void> {
  await delay(SIMULATED_DELAY)
  console.log("[v0] Sign in with email:", email)
  // In a real app, call authentication API
}

export async function signUpWithAzure(): Promise<void> {
  await delay(SIMULATED_DELAY)
  console.log("[v0] Sign up with Azure AD")
  // In a real app, redirect to Azure AD OAuth flow
}

export async function signUpWithEmail(name: string, email: string, password: string): Promise<void> {
  await delay(SIMULATED_DELAY)
  console.log("[v0] Sign up with email:", name, email)
  // In a real app, call registration API
}
