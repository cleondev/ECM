"use client"

import type { LucideIcon } from "lucide-react"
import {
  Search,
  SlidersHorizontal,
  Menu,
  Bell,
  BellRing,
  CalendarDays,
  AlertTriangle,
  CheckCircle2,
  ClipboardList,
  Megaphone,
  Eye,
  X,
} from "lucide-react"
import { Input } from "@/components/ui/input"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { useState, useEffect, useMemo, useCallback } from "react"
import { fetchNotifications, fetchTags } from "@/lib/api"
import type { NotificationItem, SelectedTag, TagNode } from "@/lib/types"
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import { Label } from "@/components/ui/label"
import { Checkbox } from "@/components/ui/checkbox"
import { BrandLogo } from "@/components/brand-logo"
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover"
import { ScrollArea } from "@/components/ui/scroll-area"
import { cn } from "@/lib/utils"

const notificationTypeConfig: Record<NotificationItem["type"], { icon: LucideIcon; label: string; className: string }> = {
  system: { icon: Megaphone, label: "Hệ thống", className: "bg-primary/10 text-primary" },
  event: { icon: CalendarDays, label: "Sự kiện", className: "bg-blue-100 text-blue-600 dark:bg-blue-500/10 dark:text-blue-400" },
  reminder: { icon: BellRing, label: "Nhắc việc", className: "bg-amber-100 text-amber-600 dark:bg-amber-500/10 dark:text-amber-400" },
  task: { icon: ClipboardList, label: "Nhiệm vụ", className: "bg-emerald-100 text-emerald-600 dark:bg-emerald-500/10 dark:text-emerald-400" },
  alert: { icon: AlertTriangle, label: "Cảnh báo", className: "bg-red-100 text-red-600 dark:bg-red-500/10 dark:text-red-400" },
}

function formatRelativeTime(isoString: string): string {
  const date = new Date(isoString)
  if (Number.isNaN(date.getTime())) {
    return isoString
  }

  const now = Date.now()
  const diff = date.getTime() - now
  const absDiff = Math.abs(diff)
  const formatter = new Intl.RelativeTimeFormat("vi", { numeric: "auto" })

  const units: { unit: Intl.RelativeTimeFormatUnit; ms: number }[] = [
    { unit: "day", ms: 1000 * 60 * 60 * 24 },
    { unit: "hour", ms: 1000 * 60 * 60 },
    { unit: "minute", ms: 1000 * 60 },
  ]

  for (const { unit, ms } of units) {
    if (absDiff >= ms) {
      return formatter.format(Math.round(diff / ms), unit)
    }
  }

  return formatter.format(Math.round(diff / 1000), "second")
}

type AppHeaderProps = {
  searchQuery: string
  onSearchChange: (query: string) => void
  selectedTag: SelectedTag | null
  onClearTag: () => void
  isLeftSidebarCollapsed: boolean
  onToggleLeftSidebar: () => void // Changed from onExpandLeftSidebar to onToggleLeftSidebar
  isMobile?: boolean
}

export function AppHeader({
  searchQuery,
  onSearchChange,
  selectedTag,
  onClearTag,
  isLeftSidebarCollapsed,
  onToggleLeftSidebar, // Updated prop name
  isMobile = false,
}: AppHeaderProps) {
  const [isAdvancedSearchOpen, setIsAdvancedSearchOpen] = useState(false)
  const [tags, setTags] = useState<TagNode[]>([])
  const [advancedSearchTags, setAdvancedSearchTags] = useState<string[]>([])
  const [notifications, setNotifications] = useState<NotificationItem[]>([])
  const [isLoadingNotifications, setIsLoadingNotifications] = useState(false)
  const [isNotificationsOpen, setIsNotificationsOpen] = useState(false)

  useEffect(() => {
    fetchTags().then(setTags)
  }, [])

  useEffect(() => {
    let isMounted = true
    setIsLoadingNotifications(true)
    fetchNotifications()
      .then((data) => {
        if (isMounted) {
          setNotifications(data)
        }
      })
      .catch((error) => {
        console.error("[ui] Không thể tải thông báo:", error)
        if (isMounted) {
          setNotifications([])
        }
      })
      .finally(() => {
        if (isMounted) {
          setIsLoadingNotifications(false)
        }
      })

    return () => {
      isMounted = false
    }
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

  const markNotificationAsRead = useCallback((id: string) => {
    setNotifications((prev) =>
      prev.map((notification) =>
        notification.id === id ? { ...notification, isRead: true } : notification,
      ),
    )
  }, [])

  const markAllNotificationsAsRead = useCallback(() => {
    setNotifications((prev) =>
      prev.map((notification) =>
        notification.isRead ? notification : { ...notification, isRead: true },
      ),
    )
  }, [])

  const { unreadNotifications, readNotifications, unreadCount } = useMemo(() => {
    const unread = notifications.filter((notification) => !notification.isRead)
    const read = notifications.filter((notification) => notification.isRead)

    return {
      unreadNotifications: unread,
      readNotifications: read,
      unreadCount: unread.length,
    }
  }, [notifications])

  const hasNotifications = notifications.length > 0

  const renderNotification = useCallback(
    (notification: NotificationItem) => {
      const config = notificationTypeConfig[notification.type]
      const Icon = config.icon
      const isRead = Boolean(notification.isRead)

      return (
        <div
          key={notification.id}
          className={cn(
            "group relative flex items-start gap-3 rounded-lg border p-3 text-sm transition-colors",
            isRead
              ? "bg-card hover:bg-muted/70"
              : "border-primary/30 bg-primary/5 hover:bg-primary/10",
          )}
        >
          <span className={cn("flex h-8 w-8 shrink-0 items-center justify-center rounded-full", config.className)}>
            <Icon className="h-4 w-4" />
          </span>
          <div className="flex-1 space-y-1">
            <div className="flex items-start justify-between gap-2">
              <p className="font-medium leading-5">{notification.title}</p>
              <span className="whitespace-nowrap text-xs text-muted-foreground">
                {formatRelativeTime(notification.createdAt)}
              </span>
            </div>
            {notification.description && (
              <p className="text-xs leading-relaxed text-muted-foreground">
                {notification.description}
              </p>
            )}
            <div className="flex flex-wrap items-center gap-2 pt-1">
              <Badge variant="outline" className="text-[10px] font-semibold uppercase tracking-wide">
                {config.label}
              </Badge>
              {notification.actionUrl ? (
                <a href={notification.actionUrl} className="text-xs font-medium text-primary hover:underline">
                  Xem chi tiết
                </a>
              ) : null}
            </div>
          </div>
          {!isRead && (
            <Button
              variant="ghost"
              size="icon"
              onClick={() => markNotificationAsRead(notification.id)}
              className="mt-1 h-7 w-7 shrink-0 opacity-0 transition-opacity hover:bg-transparent hover:text-primary focus:opacity-100 group-hover:opacity-100"
              aria-label="Đánh dấu đã đọc"
            >
              <Eye className="h-4 w-4" />
            </Button>
          )}
        </div>
      )
    },
    [markNotificationAsRead],
  )

  return (
    <div className="bg-card">
      <div className="grid w-full grid-cols-[auto_minmax(0,1fr)_auto] items-center gap-3 px-4 py-2 md:gap-4">
        <div className="flex shrink-0 items-center gap-2 justify-self-start">
          <Button
            variant="ghost"
            size="icon"
            onClick={onToggleLeftSidebar}
            title={isLeftSidebarCollapsed ? "Expand sidebar" : "Collapse sidebar"}
            className="h-9 w-9"
          >
            <Menu className="h-5 w-5" />
          </Button>

          <BrandLogo
            className="gap-2"
            imageClassName="h-10 w-10"
            textClassName="hidden md:block font-semibold text-lg"
          />
        </div>

        <div className="flex min-w-0 justify-center justify-self-center">
          <div className="flex w-full min-w-0 max-w-5xl flex-col gap-2 md:flex-row md:items-center md:gap-3">
            <div className="relative flex-1 min-w-0 md:min-w-[520px] md:max-w-3xl">
              <div className="relative flex h-11 items-center rounded-full border border-border/60 bg-background/80 pl-4 pr-20 text-sm shadow-sm transition-colors focus-within:border-primary focus-within:ring-2 focus-within:ring-primary/20 md:h-14 md:pl-5 md:pr-28 md:text-lg">
                <Search className="mr-2 h-4 w-4 text-muted-foreground md:h-5 md:w-5" />
                <Input
                  placeholder="Search files..."
                  value={searchQuery}
                  onChange={(e) => {
                    onSearchChange(e.target.value)
                  }}
                  className="h-full flex-1 border-0 bg-transparent px-0 text-sm shadow-none focus-visible:border-0 focus-visible:ring-0 focus-visible:ring-offset-0 md:text-lg"
                />
                <div className="pointer-events-none absolute inset-y-0 right-0 w-20 md:w-28" />
              </div>
              <div className="pointer-events-none absolute inset-y-0 right-2 flex items-center gap-1">
                {selectedTag && (
                  <Badge
                    variant="secondary"
                    className="pointer-events-auto gap-1 rounded-full bg-primary/10 text-primary"
                  >
                    {selectedTag.name}
                    <Button
                      variant="ghost"
                      size="icon"
                      className="h-5 w-5 p-0 text-primary hover:bg-transparent"
                      onClick={onClearTag}
                    >
                      <span className="sr-only">Clear tag filter</span>
                      <X className="h-3 w-3" />
                    </Button>
                  </Badge>
                )}
                <Button
                  variant="ghost"
                  size="icon"
                  className="pointer-events-auto h-8 w-8 md:hidden"
                  title="Advanced search"
                  onClick={() => setIsAdvancedSearchOpen(true)}
                >
                  <SlidersHorizontal className="h-4 w-4" />
                </Button>
              </div>
            </div>
            <Button
              variant="ghost"
              size="icon"
              className="hidden md:inline-flex h-9 w-9 shrink-0 rounded-full border border-border/60 bg-background/80 p-0 shadow-sm"
              onClick={() => setIsAdvancedSearchOpen(true)}
              title="Mở tìm kiếm nâng cao"
            >
              <span className="sr-only">Mở tìm kiếm nâng cao</span>
              <SlidersHorizontal className="h-4 w-4" />
            </Button>
          </div>
        </div>

        <Dialog open={isAdvancedSearchOpen} onOpenChange={setIsAdvancedSearchOpen}>
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
                <div className="flex flex-wrap gap-2 min-h-[80px] max-h-[120px] overflow-y-auto rounded-md border p-3">
                  {getAllTags(tags).map((tag) => (
                    <Badge
                      key={tag.id}
                      variant={advancedSearchTags.includes(tag.name) ? "default" : "outline"}
                      className="h-fit cursor-pointer"
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

        <div className="flex shrink-0 items-center gap-3 justify-self-end">
          <Popover open={isNotificationsOpen} onOpenChange={setIsNotificationsOpen}>
            <PopoverTrigger asChild>
              <Button
                variant="ghost"
                size="icon"
                className="relative h-11 w-11 rounded-full border border-border/60 bg-background/80 p-0 shadow-sm"
                aria-label="Thông báo"
              >
                <Bell className="h-5 w-5" />
                <span className="sr-only">Xem thông báo</span>
                {unreadCount > 0 && (
                  <span className="absolute top-2.5 right-2.5 block h-2 w-2 rounded-full bg-destructive" />
                )}
              </Button>
            </PopoverTrigger>
            <PopoverContent
              align="end"
              sideOffset={8}
              className="flex w-96 max-w-[90vw] flex-col overflow-hidden p-0"
              style={{ maxHeight: "min(70vh, 24rem)" }}
            >
              <div className="flex shrink-0 items-start justify-between gap-2 border-b px-4 py-3">
                <div className="space-y-0.5">
                  <p className="text-sm font-semibold">Thông báo</p>
                  <p className="text-xs text-muted-foreground">
                    {unreadCount > 0 ? `${unreadCount} thông báo chưa đọc` : "Tất cả thông báo đã xem"}
                  </p>
                </div>
                {unreadCount > 0 && (
                  <Button
                    variant="ghost"
                    size="sm"
                    className="h-7 shrink-0 px-2 text-xs text-primary hover:bg-primary/10"
                    onClick={markAllNotificationsAsRead}
                  >
                    Đánh dấu tất cả đã đọc
                  </Button>
                )}
              </div>
              {isLoadingNotifications ? (
                <div className="flex flex-1 items-center justify-center px-4 py-10 text-sm text-muted-foreground">
                  Đang tải thông báo...
                </div>
              ) : hasNotifications ? (
                <ScrollArea className="flex-1" style={{ maxHeight: "calc(min(70vh, 24rem) - 64px)" }}>
                  <div className="space-y-5 px-4 py-3">
                    {unreadNotifications.length > 0 && (
                      <div className="space-y-3">
                        <p className="text-xs font-semibold uppercase text-muted-foreground">Chưa đọc</p>
                        <div className="space-y-3">
                          {unreadNotifications.map((notification) => renderNotification(notification))}
                        </div>
                      </div>
                    )}
                    {readNotifications.length > 0 && (
                      <div className="space-y-3">
                        <p className="text-xs font-semibold uppercase text-muted-foreground">Đã đọc</p>
                        <div className="space-y-3">
                          {readNotifications.map((notification) => renderNotification(notification))}
                        </div>
                      </div>
                    )}
                  </div>
                </ScrollArea>
              ) : (
                <div className="flex flex-1 items-center justify-center px-4 py-10 text-center text-sm text-muted-foreground">
                  Hiện chưa có thông báo nào.
                </div>
              )}
            </PopoverContent>
          </Popover>

        </div>
      </div>
    </div>
  )
}
