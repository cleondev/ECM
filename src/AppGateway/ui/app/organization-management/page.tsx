"use client"

import { useEffect, useMemo, useState } from "react"
import Link from "next/link"
import {
  Check,
  ChevronDown,
  ChevronRight,
  Edit,
  FolderCog,
  MoreVertical,
  Pencil,
  RefreshCcw,
  Save,
  Search,
  Shield,
  Tags,
  UserCheck,
  Users,
  X,
} from "lucide-react"

import { Button } from "@/components/ui/button"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Separator } from "@/components/ui/separator"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { Badge } from "@/components/ui/badge"
import { ScrollArea } from "@/components/ui/scroll-area"
import { Input } from "@/components/ui/input"
import { Table, TableBody, TableCaption, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { Switch } from "@/components/ui/switch"
import {
  ContextMenu,
  ContextMenuContent,
  ContextMenuItem,
  ContextMenuTrigger,
} from "@/components/ui/context-menu"
import { TagManagementDialog } from "@/components/tag-management-dialog"
import { useAuthGuard } from "@/hooks/use-auth-guard"
import {
  createTag,
  deleteTag,
  fetchCurrentUserProfile,
  fetchDocumentTypes,
  fetchGroups,
  fetchTags,
  fetchUsers,
  updateTag,
} from "@/lib/api"
import { getCachedAuthSnapshot } from "@/lib/auth-state"
import type { DocumentType, Group, TagNode, TagUpdateData, User } from "@/lib/types"
import { cn } from "@/lib/utils"

const ORG_MANAGEMENT_ROUTE = "/organization-management/"

const roleCatalog = [
  {
    key: "admin",
    name: "System Admin",
    description: "Toàn quyền cấu hình hệ thống, vai trò và kiểm soát truy cập.",
  },
  {
    key: "compliance",
    name: "Compliance Officer",
    description: "Theo dõi, kiểm duyệt và kiểm tra các hoạt động liên quan tới dữ liệu nhạy cảm.",
  },
  {
    key: "manager",
    name: "Department Manager",
    description: "Quản trị nhóm/bộ phận, duyệt quyền truy cập và phân công nhiệm vụ.",
  },
  {
    key: "member",
    name: "Standard User",
    description: "Người dùng thông thường với quyền truy cập tài liệu được cấp.",
  },
]

function isAdminUser(user: User | null): boolean {
  if (!user?.roles?.length) return false
  return user.roles.some((role) => role.toLowerCase().includes("admin"))
}

const DEFAULT_TAG_ICON = "📁"

type TagDialogMode = "create" | "edit" | "add-child"

function TagTreeItem({
  tag,
  level = 0,
  onEditTag,
  onAddChildTag,
  onDeleteTag,
}: {
  tag: TagNode
  level?: number
  onEditTag: (tag: TagNode) => void
  onAddChildTag: (parentTag: TagNode) => void
  onDeleteTag: (tagId: string) => void
}) {
  const [isExpanded, setIsExpanded] = useState(true)
  const hasChildren = Boolean(tag.children?.length)
  const isNamespace = tag.kind === "namespace"
  const tagScope = tag.namespaceScope ?? "user"
  const isManageableLabel = tag.kind === "label" && !tag.isSystem
  const canAddChild = (isNamespace && !tag.isSystem) || isManageableLabel
  const displayIcon = tag.iconKey && tag.iconKey.trim() !== "" ? tag.iconKey : DEFAULT_TAG_ICON
  const indicatorStyle = tag.color ? { backgroundColor: tag.color, borderColor: tag.color } : undefined

  return (
    <ContextMenu>
      <ContextMenuTrigger asChild>
        <div className="rounded-md border bg-background/80" style={{ marginLeft: `${level * 12}px` }}>
          <div className="flex items-center justify-between gap-3 px-3 py-2">
            <div className="flex items-center gap-2 min-w-0">
              {hasChildren ? (
                <button
                  type="button"
                  onClick={(event) => {
                    event.preventDefault()
                    setIsExpanded((prev) => !prev)
                  }}
                  className="text-muted-foreground"
                >
                  {isExpanded ? <ChevronDown className="h-4 w-4" /> : <ChevronRight className="h-4 w-4" />}
                </button>
              ) : (
                <div className="w-4" />
              )}
              <span
                className={cn(
                  "leftbar-tag-indicator h-2.5 w-2.5 flex-shrink-0 rounded-full border transition-all duration-200",
                  tag.color ? "leftbar-tag-indicator--custom" : null,
                )}
                style={indicatorStyle}
              />
              <span className="text-sm" aria-hidden>
                {displayIcon}
              </span>
              <div className="flex flex-col min-w-0">
                <div className="flex items-center gap-2 min-w-0">
                  <span className="text-sm font-semibold truncate">{tag.name}</span>
                  {isNamespace && tag.namespaceLabel ? (
                    <Badge variant="secondary" className="text-[10px]">{tag.namespaceLabel}</Badge>
                  ) : null}
                  {isNamespace ? (
                    <Badge variant="outline" className="text-[10px]">Phạm vi: {tagScope}</Badge>
                  ) : null}
                  {tag.isSystem ? <Badge className="text-[10px]">System</Badge> : null}
                </div>
                {!isNamespace ? (
                  <p className="text-xs text-muted-foreground truncate">
                    Nằm trong {tag.namespaceLabel || "namespace mặc định"}
                  </p>
                ) : null}
              </div>
            </div>
            <MoreVertical className="h-4 w-4 text-muted-foreground" />
          </div>
          {hasChildren && isExpanded ? (
            <div className="space-y-2 px-2 pb-2">
              {tag.children?.map((child) => (
                <TagTreeItem
                  key={child.id}
                  tag={child}
                  level={level + 1}
                  onEditTag={onEditTag}
                  onAddChildTag={onAddChildTag}
                  onDeleteTag={onDeleteTag}
                />
              ))}
            </div>
          ) : null}
        </div>
      </ContextMenuTrigger>
      <ContextMenuContent className="w-48">
        <ContextMenuItem inset disabled={isNamespace || !isManageableLabel} onSelect={() => onEditTag(tag)}>
          <Edit className="mr-2 h-4 w-4" /> Chỉnh sửa
        </ContextMenuItem>
        <ContextMenuItem inset disabled={!canAddChild} onSelect={() => onAddChildTag(tag)}>
          <Tags className="mr-2 h-4 w-4" /> Thêm tag con
        </ContextMenuItem>
        <ContextMenuItem
          inset
          disabled={!isManageableLabel}
          className="text-destructive focus:text-destructive"
          onSelect={() => onDeleteTag(tag.id)}
        >
          <FolderCog className="mr-2 h-4 w-4" /> Xóa tag
        </ContextMenuItem>
      </ContextMenuContent>
    </ContextMenu>
  )
}

export default function OrganizationManagementPage() {
  const { isAuthenticated, isChecking } = useAuthGuard(ORG_MANAGEMENT_ROUTE)
  const [user, setUser] = useState<User | null>(() => getCachedAuthSnapshot()?.user ?? null)
  const [isAuthorizing, setIsAuthorizing] = useState(true)
  const [authorizationError, setAuthorizationError] = useState<string | null>(null)
  const [tags, setTags] = useState<TagNode[]>([])
  const [isLoadingTags, setIsLoadingTags] = useState(false)
  const [tagDialogMode, setTagDialogMode] = useState<TagDialogMode>("create")
  const [isTagDialogOpen, setIsTagDialogOpen] = useState(false)
  const [editingTag, setEditingTag] = useState<TagNode | null>(null)
  const [parentTag, setParentTag] = useState<TagNode | null>(null)

  const [users, setUsers] = useState<User[]>([])
  const [groups, setGroups] = useState<Group[]>([])
  const [documentTypes, setDocumentTypes] = useState<DocumentType[]>([])
  const [isLoadingUsers, setIsLoadingUsers] = useState(false)
  const [isLoadingGroups, setIsLoadingGroups] = useState(false)
  const [isLoadingDocumentTypes, setIsLoadingDocumentTypes] = useState(false)

  const [userSearch, setUserSearch] = useState("")
  const [groupSearch, setGroupSearch] = useState("")
  const [docTypeSearch, setDocTypeSearch] = useState("")
  const [roleSearch, setRoleSearch] = useState("")

  const [editingUserId, setEditingUserId] = useState<string | null>(null)
  const [userDrafts, setUserDrafts] = useState<Record<string, Partial<User>>>({})
  const [editingGroupId, setEditingGroupId] = useState<string | null>(null)
  const [groupDrafts, setGroupDrafts] = useState<Record<string, Partial<Group>>>({})
  const [editingDocTypeId, setEditingDocTypeId] = useState<string | null>(null)
  const [docTypeDrafts, setDocTypeDrafts] = useState<Record<string, Partial<DocumentType>>>({})
  const [editingRoleKey, setEditingRoleKey] = useState<string | null>(null)
  const [roleDrafts, setRoleDrafts] = useState<Record<string, { name?: string; description?: string }>>({})

  const isAdmin = useMemo(() => isAdminUser(user), [user])
  const activeUsers = useMemo(() => users.filter((item) => item.isActive ?? true).length, [users])
  const roleAssignments = useMemo(
    () =>
      roleCatalog.map((role) => ({
        ...role,
        name: roleDrafts[role.key]?.name ?? role.name,
        description: roleDrafts[role.key]?.description ?? role.description,
        memberCount: users.filter((u) => u.roles.some((assigned) => assigned.toLowerCase().includes(role.key))).length,
      })),
    [roleDrafts, users],
  )

  const namespaceNodes = useMemo(() => tags.filter((tag) => tag.kind === "namespace"), [tags])
  const filteredUsers = useMemo(() => {
    const query = userSearch.trim().toLowerCase()
    if (!query) return users

    return users.filter(
      (item) =>
        item.displayName.toLowerCase().includes(query) ||
        item.email.toLowerCase().includes(query) ||
        item.roles.some((role) => role.toLowerCase().includes(query)),
    )
  }, [userSearch, users])

  const filteredGroups = useMemo(() => {
    const query = groupSearch.trim().toLowerCase()
    if (!query) return groups

    return groups.filter((group) => group.name.toLowerCase().includes(query) || (group.description ?? "").toLowerCase().includes(query))
  }, [groupSearch, groups])

  const filteredDocumentTypes = useMemo(() => {
    const query = docTypeSearch.trim().toLowerCase()
    if (!query) return documentTypes

    return documentTypes.filter(
      (docType) =>
        docType.typeName.toLowerCase().includes(query) ||
        docType.typeKey.toLowerCase().includes(query) ||
        (docType.isActive ? "active" : "inactive").includes(query),
    )
  }, [docTypeSearch, documentTypes])

  const filteredRoles = useMemo(() => {
    const query = roleSearch.trim().toLowerCase()
    if (!query) return roleAssignments

    return roleAssignments.filter(
      (role) => role.name.toLowerCase().includes(query) || (role.description ?? "").toLowerCase().includes(query),
    )
  }, [roleAssignments, roleSearch])

  useEffect(() => {
    let active = true

    const loadUser = async () => {
      if (!isAuthenticated) {
        setIsAuthorizing(false)
        return
      }

      try {
        setAuthorizationError(null)
        const profile = await fetchCurrentUserProfile()
        if (!active) return
        setUser(profile)
      } catch (error) {
        console.error("[org-settings] Unable to load profile:", error)
        if (!active) return
        setAuthorizationError("Không thể tải thông tin người dùng. Vui lòng thử lại.")
      } finally {
        if (active) {
          setIsAuthorizing(false)
        }
      }
    }

    loadUser()

    return () => {
      active = false
    }
  }, [isAuthenticated])

  useEffect(() => {
    let active = true
    if (!isAuthenticated || !isAdmin) {
      return undefined
    }

    const loadTags = async () => {
      try {
        setIsLoadingTags(true)
        const data = await fetchTags()
        if (!active) return
        setTags(data)
      } catch (error) {
        console.error("[org-settings] Unable to load tags:", error)
        if (active) {
          setTags([])
        }
      } finally {
        if (active) {
          setIsLoadingTags(false)
        }
      }
    }

    loadTags()

    return () => {
      active = false
    }
  }, [isAuthenticated, isAdmin])

  useEffect(() => {
    let active = true
    if (!isAuthenticated || !isAdmin) {
      return undefined
    }

    const loadUsers = async () => {
      try {
        setIsLoadingUsers(true)
        const data = await fetchUsers()
        if (!active) return
        setUsers(data)
      } catch (error) {
        console.error("[org-settings] Unable to load users:", error)
        if (active) setUsers([])
      } finally {
        if (active) setIsLoadingUsers(false)
      }
    }

    const loadGroups = async () => {
      try {
        setIsLoadingGroups(true)
        const data = await fetchGroups()
        if (!active) return
        setGroups(data)
      } catch (error) {
        console.error("[org-settings] Unable to load groups:", error)
        if (active) setGroups([])
      } finally {
        if (active) setIsLoadingGroups(false)
      }
    }

    const loadDocumentTypes = async () => {
      try {
        setIsLoadingDocumentTypes(true)
        const data = await fetchDocumentTypes()
        if (!active) return
        setDocumentTypes(data)
      } catch (error) {
        console.error("[org-settings] Unable to load document types:", error)
        if (active) setDocumentTypes([])
      } finally {
        if (active) setIsLoadingDocumentTypes(false)
      }
    }

    loadUsers()
    loadGroups()
    loadDocumentTypes()

    return () => {
      active = false
    }
  }, [isAuthenticated, isAdmin])

  const reloadTags = async () => {
    const data = await fetchTags()
    setTags(data)
  }

  const reloadUsers = async () => {
    const data = await fetchUsers()
    setUsers(data)
  }

  const reloadGroups = async () => {
    const data = await fetchGroups()
    setGroups(data)
  }

  const reloadDocumentTypes = async () => {
    const data = await fetchDocumentTypes()
    setDocumentTypes(data)
  }

  const handleEditTag = (tag: TagNode) => {
    setEditingTag(tag)
    setParentTag(null)
    setTagDialogMode("edit")
    setIsTagDialogOpen(true)
  }

  const handleAddChildTag = (parent: TagNode) => {
    setParentTag(parent)
    setEditingTag(null)
    setTagDialogMode("add-child")
    setIsTagDialogOpen(true)
  }

  const handleDeleteTag = async (tagId: string) => {
    await deleteTag(tagId)
    await reloadTags()
  }

  const handleCreateNewTag = () => {
    setEditingTag(null)
    setParentTag(null)
    setTagDialogMode("create")
    setIsTagDialogOpen(true)
  }

  const findCreatableNamespace = (nodes: TagNode[]): TagNode | null =>
    nodes.find((node) => node.kind === "namespace" && (node.namespaceScope ?? "user") === "user") ?? null

  const resolveNamespaceNode = async (): Promise<TagNode | null> => {
    const existing = findCreatableNamespace(tags)
    if (existing) {
      return existing
    }

    const refreshed = await fetchTags()
    setTags(refreshed)

    return findCreatableNamespace(refreshed)
  }

  const handleSaveTag = async (data: TagUpdateData) => {
    if (tagDialogMode === "edit" && editingTag) {
      await updateTag(editingTag, data)
    } else if (tagDialogMode === "add-child" && parentTag) {
      await createTag(data, parentTag)
    } else {
      const namespaceNode = await resolveNamespaceNode()
      if (!namespaceNode) {
        console.warn("[org-settings] Unable to determine namespace for new tag creation")
        return
      }
      await createTag(data, namespaceNode)
    }
    await reloadTags()
  }

  const updateUserDraft = (userId: string, data: Partial<User>) =>
    setUserDrafts((previous) => ({ ...previous, [userId]: { ...(previous[userId] ?? users.find((u) => u.id === userId)), ...data } }))

  const updateGroupDraft = (groupId: string, data: Partial<Group>) =>
    setGroupDrafts((previous) => ({ ...previous, [groupId]: { ...(previous[groupId] ?? groups.find((group) => group.id === groupId)), ...data } }))

  const updateDocTypeDraft = (docTypeId: string, data: Partial<DocumentType>) =>
    setDocTypeDrafts((previous) => ({
      ...previous,
      [docTypeId]: { ...(previous[docTypeId] ?? documentTypes.find((docType) => docType.id === docTypeId)), ...data },
    }))

  const startEditingUser = (user: User) => {
    setEditingUserId(user.id)
    setUserDrafts((previous) => ({ ...previous, [user.id]: { ...user } }))
  }

  const saveUserInline = (userId: string) => {
    const draft = userDrafts[userId]
    if (!draft) return

    setUsers((previous) => previous.map((user) => (user.id === userId ? { ...user, ...draft } : user)))
    setEditingUserId(null)
  }

  const cancelUserInline = (userId: string) => {
    setEditingUserId(null)
    setUserDrafts((previous) => {
      const next = { ...previous }
      delete next[userId]
      return next
    })
  }

  const startEditingGroup = (group: Group) => {
    setEditingGroupId(group.id)
    setGroupDrafts((previous) => ({ ...previous, [group.id]: { ...group } }))
  }

  const saveGroupInline = (groupId: string) => {
    const draft = groupDrafts[groupId]
    if (!draft) return

    setGroups((previous) => previous.map((group) => (group.id === groupId ? { ...group, ...draft } : group)))
    setEditingGroupId(null)
  }

  const cancelGroupInline = (groupId: string) => {
    setEditingGroupId(null)
    setGroupDrafts((previous) => {
      const next = { ...previous }
      delete next[groupId]
      return next
    })
  }

  const startEditingDocType = (docType: DocumentType) => {
    setEditingDocTypeId(docType.id)
    setDocTypeDrafts((previous) => ({ ...previous, [docType.id]: { ...docType } }))
  }

  const saveDocTypeInline = (docTypeId: string) => {
    const draft = docTypeDrafts[docTypeId]
    if (!draft) return

    setDocumentTypes((previous) => previous.map((item) => (item.id === docTypeId ? { ...item, ...draft } : item)))
    setEditingDocTypeId(null)
  }

  const cancelDocTypeInline = (docTypeId: string) => {
    setEditingDocTypeId(null)
    setDocTypeDrafts((previous) => {
      const next = { ...previous }
      delete next[docTypeId]
      return next
    })
  }

  const startEditingRole = (roleKey: string) => {
    const role = roleCatalog.find((item) => item.key === roleKey)
    setEditingRoleKey(roleKey)
    setRoleDrafts((previous) => ({ ...previous, [roleKey]: { ...role } }))
  }

  const saveRoleInline = (roleKey: string) => {
    const draft = roleDrafts[roleKey]
    if (!draft) return

    setRoleDrafts((previous) => ({ ...previous, [roleKey]: draft }))
    setEditingRoleKey(null)
  }

  const cancelRoleInline = (roleKey: string) => {
    setEditingRoleKey(null)
    setRoleDrafts((previous) => {
      const next = { ...previous }
      delete next[roleKey]
      return next
    })
  }

  const handleAddTempUser = () => {
    const newUser: User = {
      id: `temp-user-${Date.now()}`,
      displayName: "Người dùng mới",
      email: "user@example.com",
      roles: ["member"],
      isActive: true,
      createdAtUtc: new Date().toISOString(),
    }
    setUsers((previous) => [newUser, ...previous])
    startEditingUser(newUser)
  }

  const handleAddTempGroup = () => {
    const newGroup: Group = {
      id: `temp-group-${Date.now()}`,
      name: "Nhóm mới",
      description: "Mô tả nhóm",
    }
    setGroups((previous) => [newGroup, ...previous])
    startEditingGroup(newGroup)
  }

  const handleAddTempDocType = () => {
    const newDocType: DocumentType = {
      id: `temp-doc-${Date.now()}`,
      typeKey: `doc-${documentTypes.length + 1}`,
      typeName: "Loại tài liệu mới",
      isActive: true,
      createdAtUtc: new Date().toISOString(),
    }
    setDocumentTypes((previous) => [newDocType, ...previous])
    startEditingDocType(newDocType)
  }

  if (isChecking || isAuthorizing) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-muted/40 text-muted-foreground">
        <div className="space-y-3 text-center">
          <p className="text-lg font-semibold">Đang kiểm tra quyền truy cập…</p>
          <p className="text-sm">Vui lòng chờ trong giây lát.</p>
        </div>
      </div>
    )
  }

  if (!isAuthenticated) {
    return null
  }

  if (!isAdmin) {
    return (
      <div className="mx-auto flex min-h-screen max-w-3xl flex-col justify-center gap-6 px-6 py-10 text-center">
        <Card>
          <CardHeader>
            <CardTitle>Bạn không có quyền truy cập</CardTitle>
            <CardDescription>
              Trang Organization Management chỉ dành cho tài khoản quản trị. Vui lòng liên hệ quản trị viên để được cấp quyền.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <div className="flex flex-wrap items-center justify-center gap-3">
              <Button asChild variant="outline">
                <Link href="/app/">Quay lại trang chính</Link>
              </Button>
              <Button asChild>
                <Link href="/settings">Cập nhật hồ sơ cá nhân</Link>
              </Button>
            </div>
          </CardContent>
        </Card>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-background">
      <div className="mx-auto max-w-6xl px-6 py-10">
        <div className="flex items-center justify-between gap-4">
          <div className="space-y-2">
            <p className="text-sm uppercase tracking-[0.14em] text-muted-foreground">Quản lý tổ chức</p>
            <h1 className="text-3xl font-bold">Organization Management</h1>
            <p className="text-sm text-muted-foreground">
              Cấu hình phạm vi toàn tổ chức: tag/namespace, người dùng, nhóm và loại tài liệu.
            </p>
          </div>
          <div className="flex flex-wrap gap-2">
            <Button asChild variant="outline">
              <Link href="/app/">Quay lại app</Link>
            </Button>
            <Button asChild>
              <Link href="/settings">Cá nhân hóa tài khoản</Link>
            </Button>
          </div>
        </div>

        {authorizationError ? (
          <Card className="mt-6 border-destructive/40 bg-destructive/5">
            <CardHeader>
              <CardTitle className="text-destructive">Không thể tải thông tin</CardTitle>
              <CardDescription className="text-destructive">
                {authorizationError}
              </CardDescription>
            </CardHeader>
          </Card>
        ) : null}

        <Separator className="my-8" />

        <Tabs defaultValue="tags" className="space-y-6">
          <TabsList className="grid w-full grid-cols-5">
            <TabsTrigger value="tags" className="text-sm">
              <Tags className="mr-2 h-4 w-4" /> Tag & Namespace
            </TabsTrigger>
            <TabsTrigger value="users" className="text-sm">
              <UserCheck className="mr-2 h-4 w-4" /> Người dùng
            </TabsTrigger>
            <TabsTrigger value="roles" className="text-sm">
              <Shield className="mr-2 h-4 w-4" /> Roles
            </TabsTrigger>
            <TabsTrigger value="groups" className="text-sm">
              <Users className="mr-2 h-4 w-4" /> Nhóm
            </TabsTrigger>
            <TabsTrigger value="doc-types" className="text-sm">
              <FolderCog className="mr-2 h-4 w-4" /> Loại tài liệu
            </TabsTrigger>
          </TabsList>

          <TabsContent value="tags" className="space-y-4">
            <Card>
              <CardHeader className="flex flex-col gap-3 md:flex-row md:items-start md:justify-between">
                <div className="space-y-2">
                  <CardTitle>Quản trị tag & namespace</CardTitle>
                  <CardDescription>
                    Cây tag/namespace tương tự thanh bên trái nhưng hiển thị đầy đủ tag group/global để bạn quản lý tập trung.
                  </CardDescription>
                  <div className="flex flex-wrap gap-2 text-xs text-muted-foreground">
                    <Badge variant="outline">Tổng số node: {tags.length}</Badge>
                    <Badge variant="secondary">Namespaces: {namespaceNodes.length}</Badge>
                    <Badge variant="outline">
                      Tag group/global: {tags.filter((tag) => (tag.namespaceScope ?? "") !== "user").length}
                    </Badge>
                  </div>
                </div>
                <div className="flex flex-wrap gap-2">
                  <Button variant="outline" size="sm" onClick={reloadTags} disabled={isLoadingTags}>
                    <RefreshCcw className="mr-2 h-4 w-4" /> Làm mới cây tag
                  </Button>
                  <Button size="sm" onClick={handleCreateNewTag} disabled={isLoadingTags}>
                    <Plus className="mr-2 h-4 w-4" /> Thêm tag mới
                  </Button>
                </div>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="flex flex-wrap gap-2 text-xs text-muted-foreground">
                  <Badge variant="secondary">Hỗ trợ chỉnh sửa cả namespace group & global</Badge>
                  <Badge variant="outline">Nhấp chuột phải hoặc nút menu để chỉnh sửa/xóa</Badge>
                </div>
                {isLoadingTags ? (
                  <p className="text-sm text-muted-foreground">Đang tải cây tag…</p>
                ) : namespaceNodes.length ? (
                  <ScrollArea className="max-h-[540px] rounded-lg border bg-muted/20">
                    <div className="space-y-2 p-3">
                      {namespaceNodes.map((node) => (
                        <TagTreeItem
                          key={node.id}
                          tag={node}
                          onEditTag={handleEditTag}
                          onAddChildTag={handleAddChildTag}
                          onDeleteTag={handleDeleteTag}
                        />
                      ))}
                    </div>
                  </ScrollArea>
                ) : (
                  <p className="text-sm text-muted-foreground">Chưa có tag hoặc namespace nào được cấu hình.</p>
                )}
              </CardContent>
            </Card>
            <TagManagementDialog
              open={isTagDialogOpen}
              onOpenChange={setIsTagDialogOpen}
              mode={tagDialogMode}
              editingTag={editingTag ?? undefined}
              parentTag={parentTag ?? undefined}
              onSave={handleSaveTag}
            />
          </TabsContent>

          <TabsContent value="users" className="space-y-4">
            <Card>
              <CardHeader>
                <CardTitle>Quản trị người dùng</CardTitle>
                <CardDescription>
                  Kiểm soát tài khoản, vai trò và bảo mật đăng nhập cho toàn bộ tổ chức.
                </CardDescription>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="flex flex-wrap items-center justify-between gap-3">
                  <div className="flex flex-wrap items-center gap-2 text-sm text-muted-foreground">
                    <Badge variant="outline">Tổng: {users.length}</Badge>
                    <Badge variant="secondary">Đang hoạt động: {activeUsers}</Badge>
                  </div>
                  <div className="flex flex-wrap items-center gap-2">
                    <div className="relative w-56">
                      <Search className="pointer-events-none absolute left-2 top-2.5 h-4 w-4 text-muted-foreground" />
                      <Input
                        value={userSearch}
                        onChange={(event) => setUserSearch(event.target.value)}
                        placeholder="Tìm theo tên, email, role"
                        className="pl-8"
                      />
                    </div>
                    <Button variant="outline" size="sm" onClick={reloadUsers} disabled={isLoadingUsers}>
                      <RefreshCcw className="mr-2 h-4 w-4" /> Làm mới
                    </Button>
                    <Button size="sm" onClick={handleAddTempUser}>
                      <Plus className="mr-2 h-4 w-4" /> Thêm user
                    </Button>
                  </div>
                </div>

                <div className="overflow-hidden rounded-lg border">
                  <Table>
                    <TableHeader>
                      <TableRow>
                        <TableHead>Tên hiển thị</TableHead>
                        <TableHead>Email</TableHead>
                        <TableHead>Roles</TableHead>
                        <TableHead>Trạng thái</TableHead>
                        <TableHead>Ngày tạo</TableHead>
                        <TableHead className="text-right">Thao tác</TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {isLoadingUsers ? (
                        <TableRow>
                          <TableCell colSpan={6} className="text-center text-sm text-muted-foreground">
                            Đang tải danh sách người dùng…
                          </TableCell>
                        </TableRow>
                      ) : filteredUsers.length === 0 ? (
                        <TableRow>
                          <TableCell colSpan={6} className="text-center text-sm text-muted-foreground">
                            Không tìm thấy người dùng nào phù hợp.
                          </TableCell>
                        </TableRow>
                      ) : (
                        filteredUsers.map((item) => {
                          const isEditing = editingUserId === item.id
                          const draft = (isEditing ? userDrafts[item.id] : undefined) ?? item

                          return (
                            <TableRow key={item.id} className={isEditing ? "bg-muted/50" : undefined}>
                              <TableCell>
                                {isEditing ? (
                                  <Input
                                    value={draft.displayName}
                                    onChange={(event) => updateUserDraft(item.id, { displayName: event.target.value })}
                                  />
                                ) : (
                                  <span className="font-medium">{draft.displayName}</span>
                                )}
                              </TableCell>
                              <TableCell>
                                {isEditing ? (
                                  <Input
                                    value={draft.email}
                                    onChange={(event) => updateUserDraft(item.id, { email: event.target.value })}
                                  />
                                ) : (
                                  <span className="text-sm text-muted-foreground">{draft.email}</span>
                                )}
                              </TableCell>
                              <TableCell>
                                {isEditing ? (
                                  <Input
                                    value={(draft.roles ?? []).join(", ")}
                                    onChange={(event) =>
                                      updateUserDraft(item.id, {
                                        roles: event.target.value
                                          .split(",")
                                          .map((role) => role.trim())
                                          .filter(Boolean),
                                      })
                                    }
                                  />
                                ) : (
                                  <div className="flex flex-wrap gap-1">
                                    {(draft.roles ?? []).map((role) => (
                                      <Badge key={role} variant="outline" className="text-[11px]">
                                        {role}
                                      </Badge>
                                    ))}
                                  </div>
                                )}
                              </TableCell>
                              <TableCell>
                                <div className="flex items-center gap-2">
                                  <Switch
                                    checked={draft.isActive ?? true}
                                    disabled={!isEditing}
                                    onCheckedChange={(checked) => updateUserDraft(item.id, { isActive: checked })}
                                  />
                                  <span className="text-xs text-muted-foreground">
                                    {draft.isActive === false ? "Tạm khóa" : "Đang hoạt động"}
                                  </span>
                                </div>
                              </TableCell>
                              <TableCell className="text-sm text-muted-foreground">
                                {draft.createdAtUtc
                                  ? new Date(draft.createdAtUtc).toLocaleDateString("vi-VN")
                                  : "--"}
                              </TableCell>
                              <TableCell className="text-right">
                                <div className="flex justify-end gap-2">
                                  {isEditing ? (
                                    <>
                                      <Button size="sm" variant="secondary" onClick={() => saveUserInline(item.id)}>
                                        <Save className="mr-2 h-4 w-4" /> Lưu
                                      </Button>
                                      <Button size="sm" variant="ghost" onClick={() => cancelUserInline(item.id)}>
                                        <X className="mr-2 h-4 w-4" /> Hủy
                                      </Button>
                                    </>
                                  ) : (
                                    <Button size="sm" variant="ghost" onClick={() => startEditingUser(item)}>
                                      <Pencil className="mr-2 h-4 w-4" /> Sửa nhanh
                                    </Button>
                                  )}
                                </div>
                              </TableCell>
                            </TableRow>
                          )
                        })
                      )}
                    </TableBody>
                    <TableCaption>Action bar phía trên hỗ trợ tìm kiếm, thêm mới và refresh danh sách.</TableCaption>
                  </Table>
                </div>
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="roles" className="space-y-4">
            <Card>
              <CardHeader>
                <CardTitle>Danh sách role</CardTitle>
                <CardDescription>
                  Tổng hợp role chuẩn cùng số lượng thành viên đang sở hữu, hỗ trợ chỉnh sửa mô tả inline.
                </CardDescription>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="flex flex-wrap items-center justify-between gap-3">
                  <div className="flex flex-wrap items-center gap-2 text-sm text-muted-foreground">
                    <Badge variant="outline">Tổng role: {filteredRoles.length}</Badge>
                  </div>
                  <div className="relative w-56">
                    <Search className="pointer-events-none absolute left-2 top-2.5 h-4 w-4 text-muted-foreground" />
                    <Input
                      value={roleSearch}
                      onChange={(event) => setRoleSearch(event.target.value)}
                      placeholder="Lọc theo tên, mô tả"
                      className="pl-8"
                    />
                  </div>
                </div>
                <div className="overflow-hidden rounded-lg border">
                  <Table>
                    <TableHeader>
                      <TableRow>
                        <TableHead>Tên role</TableHead>
                        <TableHead>Mô tả</TableHead>
                        <TableHead>Số thành viên</TableHead>
                        <TableHead className="text-right">Thao tác</TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {filteredRoles.map((role) => {
                        const isEditing = editingRoleKey === role.key
                        const draft = roleDrafts[role.key] ?? role

                        return (
                          <TableRow key={role.key} className={isEditing ? "bg-muted/50" : undefined}>
                            <TableCell>
                              {isEditing ? (
                                <Input
                                  value={draft.name}
                                  onChange={(event) =>
                                    setRoleDrafts((previous) => ({
                                      ...previous,
                                      [role.key]: { ...(previous[role.key] ?? role), name: event.target.value },
                                    }))
                                  }
                                />
                              ) : (
                                <span className="font-medium">{draft.name}</span>
                              )}
                            </TableCell>
                            <TableCell>
                              {isEditing ? (
                                <Input
                                  value={draft.description}
                                  onChange={(event) =>
                                    setRoleDrafts((previous) => ({
                                      ...previous,
                                      [role.key]: { ...(previous[role.key] ?? role), description: event.target.value },
                                    }))
                                  }
                                />
                              ) : (
                                <span className="text-sm text-muted-foreground">{draft.description}</span>
                              )}
                            </TableCell>
                            <TableCell>
                              <Badge variant="secondary">{role.memberCount} thành viên</Badge>
                            </TableCell>
                            <TableCell className="text-right">
                              {isEditing ? (
                                <div className="flex justify-end gap-2">
                                  <Button size="sm" variant="secondary" onClick={() => saveRoleInline(role.key)}>
                                    <Check className="mr-2 h-4 w-4" /> Lưu
                                  </Button>
                                  <Button size="sm" variant="ghost" onClick={() => cancelRoleInline(role.key)}>
                                    <X className="mr-2 h-4 w-4" /> Hủy
                                  </Button>
                                </div>
                              ) : (
                                <Button size="sm" variant="ghost" onClick={() => startEditingRole(role.key)}>
                                  <Pencil className="mr-2 h-4 w-4" /> Sửa mô tả
                                </Button>
                              )}
                            </TableCell>
                          </TableRow>
                        )
                      })}
                    </TableBody>
                    <TableCaption>Bảng roles hỗ trợ chỉnh sửa inline để đồng bộ mô tả và tên hiển thị.</TableCaption>
                  </Table>
                </div>
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="groups" className="space-y-4">
            <Card>
              <CardHeader>
                <CardTitle>Quản trị nhóm</CardTitle>
                <CardDescription>
                  Thiết lập nhóm chức năng/dự án, quyền kế thừa và đồng bộ thành viên để áp dụng phân quyền tập trung.
                </CardDescription>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="flex flex-wrap items-center justify-between gap-3">
                  <div className="flex flex-wrap items-center gap-2 text-sm text-muted-foreground">
                    <Badge variant="outline">Tổng nhóm: {groups.length}</Badge>
                    <Badge variant="secondary">Đang xem: {filteredGroups.length}</Badge>
                  </div>
                  <div className="flex flex-wrap items-center gap-2">
                    <div className="relative w-56">
                      <Search className="pointer-events-none absolute left-2 top-2.5 h-4 w-4 text-muted-foreground" />
                      <Input
                        value={groupSearch}
                        onChange={(event) => setGroupSearch(event.target.value)}
                        placeholder="Tìm theo tên nhóm"
                        className="pl-8"
                      />
                    </div>
                    <Button variant="outline" size="sm" onClick={reloadGroups} disabled={isLoadingGroups}>
                      <RefreshCcw className="mr-2 h-4 w-4" /> Làm mới
                    </Button>
                    <Button size="sm" onClick={handleAddTempGroup}>
                      <Plus className="mr-2 h-4 w-4" /> Thêm nhóm
                    </Button>
                  </div>
                </div>

                <div className="overflow-hidden rounded-lg border">
                  <Table>
                    <TableHeader>
                      <TableRow>
                        <TableHead>Tên nhóm</TableHead>
                        <TableHead>Mô tả</TableHead>
                        <TableHead className="text-right">Thao tác</TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {isLoadingGroups ? (
                        <TableRow>
                          <TableCell colSpan={3} className="text-center text-sm text-muted-foreground">
                            Đang tải danh sách nhóm…
                          </TableCell>
                        </TableRow>
                      ) : filteredGroups.length === 0 ? (
                        <TableRow>
                          <TableCell colSpan={3} className="text-center text-sm text-muted-foreground">
                            Không có nhóm nào thỏa điều kiện lọc.
                          </TableCell>
                        </TableRow>
                      ) : (
                        filteredGroups.map((group) => {
                          const isEditing = editingGroupId === group.id
                          const draft = (isEditing ? groupDrafts[group.id] : undefined) ?? group

                          return (
                            <TableRow key={group.id} className={isEditing ? "bg-muted/50" : undefined}>
                              <TableCell>
                                {isEditing ? (
                                  <Input
                                    value={draft.name}
                                    onChange={(event) => updateGroupDraft(group.id, { name: event.target.value })}
                                  />
                                ) : (
                                  <span className="font-medium">{draft.name}</span>
                                )}
                              </TableCell>
                              <TableCell>
                                {isEditing ? (
                                  <Input
                                    value={draft.description ?? ""}
                                    onChange={(event) => updateGroupDraft(group.id, { description: event.target.value })}
                                    placeholder="Thêm mô tả"
                                  />
                                ) : (
                                  <span className="text-sm text-muted-foreground">{draft.description || "Chưa có mô tả"}</span>
                                )}
                              </TableCell>
                              <TableCell className="text-right">
                                {isEditing ? (
                                  <div className="flex justify-end gap-2">
                                    <Button size="sm" variant="secondary" onClick={() => saveGroupInline(group.id)}>
                                      <Save className="mr-2 h-4 w-4" /> Lưu
                                    </Button>
                                    <Button size="sm" variant="ghost" onClick={() => cancelGroupInline(group.id)}>
                                      <X className="mr-2 h-4 w-4" /> Hủy
                                    </Button>
                                  </div>
                                ) : (
                                  <Button size="sm" variant="ghost" onClick={() => startEditingGroup(group)}>
                                    <Pencil className="mr-2 h-4 w-4" /> Sửa inline
                                  </Button>
                                )}
                              </TableCell>
                            </TableRow>
                          )
                        })
                      )}
                    </TableBody>
                    <TableCaption>Chỉnh sửa trực tiếp để cập nhật tên nhóm và mô tả.</TableCaption>
                  </Table>
                </div>
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="doc-types" className="space-y-4">
            <Card>
              <CardHeader>
                <CardTitle>Danh sách loại tài liệu</CardTitle>
                <CardDescription>
                  Bổ sung danh sách loại tài liệu đang được kích hoạt để tiện rà soát và chuẩn hóa metadata.
                </CardDescription>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="flex flex-wrap items-center justify-between gap-3">
                  <div className="flex flex-wrap items-center gap-2 text-sm text-muted-foreground">
                    <Badge variant="outline">Tổng loại: {documentTypes.length}</Badge>
                    <Badge variant="secondary">
                      Đang hoạt động: {documentTypes.filter((doc) => doc.isActive).length}
                    </Badge>
                  </div>
                  <div className="flex flex-wrap items-center gap-2">
                    <div className="relative w-56">
                      <Search className="pointer-events-none absolute left-2 top-2.5 h-4 w-4 text-muted-foreground" />
                      <Input
                        value={docTypeSearch}
                        onChange={(event) => setDocTypeSearch(event.target.value)}
                        placeholder="Tìm theo tên hoặc key"
                        className="pl-8"
                      />
                    </div>
                    <Button variant="outline" size="sm" onClick={reloadDocumentTypes} disabled={isLoadingDocumentTypes}>
                      <RefreshCcw className="mr-2 h-4 w-4" /> Làm mới
                    </Button>
                    <Button size="sm" onClick={handleAddTempDocType}>
                      <Plus className="mr-2 h-4 w-4" /> Thêm loại
                    </Button>
                  </div>
                </div>

                <div className="overflow-hidden rounded-lg border">
                  <Table>
                    <TableHeader>
                      <TableRow>
                        <TableHead>Tên loại</TableHead>
                        <TableHead>Key</TableHead>
                        <TableHead>Trạng thái</TableHead>
                        <TableHead>Ngày tạo</TableHead>
                        <TableHead className="text-right">Thao tác</TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {isLoadingDocumentTypes ? (
                        <TableRow>
                          <TableCell colSpan={5} className="text-center text-sm text-muted-foreground">
                            Đang tải loại tài liệu…
                          </TableCell>
                        </TableRow>
                      ) : filteredDocumentTypes.length === 0 ? (
                        <TableRow>
                          <TableCell colSpan={5} className="text-center text-sm text-muted-foreground">
                            Không có loại tài liệu nào.
                          </TableCell>
                        </TableRow>
                      ) : (
                        filteredDocumentTypes.map((docType) => {
                          const isEditing = editingDocTypeId === docType.id
                          const draft = (isEditing ? docTypeDrafts[docType.id] : undefined) ?? docType

                          return (
                            <TableRow key={docType.id} className={isEditing ? "bg-muted/50" : undefined}>
                              <TableCell>
                                {isEditing ? (
                                  <Input
                                    value={draft.typeName}
                                    onChange={(event) => updateDocTypeDraft(docType.id, { typeName: event.target.value })}
                                  />
                                ) : (
                                  <span className="font-medium">{draft.typeName}</span>
                                )}
                              </TableCell>
                              <TableCell>
                                <Input value={draft.typeKey} disabled />
                              </TableCell>
                              <TableCell>
                                <div className="flex items-center gap-2">
                                  <Switch
                                    checked={draft.isActive}
                                    disabled={!isEditing}
                                    onCheckedChange={(checked) => updateDocTypeDraft(docType.id, { isActive: checked })}
                                  />
                                  <span className="text-xs text-muted-foreground">
                                    {draft.isActive ? "Đang dùng" : "Không hoạt động"}
                                  </span>
                                </div>
                              </TableCell>
                              <TableCell className="text-sm text-muted-foreground">
                                {new Date(draft.createdAtUtc).toLocaleDateString("vi-VN", {
                                  year: "numeric",
                                  month: "short",
                                  day: "numeric",
                                })}
                              </TableCell>
                              <TableCell className="text-right">
                                {isEditing ? (
                                  <div className="flex justify-end gap-2">
                                    <Button size="sm" variant="secondary" onClick={() => saveDocTypeInline(docType.id)}>
                                      <Check className="mr-2 h-4 w-4" /> Lưu
                                    </Button>
                                    <Button size="sm" variant="ghost" onClick={() => cancelDocTypeInline(docType.id)}>
                                      <X className="mr-2 h-4 w-4" /> Hủy
                                    </Button>
                                  </div>
                                ) : (
                                  <Button size="sm" variant="ghost" onClick={() => startEditingDocType(docType)}>
                                    <Pencil className="mr-2 h-4 w-4" /> Sửa inline
                                  </Button>
                                )}
                              </TableCell>
                            </TableRow>
                          )
                        })
                      )}
                    </TableBody>
                    <TableCaption>Bảng loại tài liệu được hiển thị dưới dạng data grid với hỗ trợ chỉnh sửa nhanh.</TableCaption>
                  </Table>
                </div>
              </CardContent>
            </Card>
          </TabsContent>
        </Tabs>
      </div>
    </div>
  )
}
