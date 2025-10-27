"use client"

import { Search, Grid3x3, List, SlidersHorizontal, X, Menu } from "lucide-react"
import { Input } from "@/components/ui/input"
import { Button } from "@/components/ui/button"
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from "@/components/ui/dropdown-menu"
import { Badge } from "@/components/ui/badge"
import type { SelectedTag } from "@/lib/types"

type FileHeaderProps = {
  viewMode: "grid" | "list"
  onViewModeChange: (mode: "grid" | "list") => void
  searchQuery: string
  onSearchChange: (query: string) => void
  fileCount: number
  selectedTag: SelectedTag | null
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
  return (
    <div className="border-b border-border bg-card">
      <div className="flex flex-col gap-3 p-3 sm:p-4 md:flex-row md:items-center md:gap-4 md:justify-between">
        <div className="flex items-center gap-2 md:flex-1">
          {isLeftSidebarCollapsed && (
            <Button variant="ghost" size="icon" onClick={onExpandLeftSidebar}>
              <Menu className="h-4 w-4" />
            </Button>
          )}

          <div className="flex-1 md:max-w-2xl">
            <div className="relative">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
              <Input
                placeholder="Search files..."
                value={searchQuery}
                onChange={(e) => {
                  onSearchChange(e.target.value)
                }}
                className="h-11 md:h-12 pl-9 md:text-base"
              />
              {selectedTag && (
                <Badge variant="secondary" className="absolute right-2 top-1/2 -translate-y-1/2 gap-1 pr-1">
                  {selectedTag.name}
                  <Button variant="ghost" size="icon" className="h-4 w-4 p-0 hover:bg-transparent" onClick={onClearTag}>
                    <X className="h-3 w-3" />
                  </Button>
                </Badge>
              )}
            </div>
          </div>
        </div>

        <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-end sm:gap-3 md:w-auto">
          <span className="text-sm text-muted-foreground sm:mr-2">
            {fileCount} {fileCount === 1 ? "file" : "files"}
          </span>

          <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:gap-2">
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

            <div className="flex w-full items-stretch overflow-hidden rounded-md border border-border sm:w-auto">
              <Button
                variant={viewMode === "grid" ? "secondary" : "ghost"}
                size="icon"
                onClick={() => onViewModeChange("grid")}
                className="flex-1 rounded-none sm:flex-initial"
              >
                <Grid3x3 className="h-4 w-4" />
              </Button>
              <Button
                variant={viewMode === "list" ? "secondary" : "ghost"}
                size="icon"
                onClick={() => onViewModeChange("list")}
                className="flex-1 rounded-none sm:flex-initial"
              >
                <List className="h-4 w-4" />
              </Button>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}
