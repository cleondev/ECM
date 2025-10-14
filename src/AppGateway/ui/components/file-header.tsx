"use client"

import { Search, Grid3x3, List, SlidersHorizontal, X, Menu } from "lucide-react"
import { Input } from "@/components/ui/input"
import { Button } from "@/components/ui/button"
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from "@/components/ui/dropdown-menu"
import { Badge } from "@/components/ui/badge"

type FileHeaderProps = {
  viewMode: "grid" | "list"
  onViewModeChange: (mode: "grid" | "list") => void
  searchQuery: string
  onSearchChange: (query: string) => void
  fileCount: number
  selectedTag: string | null
  onClearTag: () => void
  isLeftSidebarCollapsed: boolean
  onExpandLeftSidebar: () => void
}

export function FileHeader({
  viewMode,
  onViewModeChange,
  searchQuery,
  onSearchChange,
  fileCount,
  selectedTag,
  onClearTag,
  isLeftSidebarCollapsed,
  onExpandLeftSidebar,
}: FileHeaderProps) {
  const displayQuery = searchQuery.replace(/tag:[^\s]+\s*/g, "").trim()

  return (
    <div className="border-b border-border bg-card">
      <div className="flex items-center justify-between p-4 gap-4">
        {isLeftSidebarCollapsed && (
          <Button variant="ghost" size="icon" onClick={onExpandLeftSidebar}>
            <Menu className="h-4 w-4" />
          </Button>
        )}

        <div className="flex-1 max-w-md">
          <div className="relative">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
            <Input
              placeholder="Search files..."
              value={displayQuery}
              onChange={(e) => {
                const newTextQuery = e.target.value
                if (selectedTag) {
                  onSearchChange(`tag:${selectedTag} ${newTextQuery}`.trim())
                } else {
                  onSearchChange(newTextQuery)
                }
              }}
              className="pl-9"
            />
            {selectedTag && (
              <Badge variant="secondary" className="absolute right-2 top-1/2 -translate-y-1/2 gap-1 pr-1">
                {selectedTag}
                <Button variant="ghost" size="icon" className="h-4 w-4 p-0 hover:bg-transparent" onClick={onClearTag}>
                  <X className="h-3 w-3" />
                </Button>
              </Badge>
            )}
          </div>
        </div>

        <div className="flex items-center gap-2">
          <span className="text-sm text-muted-foreground mr-2">
            {fileCount} {fileCount === 1 ? "file" : "files"}
          </span>

          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="outline" size="icon">
                <SlidersHorizontal className="h-4 w-4" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem>Sort by Name</DropdownMenuItem>
              <DropdownMenuItem>Sort by Date</DropdownMenuItem>
              <DropdownMenuItem>Sort by Size</DropdownMenuItem>
              <DropdownMenuItem>Sort by Type</DropdownMenuItem>
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

function cn(...classes: (string | boolean | undefined)[]) {
  return classes.filter(Boolean).join(" ")
}
