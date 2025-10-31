"use client"

import { useCallback, useEffect, useMemo, useState } from "react"
import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Label } from "@/components/ui/label"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Input } from "@/components/ui/input"
import { Alert, AlertDescription } from "@/components/ui/alert"
import { Badge } from "@/components/ui/badge"
import { Switch } from "@/components/ui/switch"
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover"
import {
  Command,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
} from "@/components/ui/command"
import { cn } from "@/lib/utils"
import { fetchGroups, fetchUsers } from "@/lib/api"
import {
  Check,
  Copy,
  ExternalLink,
  Link2,
  Loader2,
  Globe2,
  User as UserIcon,
  Users,
  ChevronDown,
  Lock,
} from "lucide-react"
import type {
  FileItem,
  ShareLink,
  ShareOptions,
  ShareSubjectType,
  Group,
  User,
} from "@/lib/types"

const SHARE_DURATION_OPTIONS = [
  { label: "15 minutes", value: 15 },
  { label: "1 hour", value: 60 },
  { label: "8 hours", value: 480 },
  { label: "24 hours", value: 1440 },
  { label: "7 days", value: 10080 },
] as const

const DEFAULT_DURATION = SHARE_DURATION_OPTIONS[3].value

type ShareDialogProps = {
  open: boolean
  file: FileItem | null
  onOpenChange: (open: boolean) => void
  onConfirm: (options: ShareOptions) => Promise<void>
  isLoading: boolean
  result: ShareLink | null
  error?: string | null
  onReset: () => void
}

export function ShareDialog({
  open,
  file,
  onOpenChange,
  onConfirm,
  isLoading,
  result,
  error,
  onReset,
}: ShareDialogProps) {
  const [shareTarget, setShareTarget] = useState<ShareSubjectType>("public")
  const [expiresInMinutes, setExpiresInMinutes] = useState<number>(DEFAULT_DURATION)
  const [copiedShort, setCopiedShort] = useState(false)
  const [copiedFull, setCopiedFull] = useState(false)
  const [users, setUsers] = useState<User[]>([])
  const [groups, setGroups] = useState<Group[]>([])
  const [isLoadingUsers, setIsLoadingUsers] = useState(false)
  const [isLoadingGroups, setIsLoadingGroups] = useState(false)
  const [userPickerOpen, setUserPickerOpen] = useState(false)
  const [groupPickerOpen, setGroupPickerOpen] = useState(false)
  const [selectedUserId, setSelectedUserId] = useState<string | null>(null)
  const [selectedGroupId, setSelectedGroupId] = useState<string | null>(null)
  const [validationError, setValidationError] = useState<string | null>(null)
  const [passwordEnabled, setPasswordEnabled] = useState(false)
  const [password, setPassword] = useState("")

  const resetCopyState = useCallback(() => {
    setCopiedShort(false)
    setCopiedFull(false)
  }, [])

  useEffect(() => {
    if (!open) {
      resetCopyState()
      setUserPickerOpen(false)
      setGroupPickerOpen(false)
      setPasswordEnabled(false)
      setPassword("")
      return
    }

    resetCopyState()
    setShareTarget("public")
    setExpiresInMinutes(DEFAULT_DURATION)
    setSelectedUserId(null)
    setSelectedGroupId(null)
    setValidationError(null)
    setPasswordEnabled(false)
    setPassword("")
  }, [open, file?.id, resetCopyState])

  useEffect(() => {
    if (!result) {
      resetCopyState()
    }
  }, [resetCopyState, result?.shortUrl, result?.url])

  useEffect(() => {
    if (!open) {
      return
    }

    let cancelled = false

    const loadAudiences = async () => {
      if (users.length === 0) {
        setIsLoadingUsers(true)
        try {
          const list = await fetchUsers()
          if (!cancelled) {
            setUsers(list)
          }
        } catch (err) {
          console.error("[ui] Failed to load users for share dialog:", err)
        } finally {
          if (!cancelled) {
            setIsLoadingUsers(false)
          }
        }
      }

      if (groups.length === 0) {
        setIsLoadingGroups(true)
        try {
          const list = await fetchGroups()
          if (!cancelled) {
            setGroups(list)
          }
        } catch (err) {
          console.error("[ui] Failed to load groups for share dialog:", err)
        } finally {
          if (!cancelled) {
            setIsLoadingGroups(false)
          }
        }
      }
    }

    void loadAudiences()

    return () => {
      cancelled = true
    }
  }, [open, users.length, groups.length])

  useEffect(() => {
    if (shareTarget !== "user") {
      setUserPickerOpen(false)
    }
    if (shareTarget !== "group") {
      setGroupPickerOpen(false)
    }
    setValidationError(null)
  }, [shareTarget])

  const selectedUser = useMemo(() => {
    if (!selectedUserId) {
      return null
    }
    return users.find((user) => user.id === selectedUserId) ?? null
  }, [selectedUserId, users])

  const selectedGroup = useMemo(() => {
    if (!selectedGroupId) {
      return null
    }
    return groups.find((group) => group.id === selectedGroupId) ?? null
  }, [selectedGroupId, groups])

  const resultUser = useMemo(() => {
    if (!result || result.subjectType !== "user" || !result.subjectId) {
      return null
    }
    return users.find((user) => user.id === result.subjectId) ?? null
  }, [result?.subjectId, result?.subjectType, users])

  const resultGroup = useMemo(() => {
    if (!result || result.subjectType !== "group" || !result.subjectId) {
      return null
    }
    return groups.find((group) => group.id === result.subjectId) ?? null
  }, [result?.subjectId, result?.subjectType, groups])

  const audienceType: ShareSubjectType = result?.subjectType ?? shareTarget
  const audienceUser = audienceType === "user" ? resultUser ?? selectedUser : null
  const audienceGroup = audienceType === "group" ? resultGroup ?? selectedGroup : null

  const requiresPassword = useMemo(() => {
    if (result) {
      return Boolean(result.requiresPassword)
    }

    if (!passwordEnabled) {
      return false
    }

    return password.trim().length > 0
  }, [result?.requiresPassword, passwordEnabled, password])

  const audienceBadgeLabel = useMemo(() => {
    switch (audienceType) {
      case "user":
        return "Specific user"
      case "group":
        return "Group access"
      default:
        return "Public link"
    }
  }, [audienceType])

  const selectionDescription = useMemo(() => {
    switch (shareTarget) {
      case "user":
        return selectedUser
          ? `Only ${selectedUser.displayName} can use this link.`
          : "Only the selected user will be able to use this link."
      case "group":
        return selectedGroup
          ? `Members of ${selectedGroup.name} can use this link.`
          : "Members of the selected group will be able to use this link."
      default:
        return "Anyone with the link can download this file until it expires."
    }
  }, [shareTarget, selectedUser, selectedGroup])

  const audienceSummary = useMemo(() => {
    let summary = (() => {
      switch (audienceType) {
        case "user":
          return audienceUser?.displayName
            ? `Only ${audienceUser.displayName} can use this link.`
            : "Only the designated user can use this link."
        case "group":
          return audienceGroup?.name
            ? `Members of ${audienceGroup.name} can use this link.`
            : "Members of the designated group can use this link."
        default:
          return "Anyone with the link can download this file until it expires."
      }
    })()

    if (requiresPassword) {
      summary = `${summary} Recipients must enter the password to access this link.`
    }

    return summary
  }, [audienceType, audienceUser, audienceGroup, requiresPassword])

  const handleAudienceChange = (value: ShareSubjectType) => {
    setShareTarget(value)
    setValidationError(null)
  }

  const handleUserSelect = (userId: string) => {
    setSelectedUserId(userId)
    setValidationError(null)
    setUserPickerOpen(false)
  }

  const handleGroupSelect = (groupId: string) => {
    setSelectedGroupId(groupId)
    setValidationError(null)
    setGroupPickerOpen(false)
  }

  const userButtonLabel = selectedUser
    ? selectedUser.displayName
    : isLoadingUsers
      ? "Loading users..."
      : "Select user"

  const groupButtonLabel = selectedGroup
    ? selectedGroup.name
    : isLoadingGroups
      ? "Loading groups..."
      : "Select group"

  const userButtonDisabled = isLoadingUsers && users.length === 0
  const groupButtonDisabled = isLoadingGroups && groups.length === 0

  const formattedExpiry = useMemo(() => {
    if (!result?.expiresAtUtc) {
      return null
    }

    try {
      return new Date(result.expiresAtUtc).toLocaleString()
    } catch (err) {
      console.warn("[ui] Failed to format share link expiration:", err)
      return result.expiresAtUtc
    }
  }, [result?.expiresAtUtc])

  const handleOpenChange = (nextOpen: boolean) => {
    onOpenChange(nextOpen)
    if (!nextOpen) {
      setUserPickerOpen(false)
      setGroupPickerOpen(false)
      setValidationError(null)
      setPasswordEnabled(false)
      setPassword("")
      onReset()
    }
  }

  const handleSubmit = async () => {
    if (!file || isLoading) {
      return
    }

    const subjectId =
      shareTarget === "user"
        ? selectedUserId
        : shareTarget === "group"
          ? selectedGroupId
          : null

    if (shareTarget !== "public" && !subjectId) {
      setValidationError(
        shareTarget === "user"
          ? "Select a user to share this link with before creating it."
          : "Select a group to share this link with before creating it.",
      )
      return
    }

    const trimmedPassword = password.trim()
    if (passwordEnabled && trimmedPassword.length < 4) {
      setValidationError("Set a password with at least 4 characters before creating the link.")
      return
    }

    resetCopyState()
    setValidationError(null)
    await onConfirm({
      subjectType: shareTarget,
      subjectId,
      expiresInMinutes,
      password: passwordEnabled ? trimmedPassword : null,
    })
  }

  const handlePasswordToggle = (checked: boolean) => {
    setPasswordEnabled(checked)
    if (!checked) {
      setPassword("")
    }
    setValidationError(null)
  }

  const handleCopy = async (value: string | null | undefined, type: "short" | "full") => {
    if (!value) {
      return
    }

    try {
      await navigator.clipboard.writeText(value)
      setCopiedShort(type === "short")
      setCopiedFull(type === "full")
    } catch (err) {
      console.error("[ui] Failed to copy share link to clipboard:", err)
      resetCopyState()
    }
  }

  const hasResult = Boolean(result)

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent className="max-w-3xl sm:max-w-4xl">
        <DialogHeader>
          <DialogTitle>Share file</DialogTitle>
          <DialogDescription>
            Generate a temporary link to share “{file?.name ?? "No file selected"}”. The link will expire automatically.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-6">
          <div className="space-y-6">
            <div className="rounded-xl border border-dashed border-primary/40 bg-primary/5 p-4">
              <div className="flex flex-wrap items-center justify-between gap-3">
                <div className="space-y-1">
                  <p className="text-sm font-medium text-card-foreground">{file?.name ?? "Select a file"}</p>
                  <p className="text-xs text-muted-foreground">
                    {file?.size ?? ""}
                    {file?.latestVersionNumber ? ` • Version ${file.latestVersionNumber}` : ""}
                  </p>
                </div>
                <div className="flex flex-wrap items-center gap-2">
                  <Badge variant="outline" className="gap-1 border-primary/50 text-primary">
                    <Link2 className="h-3.5 w-3.5" />
                    Short link ready
                  </Badge>
                  <Badge variant="secondary" className="gap-1 bg-primary/10 text-primary">
                    {audienceType === "user" ? (
                      <UserIcon className="h-3.5 w-3.5" />
                    ) : audienceType === "group" ? (
                      <Users className="h-3.5 w-3.5" />
                    ) : (
                      <Globe2 className="h-3.5 w-3.5" />
                    )}
                    {audienceBadgeLabel}
                  </Badge>
                  {requiresPassword ? (
                    <Badge variant="secondary" className="gap-1 bg-amber-100 text-amber-900 dark:bg-amber-500/10 dark:text-amber-100">
                      <Lock className="h-3.5 w-3.5" />
                      Password required
                    </Badge>
                  ) : null}
                </div>
              </div>
              <p className="mt-2 text-xs text-muted-foreground">{audienceSummary}</p>
            </div>
            <div className="space-y-4 rounded-xl border bg-card p-4 shadow-sm">
              <div className="space-y-2">
                <Label htmlFor="share-audience" className="font-semibold">
                  Share with
                </Label>
                <Select
                  value={shareTarget}
                  onValueChange={(value) => handleAudienceChange(value as ShareSubjectType)}
                >
                  <SelectTrigger id="share-audience" className="w-full">
                    <SelectValue placeholder="Select who can access this link" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="public">Anyone with the link</SelectItem>
                    <SelectItem value="user">Specific user</SelectItem>
                    <SelectItem value="group">Group</SelectItem>
                  </SelectContent>
                </Select>
                <p className="text-xs text-muted-foreground">{selectionDescription}</p>
              </div>

              {shareTarget === "user" ? (
                <div className="space-y-2">
                  <Label className="font-semibold">Select user</Label>
                  <Popover open={userPickerOpen} onOpenChange={setUserPickerOpen}>
                    <PopoverTrigger asChild>
                      <Button
                        type="button"
                        variant="outline"
                        role="combobox"
                        className={cn(
                          "w-full justify-between",
                          !selectedUser && "text-muted-foreground",
                        )}
                        disabled={userButtonDisabled}
                      >
                        <span className="flex items-center gap-2">
                          <UserIcon className="h-4 w-4" />
                          {userButtonLabel}
                        </span>
                        <ChevronDown className="h-4 w-4 opacity-50" />
                      </Button>
                    </PopoverTrigger>
                    <PopoverContent className="w-[320px] p-0" align="start">
                      <Command>
                        <CommandInput placeholder="Search users..." />
                        <CommandEmpty>No users found.</CommandEmpty>
                        <CommandList>
                          <CommandGroup>
                            {users.map((user) => {
                              const isSelected = user.id === selectedUserId
                              return (
                                <CommandItem key={user.id} value={user.id} onSelect={() => handleUserSelect(user.id)}>
                                  <Check
                                    className={cn(
                                      "mr-2 h-4 w-4",
                                      isSelected ? "opacity-100" : "opacity-0",
                                    )}
                                  />
                                  <div className="flex flex-col">
                                    <span className="text-sm font-medium">{user.displayName}</span>
                                    {user.email ? (
                                      <span className="text-xs text-muted-foreground">{user.email}</span>
                                    ) : null}
                                  </div>
                                </CommandItem>
                              )
                            })}
                          </CommandGroup>
                        </CommandList>
                      </Command>
                    </PopoverContent>
                  </Popover>
                  {selectedUser?.email ? (
                    <p className="text-xs text-muted-foreground">Email: {selectedUser.email}</p>
                  ) : null}
                </div>
              ) : null}

              {shareTarget === "group" ? (
                <div className="space-y-2">
                  <Label className="font-semibold">Select group</Label>
                  <Popover open={groupPickerOpen} onOpenChange={setGroupPickerOpen}>
                    <PopoverTrigger asChild>
                      <Button
                        type="button"
                        variant="outline"
                        role="combobox"
                        className={cn(
                          "w-full justify-between",
                          !selectedGroup && "text-muted-foreground",
                        )}
                        disabled={groupButtonDisabled}
                      >
                        <span className="flex items-center gap-2">
                          <Users className="h-4 w-4" />
                          {groupButtonLabel}
                        </span>
                        <ChevronDown className="h-4 w-4 opacity-50" />
                      </Button>
                    </PopoverTrigger>
                    <PopoverContent className="w-[320px] p-0" align="start">
                      <Command>
                        <CommandInput placeholder="Search groups..." />
                        <CommandEmpty>No groups found.</CommandEmpty>
                        <CommandList>
                          <CommandGroup>
                            {groups.map((group) => {
                              const isSelected = group.id === selectedGroupId
                              return (
                                <CommandItem key={group.id} value={group.id} onSelect={() => handleGroupSelect(group.id)}>
                                  <Check
                                    className={cn(
                                      "mr-2 h-4 w-4",
                                      isSelected ? "opacity-100" : "opacity-0",
                                    )}
                                  />
                                  <div className="flex flex-col">
                                    <span className="text-sm font-medium">{group.name}</span>
                                    {group.description ? (
                                      <span className="text-xs text-muted-foreground">{group.description}</span>
                                    ) : null}
                                  </div>
                                </CommandItem>
                              )
                            })}
                          </CommandGroup>
                        </CommandList>
                      </Command>
                    </PopoverContent>
                  </Popover>
                  {selectedGroup?.description ? (
                    <p className="text-xs text-muted-foreground">{selectedGroup.description}</p>
                  ) : null}
                </div>
              ) : null}

              <div className="space-y-2">
                <Label htmlFor="share-duration" className="font-semibold">
                  Share duration
                </Label>
                <Select
                  value={expiresInMinutes.toString()}
                  onValueChange={(value) => setExpiresInMinutes(Number.parseInt(value, 10))}
                >
                  <SelectTrigger id="share-duration" className="w-full">
                    <SelectValue placeholder="Select how long the link should stay active" />
                  </SelectTrigger>
                  <SelectContent>
                    {SHARE_DURATION_OPTIONS.map((option) => (
                      <SelectItem key={option.value} value={option.value.toString()}>
                        {option.label}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>

              <div className="space-y-2">
                <div className="flex items-center justify-between gap-2">
                  <Label htmlFor="share-password-toggle" className="font-semibold">
                    Password protection
                  </Label>
                  <Switch
                    id="share-password-toggle"
                    checked={passwordEnabled}
                    onCheckedChange={handlePasswordToggle}
                  />
                </div>
                <p className="text-xs text-muted-foreground">
                  Require recipients to enter a password before accessing the shared file. Use at least 4 characters.
                </p>
                {passwordEnabled ? (
                  <Input
                    id="share-password"
                    type="password"
                    placeholder="Enter a password"
                    value={password}
                    onChange={(event) => setPassword(event.target.value)}
                    autoComplete="new-password"
                  />
                ) : null}
              </div>
            </div>

            {validationError || error ? (
              <Alert variant="destructive">
                <AlertDescription className="space-y-1">
                  {validationError ? <p>{validationError}</p> : null}
                  {error ? <p>{error}</p> : null}
                </AlertDescription>
              </Alert>
            ) : null}
          </div>

          {hasResult ? (
            <div className="space-y-4 rounded-xl border border-primary/40 bg-primary/5 p-4">
              <div className="space-y-3">
                <div className="flex flex-wrap items-start justify-between gap-3">
                  <div>
                    <p className="text-sm font-semibold text-primary">Short link</p>
                    <p className="text-xs text-muted-foreground">
                      Use this link for quick sharing in chat or email.
                    </p>
                  </div>
                  <Button asChild variant="ghost" size="sm" className="gap-1">
                    <a href={result.shortUrl} target="_blank" rel="noreferrer">
                      Open
                      <ExternalLink className="h-3.5 w-3.5" />
                    </a>
                  </Button>
                </div>
                <div className="flex flex-col gap-2 sm:flex-row sm:items-center">
                  <Input value={result.shortUrl} readOnly className="sm:flex-1" />
                  <Button
                    variant="outline"
                    size="icon"
                    onClick={() => handleCopy(result.shortUrl, "short")}
                    className={cn(
                      "transition-colors",
                      copiedShort ? "border-primary text-primary" : undefined,
                    )}
                  >
                    {copiedShort ? <Check className="h-4 w-4" /> : <Copy className="h-4 w-4" />}
                    <span className="sr-only">Copy short link</span>
                  </Button>
                </div>
              </div>

              {formattedExpiry ? (
                <p className="text-xs text-muted-foreground">
                  Links expire on {formattedExpiry}. Share recipients will no longer have access afterwards.
                </p>
              ) : null}
            </div>
          ) : null}
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={() => handleOpenChange(false)}>
            Cancel
          </Button>
          <Button onClick={handleSubmit} disabled={!file || isLoading} className="gap-2">
            {isLoading ? <Loader2 className="h-4 w-4 animate-spin" /> : null}
            Create link
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
