"use client"

import {
  Folder,
  Star,
  Clock,
  Trash2,
  ChevronRight,
  ChevronDown,
  Tag,
  MoreVertical,
  Plus,
  Edit,
  Trash,
  Settings,
  LogOut,
  User,
} from "lucide-react"
import type { LucideIcon } from "lucide-react"
import { cn } from "@/lib/utils"
import { useState, useEffect, useMemo } from "react"
import { Button } from "@/components/ui/button"
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover"
import { TagManagementDialog } from "./tag-management-dialog"
import type { SelectedTag, TagNode, TagUpdateData, User as UserType, Group } from "@/lib/types"
import { fetchTags, createTag, updateTag, deleteTag, fetchUser, signOut, fetchGroups } from "@/lib/api"
import { ScrollArea } from "@/components/ui/scroll-area"
import { ThemeSwitcher } from "./theme-switcher"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"
import {
  ContextMenu,
  ContextMenuContent,
  ContextMenuItem,
  ContextMenuTrigger,
} from "@/components/ui/context-menu"

const DEFAULT_TAG_ICON = "📁"

type TagAction = {
  key: string
  label: string
  icon: LucideIcon
  onSelect: () => void
  className?: string
}

type LeftSidebarProps = {
  selectedFolder: string
  onFolderSelect: (folder: string) => void
  selectedTag: SelectedTag | null
  onTagClick: (tag: SelectedTag) => void
}

const folders = [
  { name: "All Files", icon: Folder, count: 8 },
  { name: "Projects", icon: Folder, count: 3 },
  { name: "Documents", icon: Folder, count: 2 },
  { name: "Images", icon: Folder, count: 1 },
  { name: "Videos", icon: Folder, count: 1 },
  { name: "Code", icon: Folder, count: 1 },
]

const systemFolders = [
  { name: "Starred", icon: Star, count: 0 },
  { name: "Recent", icon: Clock, count: 8 },
  { name: "Trash", icon: Trash2, count: 0 },
]

function TagTreeItem({
  tag,
  level = 0,
  selectedTag,
  onTagClick,
  onEditTag,
  onAddChildTag,
  onDeleteTag,
}: {
  tag: TagNode
  level?: number
  selectedTag: SelectedTag | null
  onTagClick: (tag: SelectedTag) => void
  onEditTag: (tag: TagNode) => void
  onAddChildTag: (parentTag: TagNode) => void
  onDeleteTag: (tagId: string) => void
}) {
  const [isExpanded, setIsExpanded] = useState(true)
  const [isPopoverOpen, setIsPopoverOpen] = useState(false)
  const hasChildren = tag.children && tag.children.length > 0
  const isSelected = selectedTag?.id === tag.id
  const isNamespace = tag.kind === "namespace"
  const canSelect = !isNamespace
  const canManage = tag.kind === "label"
  const canAddChild = tag.kind === "namespace" || tag.kind === "label"

  const displayIcon = tag.icon && tag.icon.trim() !== "" ? tag.icon : DEFAULT_TAG_ICON

  const tagActions: TagAction[] = [
    ...(canManage
      ? [
          {
            key: "edit",
            label: "Edit Tag",
            icon: Edit,
            onSelect: () => onEditTag(tag),
          } satisfies TagAction,
        ]
      : []),
    ...(canAddChild
      ? [
          {
            key: "add",
            label: "Add Tag",
            icon: Plus,
            onSelect: () => onAddChildTag(tag),
          } satisfies TagAction,
        ]
      : []),
    ...(canManage
      ? [
          {
            key: "delete",
            label: "Delete Tag",
            icon: Trash,
            onSelect: () => onDeleteTag(tag.id),
            className: "text-destructive focus:text-destructive",
          } satisfies TagAction,
        ]
      : []),
  ]

  return (
    <ContextMenu>
      <div>
        <ContextMenuTrigger asChild>
          <div
            className={cn(
              "w-full flex items-center gap-1 px-2 py-0.5 rounded-md text-sm transition-colors group min-w-0",
              isSelected
                ? "bg-sidebar-accent text-sidebar-accent-foreground font-medium"
                : "text-sidebar-foreground hover:bg-sidebar-accent/50",
            )}
            style={{ paddingLeft: `${level * 12 + 8}px` }}
          >
            {hasChildren && (
              <button
                type="button"
                onClick={(e) => {
                  e.stopPropagation()
                  setIsExpanded(!isExpanded)
                }}
                className="p-0 hover:bg-transparent flex-shrink-0"
              >
                {isExpanded ? (
                  <ChevronDown className="h-3 w-3 text-muted-foreground" />
                ) : (
                  <ChevronRight className="h-3 w-3 text-muted-foreground" />
                )}
              </button>
            )}
            {!hasChildren && <div className="w-3 flex-shrink-0" />}
            <div className="flex items-center gap-1.5 flex-1 min-w-0">
              <button
                type="button"
                onClick={() => {
                  if (canSelect) {
                    onTagClick({ id: tag.id, name: tag.name })
                  }
                }}
                className="flex items-center gap-1.5 flex-1 min-w-0 text-left disabled:cursor-default"
                disabled={!canSelect}
              >
                <div
                  className={cn(
                    "flex items-center gap-2 flex-1 min-w-0 rounded px-1.5 py-0.5 transition-colors",
                    canSelect ? "hover:bg-muted/60" : "opacity-80",
                  )}
                >
                  <span
                    className={cn(
                      "leftbar-tag-indicator h-2.5 w-2.5 flex-shrink-0 rounded-full border transition-all duration-200",
                      tag.color ? ["leftbar-tag-indicator--custom", tag.color] : null,
                    )}
                  />
                  <span className="text-xs flex-shrink-0">{displayIcon}</span>
                  <span className="truncate text-sm text-foreground" title={tag.name}>
                    {tag.name}
                  </span>
                </div>
              </button>

              {(canManage || canAddChild) && (
                <Popover open={isPopoverOpen} onOpenChange={setIsPopoverOpen}>
                  <PopoverTrigger asChild>
                    <Button
                      variant="ghost"
                      size="icon"
                      className="h-6 w-6 flex-shrink-0 opacity-0 group-hover:opacity-100 group-focus-within:opacity-100 data-[state=open]:opacity-100"
                      onClick={(e) => e.stopPropagation()}
                    >
                      <MoreVertical className="h-3 w-3" />
                    </Button>
                  </PopoverTrigger>
                  <PopoverContent className="w-48 p-1" align="end">
                    {tagActions.map((action) => (
                      <button
                        key={action.key}
                        type="button"
                        onClick={() => {
                          action.onSelect()
                          setIsPopoverOpen(false)
                        }}
                        className={cn(
                          "w-full flex items-center gap-2 px-2 py-1.5 text-md rounded hover:bg-accent",
                          action.className,
                        )}
                      >
                        <action.icon className="h-3 w-3" />
                        {action.label}
                      </button>
                    ))}
                  </PopoverContent>
                </Popover>
              )}
            </div>
          </div>
        </ContextMenuTrigger>

        {hasChildren && isExpanded && (
          <div className="space-y-0">
            {tag.children!.map((child) => (
              <TagTreeItem
                key={child.id}
                tag={child}
                level={level + 1}
                selectedTag={selectedTag}
                onTagClick={onTagClick}
                onEditTag={onEditTag}
                onAddChildTag={onAddChildTag}
                onDeleteTag={onDeleteTag}
              />
            ))}
          </div>
        )}
      </div>

      {tagActions.length > 0 && (
        <ContextMenuContent className="w-48">
          {tagActions.map((action) => (
            <ContextMenuItem
              key={action.key}
              onSelect={() => action.onSelect()}
              className={cn("flex items-center gap-2 text-sm", action.className)}
            >
              <action.icon className="h-3 w-3" />
              {action.label}
            </ContextMenuItem>
          ))}
        </ContextMenuContent>
      )}
    </ContextMenu>
  )
}

export function LeftSidebar({ selectedFolder, onFolderSelect, selectedTag, onTagClick }: LeftSidebarProps) {
  const [tagTree, setTagTree] = useState<TagNode[]>([])
  const [isTagDialogOpen, setIsTagDialogOpen] = useState(false)
  const [editingTag, setEditingTag] = useState<TagNode | null>(null)
  const [parentTag, setParentTag] = useState<TagNode | null>(null)
  const [dialogMode, setDialogMode] = useState<"create" | "edit" | "add-child">("create")
  const [user, setUser] = useState<UserType | null>(null)
  const [groups, setGroups] = useState<Group[]>([])
  const [isSigningOut, setIsSigningOut] = useState(false)
  const [sectionsExpanded, setSectionsExpanded] = useState({
    tags: true,
    folders: false,
    system: false,
  })

  useEffect(() => {
    fetchTags().then(setTagTree)
  }, [])

  useEffect(() => {
    fetchUser()
      .then(setUser)
      .catch(() => setUser(null))
  }, [])

  useEffect(() => {
    let active = true

    fetchGroups()
      .then((data) => {
        if (!active) return
        setGroups(data)
      })
      .catch((error) => {
        console.error("[sidebar] Failed to load groups:", error)
        if (!active) return
        setGroups([])
      })

    return () => {
      active = false
    }
  }, [])

  const handleEditTag = (tag: TagNode) => {
    setEditingTag(tag)
    setParentTag(null)
    setDialogMode("edit")
    setIsTagDialogOpen(true)
  }

  const handleAddChildTag = (parent: TagNode) => {
    setParentTag(parent)
    setEditingTag(null)
    setDialogMode("add-child")
    setIsTagDialogOpen(true)
  }

  const handleDeleteTag = async (tagId: string) => {
    await deleteTag(tagId)
    const updatedTags = await fetchTags()
    setTagTree(updatedTags)
  }

  const handleCreateNewTag = () => {
    setEditingTag(null)
    setParentTag(null)
    setDialogMode("create")
    setIsTagDialogOpen(true)
  }

  const handleSaveTag = async (data: TagUpdateData) => {
    if (dialogMode === "edit" && editingTag) {
      await updateTag(editingTag, data)
    } else if (dialogMode === "add-child" && parentTag) {
      await createTag(data, parentTag)
    } else {
      await createTag(data)
    }
    const updatedTags = await fetchTags()
    setTagTree(updatedTags)
  }

  const handleSignOut = async () => {
    setIsSigningOut(true)
    try {
      await signOut()
      window.location.href = "/login"
    } catch (error) {
      console.error("[ui] Failed to sign out:", error)
    } finally {
      setIsSigningOut(false)
    }
  }

  const primaryRole = useMemo(() => user?.roles?.[0] ?? "", [user?.roles])
  const primaryGroupName = useMemo(() => {
    if (!user?.primaryGroupId) {
      return ""
    }

    return groups.find((group) => group.id === user.primaryGroupId)?.name ?? ""
  }, [groups, user?.primaryGroupId])

  const secondaryGroupNames = useMemo(() => {
    if (!user?.groupIds?.length) {
      return [] as string[]
    }

    return user.groupIds
      .filter((groupId) => groupId !== user.primaryGroupId)
      .map((groupId) => groups.find((group) => group.id === groupId)?.name)
      .filter((name): name is string => Boolean(name))
  }, [groups, user?.groupIds, user?.primaryGroupId])
  const toggleSection = (section: "tags" | "folders" | "system") => {
    setSectionsExpanded((prev) => ({ ...prev, [section]: !prev[section] }))
  }

  return (
    <div className="w-full h-full border-r border-sidebar-border border-t-0 bg-sidebar text-sidebar-foreground flex flex-col">
      <div className="flex-1 min-h-0 flex flex-col">
        <div className="flex-1 min-h-0 flex flex-col gap-3 p-3">
          <div
            className={cn(
              "rounded-lg border leftbar-section-surface",
              sectionsExpanded.tags ? "flex-1 min-h-0 flex flex-col" : "",
            )}
          >
            <button
              type="button"
              onClick={() => toggleSection("tags")}
              className="flex w-full items-center justify-between gap-2 rounded-lg px-3 py-2 text-xs font-semibold uppercase tracking-wider text-muted-foreground transition-colors hover:bg-sidebar-accent/50"
            >
              <span className="flex items-center gap-2 text-[11px]">
                <Tag className="h-3 w-3" />
                Tags
              </span>
              <div className="flex items-center gap-1">
                <Button
                  variant="ghost"
                  size="icon"
                  className="h-6 w-6"
                  title="Create new tag"
                  onClick={(event) => {
                    event.stopPropagation()
                    handleCreateNewTag()
                  }}
                >
                  <Plus className="h-3 w-3" />
                </Button>
                <ChevronDown
                  className={cn(
                    "h-4 w-4 text-muted-foreground transition-transform",
                    sectionsExpanded.tags ? "rotate-180" : "",
                  )}
                />
              </div>
            </button>

            {sectionsExpanded.tags ? (
              <ScrollArea className="flex-1 min-h-0 px-2 pb-3 pt-2">
                <div className="space-y-0 pr-3">
                  {tagTree.map((tag) => (
                    <TagTreeItem
                      key={tag.id}
                      tag={tag}
                      selectedTag={selectedTag}
                      onTagClick={onTagClick}
                      onEditTag={handleEditTag}
                      onAddChildTag={handleAddChildTag}
                      onDeleteTag={handleDeleteTag}
                    />
                  ))}
                </div>
              </ScrollArea>
            ) : null}
          </div>

          <div className="rounded-lg border leftbar-section-surface">
            <button
              type="button"
              onClick={() => toggleSection("folders")}
              className="flex w-full items-center justify-between gap-2 rounded-lg px-3 py-2 text-[11px] font-semibold uppercase tracking-wider text-muted-foreground transition-colors hover:bg-sidebar-accent/50"
            >
              <span>Folders</span>
              <ChevronDown
                className={cn(
                  "h-4 w-4 text-muted-foreground transition-transform",
                  sectionsExpanded.folders ? "rotate-180" : "",
                )}
              />
            </button>

            {sectionsExpanded.folders ? (
              <div className="px-2 pb-2 pt-1 space-y-1">
                {folders.map((folder) => {
                  const Icon = folder.icon
                  return (
                    <button
                      key={folder.name}
                      onClick={() => onFolderSelect(folder.name)}
                      className={cn(
                        "w-full flex items-center justify-between rounded-md px-2 py-1.5 text-sm transition-colors",
                        selectedFolder === folder.name
                          ? "bg-sidebar-accent text-sidebar-accent-foreground font-medium"
                          : "text-sidebar-foreground hover:bg-sidebar-accent/50",
                      )}
                    >
                      <div className="flex items-center gap-2">
                        <Icon className="h-4 w-4" />
                        <span>{folder.name}</span>
                      </div>
                      <span className="text-[11px] text-muted-foreground">{folder.count}</span>
                    </button>
                  )
                })}
              </div>
            ) : null}
          </div>

          <div className="rounded-lg border leftbar-section-surface">
            <button
              type="button"
              onClick={() => toggleSection("system")}
              className="flex w-full items-center justify-between gap-2 rounded-lg px-3 py-2 text-[11px] font-semibold uppercase tracking-wider text-muted-foreground transition-colors hover:bg-sidebar-accent/50"
            >
              <span>System</span>
              <ChevronDown
                className={cn(
                  "h-4 w-4 text-muted-foreground transition-transform",
                  sectionsExpanded.system ? "rotate-180" : "",
                )}
              />
            </button>

            {sectionsExpanded.system ? (
              <div className="px-2 pb-2 pt-1 space-y-1">
                {systemFolders.map((folder) => {
                  const Icon = folder.icon
                  return (
                    <button
                      key={folder.name}
                      onClick={() => onFolderSelect(folder.name)}
                      className={cn(
                        "w-full flex items-center justify-between rounded-md px-2 py-1.5 text-sm transition-colors",
                        selectedFolder === folder.name
                          ? "bg-sidebar-accent text-sidebar-accent-foreground font-medium"
                          : "text-sidebar-foreground hover:bg-sidebar-accent/50",
                      )}
                    >
                      <div className="flex items-center gap-2">
                        <Icon className="h-4 w-4" />
                        <span>{folder.name}</span>
                      </div>
                      <span className="text-[11px] text-muted-foreground">{folder.count}</span>
                    </button>
                  )
                })}
              </div>
            ) : null}
          </div>
        </div>
      </div>

      <div className="border-t border-sidebar-border p-3 space-y-3">
        <ThemeSwitcher className="w-full" />

        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button
              variant="ghost"
              size="lg"
              className="w-full justify-start gap-4 px-4 py-3 h-auto min-h-[4.5rem] rounded-xl border leftbar-section-surface text-left transition-colors hover:bg-sidebar-accent/50"
            >
              <Avatar className="h-12 w-12 border border-sidebar-border/70 bg-background/80">
                <AvatarImage src={user?.avatar || "/default-user-icon.svg"} alt={user?.displayName} />
                <AvatarFallback>{user?.displayName?.charAt(0) || "U"}</AvatarFallback>
              </Avatar>
              <div className="flex flex-col items-start text-left gap-0.5">
                <span className="text-sm font-semibold truncate w-full">{user?.displayName || "Loading..."}</span>
                <span className="text-xs text-muted-foreground truncate w-full">
                  {primaryGroupName || primaryRole || ""}
                </span>
              </div>
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="start" sideOffset={8} className="w-60">
            <DropdownMenuLabel>
              <div className="flex flex-col gap-1">
                <span className="font-medium">{user?.displayName || ""}</span>
                <span className="text-xs text-muted-foreground font-normal">{user?.email || ""}</span>
                {primaryGroupName && (
                  <span className="text-xs text-muted-foreground font-normal">Primary group: {primaryGroupName}</span>
                )}
                {secondaryGroupNames.length > 0 && (
                  <span className="text-xs text-muted-foreground font-normal">
                    Also in: {secondaryGroupNames.join(", ")}
                  </span>
                )}
                {primaryRole && (
                  <span className="text-xs text-muted-foreground font-normal">Role: {primaryRole}</span>
                )}
              </div>
            </DropdownMenuLabel>
            <DropdownMenuSeparator />
            <DropdownMenuItem asChild>
              <a href="/me/" className="cursor-pointer">
                <User className="mr-2 h-4 w-4" />
                Profile
              </a>
            </DropdownMenuItem>
            <DropdownMenuItem asChild>
              <a href="/settings" className="cursor-pointer">
                <Settings className="mr-2 h-4 w-4" />
                Settings
              </a>
            </DropdownMenuItem>
            <DropdownMenuSeparator />
            <DropdownMenuItem
              className="text-red-600"
              variant="destructive"
              onSelect={(event) => {
                event.preventDefault()
                handleSignOut()
              }}
              disabled={isSigningOut}
            >
              <LogOut className="mr-2 h-4 w-4" />
              {isSigningOut ? "Signing out..." : "Logout"}
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>

      <TagManagementDialog
        open={isTagDialogOpen}
        onOpenChange={setIsTagDialogOpen}
        mode={dialogMode}
        editingTag={editingTag ?? undefined}
        parentTag={parentTag ?? undefined}
        onSave={handleSaveTag}
      />
    </div>
  )
}
