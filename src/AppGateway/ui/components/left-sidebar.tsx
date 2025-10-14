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
} from "lucide-react"
import { cn } from "@/lib/utils"
import { useState, useEffect } from "react"
import { Button } from "@/components/ui/button"
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover"
import { TagManagementDialog } from "./tag-management-dialog"
import type { TagNode, TagUpdateData } from "@/lib/types"
import { fetchTags, createTag, updateTag, deleteTag } from "@/lib/api"

type LeftSidebarProps = {
  selectedFolder: string
  onFolderSelect: (folder: string) => void
  selectedTag: string | null
  onTagClick: (tagName: string) => void
  onCollapse: () => void
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
  selectedTag: string | null
  onTagClick: (tagName: string) => void
  onEditTag: (tag: TagNode) => void
  onAddChildTag: (parentTag: TagNode) => void
  onDeleteTag: (tagId: string) => void
}) {
  const [isExpanded, setIsExpanded] = useState(true)
  const [isPopoverOpen, setIsPopoverOpen] = useState(false)
  const hasChildren = tag.children && tag.children.length > 0
  const isSelected = selectedTag === tag.name

  return (
    <div>
      <div
        className={cn(
          "w-full flex items-center gap-1 px-2 py-0.5 rounded-md text-sm transition-colors group",
          isSelected
            ? "bg-sidebar-accent text-sidebar-accent-foreground font-medium"
            : "text-sidebar-foreground hover:bg-sidebar-accent/50",
        )}
        style={{ paddingLeft: `${level * 12 + 8}px` }}
      >
        {hasChildren && (
          <button
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

        <button onClick={() => onTagClick(tag.name)} className="flex items-center gap-1.5 flex-1 min-w-0 text-left">
          <div className={cn("flex items-center gap-1 px-1.5 py-0.5 rounded flex-1 min-w-0", tag.color)}>
            {tag.icon && <span className="text-xs flex-shrink-0">{tag.icon}</span>}
            <span className="truncate text-sm text-foreground">{tag.name}</span>
          </div>
        </button>

        <Popover open={isPopoverOpen} onOpenChange={setIsPopoverOpen}>
          <PopoverTrigger asChild>
            <Button
              variant="ghost"
              size="icon"
              className="h-6 w-6 opacity-0 group-hover:opacity-100 flex-shrink-0"
              onClick={(e) => e.stopPropagation()}
            >
              <MoreVertical className="h-3 w-3" />
            </Button>
          </PopoverTrigger>
          <PopoverContent className="w-48 p-1" align="end">
            <button
              onClick={() => {
                onEditTag(tag)
                setIsPopoverOpen(false)
              }}
              className="w-full flex items-center gap-2 px-2 py-1.5 text-sm rounded hover:bg-accent"
            >
              <Edit className="h-3 w-3" />
              Edit Tag
            </button>
            <button
              onClick={() => {
                onAddChildTag(tag)
                setIsPopoverOpen(false)
              }}
              className="w-full flex items-center gap-2 px-2 py-1.5 text-sm rounded hover:bg-accent"
            >
              <Plus className="h-3 w-3" />
              Add Child Tag
            </button>
            <button
              onClick={() => {
                onDeleteTag(tag.id)
                setIsPopoverOpen(false)
              }}
              className="w-full flex items-center gap-2 px-2 py-1.5 text-sm rounded hover:bg-accent text-destructive"
            >
              <Trash className="h-3 w-3" />
              Delete Tag
            </button>
          </PopoverContent>
        </Popover>
      </div>

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
  )
}

export function LeftSidebar({ selectedFolder, onFolderSelect, selectedTag, onTagClick, onCollapse }: LeftSidebarProps) {
  const [tagTree, setTagTree] = useState<TagNode[]>([])
  const [isTagDialogOpen, setIsTagDialogOpen] = useState(false)
  const [editingTag, setEditingTag] = useState<TagNode | null>(null)
  const [parentTag, setParentTag] = useState<TagNode | null>(null)
  const [dialogMode, setDialogMode] = useState<"create" | "edit" | "add-child">("create")

  useEffect(() => {
    fetchTags().then(setTagTree)
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
      await updateTag(editingTag.id, data)
    } else if (dialogMode === "add-child" && parentTag) {
      await createTag(data, parentTag.id)
    } else {
      await createTag(data)
    }
    const updatedTags = await fetchTags()
    setTagTree(updatedTags)
  }

  return (
    <div className="w-full h-full border-r border-border bg-sidebar flex flex-col">
      <div className="flex-1 overflow-y-auto">
        <div className="p-3">
          <div className="flex items-center justify-between mb-2 px-2">
            <h2 className="text-xs font-semibold text-muted-foreground uppercase tracking-wider flex items-center gap-2">
              <Tag className="h-3 w-3" />
              Tags
            </h2>
            <Button variant="ghost" size="icon" className="h-6 w-6" onClick={handleCreateNewTag} title="Create new tag">
              <Plus className="h-3 w-3" />
            </Button>
          </div>
          <div className="space-y-0">
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
        </div>

        <div className="p-3 border-t border-sidebar-border">
          <h2 className="text-xs font-semibold text-muted-foreground uppercase tracking-wider mb-2 px-2">Folders</h2>
          <div className="space-y-1">
            {folders.map((folder) => {
              const Icon = folder.icon
              return (
                <button
                  key={folder.name}
                  onClick={() => onFolderSelect(folder.name)}
                  className={cn(
                    "w-full flex items-center justify-between px-3 py-2 rounded-md text-sm transition-colors",
                    selectedFolder === folder.name
                      ? "bg-sidebar-accent text-sidebar-accent-foreground font-medium"
                      : "text-sidebar-foreground hover:bg-sidebar-accent/50",
                  )}
                >
                  <div className="flex items-center gap-2">
                    <Icon className="h-4 w-4" />
                    <span>{folder.name}</span>
                  </div>
                  <span className="text-xs text-muted-foreground">{folder.count}</span>
                </button>
              )
            })}
          </div>
        </div>

        <div className="p-3 border-t border-sidebar-border">
          <h2 className="text-xs font-semibold text-muted-foreground uppercase tracking-wider mb-2 px-2">System</h2>
          <div className="space-y-1">
            {systemFolders.map((folder) => {
              const Icon = folder.icon
              return (
                <button
                  key={folder.name}
                  onClick={() => onFolderSelect(folder.name)}
                  className={cn(
                    "w-full flex items-center justify-between px-3 py-2 rounded-md text-sm transition-colors",
                    selectedFolder === folder.name
                      ? "bg-sidebar-accent text-sidebar-accent-foreground font-medium"
                      : "text-sidebar-foreground hover:bg-sidebar-accent/50",
                  )}
                >
                  <div className="flex items-center gap-2">
                    <Icon className="h-4 w-4" />
                    <span>{folder.name}</span>
                  </div>
                  <span className="text-xs text-muted-foreground">{folder.count}</span>
                </button>
              )
            })}
          </div>
        </div>
      </div>

      <TagManagementDialog
        open={isTagDialogOpen}
        onOpenChange={setIsTagDialogOpen}
        mode={dialogMode}
        editingTag={editingTag}
        parentTag={parentTag}
        onSave={handleSaveTag}
      />
    </div>
  )
}
