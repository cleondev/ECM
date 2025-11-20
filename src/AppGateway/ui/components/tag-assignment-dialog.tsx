"use client"

import { useEffect, useMemo, useState } from "react"

import { CheckCircle2, ChevronDown, ChevronRight, Loader2, MinusCircle } from "lucide-react"
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import { ScrollArea } from "@/components/ui/scroll-area"
import { Badge } from "@/components/ui/badge"
import { Switch } from "@/components/ui/switch"
import { cn } from "@/lib/utils"
import { applyTagsToDocument, fetchTags } from "@/lib/api"
import type { DocumentTag, FileItem, SelectedTag, TagNode } from "@/lib/types"
import { useToast } from "@/hooks/use-toast"

const DEFAULT_TAG_ICON = "📁"

function flattenTagTree(nodes: TagNode[]): Map<string, TagNode> {
  const map = new Map<string, TagNode>()

  const visit = (items: TagNode[]) => {
    for (const item of items) {
      map.set(item.id, item)
      if (item.children?.length) {
        visit(item.children)
      }
    }
  }

  visit(nodes)
  return map
}

function expandAllNodes(nodes: TagNode[], initial: Record<string, boolean> = {}) {
  for (const node of nodes) {
    if (!(node.id in initial)) {
      initial[node.id] = true
    }
    if (node.children?.length) {
      expandAllNodes(node.children, initial)
    }
  }
  return initial
}

function isSelectableTag(tag: TagNode) {
  return !tag.kind || tag.kind === "label"
}

type TagAssignmentDialogProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
  files: FileItem[]
  onTagsAssigned?: (updates: Array<{ fileId: string; tags: DocumentTag[] }>) => void
}

type SelectionMode = "add" | "remove"

export function TagAssignmentDialog({ open, onOpenChange, files, onTagsAssigned }: TagAssignmentDialogProps) {
  const [tagTree, setTagTree] = useState<TagNode[]>([])
  const [expandedTags, setExpandedTags] = useState<Record<string, boolean>>({})
  const [tagsToAdd, setTagsToAdd] = useState<Set<string>>(new Set())
  const [tagsToRemove, setTagsToRemove] = useState<Set<string>>(new Set())
  const [selectionMode, setSelectionMode] = useState<SelectionMode>("add")
  const [isLoading, setIsLoading] = useState(false)
  const [isSaving, setIsSaving] = useState(false)
  const [autoSelectParents, setAutoSelectParents] = useState(false)
  const [autoParentByChild, setAutoParentByChild] = useState<Map<string, Set<string>>>(new Map())
  const [explicitAddSelections, setExplicitAddSelections] = useState<Set<string>>(new Set())
  const { toast } = useToast()

  const tagMap = useMemo(() => flattenTagTree(tagTree), [tagTree])
  const primaryFile = files[0] ?? null
  const isBulkMode = files.length > 1
  const existingTagIds = useMemo(() => {
    const present = new Set<string>()
    for (const file of files) {
      for (const tag of file.tags) {
        present.add(tag.id)
      }
    }
    return present
  }, [files])

  const tagsOnAllFiles = useMemo(() => {
    if (files.length === 0) {
      return new Set<string>()
    }

    const [firstFile, ...rest] = files
    const intersection = new Set(firstFile.tags.map((tag) => tag.id))

    for (const file of rest) {
      for (const tagId of Array.from(intersection)) {
        if (!file.tags.some((tag) => tag.id === tagId)) {
          intersection.delete(tagId)
        }
      }
      if (intersection.size === 0) {
        break
      }
    }

    return intersection
  }, [files])
  const hasChanges = tagsToAdd.size > 0 || tagsToRemove.size > 0

  const getAncestorIds = (tagId: string) => {
    const ancestors: string[] = []
    let current = tagMap.get(tagId)

    while (current?.parentId) {
      const parent = tagMap.get(current.parentId)
      if (!parent) break
      ancestors.push(parent.id)
      current = parent
    }

    return ancestors
  }

  useEffect(() => {
    if (!open) {
      setTagsToAdd(new Set())
      setTagsToRemove(new Set())
      setSelectionMode("add")
      setAutoSelectParents(false)
      setAutoParentByChild(new Map())
      setExplicitAddSelections(new Set())
      return
    }

    setTagsToAdd(new Set())
    setTagsToRemove(new Set())
    setAutoParentByChild(new Map())
    setExplicitAddSelections(new Set())
  }, [open, files])

  useEffect(() => {
    if (!open) {
      return
    }

    setIsLoading(true)
    fetchTags()
      .then((nodes) => {
        setTagTree(nodes)
        setExpandedTags((previous) => expandAllNodes(nodes, { ...previous }))
      })
      .catch((error) => {
        console.error("[ui] Failed to load tags for assignment:", error)
        toast({
          title: "Unable to load tags",
          description: "We couldn't load tags right now. Please try again later.",
          variant: "destructive",
        })
      })
      .finally(() => {
        setIsLoading(false)
      })
  }, [open, toast])

  const toggleTagExpansion = (tagId: string) => {
    setExpandedTags((previous) => ({ ...previous, [tagId]: !previous[tagId] }))
  }

  const handleAutoParentToggle = (enabled: boolean) => {
    if (!enabled) {
      const autoAddedParents = new Set<string>()
      for (const parents of autoParentByChild.values()) {
        for (const parentId of parents) {
          autoAddedParents.add(parentId)
        }
      }

      setTagsToAdd((previous) => {
        const next = new Set(previous)
        for (const parentId of autoAddedParents) {
          if (!explicitAddSelections.has(parentId)) {
            next.delete(parentId)
          }
        }
        return next
      })

      setAutoParentByChild(new Map())
    }

    setAutoSelectParents(enabled)
  }

  const toggleTagSelection = (tag: TagNode) => {
    if (!isSelectableTag(tag)) {
      return
    }

    if (selectionMode === "add") {
      const isSelected = tagsToAdd.has(tag.id)
      const nextTagsToAdd = new Set(tagsToAdd)
      const nextTagsToRemove = new Set(tagsToRemove)
      const nextExplicitSelections = new Set(explicitAddSelections)
      const nextAutoMap = new Map(autoParentByChild)

      if (isSelected) {
        nextTagsToAdd.delete(tag.id)
        nextExplicitSelections.delete(tag.id)

        const parents = nextAutoMap.get(tag.id)
        nextAutoMap.delete(tag.id)

        if (parents) {
          for (const parentId of parents) {
            const referencedElsewhere = Array.from(nextAutoMap.values()).some((set) => set.has(parentId))
            const isExplicitParent = nextExplicitSelections.has(parentId)

            if (!referencedElsewhere && !isExplicitParent) {
              nextTagsToAdd.delete(parentId)
            }
          }
        }
      } else {
        nextTagsToAdd.add(tag.id)
        nextExplicitSelections.add(tag.id)
        nextTagsToRemove.delete(tag.id)

        if (autoSelectParents) {
          const parentIds = new Set(getAncestorIds(tag.id))
          nextAutoMap.set(tag.id, parentIds)

          for (const parentId of parentIds) {
            nextTagsToAdd.add(parentId)
            nextTagsToRemove.delete(parentId)
          }
        } else {
          nextAutoMap.delete(tag.id)
        }
      }

      setTagsToAdd(nextTagsToAdd)
      setTagsToRemove(nextTagsToRemove)
      setExplicitAddSelections(nextExplicitSelections)
      setAutoParentByChild(nextAutoMap)

      return
    }

    setTagsToRemove((previous) => {
      const next = new Set(previous)
      next.has(tag.id) ? next.delete(tag.id) : next.add(tag.id)
      return next
    })
    setTagsToAdd((previous) => {
      if (!previous.has(tag.id)) {
        return previous
      }
      const next = new Set(previous)
      next.delete(tag.id)
      return next
    })
  }

  const buildTagsToAdd = (): SelectedTag[] => {
    const selections = new Map<string, SelectedTag>()

    for (const id of tagsToAdd) {
      const node = tagMap.get(id)
      if (!node || !isSelectableTag(node)) {
        continue
      }

      if (!selections.has(node.id)) {
        selections.set(node.id, {
          id: node.id,
          name: node.name,
          namespaceId: node.namespaceId,
        })
      }
    }

    return Array.from(selections.values())
  }

  const toDocumentTags = (selected: SelectedTag[]): DocumentTag[] => {
    return selected.map((tag) => {
      const node = tagMap.get(tag.id)
      return {
        id: tag.id,
        namespaceId: node?.namespaceId ?? tag.namespaceId ?? "",
        namespaceDisplayName: node?.namespaceLabel ?? null,
        parentId: node?.parentId ?? null,
        name: node?.name ?? tag.name,
        color: node?.color ?? null,
        iconKey: node?.iconKey ?? null,
        isSystem: node?.isSystem,
      }
    })
  }

  const buildUpdatedDocumentTags = (
    currentTags: DocumentTag[],
    additions: DocumentTag[],
    removals: string[],
  ): DocumentTag[] => {
    const remaining = currentTags.filter((tag) => !removals.includes(tag.id))
    const additionMap = new Map(additions.map((tag) => [tag.id, tag]))

    const merged = remaining.map((tag) => {
      const replacement = additionMap.get(tag.id)
      if (replacement) {
        additionMap.delete(tag.id)
        return { ...tag, ...replacement }
      }
      return tag
    })

    additionMap.forEach((tag) => {
      merged.push(tag)
    })

    return merged
  }

  const handleApplyTags = async () => {
    if (files.length === 0) {
      return
    }

    const selections = buildTagsToAdd()
    const removals = Array.from(tagsToRemove)

    if (selections.length === 0 && removals.length === 0) {
      toast({
        title: "No tag changes",
        description: "Select tags to add or remove before saving.",
      })
      return
    }

    setIsSaving(true)

    try {
      const updates = await Promise.all(
        files.map(async (file) => {
          const existingIds = new Set(file.tags.map((tag) => tag.id))
          const additionsForFile = selections.filter((tag) => !existingIds.has(tag.id))
          const removalsForFile = removals.filter((id) => existingIds.has(id))

          if (additionsForFile.length === 0 && removalsForFile.length === 0) {
            return null
          }

          await applyTagsToDocument(file.id, additionsForFile, removalsForFile)
          const updatedTags = buildUpdatedDocumentTags(
            file.tags,
            toDocumentTags(additionsForFile),
            removalsForFile,
          )

          return {
            fileId: file.id,
            tags: updatedTags,
            added: additionsForFile.length,
            removed: removalsForFile.length,
          }
        }),
      )

      const appliedUpdates = updates.filter(Boolean) as Array<{
        fileId: string
        tags: DocumentTag[]
        added: number
        removed: number
      }>

      if (appliedUpdates.length === 0) {
        toast({
          title: "No tag updates applied",
          description: "Selected tags already matched the chosen files.",
        })
        return
      }

      onTagsAssigned?.(appliedUpdates.map(({ fileId, tags }) => ({ fileId, tags })))

      const totalAdded = appliedUpdates.reduce((sum, update) => sum + update.added, 0)
      const totalRemoved = appliedUpdates.reduce((sum, update) => sum + update.removed, 0)

      const actionParts: string[] = []
      if (totalAdded > 0) {
        actionParts.push(`added ${totalAdded} tag${totalAdded === 1 ? "" : "s"}`)
      }
      if (totalRemoved > 0) {
        actionParts.push(`removed ${totalRemoved} tag${totalRemoved === 1 ? "" : "s"}`)
      }

      toast({
        title: "Tags updated",
        description:
          actionParts.length > 0
            ? `Successfully ${actionParts.join(" and ")} across ${appliedUpdates.length} file${
                appliedUpdates.length === 1 ? "" : "s"
              }.`
            : `No tag changes were applied to the selected files.`,
      })

      onOpenChange(false)
    } catch (error) {
      console.error(`[ui] Failed to assign tags to documents '${files.map((f) => f.id).join(",")}'`, error)
      toast({
        title: "Unable to update tags",
        description:
          error instanceof Error
            ? error.message
            : "An unexpected error occurred while updating tags. Please try again.",
        variant: "destructive",
      })
    } finally {
      setIsSaving(false)
    }
  }

  const renderTagTree = (nodes: TagNode[], level = 0): JSX.Element[] => {
    return nodes.map((tag) => {
      const hasChildren = Boolean(tag.children && tag.children.length > 0)
      const isExpanded = expandedTags[tag.id] ?? true
      const isSelectedForAdd = tagsToAdd.has(tag.id)
      const isSelectedForRemoval = tagsToRemove.has(tag.id)
      const canSelect = isSelectableTag(tag)
      const isNamespace = tag.kind === "namespace"
      const displayIcon = tag.iconKey && tag.iconKey.trim() !== "" ? tag.iconKey : DEFAULT_TAG_ICON
      const indicatorStyle = tag.color
        ? {
            backgroundColor: tag.color,
            borderColor: tag.color,
          }
        : undefined

      const statusLabel = isSelectedForAdd
        ? "Will add"
        : isSelectedForRemoval
          ? "Will remove"
          : tagsOnAllFiles.has(tag.id)
            ? "Already on all files"
            : existingTagIds.has(tag.id)
              ? "Already on some files"
              : null

      return (
        <div key={tag.id} className="space-y-1">
          <div
            className={cn(
              "w-full flex items-center gap-1 px-2 py-0.5 rounded-md text-sm transition-colors group min-w-0",
              isNamespace ? "hover:bg-transparent" : null,
              isSelectedForRemoval
                ? "border border-destructive/40 bg-destructive/10 text-destructive"
                : isSelectedForAdd
                  ? "bg-primary/10 text-primary border border-primary/30"
                  : "text-muted-foreground hover:bg-muted/50",
            )}
            style={{ paddingLeft: `${level * 12 + 8}px` }}
          >
            {hasChildren ? (
              <button
                type="button"
                onClick={(event) => {
                  event.stopPropagation()
                  toggleTagExpansion(tag.id)
                }}
                className="p-0.5 rounded hover:bg-muted/80 text-muted-foreground"
              >
                {isExpanded ? <ChevronDown className="h-3 w-3" /> : <ChevronRight className="h-3 w-3" />}
              </button>
            ) : (
              <span className="w-3" />
            )}

            <div className="flex items-center gap-1.5 flex-1 min-w-0">
              <button
                type="button"
                onClick={() => toggleTagSelection(tag)}
                disabled={!canSelect}
                className={cn(
                  "flex items-center gap-1.5 flex-1 min-w-0 text-left disabled:cursor-default",
                  !canSelect ? "opacity-70" : null,
                )}
              >
                <div
                  className={cn(
                    "flex flex-1 min-w-0 rounded px-2 py-1 transition-colors",
                    isNamespace
                      ? "flex-col items-start gap-1 bg-muted/40 border border-dashed border-border/70"
                      : "items-center gap-2 bg-card/70 hover:bg-muted/60",
                    canSelect ? "text-foreground" : "text-muted-foreground",
                    isSelectedForAdd && !isSelectedForRemoval ? "ring-1 ring-primary/60 bg-primary/5" : null,
                    isSelectedForRemoval ? "ring-1 ring-destructive/60 bg-destructive/5" : null,
                  )}
                >
                  {isNamespace ? (
                    <div className="flex w-full flex-col gap-1 min-w-0">
                      <span
                        className="truncate text-[10px] font-medium uppercase tracking-[0.16em] text-muted-foreground"
                        title={tag.name}
                      >
                        {tag.name}
                      </span>
                      {statusLabel ? (
                        <span
                          className={cn(
                            "text-[10px] uppercase tracking-wide",
                            isSelectedForRemoval
                              ? "text-destructive"
                              : isSelectedForAdd
                                ? "text-primary/70"
                                : "text-muted-foreground",
                          )}
                        >
                          {statusLabel}
                        </span>
                      ) : null}
                    </div>
                  ) : (
                    <>
                      <span
                        className={cn(
                          "leftbar-tag-indicator h-2.5 w-2.5 flex-shrink-0 rounded-full border transition-all duration-200",
                          tag.color ? "leftbar-tag-indicator--custom" : null,
                        )}
                        style={indicatorStyle}
                      />
                      <span className="text-xs flex-shrink-0">{displayIcon}</span>
                      <span className="truncate text-sm" title={tag.name}>
                        {tag.name}
                      </span>
                      {statusLabel ? (
                        <span
                          className={cn(
                            "ml-auto text-[10px] uppercase tracking-wide",
                            isSelectedForRemoval
                              ? "text-destructive"
                              : isSelectedForAdd
                                ? "text-primary/70"
                                : "text-muted-foreground",
                          )}
                        >
                          {statusLabel}
                        </span>
                      ) : null}
                    </>
                  )}
                </div>
              </button>

              {isSelectedForAdd ? (
                <CheckCircle2 className="mr-2 h-4 w-4 text-primary" />
              ) : isSelectedForRemoval ? (
                <MinusCircle className="mr-2 h-4 w-4 text-destructive" />
              ) : null}
            </div>
          </div>

          {hasChildren && isExpanded ? (
            <div className="space-y-1">{renderTagTree(tag.children!, level + 1)}</div>
          ) : null}
        </div>
      )
    })
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-2xl sm:max-w-3xl">
        <DialogHeader>
          <DialogTitle>Assign tags</DialogTitle>
          <DialogDescription>
            {files.length === 0
              ? "Select at least one file to assign tags."
              : isBulkMode
                ? `Select tags below to add to or remove from ${files.length} files. Tags already present on a file will be skipped.`
                : `Select tags below to add to or remove from “${primaryFile?.name}”.`}
          </DialogDescription>
        </DialogHeader>

        {isLoading ? (
          <div className="flex h-40 items-center justify-center">
            <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
          </div>
        ) : (
          <div className="flex flex-col gap-4">
            {primaryFile && !isBulkMode ? (
              <div className="space-y-2">
                <p className="text-xs font-medium text-muted-foreground uppercase tracking-wider">Currently assigned</p>
                <div className="flex flex-wrap gap-1.5">
                  {primaryFile.tags.length > 0 ? (
                    primaryFile.tags.map((tag) => (
                      <Badge
                        key={tag.id}
                        variant={tag.color ? "secondary" : "outline"}
                        className="text-xs"
                        style={tag.color ? { backgroundColor: tag.color, borderColor: tag.color } : undefined}
                      >
                        {tag.name}
                      </Badge>
                    ))
                  ) : (
                    <span className="text-sm italic text-muted-foreground">No tags assigned yet.</span>
                  )}
                </div>
              </div>
            ) : null}

            <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
              <div className="flex flex-wrap gap-2">
                <Button
                  variant={selectionMode === "add" ? "default" : "outline"}
                  size="sm"
                  onClick={() => setSelectionMode("add")}
                >
                  Add tags
                </Button>
                <Button
                  variant={selectionMode === "remove" ? "destructive" : "outline"}
                  size="sm"
                  onClick={() => setSelectionMode("remove")}
                >
                  Remove tags
                </Button>
              </div>

              <div className="flex items-start gap-3 rounded-md border border-border/60 bg-muted/30 p-3 sm:items-center">
                <Switch
                  id="auto-parent-tags"
                  checked={autoSelectParents}
                  disabled={selectionMode !== "add"}
                  onCheckedChange={handleAutoParentToggle}
                />
                <div className="space-y-1">
                  <label htmlFor="auto-parent-tags" className="text-sm font-medium leading-none">
                    Add parent tags automatically
                  </label>
                </div>
              </div>
            </div>

            <div className="text-sm text-muted-foreground">
              {hasChanges
                ? [
                    tagsToAdd.size > 0
                      ? `Adding ${tagsToAdd.size} tag${tagsToAdd.size === 1 ? "" : "s"}`
                      : null,
                    tagsToRemove.size > 0
                      ? `Removing ${tagsToRemove.size} tag${tagsToRemove.size === 1 ? "" : "s"}`
                      : null,
                  ]
                    .filter(Boolean)
                    .join(" · ")
                : "Choose tags from the list below to update the selected file(s)."}
            </div>

            <div className="border rounded-lg bg-muted/30">
              <ScrollArea className="h-[320px]">
                <div className="p-3 space-y-2">
                  {tagTree.length > 0 ? (
                    renderTagTree(tagTree)
                  ) : (
                    <p className="text-sm text-muted-foreground px-2 py-8 text-center">No tags available.</p>
                  )}
                </div>
              </ScrollArea>
            </div>
          </div>
        )}

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)} disabled={isSaving}>
            Cancel
          </Button>
          <Button onClick={handleApplyTags} disabled={isSaving || files.length === 0 || !hasChanges}>
            {isSaving ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : null}
            Save changes
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
