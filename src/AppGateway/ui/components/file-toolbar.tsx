"use client"

import { Check, Grid3x3, List, SlidersHorizontal, Upload } from "lucide-react"
import { Button } from "@/components/ui/button"
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from "@/components/ui/dropdown-menu"

type FileToolbarProps = {
  viewMode: "grid" | "list"
  onViewModeChange: (mode: "grid" | "list") => void
  onUploadClick: () => void
  sortBy: "name" | "modified" | "size"
  sortOrder: "asc" | "desc"
  onSortChange: (sortBy: "name" | "modified" | "size", sortOrder: "asc" | "desc") => void
}

const SORT_OPTIONS: Array<{
  id: string
  label: string
  sortBy: "name" | "modified" | "size"
  sortOrder: "asc" | "desc"
}> = [
  { id: "name-asc", label: "Name (A-Z)", sortBy: "name", sortOrder: "asc" },
  { id: "name-desc", label: "Name (Z-A)", sortBy: "name", sortOrder: "desc" },
  { id: "modified-desc", label: "Last modified (newest)", sortBy: "modified", sortOrder: "desc" },
  { id: "modified-asc", label: "Last modified (oldest)", sortBy: "modified", sortOrder: "asc" },
  { id: "size-desc", label: "File size (largest)", sortBy: "size", sortOrder: "desc" },
  { id: "size-asc", label: "File size (smallest)", sortBy: "size", sortOrder: "asc" },
]

export function FileToolbar({ viewMode, onViewModeChange, onUploadClick, sortBy, sortOrder, onSortChange }: FileToolbarProps) {
  return (
    <div className="border-b border-border bg-card">
      <div className="flex items-center justify-between p-4 gap-4">
        <Button onClick={onUploadClick} className="gap-2">
          <Upload className="h-4 w-4" />
          Upload File
        </Button>

        <div className="flex items-center gap-2">
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="outline" size="icon">
                <SlidersHorizontal className="h-4 w-4" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              {SORT_OPTIONS.map((option) => {
                const isActive = option.sortBy === sortBy && option.sortOrder === sortOrder
                return (
                  <DropdownMenuItem
                    key={option.id}
                    onClick={() => onSortChange(option.sortBy, option.sortOrder)}
                    className="flex items-center gap-2"
                  >
                    {isActive ? <Check className="h-4 w-4" /> : <span className="h-4 w-4" />}
                    <span>{option.label}</span>
                  </DropdownMenuItem>
                )
              })}
            </DropdownMenuContent>
          </DropdownMenu>

          <div className="flex items-center border border-border rounded-md">
            <Button
              variant={viewMode === "grid" ? "secondary" : "ghost"}
              size="icon"
              onClick={() => onViewModeChange("grid")}
              className="rounded-r-none"
            >
              <Grid3x3 className="h-4 w-4" />
            </Button>
            <Button
              variant={viewMode === "list" ? "secondary" : "ghost"}
              size="icon"
              onClick={() => onViewModeChange("list")}
              className="rounded-l-none"
            >
              <List className="h-4 w-4" />
            </Button>
          </div>
        </div>
      </div>
    </div>
  )
}
