"use client"

import { useState, useEffect } from "react"
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Checkbox } from "@/components/ui/checkbox"
import { cn } from "@/lib/utils"
import type { TagNode, TagUpdateData } from "@/lib/types"

type TagManagementDialogProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
  mode: "edit" | "add-child" | "create"
  editingTag?: TagNode
  parentTag?: TagNode
  onSave: (data: TagUpdateData) => void
}

const colorOptions = [
  { name: "Blue", value: "bg-blue-200 dark:bg-blue-900" },
  { name: "Purple", value: "bg-purple-200 dark:bg-purple-900" },
  { name: "Green", value: "bg-green-200 dark:bg-green-900" },
  { name: "Red", value: "bg-red-200 dark:bg-red-900" },
  { name: "Yellow", value: "bg-yellow-200 dark:bg-yellow-900" },
  { name: "Pink", value: "bg-pink-200 dark:bg-pink-900" },
  { name: "Orange", value: "bg-orange-200 dark:bg-orange-900" },
  { name: "Teal", value: "bg-teal-200 dark:bg-teal-900" },
  { name: "Cyan", value: "bg-cyan-200 dark:bg-cyan-900" },
  { name: "Indigo", value: "bg-indigo-200 dark:bg-indigo-900" },
  { name: "Violet", value: "bg-violet-200 dark:bg-violet-900" },
  { name: "Fuchsia", value: "bg-fuchsia-200 dark:bg-fuchsia-900" },
]

const DEFAULT_TAG_ICON = "📁"
const NO_ICON_VALUE = ""

const iconOptions = [
  DEFAULT_TAG_ICON,
  "💼",
  "🎨",
  "📊",
  "💻",
  "📢",
  "🌐",
  "✨",
  "🖼️",
  "📝",
  "🔬",
  "🎬",
  "🎥",
  "🎮",
  "📱",
  "🎯",
  "🚀",
  "⭐",
  "🔥",
  "💡",
  "🎪",
]

const DEFAULT_TAG_COLOR = "bg-blue-200 dark:bg-blue-900"

export function TagManagementDialog({
  open,
  onOpenChange,
  mode,
  editingTag,
  parentTag,
  onSave,
}: TagManagementDialogProps) {
  const [tagName, setTagName] = useState("")
  const [tagColor, setTagColor] = useState(DEFAULT_TAG_COLOR)
  const [tagIcon, setTagIcon] = useState(DEFAULT_TAG_ICON)
  const [applyColorToChildren, setApplyColorToChildren] = useState(false)

  useEffect(() => {
    if (mode === "edit" && editingTag) {
      setTagName(editingTag.name)
      setTagColor(editingTag.color ?? DEFAULT_TAG_COLOR)
      setTagIcon(editingTag.icon ?? DEFAULT_TAG_ICON)
    } else {
      setTagName("")
      setTagColor(DEFAULT_TAG_COLOR)
      setTagIcon(DEFAULT_TAG_ICON)
    }
    setApplyColorToChildren(false)
  }, [mode, editingTag, open])

  const handleSave = () => {
    onSave({
      name: tagName,
      color: tagColor,
      icon: tagIcon,
      applyColorToChildren,
    })
    onOpenChange(false)
  }

  const getTitle = () => {
    if (mode === "edit") return "Edit Tag"
    if (mode === "add-child" && parentTag) return `Add Child Tag to "${parentTag.name}"`
    return "Create New Tag"
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle>{getTitle()}</DialogTitle>
          <DialogDescription>
            {mode === "edit" ? "Update tag properties" : "Create a new tag with custom color and icon"}
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4">
          <Label className="text-xs text-muted-foreground block">Preview</Label>
          <div className="p-4 border border-border rounded-lg bg-muted/30">
            <div className={cn("flex items-center gap-2 px-3 py-2 rounded-md w-fit", tagColor)}>
              {tagIcon && <span className="text-base">{tagIcon}</span>}
              <span className="text-sm font-medium text-foreground">{tagName || "Tag Name"}</span>
            </div>
          </div>

          <div className="space-y-2">
            <Label htmlFor="tag-name">Tag Name</Label>
            <div className="flex items-center gap-2">
              {tagIcon && (
                <div className="flex-shrink-0 w-8 h-8 flex items-center justify-center">
                  <span className="text-lg">{tagIcon}</span>
                </div>
              )}
              <Input
                id="tag-name"
                value={tagName}
                onChange={(e) => setTagName(e.target.value)}
                placeholder="Enter tag name"
                className="flex-1"
              />
            </div>
          </div>

          <div className="space-y-2">
            <Label>Color</Label>
            <div className="grid grid-cols-6 gap-2">
              {colorOptions.map((color) => (
                <button
                  key={color.value}
                  onClick={() => setTagColor(color.value)}
                  className={cn(
                    "w-full aspect-square rounded-md border-2 transition-all",
                    color.value,
                    tagColor === color.value
                      ? "border-foreground scale-110 ring-2 ring-offset-2 ring-foreground"
                      : "border-transparent hover:border-muted-foreground",
                  )}
                  title={color.name}
                />
              ))}
            </div>
          </div>

          <div className="space-y-2">
            <Label>Icon (Optional)</Label>
            <div className="grid grid-cols-10 gap-1">
              <button
                type="button"
                onClick={() => setTagIcon(NO_ICON_VALUE)}
                className={cn(
                  "aspect-square rounded border flex items-center justify-center text-xs transition-colors",
                  tagIcon === NO_ICON_VALUE
                    ? "border-foreground bg-accent font-bold"
                    : "border-border hover:bg-accent/50",
                )}
                title="No icon"
              >
                ✕
              </button>
              {iconOptions.map((icon) => (
                <button
                  key={icon}
                  onClick={() => setTagIcon(icon)}
                  className={cn(
                    "aspect-square rounded border flex items-center justify-center transition-colors",
                    tagIcon === icon ? "border-foreground bg-accent scale-110" : "border-border hover:bg-accent/50",
                  )}
                >
                  {icon}
                </button>
              ))}
            </div>
          </div>

          {mode === "edit" && editingTag && editingTag.children && editingTag.children.length > 0 && (
            <div className="flex items-center space-x-2">
              <Checkbox
                id="apply-to-children"
                checked={applyColorToChildren}
                onCheckedChange={(checked) => setApplyColorToChildren(checked as boolean)}
              />
              <Label htmlFor="apply-to-children" className="text-sm font-normal cursor-pointer">
                Apply color to all child tags ({editingTag.children.length} children)
              </Label>
            </div>
          )}

          <div className="flex gap-2 pt-4">
            <Button className="flex-1" onClick={handleSave} disabled={!tagName.trim()}>
              {mode === "edit" ? "Save Changes" : "Create Tag"}
            </Button>
            <Button className="flex-1 bg-transparent" variant="outline" onClick={() => onOpenChange(false)}>
              Cancel
            </Button>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  )
}
