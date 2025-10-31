"use client"

import { useEffect, useMemo, useState } from "react"

import { CheckCircle2, ChevronDown, ChevronRight, Loader2 } from "lucide-react"
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
      if (next.has(tag.id)) {
        if (initialTagIds.has(tag.id)) {
          return next
        }
        next.delete(tag.id)
      } else {
        next.add(tag.id)
      }
      return next
    })
  }

  const renderTagTree = (nodes: TagNode[], level = 0): JSX.Element[] => {
    return nodes.map((tag) => {
      const hasChildren = Boolean(tag.children && tag.children.length > 0)
      const isExpanded = expandedTags[tag.id] ?? true
      const isSelected = selectedTagIds.has(tag.id)
      const isLocked = initialTagIds.has(tag.id)
      const canSelect = isSelectableTag(tag) && !isLocked
      const displayIcon = tag.iconKey && tag.iconKey.trim() !== "" ? tag.iconKey : DEFAULT_TAG_ICON
      const backgroundStyle = tag.color
        ? {
            backgroundColor: tag.color,
            borderColor: tag.color,
          }
        : undefined

      return (
        <div key={tag.id} className="space-y-2">
          <div
            className={cn(
              "flex items-center gap-1 rounded-md text-sm transition-colors group",
              isSelected
                ? "bg-primary/10 text-primary border border-primary/30"
                : "hover:bg-muted/60 border border-transparent text-muted-foreground",
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

            <button
              type="button"
              onClick={() => toggleTagSelection(tag)}
              disabled={!canSelect}
              className={cn(
                "flex items-center gap-3 flex-1 min-w-0 rounded-md px-3 py-2 text-left transition",
                !tag.color ? "bg-muted/60" : "",
                canSelect ? "text-foreground" : "text-muted-foreground cursor-default opacity-80",
                isSelected ? "ring-1 ring-primary" : "",
              )}
              style={backgroundStyle}
            >
              <span className="text-sm flex-shrink-0">{displayIcon}</span>
              <span className="truncate">{tag.name}</span>
              {isLocked ? (
                <span className="ml-auto text-[10px] uppercase tracking-wide text-primary/70">Assigned</span>
              ) : null}
            </button>

            {isSelected ? <CheckCircle2 className="mr-2 h-4 w-4 text-primary" /> : null}
          </div>

          {hasChildren && isExpanded ? (
            <div className="space-y-2">{renderTagTree(tag.children!, level + 1)}</div>
          ) : null}
        </div>
      )
    })
  }

  const buildSelectedTags = (): SelectedTag[] => {
    return newTagIds
      .map((id) => tagMap.get(id))
      .filter((node): node is TagNode => Boolean(node))
      .map((node) => ({
        id: node.id,
        name: node.name,
        namespaceId: node.namespaceId,
      }))
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

  const handleApplyTags = async () => {
    if (!file) {
      return
    }

    const selections = buildSelectedTags()

    if (selections.length === 0) {
      toast({
        title: "No new tags selected",
        description: "Select one or more tags to add to this document.",
      })
      return
    }

    setIsSaving(true)
    try {
      await applyTagsToDocument(file.id, selections)
      const documentTags = toDocumentTags(selections)
      onTagsAssigned?.(file.id, documentTags)
      toast({
        title: "Tags added",
        description: `Added ${documentTags.length} tag${documentTags.length === 1 ? "" : "s"} to “${file.name}”.`,
      })
      onOpenChange(false)
    } catch (error) {
      console.error(`[ui] Failed to assign tags to document '${file.id}':`, error)
      toast({
        title: "Unable to add tags",
        description:
          error instanceof Error
            ? error.message
            : "An unexpected error occurred while adding tags. Please try again.",
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
              ? `Select tags below to add them to “${file.name}”. Tags already applied cannot be removed here.`
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
              {newTagIds.length > 0
                ? `Ready to add ${newTagIds.length} tag${newTagIds.length === 1 ? "" : "s"}.`
                : "Choose tags from the list below to add them to the document."}
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
          <Button onClick={handleApplyTags} disabled={isSaving || !file || newTagIds.length === 0}>
            {isSaving ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : null}
            Add Tags
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
