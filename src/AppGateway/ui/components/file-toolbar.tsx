"use client"

import {
  Check,
  Download,
  Edit3,
  FileText,
  GitBranch,
  Grid3x3,
  List,
  Share2,
  SlidersHorizontal,
  Upload,
} from "lucide-react"
import { Button } from "@/components/ui/button"
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from "@/components/ui/dropdown-menu"
import { ToggleGroup, ToggleGroupItem } from "@/components/ui/toggle-group"
import { cn } from "@/lib/utils"

type FileToolbarProps = {
  viewMode: "grid" | "list"
  onViewModeChange: (mode: "grid" | "list") => void
  onUploadClick: () => void
  onDownloadClick?: () => void
  onShareClick?: () => void
  sortBy: "name" | "modified" | "size"
  sortOrder: "asc" | "desc"
  onSortChange: (sortBy: "name" | "modified" | "size", sortOrder: "asc" | "desc") => void
  disableFileActions?: boolean
  isRightSidebarOpen: boolean
  onToggleRightSidebar: () => void
  activeRightTab: "property" | "flow" | "form"
  onRightTabChange: (tab: "property" | "flow" | "form") => void
  disableRightSidebarTabs?: boolean
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

export function FileToolbar({
  viewMode,
  onViewModeChange,
  onUploadClick,
  onDownloadClick,
  onShareClick,
  sortBy,
  sortOrder,
  onSortChange,
  disableFileActions = false,
  isRightSidebarOpen,
  onToggleRightSidebar,
  activeRightTab,
  onRightTabChange,
  disableRightSidebarTabs = false,
}: FileToolbarProps) {
  const handleRightTabChange = (value: string | undefined) => {
    if (disableRightSidebarTabs) {
      return
    }

    if (!value) {
      if (isRightSidebarOpen) {
        onToggleRightSidebar()
      }
      return
    }

    if (value === "property" || value === "flow" || value === "form") {
      if (value === activeRightTab) {
        onToggleRightSidebar()
        return
      }

      onRightTabChange(value)

      if (!isRightSidebarOpen) {
        onToggleRightSidebar()
      }
    }
  }

  const tabValue = disableRightSidebarTabs ? undefined : isRightSidebarOpen ? activeRightTab : ""

  const tabStyles: Record<"property" | "flow" | "form", string> = {
    property:
      "data-[state=on]:bg-sky-500 data-[state=on]:text-white data-[state=on]:border-sky-500 data-[state=on]:shadow-sm",
    flow: "data-[state=on]:bg-emerald-500 data-[state=on]:text-white data-[state=on]:border-emerald-500 data-[state=on]:shadow-sm",
    form: "data-[state=on]:bg-violet-500 data-[state=on]:text-white data-[state=on]:border-violet-500 data-[state=on]:shadow-sm",
  }

  return (
    <div className="border-b border-border bg-card">
      <div className="flex flex-wrap items-center justify-between p-4 gap-4">
        <div className="flex flex-wrap items-center gap-2">
          <Button onClick={onUploadClick} className="gap-2">
            <Upload className="h-4 w-4" />
            Upload File
          </Button>

          <Button
            variant="outline"
            className="gap-2"
            onClick={() => onDownloadClick?.()}
            disabled={disableFileActions}
          >
            <Download className="h-4 w-4" />
            Download
          </Button>

          <Button
            variant="outline"
            className="gap-2"
            onClick={() => onShareClick?.()}
            disabled={disableFileActions}
          >
            <Share2 className="h-4 w-4" />
            Share
          </Button>

          <ToggleGroup
            type="single"
            value={tabValue}
            onValueChange={handleRightTabChange}
            variant="outline"
            className="h-10"
            aria-label="File details sections"
          >
            <ToggleGroupItem
              value="property"
              className={cn(
                "gap-2 px-3 transition-colors",
                tabStyles.property,
                disableRightSidebarTabs && "pointer-events-none opacity-60",
              )}
              disabled={disableRightSidebarTabs}
            >
              <FileText className="h-4 w-4" />
              <span className="text-sm font-medium">Property</span>
            </ToggleGroupItem>
            <ToggleGroupItem
              value="flow"
              className={cn(
                "gap-2 px-3 transition-colors",
                tabStyles.flow,
                disableRightSidebarTabs && "pointer-events-none opacity-60",
              )}
              disabled={disableRightSidebarTabs}
            >
              <GitBranch className="h-4 w-4" />
              <span className="text-sm font-medium">Flow</span>
            </ToggleGroupItem>
            <ToggleGroupItem
              value="form"
              className={cn(
                "gap-2 px-3 transition-colors",
                tabStyles.form,
                disableRightSidebarTabs && "pointer-events-none opacity-60",
              )}
              disabled={disableRightSidebarTabs}
            >
              <Edit3 className="h-4 w-4" />
              <span className="text-sm font-medium">Form</span>
            </ToggleGroupItem>
          </ToggleGroup>
        </div>

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
              size="icon"
              onClick={() => onViewModeChange("grid")}
              aria-pressed={viewMode === "grid"}
              data-active={viewMode === "grid"}
              variant="ghost"
              className="rounded-r-none text-muted-foreground transition-colors data-[active=true]:bg-primary data-[active=true]:text-primary-foreground data-[active=true]:hover:bg-primary/90"
            >
              <Grid3x3 className="h-4 w-4" />
            </Button>
            <Button
              size="icon"
              onClick={() => onViewModeChange("list")}
              aria-pressed={viewMode === "list"}
              data-active={viewMode === "list"}
              variant="ghost"
              className="rounded-l-none text-muted-foreground transition-colors data-[active=true]:bg-primary data-[active=true]:text-primary-foreground data-[active=true]:hover:bg-primary/90"
            >
              <List className="h-4 w-4" />
            </Button>
          </div>
        </div>
      </div>
    </div>
  )
}
