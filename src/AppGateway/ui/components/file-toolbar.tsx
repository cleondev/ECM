"use client"

import {
  Check,
  Download,
  Edit3,
  FileText,
  GitBranch,
  Grid3x3,
  List,
  MessageCircle,
  Share2,
  SlidersHorizontal,
  Tag,
  Upload,
  Trash2,
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
  onAssignTagsClick?: () => void
  onDeleteClick?: () => void
  sortBy: "name" | "modified" | "size"
  sortOrder: "asc" | "desc"
  onSortChange: (sortBy: "name" | "modified" | "size", sortOrder: "asc" | "desc") => void
  disableFileActions?: boolean
  disableTagActions?: boolean
  disableDeleteAction?: boolean
  isRightSidebarOpen: boolean
  onToggleRightSidebar: () => void
  activeRightTab: "info" | "flow" | "form" | "chat"
  onRightTabChange: (tab: "info" | "flow" | "form" | "chat") => void
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
  onAssignTagsClick,
  onDeleteClick,
  sortBy,
  sortOrder,
  onSortChange,
  disableFileActions = false,
  disableTagActions = false,
  disableDeleteAction = false,
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

    if (value === "info" || value === "flow" || value === "form" || value === "chat") {
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

  const tabStyles: Record<"info" | "flow" | "form" | "chat", string> = {
    info: "data-[state=on]:bg-sky-500 data-[state=on]:text-white data-[state=on]:border-sky-500 data-[state=on]:shadow-sm",
    flow: "data-[state=on]:bg-emerald-500 data-[state=on]:text-white data-[state=on]:border-emerald-500 data-[state=on]:shadow-sm",
    form: "data-[state=on]:bg-violet-500 data-[state=on]:text-white data-[state=on]:border-violet-500 data-[state=on]:shadow-sm",
    chat: "data-[state=on]:bg-amber-500 data-[state=on]:text-white data-[state=on]:border-amber-500 data-[state=on]:shadow-sm",
  }

  return (
    <div className="border-b border-border bg-card">
      <div className="flex flex-col gap-4 p-3 sm:p-4">
        <div className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
          <div className="flex flex-col gap-2 rounded-lg border border-border bg-background p-3 sm:flex-row sm:flex-wrap sm:items-center sm:gap-2 sm:border-none sm:bg-transparent sm:p-0">
            <div className="flex w-full flex-nowrap gap-2 sm:w-auto">
              <Button onClick={onUploadClick} className="gap-0 sm:gap-2 flex-1 justify-center sm:flex-none sm:w-auto">
                <Upload className="h-4 w-4" />
                <span className="sr-only">Upload File</span>
                <span className="hidden sm:inline">Upload File</span>
              </Button>

              <Button
                variant="outline"
                className="gap-0 sm:gap-2 flex-1 justify-center sm:flex-none sm:w-auto"
                onClick={() => onDownloadClick?.()}
                disabled={disableFileActions}
              >
                <Download className="h-4 w-4" />
                <span className="sr-only">Download</span>
                <span className="hidden sm:inline">Download</span>
              </Button>

              <Button
                variant="outline"
                className="gap-0 sm:gap-2 flex-1 justify-center sm:flex-none sm:w-auto"
                onClick={() => onShareClick?.()}
                disabled={disableFileActions}
              >
                <Share2 className="h-4 w-4" />
                <span className="sr-only">Share</span>
                <span className="hidden sm:inline">Share</span>
              </Button>

              <Button
                variant="outline"
                className="gap-0 sm:gap-2 flex-1 justify-center sm:flex-none sm:w-auto"
                onClick={() => onAssignTagsClick?.()}
                disabled={disableTagActions}
              >
                <Tag className="h-4 w-4" />
                <span className="sr-only">Edit tags</span>
                <span className="hidden sm:inline">Tags</span>
              </Button>

              <Button
                variant="outline"
                className="gap-0 sm:gap-2 flex-1 justify-center sm:flex-none sm:w-auto"
                onClick={() => onDeleteClick?.()}
                disabled={disableDeleteAction}
              >
                <Trash2 className="h-4 w-4" />
                <span className="sr-only">Delete</span>
                <span className="hidden sm:inline">Delete</span>
              </Button>
            </div>

            <ToggleGroup
              type="single"
              value={tabValue}
              onValueChange={handleRightTabChange}
              variant="outline"
              className="h-10 w-full sm:w-auto"
              aria-label="File details sections"
            >
              <ToggleGroupItem
                value="info"
                className={cn(
                  "gap-2 px-3 transition-colors flex-1 sm:flex-initial",
                  tabStyles.info,
                  disableRightSidebarTabs && "pointer-events-none opacity-60",
                )}
                disabled={disableRightSidebarTabs}
              >
                <FileText className="h-4 w-4" />
                <span className="text-sm font-medium">Info</span>
              </ToggleGroupItem>
              <ToggleGroupItem
                value="flow"
                className={cn(
                  "gap-2 px-3 transition-colors flex-1 sm:flex-initial",
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
                  "gap-2 px-3 transition-colors flex-1 sm:flex-initial",
                  tabStyles.form,
                  disableRightSidebarTabs && "pointer-events-none opacity-60",
                )}
                disabled={disableRightSidebarTabs}
              >
                <Edit3 className="h-4 w-4" />
                <span className="text-sm font-medium">Form</span>
              </ToggleGroupItem>
              <ToggleGroupItem
                value="chat"
                className={cn(
                  "gap-2 px-3 transition-colors flex-1 sm:flex-initial",
                  tabStyles.chat,
                  disableRightSidebarTabs && "pointer-events-none opacity-60",
                )}
                disabled={disableRightSidebarTabs}
              >
                <MessageCircle className="h-4 w-4" />
                <span className="text-sm font-medium">Chat</span>
              </ToggleGroupItem>
            </ToggleGroup>
          </div>

          <div className="flex flex-col gap-2 rounded-lg border border-border bg-background p-3 sm:flex-row sm:flex-wrap sm:items-center sm:justify-end sm:gap-2 sm:border-none sm:bg-transparent sm:p-0 md:justify-end">
            <div className="flex w-full items-center gap-2 sm:w-auto sm:justify-end">
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button variant="outline" size="icon" className="flex-shrink-0">
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

              <div className="flex flex-1 items-stretch overflow-hidden rounded-md border border-border sm:flex-none sm:w-auto">
                <Button
                  size="icon"
                  onClick={() => onViewModeChange("grid")}
                  aria-pressed={viewMode === "grid"}
                  data-active={viewMode === "grid"}
                  variant="ghost"
                  className="flex-1 rounded-none text-muted-foreground transition-colors data-[active=true]:bg-primary data-[active=true]:text-primary-foreground data-[active=true]:hover:bg-primary/90"
                >
                  <Grid3x3 className="h-4 w-4" />
                </Button>
                <Button
                  size="icon"
                  onClick={() => onViewModeChange("list")}
                  aria-pressed={viewMode === "list"}
                  data-active={viewMode === "list"}
                  variant="ghost"
                  className="flex-1 rounded-none text-muted-foreground transition-colors data-[active=true]:bg-primary data-[active=true]:text-primary-foreground data-[active=true]:hover:bg-primary/90"
                >
                  <List className="h-4 w-4" />
                </Button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}
