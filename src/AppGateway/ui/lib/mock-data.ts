import type {
  FileItem,
  TagNode,
  Flow,
  SystemTag,
  User,
  Group,
  DocumentTag,
  FileDetail,
  FileComment,
  FilePreview,
  FileVersion,
  FileActivity,
} from "./types"

const PERSONAL_NAMESPACE_ID = "00000000-0000-0000-0000-000000000001"

const MOCK_TAG_COLORS = [
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
]

let mockColorIndex = 0
let mockTagSequence = 0

function nextMockColor(): string {
  const color = MOCK_TAG_COLORS[mockColorIndex % MOCK_TAG_COLORS.length]
  mockColorIndex += 1
  return color
}

function createMockTag(id: string, name: string, color: string): DocumentTag {
  return {
    id,
    namespaceId: PERSONAL_NAMESPACE_ID,
    parentId: null,
    name,
    color,
    iconKey: null,
    sortOrder: null,
    pathIds: [id],
    isActive: true,
    isSystem: false,
  }
}

function createMockTagForName(name: string): DocumentTag {
  mockTagSequence += 1
  const normalizedName = name.trim() || `Tag ${mockTagSequence}`
  return createMockTag(`mock-tag-${mockTagSequence}`, normalizedName, nextMockColor())
}

export const mockFiles: FileItem[] = [
  {
    id: "1",
    name: "Dashboard Design v2",
    type: "design",
    size: "2.4 MB",
    modified: "2 hours ago",
    tags: [createMockTagForName("UI"), createMockTagForName("Dashboard")],
    folder: "Projects",
    status: "in-progress",
    owner: "John Doe",
    description: "Updated dashboard design with new metrics",
  },
  {
    id: "2",
    name: "Landing Page Mockup",
    type: "design",
    size: "3.1 MB",
    modified: "5 hours ago",
    tags: [createMockTagForName("Marketing"), createMockTagForName("Web")],
    folder: "Projects",
    status: "completed",
    owner: "Jane Smith",
    description: "Marketing landing page for Q1 campaign",
  },
  {
    id: "3",
    name: "Brand Guidelines.pdf",
    type: "document",
    size: "1.8 MB",
    modified: "1 day ago",
    tags: [createMockTagForName("Brand"), createMockTagForName("Documentation")],
    folder: "Documents",
    status: "completed",
    owner: "Sarah Johnson",
    description: "Company brand guidelines and style guide",
  },
  {
    id: "4",
    name: "Hero Image.png",
    type: "image",
    size: "4.2 MB",
    modified: "3 days ago",
    tags: [createMockTagForName("Assets"), createMockTagForName("Marketing")],
    folder: "Images",
    owner: "Mike Wilson",
    description: "Hero section background image",
  },
  {
    id: "5",
    name: "Product Demo.mp4",
    type: "video",
    size: "45.6 MB",
    modified: "1 week ago",
    tags: [createMockTagForName("Video"), createMockTagForName("Demo")],
    folder: "Videos",
    status: "completed",
    owner: "Emily Brown",
    description: "Product demonstration video for sales team",
  },
  {
    id: "6",
    name: "Component Library",
    type: "code",
    size: "856 KB",
    modified: "2 days ago",
    tags: [createMockTagForName("Development"), createMockTagForName("UI")],
    folder: "Code",
    status: "in-progress",
    owner: "Alex Chen",
    description: "Reusable React component library",
  },
  {
    id: "7",
    name: "Mobile App Wireframes",
    type: "design",
    size: "1.9 MB",
    modified: "4 hours ago",
    tags: [createMockTagForName("Mobile"), createMockTagForName("UX")],
    folder: "Projects",
    status: "draft",
    owner: "John Doe",
    description: "Initial wireframes for mobile application",
  },
  {
    id: "8",
    name: "User Research Report",
    type: "document",
    size: "3.5 MB",
    modified: "2 weeks ago",
    tags: [createMockTagForName("Research"), createMockTagForName("UX")],
    folder: "Documents",
    status: "completed",
    owner: "Sarah Johnson",
    description: "Q4 user research findings and insights",
  },
  {
    id: "9",
    name: "E-commerce Dashboard",
    type: "design",
    size: "5.2 MB",
    modified: "3 hours ago",
    tags: [
      createMockTagForName("UI"),
      createMockTagForName("Dashboard"),
      createMockTagForName("Work"),
    ],
    folder: "Projects",
    status: "in-progress",
    owner: "John Doe",
    description: "E-commerce analytics dashboard design",
  },
  {
    id: "10",
    name: "Social Media Assets",
    type: "image",
    size: "12.3 MB",
    modified: "6 hours ago",
    tags: [
      createMockTagForName("Marketing"),
      createMockTagForName("Assets"),
      createMockTagForName("Brand"),
    ],
    folder: "Images",
    owner: "Jane Smith",
    description: "Social media post templates and graphics",
  },
  {
    id: "11",
    name: "API Documentation",
    type: "document",
    size: "2.1 MB",
    modified: "4 days ago",
    tags: [createMockTagForName("Development"), createMockTagForName("Documentation")],
    folder: "Documents",
    status: "completed",
    owner: "Alex Chen",
    description: "REST API documentation for developers",
  },
  {
    id: "12",
    name: "Onboarding Flow",
    type: "design",
    size: "3.8 MB",
    modified: "1 day ago",
    tags: [
      createMockTagForName("UX"),
      createMockTagForName("Mobile"),
      createMockTagForName("UI"),
    ],
    folder: "Projects",
    status: "draft",
    owner: "Sarah Johnson",
    description: "User onboarding flow for mobile app",
  },
  {
    id: "13",
    name: "Product Launch Video",
    type: "video",
    size: "89.4 MB",
    modified: "5 days ago",
    tags: [
      createMockTagForName("Video"),
      createMockTagForName("Marketing"),
      createMockTagForName("Demo"),
    ],
    folder: "Videos",
    status: "completed",
    owner: "Emily Brown",
    description: "Product launch announcement video",
  },
  {
    id: "14",
    name: "Design System Tokens",
    type: "code",
    size: "124 KB",
    modified: "1 week ago",
    tags: [
      createMockTagForName("Development"),
      createMockTagForName("UI"),
      createMockTagForName("Work"),
    ],
    folder: "Code",
    status: "completed",
    owner: "Alex Chen",
    description: "Design tokens for consistent styling",
  },
  {
    id: "15",
    name: "Customer Feedback Analysis",
    type: "document",
    size: "4.7 MB",
    modified: "3 weeks ago",
    tags: [createMockTagForName("Research"), createMockTagForName("Documentation")],
    folder: "Documents",
    status: "completed",
    owner: "Sarah Johnson",
    description: "Analysis of customer feedback from Q3",
  },
  {
    id: "16",
    name: "Icon Set v3",
    type: "image",
    size: "2.8 MB",
    modified: "2 days ago",
    tags: [
      createMockTagForName("Assets"),
      createMockTagForName("UI"),
      createMockTagForName("Brand"),
    ],
    folder: "Images",
    owner: "Mike Wilson",
    description: "Updated icon set for the design system",
  },
  {
    id: "17",
    name: "Marketing Campaign Brief",
    type: "document",
    size: "1.2 MB",
    modified: "1 day ago",
    tags: [createMockTagForName("Marketing"), createMockTagForName("Documentation")],
    folder: "Documents",
    status: "in-progress",
    owner: "Jane Smith",
    description: "Q2 marketing campaign strategy and brief",
  },
  {
    id: "18",
    name: "Prototype Animation",
    type: "video",
    size: "23.5 MB",
    modified: "4 hours ago",
    tags: [
      createMockTagForName("Demo"),
      createMockTagForName("UX"),
      createMockTagForName("Video"),
    ],
    folder: "Videos",
    status: "draft",
    owner: "John Doe",
    description: "Animated prototype for stakeholder presentation",
  },
  {
    id: "19",
    name: "Payment Gateway Integration",
    type: "code",
    size: "456 KB",
    modified: "3 hours ago",
    tags: [createMockTagForName("Development"), createMockTagForName("Backend")],
    folder: "Code",
    status: "in-progress",
    owner: "Alex Chen",
    description: "Stripe payment integration module",
  },
  {
    id: "20",
    name: "User Persona Research",
    type: "document",
    size: "2.9 MB",
    modified: "2 days ago",
    tags: [createMockTagForName("Research"), createMockTagForName("UX")],
    folder: "Documents",
    status: "completed",
    owner: "Sarah Johnson",
    description: "Detailed user persona analysis",
  },
  {
    id: "21",
    name: "Mobile Navigation Prototype",
    type: "design",
    size: "1.7 MB",
    modified: "5 hours ago",
    tags: [
      createMockTagForName("Mobile"),
      createMockTagForName("UI"),
      createMockTagForName("UX"),
    ],
    folder: "Projects",
    status: "draft",
    owner: "John Doe",
    description: "Mobile app navigation patterns",
  },
  {
    id: "22",
    name: "Product Photography",
    type: "image",
    size: "18.4 MB",
    modified: "1 week ago",
    tags: [createMockTagForName("Assets"), createMockTagForName("Marketing")],
    folder: "Images",
    owner: "Mike Wilson",
    description: "Professional product photos for catalog",
  },
  {
    id: "23",
    name: "Tutorial Series Part 1",
    type: "video",
    size: "67.8 MB",
    modified: "4 days ago",
    tags: [
      createMockTagForName("Video"),
      createMockTagForName("Demo"),
      createMockTagForName("Education"),
    ],
    folder: "Videos",
    status: "completed",
    owner: "Emily Brown",
    description: "First episode of tutorial series",
  },
  {
    id: "24",
    name: "Authentication Module",
    type: "code",
    size: "234 KB",
    modified: "1 day ago",
    tags: [createMockTagForName("Development"), createMockTagForName("Security")],
    folder: "Code",
    status: "in-progress",
    owner: "Alex Chen",
    description: "JWT authentication implementation",
  },
  {
    id: "25",
    name: "Competitor Analysis",
    type: "document",
    size: "5.3 MB",
    modified: "1 week ago",
    tags: [createMockTagForName("Research"), createMockTagForName("Marketing")],
    folder: "Documents",
    status: "completed",
    owner: "Jane Smith",
    description: "Market competitor analysis report",
  },
  {
    id: "26",
    name: "Settings Panel Design",
    type: "design",
    size: "2.2 MB",
    modified: "6 hours ago",
    tags: [createMockTagForName("UI"), createMockTagForName("Dashboard")],
    folder: "Projects",
    status: "in-progress",
    owner: "John Doe",
    description: "User settings interface design",
  },
  {
    id: "27",
    name: "Logo Variations",
    type: "image",
    size: "3.6 MB",
    modified: "3 days ago",
    tags: [createMockTagForName("Brand"), createMockTagForName("Assets")],
    folder: "Images",
    owner: "Mike Wilson",
    description: "Logo in different formats and colors",
  },
  {
    id: "28",
    name: "Feature Walkthrough",
    type: "video",
    size: "34.2 MB",
    modified: "2 days ago",
    tags: [createMockTagForName("Demo"), createMockTagForName("Video")],
    folder: "Videos",
    status: "draft",
    owner: "Emily Brown",
    description: "New feature demonstration video",
  },
  {
    id: "29",
    name: "Database Schema",
    type: "code",
    size: "89 KB",
    modified: "5 days ago",
    tags: [createMockTagForName("Development"), createMockTagForName("Backend")],
    folder: "Code",
    status: "completed",
    owner: "Alex Chen",
    description: "PostgreSQL database schema design",
  },
  {
    id: "30",
    name: "Accessibility Audit",
    type: "document",
    size: "1.4 MB",
    modified: "1 week ago",
    tags: [createMockTagForName("Documentation"), createMockTagForName("UX")],
    folder: "Documents",
    status: "completed",
    owner: "Sarah Johnson",
    description: "WCAG compliance audit report",
  },
  {
    id: "31",
    name: "Checkout Flow Redesign",
    type: "design",
    size: "4.1 MB",
    modified: "8 hours ago",
    tags: [
      createMockTagForName("UI"),
      createMockTagForName("UX"),
      createMockTagForName("Work"),
    ],
    folder: "Projects",
    status: "in-progress",
    owner: "John Doe",
    description: "Improved checkout user experience",
  },
  {
    id: "32",
    name: "Email Templates",
    type: "image",
    size: "2.7 MB",
    modified: "4 days ago",
    tags: [createMockTagForName("Marketing"), createMockTagForName("Assets")],
    folder: "Images",
    owner: "Jane Smith",
    description: "Transactional email designs",
  },
  {
    id: "33",
    name: "Customer Testimonials",
    type: "video",
    size: "56.3 MB",
    modified: "1 week ago",
    tags: [createMockTagForName("Marketing"), createMockTagForName("Video")],
    folder: "Videos",
    status: "completed",
    owner: "Emily Brown",
    description: "Customer success stories compilation",
  },
  {
    id: "34",
    name: "API Rate Limiter",
    type: "code",
    size: "67 KB",
    modified: "2 days ago",
    tags: [createMockTagForName("Development"), createMockTagForName("Backend")],
    folder: "Code",
    status: "completed",
    owner: "Alex Chen",
    description: "Rate limiting middleware implementation",
  },
  {
    id: "35",
    name: "Sprint Planning Notes",
    type: "document",
    size: "892 KB",
    modified: "3 hours ago",
    tags: [createMockTagForName("Documentation"), createMockTagForName("Work")],
    folder: "Documents",
    status: "in-progress",
    owner: "Sarah Johnson",
    description: "Sprint 24 planning and objectives",
  },
  {
    id: "36",
    name: "Dark Mode Theme",
    type: "design",
    size: "3.3 MB",
    modified: "1 day ago",
    tags: [createMockTagForName("UI"), createMockTagForName("Brand")],
    folder: "Projects",
    status: "draft",
    owner: "John Doe",
    description: "Dark theme color palette and components",
  },
  {
    id: "37",
    name: "Infographic Assets",
    type: "image",
    size: "8.9 MB",
    modified: "5 days ago",
    tags: [createMockTagForName("Marketing"), createMockTagForName("Assets")],
    folder: "Images",
    owner: "Mike Wilson",
    description: "Data visualization graphics",
  },
  {
    id: "38",
    name: "Platform Overview",
    type: "video",
    size: "42.1 MB",
    modified: "3 days ago",
    tags: [createMockTagForName("Demo"), createMockTagForName("Marketing")],
    folder: "Videos",
    status: "completed",
    owner: "Emily Brown",
    description: "Platform capabilities overview video",
  },
  {
    id: "39",
    name: "WebSocket Handler",
    type: "code",
    size: "178 KB",
    modified: "6 hours ago",
    tags: [createMockTagForName("Development"), createMockTagForName("Backend")],
    folder: "Code",
    status: "in-progress",
    owner: "Alex Chen",
    description: "Real-time communication handler",
  },
  {
    id: "40",
    name: "Q1 Performance Report",
    type: "document",
    size: "6.2 MB",
    modified: "2 weeks ago",
    tags: [createMockTagForName("Documentation"), createMockTagForName("Research")],
    folder: "Documents",
    status: "completed",
    owner: "Jane Smith",
    description: "Quarterly performance metrics and analysis",
  },
  {
    id: "41",
    name: "Project Assets.zip",
    type: "document",
    size: "156.4 MB",
    modified: "5 days ago",
    tags: [createMockTagForName("Archive"), createMockTagForName("Assets")],
    folder: "Projects",
    status: "completed",
    owner: "John Doe",
    description: "Compressed archive containing shared project resources",
  },
  {
    id: "42",
    name: "Executive Briefing.pptx",
    type: "document",
    size: "12.8 MB",
    modified: "3 days ago",
    tags: [createMockTagForName("Presentation"), createMockTagForName("Management")],
    folder: "Documents",
    status: "in-progress",
    owner: "Jane Smith",
    description: "Quarterly executive summary presentation deck",
  },
]

const SAMPLE_PREVIEW_IMAGES = [
  "https://images.unsplash.com/photo-1500530855697-b586d89ba3ee?auto=format&fit=crop&w=1600&q=80",
  "https://images.unsplash.com/photo-1469474968028-56623f02e42e?auto=format&fit=crop&w=1600&q=80",
  "https://images.unsplash.com/photo-1489515217757-5fd1be406fef?auto=format&fit=crop&w=1600&q=80",
  "https://images.unsplash.com/photo-1472289065668-ce650ac443d2?auto=format&fit=crop&w=1600&q=80",
  "https://images.unsplash.com/photo-1448932223592-d1fc686e76ea?auto=format&fit=crop&w=1600&q=80",
]

const SAMPLE_VIDEO_SOURCES = [
  "https://interactive-examples.mdn.mozilla.net/media/cc0-videos/flower.mp4",
  "https://interactive-examples.mdn.mozilla.net/media/cc0-videos/beer.mp4",
  "https://interactive-examples.mdn.mozilla.net/media/cc0-videos/paint.webm",
]

const SAMPLE_DOCUMENT_EXCERPTS = [
  "1. Tổng quan dự án với các chỉ số chính và phạm vi triển khai",
  "2. Tóm tắt hành trình người dùng và các điểm nghẽn cần cải thiện",
  "3. Phân tích định lượng dựa trên dữ liệu tương tác 90 ngày",
  "4. Đề xuất thiết kế với nguyên tắc accessibility WCAG 2.2",
  "5. Kế hoạch rollout gồm 3 giai đoạn với tiêu chí đo lường cụ thể",
]

const SAMPLE_DOCUMENT_SUMMARIES = [
  "Bản xem trước gồm 24 trang trình bày tiến độ triển khai sản phẩm trong quý gần nhất.",
  "Tài liệu trình bày insight chính từ chuỗi nghiên cứu người dùng và đề xuất tối ưu hóa.",
  "Slide deck cập nhật KPI, lộ trình tính năng và trạng thái các phê duyệt quan trọng.",
]

const SAMPLE_CODE_SNIPPETS = [
  `export async function runDataSync(jobId: string) {
  const job = await jobRepository.get(jobId)
  if (!job) throw new Error('Job not found')

  const result = await queue.dispatch({
    topic: 'data-sync',
    payload: job.payload,
  })

  return { status: 'queued', id: result.id }
}`,
  `type AuditLogEntry = {
  id: string
  action: string
  actor: string
  timestamp: string
}

export function formatAuditLog(entries: AuditLogEntry[]) {
  return entries
    .map((entry) => \`\${entry.timestamp} - \${entry.actor}: \${entry.action}\`)
    .join('\n')
}`,
]

const SAMPLE_COMMENT_MESSAGES = [
  "Giao diện xem trước hoạt động mượt, có thể bổ sung thêm trạng thái khi tài liệu đang xử lý?",
  "Tôi đã để lại ghi chú trong phiên bản mới nhất, nhờ team kiểm tra lại phong cách typography.",
  "Hãy giữ lại chart so sánh hiệu suất cũ, stakeholder vẫn cần phần đó cho buổi review tuần này.",
]

const SAMPLE_ACTIVITY_ACTIONS = [
  "đã cập nhật metadata",
  "đã chia sẻ liên kết",
  "đã tải xuống",
  "đã ghi chú phiên bản",
  "đã gắn thẻ phê duyệt",
]

const SAMPLE_VERSION_NOTES = [
  "Điều chỉnh màu sắc cho phù hợp brand guideline mới",
  "Thêm biểu đồ thể hiện tốc độ tăng trưởng người dùng",
  "Chuẩn hóa spacing và cấu trúc heading",
  "Bổ sung phụ lục với số liệu khảo sát",
]

const SAMPLE_ACTORS = [
  "Mai Anh",
  "Ngọc Hà",
  "Tuấn Phạm",
  "Lan Nguyễn",
  "Huy Bùi",
  "Trung Kiên",
  "Bảo Vy",
]

function pickFromArray<T>(items: T[], seed: number, offset = 0): T {
  if (!items.length) {
    throw new Error("Expected non-empty sample array")
  }

  const index = Math.abs(seed + offset) % items.length
  return items[index]!
}

function createPreviewForFile(file: FileItem, seed: number): FilePreview {
  const baseImage = pickFromArray(SAMPLE_PREVIEW_IMAGES, seed)

  switch (file.type) {
    case "image":
      return { kind: "image", url: baseImage, alt: file.name }
    case "design":
      return { kind: "design", url: baseImage, alt: file.name }
    case "video":
      return {
        kind: "video",
        url: pickFromArray(SAMPLE_VIDEO_SOURCES, seed),
        poster: pickFromArray(SAMPLE_PREVIEW_IMAGES, seed, 2),
      }
    case "code":
      return {
        kind: "code",
        language: seed % 2 === 0 ? "tsx" : "json",
        content: pickFromArray(SAMPLE_CODE_SNIPPETS, seed),
      }
    case "document":
      return {
        kind: "document",
        summary: pickFromArray(SAMPLE_DOCUMENT_SUMMARIES, seed),
        pages: Array.from({ length: 3 }, (_, index) => ({
          number: index + 1,
          excerpt: pickFromArray(SAMPLE_DOCUMENT_EXCERPTS, seed, index),
          thumbnail: pickFromArray(SAMPLE_PREVIEW_IMAGES, seed, index + 3),
        })),
      }
    default:
      return { kind: "image", url: baseImage, alt: file.name }
  }
}

function createMockVersions(file: FileItem, seed: number): FileVersion[] {
  const versionCount = Math.max(2, (seed % 4) + 2)
  const now = Date.now()

  return Array.from({ length: versionCount }, (_, index) => {
    const versionNumber = versionCount - index
    const createdAt = new Date(now - index * 12 * 60 * 60 * 1000).toISOString()

    return {
      id: `${file.id}-v${versionNumber}`,
      label: `Phiên bản ${versionNumber}`,
      size: file.size,
      createdAt,
      author: pickFromArray(SAMPLE_ACTORS, seed, index),
      notes: pickFromArray(SAMPLE_VERSION_NOTES, seed, index),
    }
  })
}

function createMockActivity(file: FileItem, seed: number): FileActivity[] {
  const now = Date.now()

  return Array.from({ length: 4 }, (_, index) => {
    const actor = pickFromArray(SAMPLE_ACTORS, seed, index + 1)
    const action = pickFromArray(SAMPLE_ACTIVITY_ACTIONS, seed, index)

    return {
      id: `${file.id}-activity-${index + 1}`,
      action,
      actor,
      timestamp: new Date(now - (index + 1) * 60 * 60 * 1000).toISOString(),
      description: `${actor} ${action} cho "${file.name}"`,
    }
  })
}

function createMockComments(file: FileItem, seed: number): FileComment[] {
  return Array.from({ length: 3 }, (_, index) => {
    const author = pickFromArray(SAMPLE_ACTORS, seed, index + 2)
    return {
      id: `${file.id}-comment-${index + 1}`,
      author,
      avatar: `https://api.dicebear.com/8.x/initials/svg?seed=${encodeURIComponent(author)}`,
      message: pickFromArray(SAMPLE_COMMENT_MESSAGES, seed, index),
      createdAt: new Date(Date.now() - (index + 1) * 90 * 60 * 1000).toISOString(),
      role: index === 0 ? "Owner" : index === 1 ? "Reviewer" : "Collaborator",
    }
  })
}

export function createMockDetailFromFile(file: FileItem, seed = 0): FileDetail {
  const preview = createPreviewForFile(file, seed)
  const versions = createMockVersions(file, seed)
  const activity = createMockActivity(file, seed)
  const comments = createMockComments(file, seed)
  const latestVersion = versions[0]
  const createdAtUtc =
    file.createdAtUtc ?? new Date(Date.now() - (seed + 2) * 24 * 60 * 60 * 1000).toISOString()
  const modifiedAtUtc = file.modifiedAtUtc ?? new Date(Date.now() - seed * 6 * 60 * 60 * 1000).toISOString()

  const latestVersionNumber = latestVersion
    ? Number.parseInt(latestVersion.label.replace(/[^0-9]/g, ""), 10) || versions.length
    : file.latestVersionNumber

  return {
    ...file,
    createdAtUtc,
    modifiedAtUtc,
    ownerAvatar: `https://api.dicebear.com/8.x/initials/svg?seed=${encodeURIComponent(file.owner)}`,
    preview,
    versions,
    activity,
    comments,
    latestVersionId: file.latestVersionId ?? latestVersion?.id,
    latestVersionNumber,
    latestVersionCreatedAtUtc: file.latestVersionCreatedAtUtc ?? latestVersion?.createdAt,
  }
}

export const mockFileDetails = new Map<string, FileDetail>(
  mockFiles.map((file, index) => [file.id, createMockDetailFromFile(file, index)]),
)

export const mockTagTree: TagNode[] = [
  {
    id: `ns:${PERSONAL_NAMESPACE_ID}`,
    namespaceId: PERSONAL_NAMESPACE_ID,
    name: "Personal Tags",
    namespaceLabel: "Personal Tags",
    color: "#60A5FA",
    kind: "namespace",
    isActive: true,
    isSystem: false,
    sortOrder: 0,
    children: [
      {
        id: "mock-tree-work",
        namespaceId: PERSONAL_NAMESPACE_ID,
        parentId: null,
        name: "Work",
        color: "#60A5FA",
        iconKey: "💼",
        sortOrder: 0,
        pathIds: ["mock-tree-work"],
        isActive: true,
        isSystem: false,
        kind: "label",
        children: [
          {
            id: "mock-tree-work-ui",
            namespaceId: PERSONAL_NAMESPACE_ID,
            parentId: "mock-tree-work",
            name: "UI",
            color: "#A78BFA",
            iconKey: "🎨",
            sortOrder: 0,
            pathIds: ["mock-tree-work", "mock-tree-work-ui"],
            isActive: true,
            isSystem: false,
            kind: "label",
            children: [
              {
                id: "mock-tree-work-ui-frontend",
                namespaceId: PERSONAL_NAMESPACE_ID,
                parentId: "mock-tree-work-ui",
                name: "Frontend",
                color: "#34D399",
                iconKey: "🧩",
                sortOrder: 0,
                pathIds: [
                  "mock-tree-work",
                  "mock-tree-work-ui",
                  "mock-tree-work-ui-frontend",
                ],
                isActive: true,
                isSystem: false,
                kind: "label",
              },
            ],
          },
          {
            id: "mock-tree-work-dashboard",
            namespaceId: PERSONAL_NAMESPACE_ID,
            parentId: "mock-tree-work",
            name: "Dashboard",
            color: "#6366F1",
            iconKey: "📊",
            sortOrder: 1,
            pathIds: ["mock-tree-work", "mock-tree-work-dashboard"],
            isActive: true,
            isSystem: false,
            kind: "label",
          },
        ],
      },
      {
        id: "mock-tree-marketing",
        namespaceId: PERSONAL_NAMESPACE_ID,
        parentId: null,
        name: "Marketing",
        color: "#FB923C",
        iconKey: "📢",
        sortOrder: 1,
        pathIds: ["mock-tree-marketing"],
        isActive: true,
        isSystem: false,
        kind: "label",
        children: [
          {
            id: "mock-tree-marketing-web",
            namespaceId: PERSONAL_NAMESPACE_ID,
            parentId: "mock-tree-marketing",
            name: "Web",
            color: "#22D3EE",
            iconKey: "🌐",
            sortOrder: 0,
            pathIds: ["mock-tree-marketing", "mock-tree-marketing-web"],
            isActive: true,
            isSystem: false,
            kind: "label",
          },
          {
            id: "mock-tree-marketing-brand",
            namespaceId: PERSONAL_NAMESPACE_ID,
            parentId: "mock-tree-marketing",
            name: "Brand",
            color: "#F472B6",
            iconKey: "✨",
            sortOrder: 1,
            pathIds: ["mock-tree-marketing", "mock-tree-marketing-brand"],
            isActive: true,
            isSystem: false,
            kind: "label",
          },
        ],
      },
      {
        id: "mock-tree-documentation",
        namespaceId: PERSONAL_NAMESPACE_ID,
        parentId: null,
        name: "Documentation",
        color: "#FBBF24",
        iconKey: "📝",
        sortOrder: 2,
        pathIds: ["mock-tree-documentation"],
        isActive: true,
        isSystem: false,
        kind: "label",
        children: [
          {
            id: "mock-tree-documentation-research",
            namespaceId: PERSONAL_NAMESPACE_ID,
            parentId: "mock-tree-documentation",
            name: "Research",
            color: "#EC4899",
            iconKey: "🔬",
            sortOrder: 0,
            pathIds: [
              "mock-tree-documentation",
              "mock-tree-documentation-research",
            ],
            isActive: true,
            isSystem: false,
            kind: "label",
          },
        ],
      },
      {
        id: "mock-tree-media",
        namespaceId: PERSONAL_NAMESPACE_ID,
        parentId: null,
        name: "Media",
        color: "#F87171",
        iconKey: "🎬",
        sortOrder: 3,
        pathIds: ["mock-tree-media"],
        isActive: true,
        isSystem: false,
        kind: "label",
        children: [
          {
            id: "mock-tree-media-video",
            namespaceId: PERSONAL_NAMESPACE_ID,
            parentId: "mock-tree-media",
            name: "Video",
            color: "#FB7185",
            iconKey: "🎥",
            sortOrder: 0,
            pathIds: ["mock-tree-media", "mock-tree-media-video"],
            isActive: true,
            isSystem: false,
            kind: "label",
          },
        ],
      },
    ],
  },
  {
    id: "ns:team-shared",
    namespaceId: "team-shared",
    name: "Team Tags",
    namespaceLabel: "Team Tags",
    color: "#8B5CF6",
    kind: "namespace",
    namespaceScope: "group",
    isActive: true,
    isSystem: false,
    sortOrder: 1,
    children: [
      {
        id: "team-shared-collab",
        namespaceId: "team-shared",
        parentId: null,
        name: "Collaboration",
        color: "#34D399",
        iconKey: "🤝",
        sortOrder: 0,
        pathIds: ["team-shared-collab"],
        isActive: true,
        isSystem: false,
        kind: "label",
        namespaceScope: "group",
      },
      {
        id: "team-shared-planning",
        namespaceId: "team-shared",
        parentId: null,
        name: "Planning",
        color: "#60A5FA",
        iconKey: "🗓️",
        sortOrder: 1,
        pathIds: ["team-shared-planning"],
        isActive: true,
        isSystem: false,
        kind: "label",
        namespaceScope: "group",
      },
    ],
  },
  {
    id: "ns:org-shared",
    namespaceId: "org-shared",
    name: "Organization Tags",
    namespaceLabel: "Organization Tags",
    color: "#34D399",
    kind: "namespace",
    namespaceScope: "global",
    isActive: true,
    isSystem: false,
    sortOrder: 2,
    children: [
      {
        id: "org-shared-compliance",
        namespaceId: "org-shared",
        parentId: null,
        name: "Compliance",
        color: "#F97316",
        iconKey: "🛡️",
        sortOrder: 0,
        pathIds: ["org-shared-compliance"],
        isActive: true,
        isSystem: false,
        kind: "label",
        namespaceScope: "global",
      },
      {
        id: "org-shared-branding",
        namespaceId: "org-shared",
        parentId: null,
        name: "Branding",
        color: "#F59E0B",
        iconKey: "🏷️",
        sortOrder: 1,
        pathIds: ["org-shared-branding"],
        isActive: true,
        isSystem: false,
        kind: "label",
        namespaceScope: "global",
      },
    ],
  },
]

export const mockFlowsByFile: Record<string, Flow[]> = {
  "1": [
    {
      id: "flow-1-1",
      name: "Design Review",
      status: "active",
      lastUpdated: "2 hours ago",
      lastStep: "Feedback received",
      steps: [
        {
          id: "step-1",
          title: "Feedback received",
          description: "Design team provided feedback on v2",
          timestamp: "2 hours ago",
          user: "Design Team",
          icon: "User",
          iconColor: "text-primary",
        },
        {
          id: "step-2",
          title: "Review requested",
          description: "Submitted dashboard v2 for review",
          timestamp: "1 day ago",
          user: "John Doe",
          icon: "GitBranch",
          iconColor: "text-blue-500",
        },
      ],
    },
    {
      id: "flow-1-2",
      name: "Version Control",
      status: "completed",
      lastUpdated: "3 days ago",
      lastStep: "Version 2 created",
      steps: [
        {
          id: "step-3",
          title: "Version 2 created",
          description: "Created new version with updated metrics",
          timestamp: "3 days ago",
          user: "John Doe",
          icon: "FileText",
          iconColor: "text-green-500",
        },
        {
          id: "step-4",
          title: "Version 1 archived",
          description: "Moved v1 to archive folder",
          timestamp: "3 days ago",
          user: "John Doe",
          icon: "FolderOpen",
          iconColor: "text-gray-500",
        },
      ],
    },
  ],
  "2": [
    {
      id: "flow-2-1",
      name: "Approval Process",
      status: "completed",
      lastUpdated: "5 hours ago",
      lastStep: "Approved by stakeholders",
      steps: [
        {
          id: "step-5",
          title: "Approved by stakeholders",
          description: "Final approval for Q1 campaign",
          timestamp: "5 hours ago",
          user: "Marketing Team",
          icon: "User",
          iconColor: "text-green-500",
        },
        {
          id: "step-6",
          title: "Submitted for approval",
          description: "Landing page mockup sent to stakeholders",
          timestamp: "1 day ago",
          user: "Jane Smith",
          icon: "GitBranch",
          iconColor: "text-blue-500",
        },
      ],
    },
  ],
  default: [
    {
      id: "flow-default-1",
      name: "File History",
      status: "active",
      lastUpdated: "1 hour ago",
      lastStep: "File accessed",
      steps: [
        {
          id: "step-7",
          title: "File accessed",
          description: "Opened for viewing",
          timestamp: "1 hour ago",
          user: "Current User",
          icon: "Clock",
          iconColor: "text-primary",
        },
        {
          id: "step-8",
          title: "File created",
          description: "Initial upload",
          timestamp: "1 week ago",
          user: "File Owner",
          icon: "FileText",
          iconColor: "text-green-500",
        },
      ],
    },
  ],
}

export const mockSystemTags: SystemTag[] = [
  { name: "File Type", value: "Design", editable: false },
  { name: "Size", value: "2.4 MB", editable: false },
  { name: "Created", value: "2024-01-15", editable: false },
  { name: "Modified", value: "2 hours ago", editable: false },
  { name: "Owner", value: "John Doe", editable: true },
  { name: "Folder", value: "Projects", editable: true },
]

export const mockGroups: Group[] = [
  {
    id: "group-product-design",
    name: "Product Design",
    description: "Designers working on product experiences",
  },
  {
    id: "group-design-ops",
    name: "Design Ops",
    description: "Operations and tooling for design teams",
  },
  {
    id: "group-marketing",
    name: "Marketing",
    description: "Growth and marketing initiatives",
  },
  {
    id: "group-customer-success",
    name: "Customer Success",
    description: "Customer onboarding and retention",
  },
]

export const mockUsers: User[] = [
  {
    id: "11111111-1111-1111-1111-111111111111",
    displayName: "Alice Nguyen",
    email: "alice.nguyen@example.com",
    roles: ["Product Designer"],
    isActive: true,
    createdAtUtc: new Date("2023-05-12T08:15:00Z").toISOString(),
    primaryGroupId: "group-product-design",
    groupIds: ["group-product-design", "group-design-ops"],
    hasPassword: true,
  },
  {
    id: "22222222-2222-2222-2222-222222222222",
    displayName: "Bao Tran",
    email: "bao.tran@example.com",
    roles: ["Compliance Lead"],
    isActive: true,
    createdAtUtc: new Date("2022-11-03T10:45:00Z").toISOString(),
    primaryGroupId: "group-customer-success",
    groupIds: ["group-customer-success"],
    hasPassword: true,
  },
  {
    id: "33333333-3333-3333-3333-333333333333",
    displayName: "Linh Pham",
    email: "linh.pham@example.com",
    roles: ["Security Analyst"],
    isActive: true,
    createdAtUtc: new Date("2024-01-20T04:20:00Z").toISOString(),
    primaryGroupId: "group-design-ops",
    groupIds: ["group-design-ops"],
    hasPassword: true,
  },
]

export const mockUser: User = {
  id: "user-1",
  displayName: "John Doe",
  email: "john.doe@company.com",
  avatar: "/diverse-user-avatars.png",
  roles: ["Senior Designer"],
  isActive: true,
  createdAtUtc: new Date("2023-09-18T08:30:00Z").toISOString(),
  primaryGroupId: "group-product-design",
  groupIds: ["group-product-design", "group-design-ops"],
}

export const mockFlows = mockFlowsByFile.default
