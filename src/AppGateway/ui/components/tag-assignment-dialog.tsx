"use client"

import { useEffect, useMemo, useState } from "react"

import { CheckCircle2, ChevronDown, ChevronRight, Loader2, MinusCircle } from "lucide-react"
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import { ScrollArea } from "@/components/ui/scroll-area"
import { Badge } from "@/components/ui/badge"
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
  return !tag.kind || tag.kind === "label" || tag.kind === "namespace"
}

function collectNamespaceLabels(namespace: TagNode): TagNode[] {
  const labels: TagNode[] = []

  const visit = (node?: TagNode | null) => {
    if (!node) {
      return
    }

    if (!node.kind || node.kind === "label") {
      labels.push(node)
    }

    if (node.children?.length) {
      node.children.forEach((child) => visit(child))
    }
  }

  namespace.children?.forEach((child) => visit(child))
  return labels
}

type TagAssignmentDialogProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
  file: FileItem | null
  onTagsAssigned?: (fileId: string, tags: DocumentTag[]) => void
}

export function TagAssignmentDialog({ open, onOpenChange, file, onTagsAssigned }: TagAssignmentDialogProps) {
  const [tagTree, setTagTree] = useState<TagNode[]>([])
  const [expandedTags, setExpandedTags] = useState<Record<string, boolean>>({})
  const [selectedTagIds, setSelectedTagIds] = useState<Set<string>>(new Set())
  const [isLoading, setIsLoading] = useState(false)
  const [isSaving, setIsSaving] = useState(false)
  const { toast } = useToast()

  const initialTagIds = useMemo(() => new Set(file?.tags.map((tag) => tag.id) ?? []), [file])
  const tagMap = useMemo(() => flattenTagTree(tagTree), [tagTree])
  const newTagIds = useMemo(
    () => Array.from(selectedTagIds).filter((id) => !initialTagIds.has(id)),
    [selectedTagIds, initialTagIds],
  )
  const removedTagIds = useMemo(() => {
    const removed: string[] = []
    initialTagIds.forEach((id) => {
      if (!selectedTagIds.has(id)) {
        removed.push(id)
      }
    })
    return removed
  }, [initialTagIds, selectedTagIds])
  const hasChanges = newTagIds.length > 0 || removedTagIds.length > 0

  useEffect(() => {
    if (!open) {
      setSelectedTagIds(new Set())
      return
    }

    setSelectedTagIds(new Set(file?.tags.map((tag) => tag.id) ?? []))
  }, [open, file])

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

  const toggleTagSelection = (tag: TagNode) => {
    if (!isSelectableTag(tag)) {
      return
    }

    setSelectedTagIds((previous) => {
      const next = new Set(previous)
      next.has(tag.id) ? next.delete(tag.id) : next.add(tag.id)
      return next
    })
  }

  const renderTagTree = (nodes: TagNode[], level = 0): JSX.Element[] => {
    return nodes.map((tag) => {
      const hasChildren = Boolean(tag.children && tag.children.length > 0)
      const isExpanded = expandedTags[tag.id] ?? true
      const isSelected = selectedTagIds.has(tag.id)
      const isInitiallyAssigned = initialTagIds.has(tag.id)
      const willRemove = isInitiallyAssigned && !isSelected
      const canSelect = isSelectableTag(tag)
      const isNamespace = tag.kind === "namespace"
      const displayIcon = tag.iconKey && tag.iconKey.trim() !== "" ? tag.iconKey : DEFAULT_TAG_ICON
      const indicatorStyle = tag.color
        ? {
            backgroundColor: tag.color,
            borderColor: tag.color,
          }
        : undefined

      const statusLabel = isInitiallyAssigned
        ? isSelected
          ? "Assigned"
          : "Will remove"
        : isSelected
          ? "Will add"
          : null

      return (
        <div key={tag.id} className="space-y-1">
          <div
            className={cn(
              "w-full flex items-center gap-1 px-2 py-0.5 rounded-md text-sm transition-colors group min-w-0",
              isNamespace ? "hover:bg-transparent" : null,
              willRemove
                ? "border border-destructive/40 bg-destructive/10 text-destructive"
                : isSelected
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
                {isExpanded ? (
                  <ChevronDown className="h-3 w-3" />
                ) : (
                  <ChevronRight className="h-3 w-3" />
                )}
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
                    isSelected && !willRemove ? "ring-1 ring-primary/60 bg-primary/5" : null,
                    willRemove ? "ring-1 ring-destructive/60 bg-destructive/5" : null,
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
                            willRemove
                              ? "text-destructive"
                              : isSelected
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
                            willRemove
                              ? "text-destructive"
                              : isSelected
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

              {isSelected ? (
                <CheckCircle2 className="mr-2 h-4 w-4 text-primary" />
              ) : willRemove ? (
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

  const buildTagsToAdd = (): SelectedTag[] => {
    const selections = new Map<string, SelectedTag>()

    for (const id of newTagIds) {
      const node = tagMap.get(id)
      if (!node) {
        continue
      }

      const targetNodes =
        node.kind === "namespace"
          ? collectNamespaceLabels(node)
          : !node.kind || node.kind === "label"
            ? [node]
            : []

      for (const target of targetNodes) {
        if (initialTagIds.has(target.id)) {
          continue
        }

        if (!selections.has(target.id)) {
          selections.set(target.id, {
            id: target.id,
            name: target.name,
            namespaceId: target.namespaceId,
          })
        }
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
    additions: DocumentTag[],
    removals: string[],
  ): DocumentTag[] => {
    const remaining = file?.tags.filter((tag) => !removals.includes(tag.id)) ?? []
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
    if (!file) {
      return
    }

    const selections = buildTagsToAdd()

    if (selections.length === 0 && removedTagIds.length === 0) {
      toast({
        title: "No tag changes",
        description: "Select tags to add or remove before saving.",
      })
      return
    }

    setIsSaving(true)
    try {
      await applyTagsToDocument(file.id, selections, removedTagIds)
      const documentTags = toDocumentTags(selections)
      const updatedTags = buildUpdatedDocumentTags(documentTags, removedTagIds)
      onTagsAssigned?.(file.id, updatedTags)
      const additionsCount = documentTags.length
      const removalsCount = removedTagIds.length
      const actionParts: string[] = []
      if (additionsCount > 0) {
        actionParts.push(`added ${additionsCount} tag${additionsCount === 1 ? "" : "s"}`)
      }
      if (removalsCount > 0) {
        actionParts.push(`removed ${removalsCount} tag${removalsCount === 1 ? "" : "s"}`)
      }
      toast({
        title: "Tags updated",
        description:
          actionParts.length > 0
            ? `Successfully ${actionParts.join(" and ")} for “${file.name}”.`
            : `No tag changes were applied to “${file.name}”.`,
      })
      onOpenChange(false)
    } catch (error) {
      console.error(`[ui] Failed to assign tags to document '${file.id}':`, error)
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

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-2xl sm:max-w-3xl">
        <DialogHeader>
          <DialogTitle>Assign tags</DialogTitle>
          <DialogDescription>
            {file
              ? `Select tags below to add to or remove from “${file.name}”.`
              : "Select a file to assign tags."}
          </DialogDescription>
        </DialogHeader>

        {isLoading ? (
          <div className="flex h-40 items-center justify-center">
            <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
          </div>
        ) : (
          <div className="flex flex-col gap-4">
            {file ? (
              <div className="space-y-2">
                <p className="text-xs font-medium text-muted-foreground uppercase tracking-wider">Currently assigned</p>
                <div className="flex flex-wrap gap-1.5">
                  {file.tags.length > 0 ? (
                    file.tags.map((tag) => (
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

            <div className="text-sm text-muted-foreground">
              {hasChanges
                ? [
                    newTagIds.length > 0
                      ? `Adding ${newTagIds.length} tag${newTagIds.length === 1 ? "" : "s"}`
                      : null,
                    removedTagIds.length > 0
                      ? `Removing ${removedTagIds.length} tag${removedTagIds.length === 1 ? "" : "s"}`
                      : null,
                  ]
                    .filter(Boolean)
                    .join(" · ")
                : "Choose tags from the list below to update the document."}
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
          <Button onClick={handleApplyTags} disabled={isSaving || !file || !hasChanges}>
            {isSaving ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : null}
            Save changes
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
