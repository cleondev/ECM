"use client"

import { Search, User, Settings, LogOut, SlidersHorizontal, Menu } from "lucide-react"
import { Input } from "@/components/ui/input"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { useState, useEffect } from "react"
import { fetchUser, signOut } from "@/lib/api"
import type { User as UserType } from "@/lib/types"
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog"
import { Label } from "@/components/ui/label"
import { Checkbox } from "@/components/ui/checkbox"
import type { SelectedTag, TagNode } from "@/lib/types"
import { fetchTags } from "@/lib/api"

type AppHeaderProps = {
  searchQuery: string
  onSearchChange: (query: string) => void
  selectedTag: SelectedTag | null
  onClearTag: () => void
  isLeftSidebarCollapsed: boolean
  onToggleLeftSidebar: () => void // Changed from onExpandLeftSidebar to onToggleLeftSidebar
}

export function AppHeader({
  searchQuery,
  onSearchChange,
  selectedTag,
  onClearTag,
  isLeftSidebarCollapsed,
  onToggleLeftSidebar, // Updated prop name
}: AppHeaderProps) {
  const [user, setUser] = useState<UserType | null>(null)
  const [isAdvancedSearchOpen, setIsAdvancedSearchOpen] = useState(false)
  const [tags, setTags] = useState<TagNode[]>([])
  const [advancedSearchTags, setAdvancedSearchTags] = useState<string[]>([])
  const [isSigningOut, setIsSigningOut] = useState(false)

  useEffect(() => {
    fetchUser()
      .then(setUser)
      .catch(() => setUser(null))
    fetchTags().then(setTags)
  }, [])

  useEffect(() => {
    if (isAdvancedSearchOpen && selectedTag) {
      setAdvancedSearchTags([selectedTag.name])
    }
  }, [isAdvancedSearchOpen, selectedTag])

  const getAllTags = (nodes: TagNode[]): TagNode[] => {
    const result: TagNode[] = []
    const traverse = (node: TagNode) => {
      if (!node.kind || node.kind === "label") {
        result.push(node)
      }
      node.children?.forEach(traverse)
    }
    nodes.forEach(traverse)
    return result
  }

  const toggleAdvancedSearchTag = (tagName: string) => {
    setAdvancedSearchTags((prev) => (prev.includes(tagName) ? prev.filter((t) => t !== tagName) : [...prev, tagName]))
  }

  const handleSignOut = async () => {
    if (isSigningOut) {
      return
    }

    setIsSigningOut(true)

    try {
      await signOut("/")
    } catch (error) {
      console.error("[ui] Đăng xuất thất bại:", error)
      setIsSigningOut(false)
    }
  }

  return (
    <div className="border-b border-border bg-card">
      <div className="flex items-center justify-between p-4 gap-4">
        <div className="flex items-center gap-2">
          <Button
            variant="ghost"
            size="icon"
            onClick={onToggleLeftSidebar}
            title={isLeftSidebarCollapsed ? "Expand sidebar" : "Collapse sidebar"}
            className="h-9 w-9"
          >
            <Menu className="h-5 w-5" />
          </Button>

          <div className="h-8 w-8 rounded-lg bg-primary flex items-center justify-center">
            <span className="text-primary-foreground font-bold text-sm">FM</span>
          </div>

          {!isLeftSidebarCollapsed && <span className="font-semibold text-lg hidden md:block">File Manager</span>}
        </div>

        <div className="flex-1 max-w-2xl">
          <div className="relative">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
            <Input
              placeholder="Search files..."
              value={searchQuery}
              onChange={(e) => {
                onSearchChange(e.target.value)
              }}
              className="pl-9 pr-24"
            />
            <div className="absolute right-2 top-1/2 -translate-y-1/2 flex items-center gap-1">
              {selectedTag && (
                <Badge variant="secondary" className="gap-1 pr-1">
                  {selectedTag.name}
                  <Button variant="ghost" size="icon" className="h-4 w-4 p-0 hover:bg-transparent" onClick={onClearTag}>
                    <span className="sr-only">Clear tag filter</span>×
                  </Button>
                </Badge>
              )}
              <Dialog open={isAdvancedSearchOpen} onOpenChange={setIsAdvancedSearchOpen}>
                <DialogTrigger asChild>
                  <Button variant="ghost" size="icon" className="h-7 w-7" title="Advanced search">
                    <SlidersHorizontal className="h-4 w-4" />
                  </Button>
                </DialogTrigger>
                <DialogContent className="sm:max-w-[500px]">
                  <DialogHeader>
                    <DialogTitle>Advanced Search</DialogTitle>
                  </DialogHeader>
                  <div className="space-y-4 py-4">
                    <div className="space-y-2">
                      <Label htmlFor="filename">File name</Label>
                      <Input id="filename" placeholder="Enter file name..." />
                    </div>
                    <div className="space-y-2">
                      <Label htmlFor="content">Contains text</Label>
                      <Input id="content" placeholder="Search within files..." />
                    </div>
                    <div className="space-y-2">
                      <Label>Tags</Label>
                      <div className="flex flex-wrap gap-2 min-h-[80px] max-h-[120px] overflow-y-auto p-3 border rounded-md">
                        {getAllTags(tags).map((tag) => (
                          <Badge
                            key={tag.id}
                            variant={advancedSearchTags.includes(tag.name) ? "default" : "outline"}
                            className="cursor-pointer h-fit"
                            onClick={() => toggleAdvancedSearchTag(tag.name)}
                          >
                            {tag.icon && <span className="mr-1">{tag.icon}</span>}
                            {tag.name}
                          </Badge>
                        ))}
                      </div>
                    </div>
                    <div className="space-y-2">
                      <Label>File type</Label>
                      <div className="grid grid-cols-2 gap-2">
                        <div className="flex items-center space-x-2">
                          <Checkbox id="pdf" />
                          <label htmlFor="pdf" className="text-sm">
                            PDF
                          </label>
                        </div>
                        <div className="flex items-center space-x-2">
                          <Checkbox id="doc" />
                          <label htmlFor="doc" className="text-sm">
                            Document
                          </label>
                        </div>
                        <div className="flex items-center space-x-2">
                          <Checkbox id="img" />
                          <label htmlFor="img" className="text-sm">
                            Image
                          </label>
                        </div>
                        <div className="flex items-center space-x-2">
                          <Checkbox id="video" />
                          <label htmlFor="video" className="text-sm">
                            Video
                          </label>
                        </div>
                      </div>
                    </div>
                    <div className="space-y-2">
                      <Label>Date range</Label>
                      <div className="grid grid-cols-2 gap-2">
                        <div>
                          <Label htmlFor="from" className="text-xs text-muted-foreground">
                            From
                          </Label>
                          <Input id="from" type="date" />
                        </div>
                        <div>
                          <Label htmlFor="to" className="text-xs text-muted-foreground">
                            To
                          </Label>
                          <Input id="to" type="date" />
                        </div>
                      </div>
                    </div>
                    <div className="space-y-2">
                      <Label htmlFor="size">File size</Label>
                      <div className="flex items-center gap-2">
                        <Input id="size-min" type="number" placeholder="Min (MB)" className="flex-1" />
                        <span className="text-muted-foreground">to</span>
                        <Input id="size-max" type="number" placeholder="Max (MB)" className="flex-1" />
                      </div>
                    </div>
                    <div className="flex justify-end gap-2 pt-4">
                      <Button variant="outline" onClick={() => setIsAdvancedSearchOpen(false)}>
                        Cancel
                      </Button>
                      <Button onClick={() => setIsAdvancedSearchOpen(false)}>Search</Button>
                    </div>
                  </div>
                </DialogContent>
              </Dialog>
            </div>
          </div>
        </div>

        <div className="flex items-center gap-2">
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" className="gap-2 px-2">
                <Avatar className="h-8 w-8">
                  <AvatarImage src={user?.avatar || "/placeholder.svg"} alt={user?.displayName} />
                  <AvatarFallback>{user?.displayName?.charAt(0) || "U"}</AvatarFallback>
                </Avatar>
                <div className="hidden md:flex flex-col items-start">
                  <span className="text-sm font-medium">{user?.displayName || "Loading..."}</span>
                  <span className="text-xs text-muted-foreground">{user?.department || ""}</span>
                </div>
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end" className="w-56">
              <DropdownMenuLabel>
                <div className="flex flex-col gap-1">
                  <span className="font-medium">{user?.displayName}</span>
                  <span className="text-xs text-muted-foreground font-normal">{user?.email}</span>
                  <span className="text-xs text-muted-foreground font-normal">{user?.department}</span>
                  <span className="text-xs text-muted-foreground font-normal">{user?.roles?.[0] ?? ''}</span>
                </div>
              </DropdownMenuLabel>
              <DropdownMenuSeparator />
              <DropdownMenuItem asChild>
                <a href="/profile" className="cursor-pointer">
                  <User className="mr-2 h-4 w-4" />
                  Profile
                </a>
              </DropdownMenuItem>
              <DropdownMenuItem asChild>
                <a href="/settings" className="cursor-pointer">
                  <Settings className="mr-2 h-4 w-4" />
                  Settings
                </a>
              </DropdownMenuItem>
              <DropdownMenuSeparator />
              <DropdownMenuItem
                className="text-red-600"
                variant="destructive"
                onSelect={(event) => {
                  event.preventDefault()
                  handleSignOut()
                }}
                disabled={isSigningOut}
              >
                <LogOut className="mr-2 h-4 w-4" />
                {isSigningOut ? "Signing out..." : "Logout"}
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      </div>
    </div>
  )
}
