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

          <div className="flex-1 md:min-w-[520px] md:max-w-3xl">
            <div className="relative flex h-11 items-center rounded-full border border-border/60 bg-background/80 pl-4 pr-4 text-sm shadow-sm transition-colors focus-within:border-primary focus-within:ring-2 focus-within:ring-primary/20 md:h-14 md:pl-5 md:pr-6 md:text-lg">
              <Search className="mr-2 h-4 w-4 text-muted-foreground md:h-5 md:w-5" />
              <Input
                placeholder="Search files..."
                value={searchQuery}
                onChange={(e) => {
                  onSearchChange(e.target.value)
                }}
                className="h-full flex-1 border-0 bg-transparent px-0 text-sm shadow-none focus-visible:border-0 focus-visible:ring-0 focus-visible:ring-offset-0 md:text-lg"
              />
              {selectedTag && (
                <Badge
                  variant="secondary"
                  className="ml-2 flex items-center gap-1 rounded-full bg-primary/10 pr-1 text-sm text-primary md:text-base"
                >
                  {selectedTag.name}
                  <Button variant="ghost" size="icon" className="h-4 w-4 p-0 text-primary hover:bg-transparent" onClick={onClearTag}>
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
