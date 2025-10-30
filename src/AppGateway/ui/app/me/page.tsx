"use client"

import type React from "react"

import { useState, useEffect, useRef, useMemo } from "react"
import { useRouter } from "next/navigation"
import {
  ArrowLeft,
  Camera,
  Mail,
  Briefcase,
  MapPin,
  Phone,
  Calendar,
  Users,
  ChevronDown,
  Check,
} from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Separator } from "@/components/ui/separator"
import { useAuthGuard } from "@/hooks/use-auth-guard"
import { fetchCurrentUserProfile, updateCurrentUserProfile, updateUserAvatar, fetchGroups } from "@/lib/api"
import { getCachedAuthSnapshot } from "@/lib/auth-state"
import type { Group, User } from "@/lib/types"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover"
import {
  Command,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
} from "@/components/ui/command"
import { cn } from "@/lib/utils"

const APP_HOME_ROUTE = "/app/"
const ME_ROUTE = "/me/"
const SIGN_IN_ROUTE = `/signin/?redirectUri=${encodeURIComponent(ME_ROUTE)}`

type ProfileFormState = {
  displayName: string
  primaryGroupId: string | null
  groupIds: string[]
}

export default function ProfilePage() {
  const router = useRouter()
  const cachedSnapshot = useMemo(() => getCachedAuthSnapshot(), [])
  const [user, setUser] = useState<User | null>(() => cachedSnapshot?.user ?? null)
  const [isEditing, setIsEditing] = useState(false)
  const [avatarPreview, setAvatarPreview] = useState<string | null>(null)
  const fileInputRef = useRef<HTMLInputElement>(null)
  const cachedUser = cachedSnapshot?.user ?? null
  const initialGroups = useMemo(() => {
    const base = Array.from(new Set(cachedUser?.groupIds ?? []))
    const primaryGroupId = cachedUser?.primaryGroupId ?? base[0] ?? null
    if (primaryGroupId && !base.includes(primaryGroupId)) {
      base.unshift(primaryGroupId)
    }
    return { list: base, primary: cachedUser?.primaryGroupId ?? base[0] ?? null }
  }, [cachedUser?.groupIds, cachedUser?.primaryGroupId])
  const [formValues, setFormValues] = useState<ProfileFormState>({
    displayName: cachedUser?.displayName ?? "",
    primaryGroupId: initialGroups.primary,
    groupIds: initialGroups.list,
  })
  const [groups, setGroups] = useState<Group[]>([])
  const [isGroupPickerOpen, setGroupPickerOpen] = useState(false)
  const [isSaving, setIsSaving] = useState(false)
  const [feedback, setFeedback] = useState<{ type: "success" | "error"; message: string } | null>(null)
  const { isAuthenticated, isChecking } = useAuthGuard(ME_ROUTE)

  useEffect(() => {
    const locationSnapshot =
      typeof window === "undefined"
        ? "(window unavailable)"
        : `${window.location.pathname}${window.location.search}${window.location.hash}`

    console.debug(
      "[profile] Trang profile được mount tại location=%s với cachedUser=%s",
      locationSnapshot,
      cachedSnapshot?.user?.id ?? "(none)",
    )
  }, [cachedSnapshot?.user?.id])

  useEffect(() => {
    let active = true

    fetchGroups()
      .then((data) => {
        if (!active) return
        setGroups(data)
      })
      .catch((error) => {
        console.error("[profile] Không thể tải danh sách group:", error)
        if (!active) return
        setGroups([])
      })

    return () => {
      active = false
    }
  }, [])

  useEffect(() => {
    if (!isAuthenticated || isChecking) {
      console.debug(
        "[profile] Bỏ qua việc tải hồ sơ vì isAuthenticated=%s, isChecking=%s",
        isAuthenticated,
        isChecking,
      )
      return
    }

    let mounted = true

    const loadProfile = async () => {
      console.debug("[profile] Bắt đầu tải hồ sơ người dùng trong trang profile.")
      try {
        const profile = await fetchCurrentUserProfile()
        if (!mounted) return

        if (!profile) {
          console.warn("[profile] API trả về null, chuyển hướng tới trang đăng nhập.")
          router.replace(SIGN_IN_ROUTE)
          return
        }

        console.debug("[profile] Nhận được hồ sơ người dùng với id:", profile.id)
        setUser(profile)
        setFormValues({
          displayName: profile.displayName,
          primaryGroupId: profile.primaryGroupId ?? profile.groupIds?.[0] ?? null,
          groupIds: Array.from(
            new Set(
              (() => {
                const base = profile.groupIds ?? []
                if (profile.primaryGroupId && !base.includes(profile.primaryGroupId)) {
                  return [profile.primaryGroupId, ...base]
                }
                return base
              })(),
            ),
          ),
        })
      } catch (error) {
        console.error("[ui] Không thể tải hồ sơ người dùng:", error)
        if (!mounted) return

        console.warn("[profile] Giữ nguyên trạng thái hiện tại do lỗi khi tải hồ sơ.")
        setFeedback({
          type: "error",
          message: "Không thể tải hồ sơ người dùng. Vui lòng thử lại.",
        })
      }
    }

    loadProfile()

    return () => {
      mounted = false
    }
  }, [isAuthenticated, isChecking, router])

  const handleAvatarChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (!file) return

    // Create preview
    const reader = new FileReader()
    reader.onloadend = () => {
      setAvatarPreview(reader.result as string)
    }
    reader.readAsDataURL(file)

    // Upload to server
    try {
      const newAvatarUrl = await updateUserAvatar(file)
      setUser((prev) => (prev ? { ...prev, avatar: newAvatarUrl } : null))
    } catch (error) {
      console.error("[v0] Error uploading avatar:", error)
    }
  }

  const handleDisplayNameChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    const value = event.target.value
    setFormValues((prev) => ({ ...prev, displayName: value }))
  }

  const toggleGroupSelection = (groupId: string) => {
    setFormValues((prev) => {
      const nextGroupIds = new Set(prev.groupIds ?? [])
      if (nextGroupIds.has(groupId)) {
        nextGroupIds.delete(groupId)
      } else {
        nextGroupIds.add(groupId)
      }

      const normalized = Array.from(nextGroupIds)
      const nextPrimary = prev.primaryGroupId && normalized.includes(prev.primaryGroupId)
        ? prev.primaryGroupId
        : normalized[0] ?? null

      return {
        ...prev,
        groupIds: normalized,
        primaryGroupId: nextPrimary,
      }
    })
  }

  const handlePrimaryGroupChange = (groupId: string | null) => {
    setFormValues((prev) => {
      const nextGroupIds = new Set(prev.groupIds ?? [])
      if (groupId) {
        nextGroupIds.add(groupId)
      }

      return {
        ...prev,
        primaryGroupId: groupId,
        groupIds: Array.from(nextGroupIds),
      }
    })
  }

  const primaryGroupName = useMemo(() => {
    if (!formValues.primaryGroupId) {
      return ""
    }

    return groups.find((group) => group.id === formValues.primaryGroupId)?.name ?? ""
  }, [formValues.primaryGroupId, groups])

  const selectedGroupNames = useMemo(() => {
    if (!formValues.groupIds.length) {
      return [] as string[]
    }

    return formValues.groupIds
      .map((groupId) => groups.find((group) => group.id === groupId)?.name)
      .filter((name): name is string => Boolean(name))
  }, [formValues.groupIds, groups])

  const selectedGroupCount = formValues.groupIds.length

  const handleCancelEdit = () => {
    if (user) {
      setFormValues({
        displayName: user.displayName,
        primaryGroupId: user.primaryGroupId ?? user.groupIds?.[0] ?? null,
        groupIds: Array.from(
          new Set(
            (() => {
              const base = user.groupIds ?? []
              if (user.primaryGroupId && !base.includes(user.primaryGroupId)) {
                return [user.primaryGroupId, ...base]
              }
              return base
            })(),
          ),
        ),
      })
    }
    setIsEditing(false)
    setFeedback(null)
  }

  const handleSaveChanges = async () => {
    if (!user) return

    setIsSaving(true)
    setFeedback(null)

    try {
      const updated = await updateCurrentUserProfile({
        displayName: formValues.displayName,
        primaryGroupId: formValues.primaryGroupId,
        groupIds: formValues.groupIds,
      })

      setUser(updated)
      const normalizedGroups = Array.from(new Set(updated.groupIds ?? []))
      const primaryGroupId = updated.primaryGroupId ?? normalizedGroups[0] ?? null
      if (primaryGroupId && !normalizedGroups.includes(primaryGroupId)) {
        normalizedGroups.unshift(primaryGroupId)
      }
      setFormValues({
        displayName: updated.displayName,
        primaryGroupId,
        groupIds: normalizedGroups,
      })
      setIsEditing(false)
      setFeedback({ type: "success", message: "Hồ sơ của bạn đã được cập nhật." })
    } catch (error) {
      console.error("[ui] Không thể cập nhật hồ sơ:", error)
      setFeedback({ type: "error", message: "Không thể cập nhật hồ sơ. Vui lòng thử lại." })
    } finally {
      setIsSaving(false)
    }
  }

  const hasChanges = useMemo(() => {
    if (!user) return false
    const normalizedName = formValues.displayName.trim()
    const currentName = user.displayName.trim()
    const normalizedPrimary = formValues.primaryGroupId ?? null
    const currentPrimary = user.primaryGroupId ?? null

    const normalizeGroupList = (list?: string[]) => Array.from(new Set(list ?? [])).sort()
    const formGroupList = normalizeGroupList(formValues.groupIds)
    const userGroupList = normalizeGroupList(user.groupIds)

    const groupsChanged =
      formGroupList.length !== userGroupList.length ||
      formGroupList.some((id, index) => id !== userGroupList[index])

    return normalizedName !== currentName || normalizedPrimary !== currentPrimary || groupsChanged
  }, [formValues.displayName, formValues.groupIds, formValues.primaryGroupId, user])

  useEffect(() => {
    if (!isEditing) {
      setGroupPickerOpen(false)
    }
  }, [isEditing])

  const joinedDate = useMemo(() => {
    if (!user?.createdAtUtc) {
      return "—"
    }

    try {
      const date = new Date(user.createdAtUtc)
      if (Number.isNaN(date.getTime())) {
        return "—"
      }

      return date.toLocaleDateString(undefined, {
        year: "numeric",
        month: "long",
        day: "numeric",
      })
    } catch {
      return "—"
    }
  }, [user?.createdAtUtc])

  if (isChecking) {
    return (
      <div className="flex items-center justify-center h-screen">
        <div className="text-muted-foreground">Đang kiểm tra trạng thái đăng nhập…</div>
      </div>
    )
  }

  if (!isAuthenticated) {
    return null
  }

  if (!user) {
    return (
      <div className="flex items-center justify-center h-screen">
        <div className="text-muted-foreground">Loading...</div>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-background">
      <div className="border-b border-border bg-card">
        <div className="container mx-auto px-4 py-4">
          <Button variant="ghost" size="sm" asChild>
            <a href={APP_HOME_ROUTE} className="gap-2">
              <ArrowLeft className="h-4 w-4" />
              Back to Files
            </a>
          </Button>
        </div>
      </div>

      <div className="container mx-auto px-4 py-8 max-w-4xl">
        <Card>
          <CardHeader>
            <CardTitle>Profile</CardTitle>
            <CardDescription>Manage your personal information and preferences</CardDescription>
          </CardHeader>
          <CardContent className="space-y-6">
            <div className="flex items-center gap-6">
              <div className="relative">
                <Avatar className="h-24 w-24">
                  <AvatarImage src={avatarPreview || user.avatar || "/placeholder.svg"} alt={user.displayName} />
                  <AvatarFallback className="text-2xl">{user.displayName?.charAt(0) ?? '?'}</AvatarFallback>
                </Avatar>
                <input
                  ref={fileInputRef}
                  type="file"
                  accept="image/*"
                  className="hidden"
                  onChange={handleAvatarChange}
                />
                <Button
                  size="icon"
                  variant="secondary"
                  className="absolute bottom-0 right-0 h-8 w-8 rounded-full"
                  title="Change avatar"
                  onClick={() => fileInputRef.current?.click()}
                >
                  <Camera className="h-4 w-4" />
                </Button>
              </div>
              <div className="flex-1">
                <h2 className="text-2xl font-bold">{user.displayName}</h2>
                <p className="text-muted-foreground">{user.roles[0] ?? "Member"}</p>
                {primaryGroupName && (
                  <p className="text-xs text-muted-foreground">Primary group: {primaryGroupName}</p>
                )}
              </div>
              <Button
                variant={isEditing ? "outline" : "default"}
                onClick={() => {
                  if (isEditing) {
                    handleCancelEdit()
                  } else {
                    setIsEditing(true)
                    setFeedback(null)
                  }
                }}
              >
                {isEditing ? "Cancel" : "Edit Profile"}
              </Button>
            </div>

            <Separator />

            {feedback && (
              <div
                className={
                  feedback.type === "error"
                    ? "text-sm text-destructive"
                    : "text-sm text-green-600"
                }
              >
                {feedback.message}
              </div>
            )}

            <div className="grid gap-6">
              <div className="grid gap-2">
                <Label htmlFor="name">Full Name</Label>
                <Input
                  id="name"
                  value={formValues.displayName}
                  onChange={handleDisplayNameChange}
                  disabled={!isEditing}
                />
              </div>

              <div className="grid gap-2">
                <Label htmlFor="email">Email</Label>
                <div className="flex items-center gap-2">
                  <Mail className="h-4 w-4 text-muted-foreground" />
                  <Input id="email" type="email" value={user.email} disabled readOnly />
                </div>
              </div>

              <div className="grid gap-2">
                <Label htmlFor="phone">Phone</Label>
                <div className="flex items-center gap-2">
                  <Phone className="h-4 w-4 text-muted-foreground" />
                  <Input id="phone" type="tel" defaultValue="+1 (555) 123-4567" disabled={!isEditing} />
                </div>
              </div>

              <div className="grid gap-2">
                <Label htmlFor="primary-group">Primary group</Label>
                <div className="flex items-center gap-2">
                  <Briefcase className="h-4 w-4 text-muted-foreground" />
                  <Select
                    value={formValues.primaryGroupId ?? undefined}
                    onValueChange={(value) => handlePrimaryGroupChange(value === "__none__" ? null : value)}
                    disabled={!isEditing || groups.length === 0}
                  >
                    <SelectTrigger id="primary-group" className="w-full">
                      <SelectValue
                        placeholder={groups.length ? "Select primary group" : "No groups available"}
                      />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="__none__">No primary group</SelectItem>
                      {groups.map((group) => (
                        <SelectItem key={group.id} value={group.id}>
                          {group.name}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
                <p className="text-xs text-muted-foreground">
                  {groups.length === 0
                    ? "No groups available"
                    : primaryGroupName
                      ? `Current primary group: ${primaryGroupName}`
                      : "Select a primary group"}
                </p>
              </div>

              <div className="grid gap-2">
                <Label>Groups</Label>
                <div className="flex items-center gap-2">
                  <Users className="h-4 w-4 text-muted-foreground" />
                  <Popover
                    open={isGroupPickerOpen && isEditing}
                    onOpenChange={(open) => {
                      if (!isEditing) return
                      setGroupPickerOpen(open)
                    }}
                  >
                    <PopoverTrigger asChild>
                      <Button
                        type="button"
                        variant="outline"
                        role="combobox"
                        className={cn(
                          "justify-between flex-1",
                          selectedGroupCount === 0 && "text-muted-foreground",
                        )}
                        disabled={!isEditing || groups.length === 0}
                      >
                        {selectedGroupCount > 0
                          ? `${selectedGroupCount} group${selectedGroupCount > 1 ? "s" : ""} selected`
                          : "Select groups"}
                        <ChevronDown className="ml-2 h-4 w-4 opacity-50" />
                      </Button>
                    </PopoverTrigger>
                    <PopoverContent className="p-0 w-[280px]" align="start">
                      <Command>
                        <CommandInput placeholder="Search groups..." />
                        <CommandEmpty>No groups found.</CommandEmpty>
                        <CommandGroup>
                          {groups.map((group) => {
                            const isSelected = formValues.groupIds.includes(group.id)
                            return (
                              <CommandItem
                                key={group.id}
                                value={group.id}
                                onSelect={() => {
                                  toggleGroupSelection(group.id)
                                  setGroupPickerOpen(true)
                                }}
                              >
                                <Check
                                  className={cn(
                                    "mr-2 h-4 w-4",
                                    isSelected ? "opacity-100" : "opacity-0",
                                  )}
                                />
                                <span className="flex-1">{group.name}</span>
                              </CommandItem>
                            )
                          })}
                        </CommandGroup>
                      </Command>
                    </PopoverContent>
                  </Popover>
                </div>
                <p className="text-xs text-muted-foreground">
                  {selectedGroupNames.length > 0
                    ? selectedGroupNames.join(", ")
                    : selectedGroupCount > 0
                      ? `${selectedGroupCount} group${selectedGroupCount > 1 ? "s" : ""} selected`
                      : "Assign groups to keep files organized"}
                </p>
              </div>

              <div className="grid gap-2">
                <Label htmlFor="location">Location</Label>
                <div className="flex items-center gap-2">
                  <MapPin className="h-4 w-4 text-muted-foreground" />
                  <Input id="location" defaultValue="San Francisco, CA" disabled={!isEditing} />
                </div>
              </div>

              <div className="grid gap-2">
                <Label htmlFor="joined">Joined Date</Label>
                <div className="flex items-center gap-2">
                  <Calendar className="h-4 w-4 text-muted-foreground" />
                  <Input id="joined" value={joinedDate} disabled readOnly />
                </div>
              </div>

              <div className="grid gap-2">
                <Label htmlFor="bio">Bio</Label>
                <Textarea
                  id="bio"
                  placeholder="Tell us about yourself..."
                  defaultValue="Passionate about document management and workflow optimization."
                  disabled={!isEditing}
                  rows={4}
                />
              </div>
            </div>

            {isEditing && (
              <>
                <Separator />
                <div className="flex justify-end gap-2">
                  <Button variant="outline" onClick={handleCancelEdit} disabled={isSaving}>
                    Cancel
                  </Button>
                  <Button onClick={handleSaveChanges} disabled={!hasChanges || isSaving}>
                    {isSaving ? "Saving..." : "Save Changes"}
                  </Button>
                </div>
              </>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
