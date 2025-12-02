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
  Plus,
  UserCheck,
  Users,
  X,
} from "lucide-react"

import { Button } from "@/components/ui/button"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Separator } from "@/components/ui/separator"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { Badge } from "@/components/ui/badge"
import { ScrollArea, ScrollBar } from "@/components/ui/scroll-area"
import { Input } from "@/components/ui/input"
import { Table, TableBody, TableCaption, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { Switch } from "@/components/ui/switch"
import {
  ContextMenu,
  ContextMenuContent,
  ContextMenuItem,
  ContextMenuTrigger,
} from "@/components/ui/context-menu"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import { Checkbox } from "@/components/ui/checkbox"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
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
  fetchRoles,
  renameRole,
  updateTag,
} from "@/lib/api"
import { getCachedAuthSnapshot } from "@/lib/auth-state"
import type { DocumentType, Group, Role, TagNode, TagUpdateData, User } from "@/lib/types"
import { cn } from "@/lib/utils"

const ORG_MANAGEMENT_ROUTE = "/organization-management/"

const roleCatalog: Role[] = [
  {
    id: "role-admin",
    name: "System Admin",
    description: "Full control over system configuration, roles, and access control.",
  },
  {
    id: "role-compliance",
    name: "Compliance Officer",
    description: "Monitor, moderate, and audit activities related to sensitive data.",
  },
  {
    id: "role-manager",
    name: "Department Manager",
    description: "Manage departments or groups, approve access, and assign work.",
  },
  {
    id: "role-member",
    name: "Standard User",
    description: "Standard user with granted document access.",
  },
]

const groupKindOptions = ["system", "team", "guess", "project"]

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
  const isReadOnly = tag.isSystem
  const isManageableLabel = tag.kind === "label" && !isReadOnly
  const canAddChild = (isNamespace && !isReadOnly) || isManageableLabel
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
                    <Badge variant="outline" className="text-[10px]">Scope: {tagScope}</Badge>
                  ) : null}
                  {tag.isSystem ? <Badge className="text-[10px]">System</Badge> : null}
                </div>
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
                  globalViewOnly={globalViewOnly}
                />
              ))}
            </div>
          ) : null}
        </div>
      </ContextMenuTrigger>
      <ContextMenuContent className="w-48">
        <ContextMenuItem inset disabled={isNamespace || !isManageableLabel} onSelect={() => onEditTag(tag)}>
          <Edit className="mr-2 h-4 w-4" /> Edit
        </ContextMenuItem>
        <ContextMenuItem inset disabled={!canAddChild} onSelect={() => onAddChildTag(tag)}>
          <Tags className="mr-2 h-4 w-4" /> Add child tag
        </ContextMenuItem>
        <ContextMenuItem
          inset
          disabled={!isManageableLabel}
          className="text-destructive focus:text-destructive"
          onSelect={() => onDeleteTag(tag.id)}
        >
          <FolderCog className="mr-2 h-4 w-4" /> Delete tag
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

  const [roles, setRoles] = useState<Role[]>(roleCatalog)
  const [isLoadingRoles, setIsLoadingRoles] = useState(false)
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

  const [editingGroupId, setEditingGroupId] = useState<string | null>(null)
  const [groupDrafts, setGroupDrafts] = useState<Record<string, Partial<Group>>>({})
  const [editingRoleId, setEditingRoleId] = useState<string | null>(null)
  const [roleDrafts, setRoleDrafts] = useState<Record<string, { name?: string; description?: string }>>({})
  const [userDialogOpen, setUserDialogOpen] = useState(false)
  const [userDialogDraft, setUserDialogDraft] = useState<User | null>(null)
  const [docTypeDialogOpen, setDocTypeDialogOpen] = useState(false)
  const [docTypeDialogDraft, setDocTypeDialogDraft] = useState<DocumentType | null>(null)
  const [groupKindFilter, setGroupKindFilter] = useState<string>("all")

  const isAdmin = useMemo(() => isAdminUser(user), [user])
  const activeUsers = useMemo(() => users.filter((item) => item.isActive ?? true).length, [users])
  const roleAssignments = useMemo(
    () =>
      (roles.length ? roles : roleCatalog).map((role) => ({
        ...role,
        name: roleDrafts[role.id]?.name ?? role.name,
        description: roleDrafts[role.id]?.description ?? role.description,
        memberCount: users.filter((u) =>
          u.roles.some((assigned) => assigned.toLowerCase().includes((role.name ?? role.id).toLowerCase())),
        ).length,
      })),
    [roleDrafts, roles, users],
  )
  const selectableRoles = useMemo(() => (roles.length ? roles : roleCatalog), [roles])

  const namespaceNodes = useMemo(() => tags.filter((tag) => tag.kind === "namespace"), [tags])
  const groupKinds = useMemo(
    () =>
      Array.from(
        new Set(
          groups
            .map((group) => group.kind?.trim().toLowerCase())
            .filter((kind): kind is string => Boolean(kind && kind.length > 0)),
        ),
      ),
    [groups],
  )
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
    const matches = groups.filter((group) =>
      group.name.toLowerCase().includes(query) || (group.description ?? "").toLowerCase().includes(query),
    )

    if (groupKindFilter === "all") {
      return matches
    }

    return matches.filter((group) => (group.kind ?? "").toLowerCase() === groupKindFilter.toLowerCase())
  }, [groupKindFilter, groupSearch, groups])

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
        setAuthorizationError("Unable to load user details. Please try again.")
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
        const data = await loadCompleteTagTree()
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

    const loadRoles = async () => {
      try {
        setIsLoadingRoles(true)
        const data = await fetchRoles()
        if (!active) return
        setRoles(data)
      } catch (error) {
        console.error("[org-settings] Unable to load roles:", error)
        if (active) setRoles(roleCatalog)
      } finally {
        if (active) setIsLoadingRoles(false)
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
    loadRoles()
    loadDocumentTypes()

    return () => {
      active = false
    }
  }, [isAuthenticated, isAdmin])

  const mergeTagChildren = (existingChildren: TagNode[] = [], newChildren: TagNode[] = []) => {
    const merged = [...existingChildren]
    newChildren.forEach((child) => {
      const matchIndex = merged.findIndex((item) => item.id === child.id)
      if (matchIndex >= 0) {
        merged[matchIndex] = {
          ...merged[matchIndex],
          ...child,
          children: mergeTagChildren(merged[matchIndex].children ?? [], child.children ?? []),
        }
      } else {
        merged.push({ ...child, children: child.children ?? [] })
      }
    })
    return merged
  }

  const mergeTagTrees = (existing: TagNode[], incoming: TagNode[]) => {
    const merged = [...existing]
    incoming.forEach((node) => {
      const matchIndex = merged.findIndex((item) => item.id === node.id)
      if (matchIndex >= 0) {
        merged[matchIndex] = {
          ...merged[matchIndex],
          ...node,
          children: mergeTagChildren(merged[matchIndex].children ?? [], node.children ?? []),
        }
      } else {
        merged.push({ ...node, children: node.children ?? [] })
      }
    })
    return merged
  }

  const loadCompleteTagTree = async (): Promise<TagNode[]> => {
    const primary = await fetchTags({ scope: "all" })
    let combined = primary

    if (!primary.some((tag) => (tag.namespaceScope ?? "user") === "global")) {
      const globalNodes = await fetchTags({ scope: "global" })
      combined = mergeTagTrees(combined, globalNodes)
    }

    if (!combined.some((tag) => (tag.namespaceScope ?? "user") === "group")) {
      const groupNodes = await fetchTags({ scope: "group" })
      combined = mergeTagTrees(combined, groupNodes)
    }

    return combined
  }

  const reloadTags = async () => {
    const data = await loadCompleteTagTree()
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

  const reloadRoles = async () => {
    const data = await fetchRoles()
    setRoles(data)
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
    const target = tags.find((tag) => tag.id === tagId)
    await deleteTag(tagId)
    await reloadTags()
  }

  const handleCreateNewTag = () => {
    setEditingTag(null)
    setParentTag(null)
    setTagDialogMode("create")
    setIsTagDialogOpen(true)
  }

  const findCreatableNamespace = (nodes: TagNode[]): TagNode | null => {
    const scored = nodes
      .filter((node) => node.kind === "namespace" && !node.isSystem)
      .map((node) => ({
        node,
        score: (node.namespaceScope === "global" ? 3 : node.namespaceScope === "group" ? 2 : 1) + (node.isSystem ? -5 : 0),
      }))

    scored.sort((a, b) => b.score - a.score)
    return scored[0]?.node ?? null
  }

  const resolveNamespaceNode = async (): Promise<TagNode | null> => {
    const existing = findCreatableNamespace(tags)
    if (existing) {
      return existing
    }

    const refreshed = await fetchTags({ scope: "all" })
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

  const updateGroupDraft = (groupId: string, data: Partial<Group>) =>
    setGroupDrafts((previous) => ({ ...previous, [groupId]: { ...(previous[groupId] ?? groups.find((group) => group.id === groupId)), ...data } }))

  const openUserDialog = (target?: User) => {
    setUserDialogDraft(
      target ?? {
        id: `temp-user-${Date.now()}`,
        displayName: "New user",
        email: "user@example.com",
        roles: ["member"],
        isActive: true,
        createdAtUtc: new Date().toISOString(),
        groupIds: [],
        primaryGroupId: null,
      },
    )
    setUserDialogOpen(true)
  }

  const updateUserDialogDraft = (data: Partial<User>) => {
    setUserDialogDraft((previous) => (previous ? { ...previous, ...data } : previous))
  }

  const saveUserDialog = () => {
    if (!userDialogDraft) return

    setUsers((previous) => {
      const exists = previous.some((item) => item.id === userDialogDraft.id)
      if (exists) {
        return previous.map((item) => (item.id === userDialogDraft.id ? { ...item, ...userDialogDraft } : item))
      }
      return [userDialogDraft, ...previous]
    })
    setUserDialogOpen(false)
    setUserDialogDraft(null)
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

  const openDocTypeDialog = (docType?: DocumentType) => {
    setDocTypeDialogDraft(
      docType ?? {
        id: `temp-doc-${Date.now()}`,
        typeKey: `doc-${documentTypes.length + 1}`,
        typeName: "New document type",
        isActive: true,
        createdAtUtc: new Date().toISOString(),
        description: "",
      },
    )
    setDocTypeDialogOpen(true)
  }

  const updateDocTypeDialogDraft = (data: Partial<DocumentType>) => {
    setDocTypeDialogDraft((previous) => (previous ? { ...previous, ...data } : previous))
  }

  const saveDocTypeDialog = () => {
    if (!docTypeDialogDraft) return

    setDocumentTypes((previous) => {
      const exists = previous.some((item) => item.id === docTypeDialogDraft.id)
      if (exists) {
        return previous.map((item) => (item.id === docTypeDialogDraft.id ? { ...item, ...docTypeDialogDraft } : item))
      }
      return [docTypeDialogDraft, ...previous]
    })
    setDocTypeDialogOpen(false)
    setDocTypeDialogDraft(null)
  }

  const startEditingRole = (roleId: string) => {
    const role = roleAssignments.find((item) => item.id === roleId)
    setEditingRoleId(roleId)
    setRoleDrafts((previous) => ({ ...previous, [roleId]: { ...role } }))
  }

  const saveRoleInline = async (roleId: string) => {
    const draft = roleDrafts[roleId]
    const existing = roles.find((item) => item.id === roleId)
    if (!draft || !existing) return

    const updated = await renameRole(roleId, draft.name ?? existing.name)
    setRoles((previous) =>
      previous.map((role) => (role.id === roleId ? { ...role, ...draft, ...(updated ?? {}) } : role)),
    )
    setEditingRoleId(null)
  }

  const cancelRoleInline = (roleId: string) => {
    setEditingRoleId(null)
    setRoleDrafts((previous) => {
      const next = { ...previous }
      delete next[roleId]
      return next
    })
  }

  const handleAddTempUser = () => {
    openUserDialog()
  }

  const handleAddTempGroup = () => {
    const newGroup: Group = {
      id: `temp-group-${Date.now()}`,
      name: "New group",
      description: "Group description",
      kind: "team",
    }
    setGroups((previous) => [newGroup, ...previous])
    startEditingGroup(newGroup)
  }

  const handleAddTempDocType = () => {
    openDocTypeDialog()
  }

  if (isChecking || isAuthorizing) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-muted/40 text-muted-foreground">
        <div className="space-y-3 text-center">
          <p className="text-lg font-semibold">Checking access…</p>
          <p className="text-sm">Please wait a moment.</p>
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
            <CardTitle>You do not have access</CardTitle>
            <CardDescription>
              The Organization Management page is restricted to administrators. Please contact an admin to request access.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <div className="flex flex-wrap items-center justify-center gap-3">
              <Button asChild variant="outline">
                <Link href="/app/">Return to home</Link>
              </Button>
              <Button asChild>
                <Link href="/settings">Update profile</Link>
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
            <p className="text-sm uppercase tracking-[0.14em] text-muted-foreground">Organization management</p>
            <h1 className="text-3xl font-bold">Organization Management</h1>
          </div>
          <div className="flex flex-wrap gap-2">
            <Button asChild variant="outline">
              <Link href="/app/">Return to app</Link>
            </Button>
          </div>
        </div>

        {authorizationError ? (
          <Card className="mt-6 border-destructive/40 bg-destructive/5">
            <CardHeader>
              <CardTitle className="text-destructive">Unable to load information</CardTitle>
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
              <UserCheck className="mr-2 h-4 w-4" /> Users
            </TabsTrigger>
            <TabsTrigger value="roles" className="text-sm">
              <Shield className="mr-2 h-4 w-4" /> Roles
            </TabsTrigger>
            <TabsTrigger value="groups" className="text-sm">
              <Users className="mr-2 h-4 w-4" /> Groups
            </TabsTrigger>
            <TabsTrigger value="doc-types" className="text-sm">
              <FolderCog className="mr-2 h-4 w-4" /> Document types
            </TabsTrigger>
          </TabsList>

          <TabsContent value="tags" className="space-y-4">
            <Card className="overflow-hidden">
              <CardHeader className="flex flex-col gap-3 md:flex-row md:items-start md:justify-between">
                <div className="space-y-2">
                  <CardTitle>Tag & namespace management</CardTitle>
                  <div className="flex flex-wrap gap-2 text-xs text-muted-foreground">
                    <Badge variant="outline">Total nodes: {tags.length}</Badge>
                    <Badge variant="secondary">Namespaces: {namespaceNodes.length}</Badge>
                    <Badge variant="outline">
                      Group/global tags: {tags.filter((tag) => (tag.namespaceScope ?? "") !== "user").length}
                    </Badge>
                  </div>
                </div>
                <div className="flex flex-wrap items-center gap-3">
                  <Button variant="outline" size="sm" onClick={reloadTags} disabled={isLoadingTags}>
                    <RefreshCcw className="mr-2 h-4 w-4" /> Refresh tag tree
                  </Button>
                  <Button size="sm" onClick={handleCreateNewTag} disabled={isLoadingTags}>
                    <Plus className="mr-2 h-4 w-4" /> Add tag
                  </Button>
                </div>
              </CardHeader>
              <CardContent className="space-y-4">
                {isLoadingTags ? (
                  <p className="text-sm text-muted-foreground">Loading tag tree…</p>
                ) : namespaceNodes.length ? (
                  <ScrollArea className="max-h-[60vh] rounded-lg border bg-muted/20">
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
                    <ScrollBar orientation="vertical" />
                  </ScrollArea>
                ) : (
                  <p className="text-sm text-muted-foreground">No tags or namespaces have been configured.</p>
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
            <Card className="overflow-hidden">
              <CardHeader>
                <CardTitle>User management</CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="flex flex-wrap items-center justify-between gap-3">
                  <div className="flex flex-wrap items-center gap-2 text-sm text-muted-foreground">
                    <Badge variant="outline">Total: {users.length}</Badge>
                    <Badge variant="secondary">Active: {activeUsers}</Badge>
                  </div>
                  <div className="flex flex-wrap items-center gap-2">
                    <div className="relative w-56">
                      <Search className="pointer-events-none absolute left-2 top-2.5 h-4 w-4 text-muted-foreground" />
                      <Input
                        value={userSearch}
                        onChange={(event) => setUserSearch(event.target.value)}
                        placeholder="Search by name, email, or role"
                        className="pl-8"
                      />
                    </div>
                    <Button variant="outline" size="sm" onClick={reloadUsers} disabled={isLoadingUsers}>
                      <RefreshCcw className="mr-2 h-4 w-4" /> Refresh
                    </Button>
                    <Button size="sm" onClick={handleAddTempUser}>
                      <Plus className="mr-2 h-4 w-4" /> Add user
                    </Button>
                  </div>
                </div>

                <div className="overflow-x-auto rounded-lg border">
                  <Table>
                    <TableHeader>
                      <TableRow>
                        <TableHead>Display name</TableHead>
                        <TableHead>Email</TableHead>
                        <TableHead>Primary group</TableHead>
                        <TableHead>Roles</TableHead>
                        <TableHead>Status</TableHead>
                        <TableHead>Created</TableHead>
                        <TableHead className="text-right">Actions</TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {isLoadingUsers ? (
                        <TableRow>
                          <TableCell colSpan={7} className="text-center text-sm text-muted-foreground">
                            Loading users…
                          </TableCell>
                        </TableRow>
                      ) : filteredUsers.length === 0 ? (
                        <TableRow>
                          <TableCell colSpan={7} className="text-center text-sm text-muted-foreground">
                            No matching users found.
                          </TableCell>
                        </TableRow>
                      ) : (
                        filteredUsers.map((item) => {
                          const primaryGroup = groups.find((group) => group.id === item.primaryGroupId)
                          const groupCount = item.groupIds?.length ?? 0

                          return (
                            <TableRow key={item.id}>
                              <TableCell>
                                <div className="flex flex-col">
                                  <span className="font-medium">{item.displayName}</span>
                                  <span className="text-xs text-muted-foreground">{item.id}</span>
                                </div>
                              </TableCell>
                              <TableCell>
                                <span className="text-sm text-muted-foreground">{item.email}</span>
                              </TableCell>
                              <TableCell>
                                {primaryGroup ? (
                                  <Badge variant="secondary">{primaryGroup.name}</Badge>
                                ) : (
                                  <Badge variant="outline">No main group</Badge>
                                )}
                                <p className="mt-1 text-xs text-muted-foreground">
                                  {groupCount > 0 ? `${groupCount} group${groupCount > 1 ? "s" : ""}` : "No groups assigned"}
                                </p>
                              </TableCell>
                              <TableCell>
                                <div className="flex flex-wrap gap-1">
                                  {(item.roles ?? []).map((role) => (
                                    <Badge key={role} variant="outline" className="text-[11px]">
                                      {role}
                                    </Badge>
                                  ))}
                                </div>
                              </TableCell>
                              <TableCell>
                                <Badge variant={item.isActive === false ? "outline" : "secondary"}>
                                  {item.isActive === false ? "Suspended" : "Active"}
                                </Badge>
                              </TableCell>
                              <TableCell className="text-sm text-muted-foreground">
                                {item.createdAtUtc
                                  ? new Date(item.createdAtUtc).toLocaleDateString("en-US")
                                  : "--"}
                              </TableCell>
                              <TableCell className="text-right">
                                <Button size="sm" variant="ghost" onClick={() => openUserDialog(item)}>
                                  <Pencil className="mr-2 h-4 w-4" /> Edit user
                                </Button>
                              </TableCell>
                            </TableRow>
                          )
                        })
                      )}
                    </TableBody>
                  </Table>
                </div>
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="roles" className="space-y-4">
            <Card className="overflow-hidden">
              <CardHeader>
                <CardTitle>Role directory</CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="flex flex-wrap items-center justify-between gap-3">
                  <div className="flex flex-wrap items-center gap-2 text-sm text-muted-foreground">
                    <Badge variant="outline">Total roles: {filteredRoles.length}</Badge>
                  </div>
                  <div className="flex flex-wrap items-center gap-2">
                    <div className="relative w-56">
                      <Search className="pointer-events-none absolute left-2 top-2.5 h-4 w-4 text-muted-foreground" />
                      <Input
                        value={roleSearch}
                        onChange={(event) => setRoleSearch(event.target.value)}
                        placeholder="Filter by name or description"
                        className="pl-8"
                      />
                    </div>
                    <Button variant="outline" size="sm" onClick={reloadRoles} disabled={isLoadingRoles}>
                      <RefreshCcw className="mr-2 h-4 w-4" /> Refresh
                    </Button>
                  </div>
                </div>
                <div className="overflow-hidden rounded-lg border">
                  <Table>
                    <TableHeader>
                      <TableRow>
                        <TableHead>Role name</TableHead>
                        <TableHead>Description</TableHead>
                        <TableHead>Members</TableHead>
                        <TableHead className="text-right">Actions</TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {isLoadingRoles ? (
                        <TableRow>
                          <TableCell colSpan={4} className="text-center text-sm text-muted-foreground">
                            Loading roles…
                          </TableCell>
                        </TableRow>
                      ) : filteredRoles.length === 0 ? (
                        <TableRow>
                          <TableCell colSpan={4} className="text-center text-sm text-muted-foreground">
                            No roles available.
                          </TableCell>
                        </TableRow>
                      ) : (
                        filteredRoles.map((role) => {
                          const isEditing = editingRoleId === role.id
                          const draft = roleDrafts[role.id] ?? role

                          return (
                            <TableRow key={role.id} className={isEditing ? "bg-muted/50" : undefined}>
                              <TableCell>
                                {isEditing ? (
                                  <Input
                                    value={draft.name}
                                    onChange={(event) =>
                                      setRoleDrafts((previous) => ({
                                        ...previous,
                                        [role.id]: { ...(previous[role.id] ?? role), name: event.target.value },
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
                                    value={draft.description ?? ""}
                                    onChange={(event) =>
                                      setRoleDrafts((previous) => ({
                                        ...previous,
                                        [role.id]: { ...(previous[role.id] ?? role), description: event.target.value },
                                      }))
                                    }
                                    placeholder="Optional description"
                                  />
                                ) : (
                                  <span className="text-sm text-muted-foreground">{draft.description ?? "No description"}</span>
                                )}
                              </TableCell>
                              <TableCell>
                                <Badge variant="secondary">{role.memberCount} members</Badge>
                              </TableCell>
                              <TableCell className="text-right">
                                {isEditing ? (
                                  <div className="flex justify-end gap-2">
                                    <Button size="sm" variant="secondary" onClick={() => saveRoleInline(role.id)}>
                                      <Check className="mr-2 h-4 w-4" /> Save
                                    </Button>
                                    <Button size="sm" variant="ghost" onClick={() => cancelRoleInline(role.id)}>
                                      <X className="mr-2 h-4 w-4" /> Cancel
                                    </Button>
                                  </div>
                                ) : (
                                  <Button size="sm" variant="ghost" onClick={() => startEditingRole(role.id)}>
                                    <Pencil className="mr-2 h-4 w-4" /> Edit description
                                  </Button>
                                )}
                              </TableCell>
                            </TableRow>
                          )
                        })
                      )}
                    </TableBody>
                  </Table>
                </div>
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="groups" className="space-y-4">
            <Card className="overflow-hidden">
              <CardHeader>
                <CardTitle>Group management</CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="flex flex-wrap items-center justify-between gap-3">
                  <div className="flex flex-wrap items-center gap-2 text-sm text-muted-foreground">
                    <Badge variant="outline">Total groups: {groups.length}</Badge>
                    <Badge variant="secondary">Showing: {filteredGroups.length}</Badge>
                  </div>
                  <div className="flex flex-wrap items-center gap-2">
                    <Select value={groupKindFilter} onValueChange={setGroupKindFilter}>
                      <SelectTrigger className="w-40">
                        <SelectValue placeholder="Filter by kind" />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="all">All group kinds</SelectItem>
                        {groupKinds.map((kind) => (
                          <SelectItem key={kind} value={kind} className="capitalize">
                            {kind}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                    <div className="relative w-56">
                      <Search className="pointer-events-none absolute left-2 top-2.5 h-4 w-4 text-muted-foreground" />
                      <Input
                        value={groupSearch}
                        onChange={(event) => setGroupSearch(event.target.value)}
                        placeholder="Search by group name"
                        className="pl-8"
                      />
                    </div>
                    <Button variant="outline" size="sm" onClick={reloadGroups} disabled={isLoadingGroups}>
                      <RefreshCcw className="mr-2 h-4 w-4" /> Refresh
                    </Button>
                    <Button size="sm" onClick={handleAddTempGroup}>
                      <Plus className="mr-2 h-4 w-4" /> Add group
                    </Button>
                  </div>
                </div>

                <div className="overflow-x-auto rounded-lg border">
                  <Table>
                    <TableHeader>
                      <TableRow>
                        <TableHead>Group name</TableHead>
                        <TableHead>Group kind</TableHead>
                        <TableHead>Description</TableHead>
                        <TableHead className="text-right">Actions</TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {isLoadingGroups ? (
                        <TableRow>
                          <TableCell colSpan={3} className="text-center text-sm text-muted-foreground">
                            Loading groups…
                          </TableCell>
                        </TableRow>
                      ) : filteredGroups.length === 0 ? (
                        <TableRow>
                          <TableCell colSpan={3} className="text-center text-sm text-muted-foreground">
                            No groups match the current filter.
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
                                  <Select
                                    value={draft.kind ?? groupKindOptions[0]}
                                    onValueChange={(value) => updateGroupDraft(group.id, { kind: value })}
                                  >
                                    <SelectTrigger className="w-44 capitalize">
                                      <SelectValue />
                                    </SelectTrigger>
                                    <SelectContent>
                                      {groupKindOptions.map((kind) => (
                                        <SelectItem key={kind} value={kind} className="capitalize">
                                          {kind}
                                        </SelectItem>
                                      ))}
                                    </SelectContent>
                                  </Select>
                                ) : draft.kind ? (
                                  <Badge variant="outline" className="capitalize text-[11px]">
                                    {draft.kind}
                                  </Badge>
                                ) : (
                                  <span className="text-sm text-muted-foreground">Not set</span>
                                )}
                              </TableCell>
                              <TableCell>
                                {isEditing ? (
                                  <Input
                                    value={draft.description ?? ""}
                                    onChange={(event) => updateGroupDraft(group.id, { description: event.target.value })}
                                    placeholder="Add description"
                                  />
                                ) : (
                                  <span className="text-sm text-muted-foreground">{draft.description || "No description"}</span>
                                )}
                              </TableCell>
                              <TableCell className="text-right">
                                {isEditing ? (
                                  <div className="flex justify-end gap-2">
                                    <Button size="sm" variant="secondary" onClick={() => saveGroupInline(group.id)}>
                                      <Save className="mr-2 h-4 w-4" /> Save
                                    </Button>
                                    <Button size="sm" variant="ghost" onClick={() => cancelGroupInline(group.id)}>
                                      <X className="mr-2 h-4 w-4" /> Cancel
                                    </Button>
                                  </div>
                                ) : (
                                  <Button size="sm" variant="ghost" onClick={() => startEditingGroup(group)}>
                                    <Pencil className="mr-2 h-4 w-4" /> Inline edit
                                  </Button>
                                )}
                              </TableCell>
                            </TableRow>
                          )
                        })
                      )}
                    </TableBody>
                  </Table>
                </div>
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="doc-types" className="space-y-4">
            <Card className="overflow-hidden">
              <CardHeader>
                <CardTitle>Document types</CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="flex flex-wrap items-center justify-between gap-3">
                  <div className="flex flex-wrap items-center gap-2 text-sm text-muted-foreground">
                    <Badge variant="outline">Total types: {documentTypes.length}</Badge>
                    <Badge variant="secondary">
                      Active: {documentTypes.filter((doc) => doc.isActive).length}
                    </Badge>
                  </div>
                  <div className="flex flex-wrap items-center gap-2">
                    <div className="relative w-56">
                      <Search className="pointer-events-none absolute left-2 top-2.5 h-4 w-4 text-muted-foreground" />
                      <Input
                        value={docTypeSearch}
                        onChange={(event) => setDocTypeSearch(event.target.value)}
                        placeholder="Search by name or key"
                        className="pl-8"
                      />
                    </div>
                    <Button variant="outline" size="sm" onClick={reloadDocumentTypes} disabled={isLoadingDocumentTypes}>
                      <RefreshCcw className="mr-2 h-4 w-4" /> Refresh
                    </Button>
                    <Button size="sm" onClick={handleAddTempDocType}>
                      <Plus className="mr-2 h-4 w-4" /> Add type
                    </Button>
                  </div>
                </div>

                <div className="overflow-x-auto rounded-lg border">
                  <Table>
                    <TableHeader>
                      <TableRow>
                        <TableHead>Type name</TableHead>
                        <TableHead>Key</TableHead>
                        <TableHead>Description</TableHead>
                        <TableHead>Created</TableHead>
                        <TableHead className="text-right">Actions</TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {isLoadingDocumentTypes ? (
                        <TableRow>
                          <TableCell colSpan={5} className="text-center text-sm text-muted-foreground">
                            Loading document types…
                          </TableCell>
                        </TableRow>
                      ) : filteredDocumentTypes.length === 0 ? (
                        <TableRow>
                          <TableCell colSpan={5} className="text-center text-sm text-muted-foreground">
                            No document types available.
                          </TableCell>
                        </TableRow>
                      ) : (
                        filteredDocumentTypes.map((docType) => (
                          <TableRow key={docType.id}>
                            <TableCell>
                              <span className="font-medium">{docType.typeName}</span>
                            </TableCell>
                            <TableCell>
                              <Input value={docType.typeKey} disabled />
                            </TableCell>
                            <TableCell>
                              <span className="text-sm text-muted-foreground line-clamp-2">
                                {docType.description ?? "No description"}
                              </span>
                            </TableCell>
                            <TableCell className="text-sm text-muted-foreground">
                              {new Date(docType.createdAtUtc).toLocaleDateString("en-US", {
                                year: "numeric",
                                month: "short",
                                day: "numeric",
                              })}
                            </TableCell>
                            <TableCell className="text-right">
                              <Button size="sm" variant="ghost" onClick={() => openDocTypeDialog(docType)}>
                                <Pencil className="mr-2 h-4 w-4" /> Edit type
                              </Button>
                            </TableCell>
                          </TableRow>
                        ))
                      )}
                    </TableBody>
                  </Table>
                </div>
              </CardContent>
            </Card>
          </TabsContent>
        </Tabs>

        <Dialog
          open={userDialogOpen}
          onOpenChange={(open) => {
            setUserDialogOpen(open)
            if (!open) {
              setUserDialogDraft(null)
            }
          }}
        >
          <DialogContent className="max-w-3xl">
            <DialogHeader>
              <DialogTitle>{userDialogDraft ? `Edit ${userDialogDraft.displayName}` : "Edit user"}</DialogTitle>
              <DialogDescription>
                Update user profile details, primary group, and assigned groups.
              </DialogDescription>
            </DialogHeader>

            {userDialogDraft ? (
              <div className="space-y-6">
                <div className="grid gap-4 md:grid-cols-2">
                  <div className="space-y-2">
                    <Label htmlFor="user-name">Display name</Label>
                    <Input
                      id="user-name"
                      value={userDialogDraft.displayName}
                      onChange={(event) => updateUserDialogDraft({ displayName: event.target.value })}
                    />
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="user-email">Email</Label>
                    <Input
                      id="user-email"
                      value={userDialogDraft.email}
                      onChange={(event) => updateUserDialogDraft({ email: event.target.value })}
                    />
                  </div>
                </div>

                <div className="grid gap-4 md:grid-cols-2">
                  <div className="space-y-2">
                    <Label>Status</Label>
                    <div className="flex items-center gap-3 rounded-md border p-3">
                      <Switch
                        checked={userDialogDraft.isActive ?? true}
                        onCheckedChange={(checked) => updateUserDialogDraft({ isActive: checked })}
                      />
                      <div>
                        <p className="text-sm font-medium">{userDialogDraft.isActive === false ? "Suspended" : "Active"}</p>
                        <p className="text-xs text-muted-foreground">
                          Control whether this account can sign in.
                        </p>
                      </div>
                    </div>
                  </div>

                  <div className="space-y-2">
                    <Label>Primary group</Label>
                    <Select
                      value={userDialogDraft.primaryGroupId ?? "none"}
                      onValueChange={(value) => {
                        const normalized = value === "none" ? null : value
                        const currentGroups = new Set(userDialogDraft.groupIds ?? [])
                        if (normalized) {
                          currentGroups.add(normalized)
                        }
                        updateUserDialogDraft({
                          primaryGroupId: normalized,
                          groupIds: Array.from(currentGroups),
                        })
                      }}
                    >
                      <SelectTrigger>
                        <SelectValue placeholder="Select primary group" />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="none">No primary group</SelectItem>
                        {groups.map((group) => (
                          <SelectItem key={group.id} value={group.id}>
                            {group.name}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </div>
                </div>

                <div className="grid gap-4 md:grid-cols-2">
                  <div className="space-y-2">
                    <Label>Roles</Label>
                    <ScrollArea className="h-36 rounded-md border p-3">
                      <div className="space-y-2">
                        {selectableRoles.map((role) => {
                          const isChecked = (userDialogDraft.roles ?? []).some(
                            (value) => value.toLowerCase() === role.name.toLowerCase(),
                          )

                          return (
                            <label key={role.id} className="flex items-center gap-2 text-sm">
                              <Checkbox
                                checked={isChecked}
                                onCheckedChange={(checked) => {
                                  const currentRoles = new Set(
                                    (userDialogDraft.roles ?? []).map((value) => value.trim()).filter(Boolean),
                                  )
                                  if (checked) {
                                    currentRoles.add(role.name)
                                  } else {
                                    currentRoles.delete(role.name)
                                  }
                                  updateUserDialogDraft({ roles: Array.from(currentRoles) })
                                }}
                              />
                              <div className="flex-1 truncate">
                                <p className="font-medium leading-tight">{role.name}</p>
                                <p className="text-xs text-muted-foreground line-clamp-2">
                                  {role.description ?? "No description"}
                                </p>
                              </div>
                            </label>
                          )
                        })}
                        {!selectableRoles.length ? (
                          <p className="text-xs text-muted-foreground">No roles available.</p>
                        ) : null}
                      </div>
                    </ScrollArea>
                  </div>
                  <div className="space-y-2">
                    <Label>Groups</Label>
                    <ScrollArea className="h-36 rounded-md border p-3">
                      <div className="space-y-2">
                        {groups.map((group) => {
                          const isChecked = (userDialogDraft.groupIds ?? []).includes(group.id)
                          const isPrimary = userDialogDraft.primaryGroupId === group.id

                          return (
                            <label key={group.id} className="flex items-center gap-2 text-sm">
                              <Checkbox
                                checked={isChecked}
                                onCheckedChange={(checked) => {
                                  const nextGroups = new Set(userDialogDraft.groupIds ?? [])
                                  if (checked) {
                                    nextGroups.add(group.id)
                                  } else {
                                    nextGroups.delete(group.id)
                                  }
                                  const nextGroupIds = Array.from(nextGroups)
                                  const nextPrimary = isPrimary && !checked ? null : userDialogDraft.primaryGroupId
                                  updateUserDialogDraft({
                                    groupIds: nextGroupIds,
                                    primaryGroupId: nextPrimary,
                                  })
                                }}
                              />
                              <span className="flex-1 truncate">{group.name}</span>
                              {group.kind ? (
                                <Badge variant="outline" className="text-[10px] capitalize">
                                  {group.kind}
                                </Badge>
                              ) : null}
                            </label>
                          )
                        })}
                        {!groups.length ? (
                          <p className="text-xs text-muted-foreground">No groups available.</p>
                        ) : null}
                      </div>
                    </ScrollArea>
                  </div>
                </div>

                <div className="flex justify-end gap-3 pt-2">
                  <Button variant="ghost" onClick={() => setUserDialogOpen(false)}>
                    Cancel
                  </Button>
                  <Button onClick={saveUserDialog} disabled={!userDialogDraft.displayName.trim()}>
                    <Save className="mr-2 h-4 w-4" /> Save changes
                  </Button>
                </div>
              </div>
            ) : (
              <p className="text-sm text-muted-foreground">Select a user to edit.</p>
            )}
          </DialogContent>
        </Dialog>

        <Dialog
          open={docTypeDialogOpen}
          onOpenChange={(open) => {
            setDocTypeDialogOpen(open)
            if (!open) {
              setDocTypeDialogDraft(null)
            }
          }}
        >
          <DialogContent className="max-w-xl">
            <DialogHeader>
              <DialogTitle>{docTypeDialogDraft ? `Edit ${docTypeDialogDraft.typeName}` : "Edit document type"}</DialogTitle>
              <DialogDescription>Update metadata shown to users when categorizing documents.</DialogDescription>
            </DialogHeader>

            {docTypeDialogDraft ? (
              <div className="space-y-4">
                <div className="space-y-2">
                  <Label htmlFor="doc-type-name">Type name</Label>
                  <Input
                    id="doc-type-name"
                    value={docTypeDialogDraft.typeName}
                    onChange={(event) => updateDocTypeDialogDraft({ typeName: event.target.value })}
                  />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="doc-type-key">Key</Label>
                  <Input id="doc-type-key" value={docTypeDialogDraft.typeKey} disabled />
                </div>
                <div className="space-y-2">
                  <Label>Description</Label>
                  <Textarea
                    value={docTypeDialogDraft.description ?? ""}
                    onChange={(event) => updateDocTypeDialogDraft({ description: event.target.value })}
                    placeholder="Add a short description"
                  />
                </div>
                <div className="flex items-center justify-between rounded-md border p-3">
                  <div>
                    <p className="text-sm font-medium">Active</p>
                    <p className="text-xs text-muted-foreground">Toggle availability for assignment.</p>
                  </div>
                  <Switch
                    checked={docTypeDialogDraft.isActive}
                    onCheckedChange={(checked) => updateDocTypeDialogDraft({ isActive: checked })}
                  />
                </div>
                <div className="flex justify-end gap-3 pt-2">
                  <Button variant="ghost" onClick={() => setDocTypeDialogOpen(false)}>
                    Cancel
                  </Button>
                  <Button onClick={saveDocTypeDialog} disabled={!docTypeDialogDraft.typeName.trim()}>
                    <Save className="mr-2 h-4 w-4" /> Save changes
                  </Button>
                </div>
              </div>
            ) : (
              <p className="text-sm text-muted-foreground">Select a document type to edit.</p>
            )}
          </DialogContent>
        </Dialog>
      </div>
    </div>
  )
}
