"use client"

import { useEffect, useMemo, useState } from "react"
import Link from "next/link"
import { ChevronDown, ChevronRight, Edit, FolderCog, MoreVertical, Shield, Tags, UserCheck, UserCog, Users } from "lucide-react"

import { Button } from "@/components/ui/button"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Separator } from "@/components/ui/separator"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { Badge } from "@/components/ui/badge"
import { ScrollArea } from "@/components/ui/scroll-area"
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

const ORG_MANAGEMENT_ROUTE = "/app/organization-management/"

const groupGovernancePlaybooks = [
  {
    title: "Cấu trúc nhóm",
    description: "Tạo nhóm chức năng, dự án hoặc chuyên môn để phân quyền nhanh chóng thay vì cấu hình lẻ tẻ.",
  },
  {
    title: "Kế thừa quyền",
    description: "Thiết lập quan hệ cha-con giữa các nhóm để quyền truy cập được kế thừa nhất quán.",
  },
  {
    title: "Đồng bộ danh bạ",
    description: "Kết nối hệ thống nhân sự/directory để tự động cập nhật thành viên và vai trò nhóm.",
  },
]

const documentTypePolicies = [
  {
    title: "Danh mục loại tài liệu",
    description: "Chuẩn hóa danh sách loại hồ sơ, tài liệu nghiệp vụ và biểu mẫu sử dụng trong toàn hệ thống.",
  },
  {
    title: "Mẫu metadata",
    description: "Định nghĩa trường bắt buộc, nhãn, và validation cho từng loại tài liệu để tránh nhập thiếu.",
  },
  {
    title: "Vòng đời & lưu trữ",
    description: "Thiết lập thời gian lưu trữ, nhắc gia hạn và quy tắc hủy cho từng loại tài liệu quan trọng.",
  },
]

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
  const isReadOnlyScope = tagScope === "group" || tagScope === "global"
  const isManageableLabel = tag.kind === "label" && !tag.isSystem && !isReadOnlyScope
  const canAddChild = (isNamespace && !isReadOnlyScope) || isManageableLabel
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

  const isAdmin = useMemo(() => isAdminUser(user), [user])
  const activeUsers = useMemo(() => users.filter((item) => item.isActive ?? true).length, [users])
  const roleAssignments = useMemo(
    () =>
      roleCatalog.map((role) => ({
        ...role,
        memberCount: users.filter((u) => u.roles.some((assigned) => assigned.toLowerCase().includes(role.key))).length,
      })),
    [users],
  )

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
              <CardHeader className="space-y-3">
                <CardTitle>Quản trị tag & namespace</CardTitle>
                <CardDescription>
                  Cây tag/namespace ở đây đồng bộ với thanh bên trái, cho phép chỉnh sửa, tạo tag con và quản lý phạm vi.
                </CardDescription>
                <div className="flex flex-wrap gap-2 text-xs text-muted-foreground">
                  <Badge variant="outline">Tổng số node: {tags.length}</Badge>
                  <Badge variant="secondary">Chỉ chỉnh sửa được tag label trong phạm vi user</Badge>
                </div>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="flex flex-wrap items-center justify-between gap-3">
                  <p className="text-sm text-muted-foreground">
                    Sử dụng menu chuột phải (context menu) để chỉnh sửa nhanh, tạo tag con hoặc xóa giống như tại thanh bên.
                  </p>
                  <div className="flex flex-wrap gap-2">
                    <Button variant="outline" onClick={reloadTags} disabled={isLoadingTags}>
                      Làm mới cây tag
                    </Button>
                    <Button onClick={handleCreateNewTag} disabled={isLoadingTags}>
                      Thêm tag mới
                    </Button>
                  </div>
                </div>
                {isLoadingTags ? (
                  <p className="text-sm text-muted-foreground">Đang tải cây tag…</p>
                ) : tags.length ? (
                  <ScrollArea className="h-[520px] rounded-md border p-4">
                    <div className="space-y-2">
                      {tags.map((node) => (
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
                <div className="flex flex-wrap items-center gap-3 text-sm text-muted-foreground">
                  <Badge variant="outline">Tổng: {users.length}</Badge>
                  <Badge variant="secondary">Đang hoạt động: {activeUsers}</Badge>
                  <Badge variant="outline">Role phổ biến: {roleAssignments[0]?.name}</Badge>
                </div>
                {isLoadingUsers ? (
                  <p className="text-sm text-muted-foreground">Đang tải danh sách người dùng…</p>
                ) : (
                  <ScrollArea className="max-h-[460px] rounded-md border">
                    <div className="divide-y">
                      {users.map((item) => (
                        <ContextMenu key={item.id}>
                          <ContextMenuTrigger asChild>
                            <div className="flex flex-col gap-1 px-4 py-3 hover:bg-muted/60">
                              <div className="flex items-center justify-between gap-3">
                                <div className="flex flex-col gap-1 min-w-0">
                                  <span className="font-semibold truncate">{item.displayName}</span>
                                  <span className="text-xs text-muted-foreground truncate">{item.email}</span>
                                </div>
                                <Badge variant={item.isActive === false ? "outline" : "secondary"}>
                                  {item.isActive === false ? "Tạm khóa" : "Đang hoạt động"}
                                </Badge>
                              </div>
                              <div className="flex flex-wrap gap-2 text-xs text-muted-foreground">
                                <Badge variant="outline">Roles: {item.roles.join(", ") || "Chưa có"}</Badge>
                                {item.primaryGroupId ? (
                                  <Badge variant="outline">Nhóm chính: {item.primaryGroupId}</Badge>
                                ) : null}
                              </div>
                            </div>
                          </ContextMenuTrigger>
                          <ContextMenuContent className="w-56">
                            <ContextMenuItem inset onSelect={(event) => event.preventDefault()}>
                              <Edit className="mr-2 h-4 w-4" /> Chỉnh sửa hồ sơ
                            </ContextMenuItem>
                            <ContextMenuItem inset onSelect={(event) => event.preventDefault()}>
                              <Users className="mr-2 h-4 w-4" /> Cập nhật nhóm
                            </ContextMenuItem>
                            <ContextMenuItem inset onSelect={(event) => event.preventDefault()}>
                              <UserCog className="mr-2 h-4 w-4" /> Cập nhật role
                            </ContextMenuItem>
                          </ContextMenuContent>
                        </ContextMenu>
                      ))}
                    </div>
                  </ScrollArea>
                )}
              </CardContent>
            </Card>
            <Card>
              <CardHeader>
                <CardTitle>Trạng thái phiên đăng nhập</CardTitle>
                <CardDescription>
                  Xem nhanh thông tin tài khoản đang dùng để đảm bảo thao tác đúng phân quyền.
                </CardDescription>
              </CardHeader>
              <CardContent className="flex flex-wrap items-center gap-3 text-sm">
                <Badge variant="secondary">User: {user?.displayName ?? "--"}</Badge>
                <Badge variant="secondary">Email: {user?.email ?? "--"}</Badge>
                <Badge variant="outline">Roles: {user?.roles?.join(", ") || "Chưa có"}</Badge>
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="roles" className="space-y-4">
            <Card>
              <CardHeader>
                <CardTitle>Danh sách role</CardTitle>
                <CardDescription>
                  Tổng hợp role chuẩn cùng số lượng thành viên đang sở hữu, giúp kiểm tra nhanh việc phân quyền.
                </CardDescription>
              </CardHeader>
              <CardContent className="grid gap-4 md:grid-cols-2">
                {roleAssignments.map((role) => (
                  <div key={role.key} className="rounded-lg border bg-muted/30 p-4">
                    <div className="flex items-center justify-between gap-2">
                      <div>
                        <p className="text-sm font-semibold">{role.name}</p>
                        <p className="text-xs text-muted-foreground">{role.description}</p>
                      </div>
                      <Badge variant="secondary">{role.memberCount} thành viên</Badge>
                    </div>
                  </div>
                ))}
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
              <CardContent className="space-y-3">
                <div className="flex flex-wrap gap-2 text-sm text-muted-foreground">
                  <Badge variant="outline">Tổng nhóm: {groups.length}</Badge>
                  <Badge variant="secondary">Sẵn sàng cho kế thừa quyền</Badge>
                </div>
                {isLoadingGroups ? (
                  <p className="text-sm text-muted-foreground">Đang tải danh sách nhóm…</p>
                ) : (
                  <div className="grid gap-3 md:grid-cols-2">
                    {groups.map((group) => (
                      <div key={group.id} className="rounded-lg border bg-muted/30 p-4">
                        <p className="font-semibold">{group.name}</p>
                        <p className="text-xs text-muted-foreground">{group.description || "Chưa có mô tả"}</p>
                      </div>
                    ))}
                  </div>
                )}
              </CardContent>
            </Card>
            <Card>
              <CardHeader>
                <CardTitle>Gợi ý triển khai</CardTitle>
                <CardDescription>
                  Bắt đầu với các nhóm lõi (quản trị, vận hành), sau đó mở rộng nhóm dự án/chuyên môn để kế thừa quyền hợp lý.
                </CardDescription>
              </CardHeader>
              <CardContent className="grid gap-4 md:grid-cols-3">
                {groupGovernancePlaybooks.map((item) => (
                  <div key={item.title} className="rounded-lg border bg-muted/30 p-4">
                    <h3 className="font-semibold">{item.title}</h3>
                    <p className="mt-2 text-sm text-muted-foreground">{item.description}</p>
                  </div>
                ))}
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
              <CardContent className="space-y-3">
                {isLoadingDocumentTypes ? (
                  <p className="text-sm text-muted-foreground">Đang tải loại tài liệu…</p>
                ) : (
                  <ScrollArea className="max-h-[360px] rounded-md border">
                    <div className="divide-y">
                      {documentTypes.map((docType) => (
                        <div key={docType.id} className="flex items-center justify-between gap-3 px-4 py-3">
                          <div className="flex flex-col min-w-0">
                            <span className="font-semibold truncate">{docType.typeName}</span>
                            <span className="text-xs text-muted-foreground truncate">Key: {docType.typeKey}</span>
                          </div>
                          <div className="flex flex-col items-end text-xs text-muted-foreground">
                            <Badge variant="secondary">{docType.isActive ? "Đang dùng" : "Không hoạt động"}</Badge>
                            <span>
                              Tạo ngày {new Date(docType.createdAtUtc).toLocaleDateString("vi-VN", {
                                year: "numeric",
                                month: "short",
                                day: "numeric",
                              })}
                            </span>
                          </div>
                        </div>
                      ))}
                    </div>
                  </ScrollArea>
                )}
              </CardContent>
            </Card>
            <Card>
              <CardHeader>
                <CardTitle>Checklist triển khai</CardTitle>
                <CardDescription>
                  Xác định loại tài liệu ưu tiên, thêm metadata bắt buộc và gắn tag/namespace mặc định trước khi mở rộng.
                </CardDescription>
              </CardHeader>
              <CardContent className="grid gap-4 md:grid-cols-3">
                {documentTypePolicies.map((item) => (
                  <div key={item.title} className="rounded-lg border bg-muted/30 p-4">
                    <h3 className="font-semibold">{item.title}</h3>
                    <p className="mt-2 text-sm text-muted-foreground">{item.description}</p>
                  </div>
                ))}
              </CardContent>
            </Card>
          </TabsContent>
        </Tabs>
      </div>
    </div>
  )
}
