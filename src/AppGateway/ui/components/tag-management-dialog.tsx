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
  { name: "Sky", value: "#60A5FA" },
  { name: "Violet", value: "#A78BFA" },
  { name: "Emerald", value: "#34D399" },
  { name: "Rose", value: "#F87171" },
  { name: "Amber", value: "#FBBF24" },
  { name: "Pink", value: "#F472B6" },
  { name: "Orange", value: "#FB923C" },
  { name: "Teal", value: "#2DD4BF" },
  { name: "Cyan", value: "#22D3EE" },
  { name: "Indigo", value: "#6366F1" },
  { name: "Fuchsia", value: "#EC4899" },
  { name: "Slate", value: "#94A3B8" },
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

const DEFAULT_TAG_COLOR = "#60A5FA"

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
  const [tagIcon, setTagIcon] = useState<string>(NO_ICON_VALUE)
  const [applyColorToChildren, setApplyColorToChildren] = useState(false)

  const previewIcon = tagIcon && tagIcon !== NO_ICON_VALUE ? tagIcon : DEFAULT_TAG_ICON
  const previewStyle = {
    backgroundColor: tagColor || DEFAULT_TAG_COLOR,
  }

  useEffect(() => {
    if (mode === "edit" && editingTag) {
      setTagName(editingTag.name)
      setTagColor(editingTag.color ?? DEFAULT_TAG_COLOR)
      setTagIcon(editingTag.iconKey ?? NO_ICON_VALUE)
    } else {
      setTagName("")
      setTagColor(DEFAULT_TAG_COLOR)
      setTagIcon(NO_ICON_VALUE)
    }
    setApplyColorToChildren(false)
  }, [mode, editingTag, open])

  const handleSave = () => {
    onSave({
      name: tagName,
      color: tagColor,
      iconKey: tagIcon && tagIcon !== NO_ICON_VALUE ? tagIcon : null,
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
            <div className="flex items-center gap-2 px-3 py-2 rounded-md w-fit text-foreground" style={previewStyle}>
              <span className="text-base">{previewIcon}</span>
              <span className="text-sm font-medium">{tagName || "Tag Name"}</span>
            </div>
          </div>

          <div className="space-y-2">
            <Label htmlFor="tag-name">Tag Name</Label>
            <div className="flex items-center gap-2">
              <div className="flex-shrink-0 w-8 h-8 flex items-center justify-center">
                <span className="text-lg">{previewIcon}</span>
              </div>
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
                  type="button"
                  onClick={() => setTagColor(color.value)}
                  className={cn(
                    "w-full aspect-square rounded-md border-2 transition-all",
                    tagColor === color.value
                      ? "border-foreground scale-110 ring-2 ring-offset-2 ring-foreground"
                      : "border-border hover:border-muted-foreground",
                  )}
                  style={{ backgroundColor: color.value }}
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
