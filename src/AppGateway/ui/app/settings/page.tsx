"use client"

import type React from "react"
import { useEffect, useMemo, useState } from "react"
import {
  ArrowLeft,
  Bell,
  Lock,
  Palette,
  Globe,
  Shield,
  Database,
  User as UserIcon,
  ChevronDown,
  Check,
} from "lucide-react"
import Link from "next/link"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Switch } from "@/components/ui/switch"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Separator } from "@/components/ui/separator"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { fetchCurrentUserProfile, updateCurrentUserProfile, fetchGroups } from "@/lib/api"
import type { Group, User } from "@/lib/types"
import { useTheme } from "@/hooks/use-theme"
import type { ThemeId } from "@/hooks/use-theme"
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover"
import {
  Command,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
} from "@/components/ui/command"
import { cn, createSignInRedirectPath } from "@/lib/utils"

const APP_HOME_ROUTE = "/app/"
const SETTINGS_ROUTE = "/app/settings/"

type ProfileFormState = {
  displayName: string
  primaryGroupId: string | null
  groupIds: string[]
}

export default function SettingsPage() {
  const { theme, setTheme, themes } = useTheme()
  const [emailNotifications, setEmailNotifications] = useState(true)
  const [pushNotifications, setPushNotifications] = useState(true)
  const [twoFactorAuth, setTwoFactorAuth] = useState(false)
  const [profile, setProfile] = useState<User | null>(null)
  const [formValues, setFormValues] = useState<ProfileFormState>({
    displayName: "",
    primaryGroupId: null,
    groupIds: [],
  })
  const [groups, setGroups] = useState<Group[]>([])
  const [isGroupPickerOpen, setGroupPickerOpen] = useState(false)
  const [isSaving, setIsSaving] = useState(false)
  const [feedback, setFeedback] = useState<{ type: "success" | "error"; message: string } | null>(null)

  useEffect(() => {
    let mounted = true

    const redirectToSignIn = () => {
      const target =
        typeof window === "undefined"
          ? SETTINGS_ROUTE
          : `${window.location.pathname}${window.location.search}${window.location.hash}`
      const signInPath = createSignInRedirectPath(target, SETTINGS_ROUTE)
      window.location.href = signInPath
    }

    fetchCurrentUserProfile()
      .then((data) => {
        if (!mounted) return

        if (!data) {
          redirectToSignIn()
          return
        }

        setProfile(data)
        const normalizedGroups = Array.from(new Set(data.groupIds ?? []))
        const primaryGroupId = data.primaryGroupId ?? normalizedGroups[0] ?? null
        if (primaryGroupId && !normalizedGroups.includes(primaryGroupId)) {
          normalizedGroups.unshift(primaryGroupId)
        }
        setFormValues({
          displayName: data.displayName,
          primaryGroupId,
          groupIds: normalizedGroups,
        })
      })
      .catch(() => {
        if (!mounted) return
        redirectToSignIn()
      })

    return () => {
      mounted = false
    }
  }, [])

  useEffect(() => {
    let active = true

    fetchGroups()
      .then((data) => {
        if (!active) return
        setGroups(data)
      })
      .catch((error) => {
        console.error("[ui] Failed to load groups for settings page:", error)
        if (!active) return
        setGroups([])
      })

    return () => {
      active = false
    }
  }, [])

  const handleDisplayNameChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    const value = event.target.value
    setFormValues((prev) => ({ ...prev, displayName: value }))
  }

  const handleReset = () => {
    if (!profile) return
    const normalizedGroups = Array.from(new Set(profile.groupIds ?? []))
    const primaryGroupId = profile.primaryGroupId ?? normalizedGroups[0] ?? null
    if (primaryGroupId && !normalizedGroups.includes(primaryGroupId)) {
      normalizedGroups.unshift(primaryGroupId)
    }
    setFormValues({
      displayName: profile.displayName,
      primaryGroupId,
      groupIds: normalizedGroups,
    })
    setFeedback(null)
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

  const handleSave = async () => {
    if (!profile) return

    setIsSaving(true)
    setFeedback(null)

    try {
      const updated = await updateCurrentUserProfile({
        displayName: formValues.displayName,
        primaryGroupId: formValues.primaryGroupId,
        groupIds: formValues.groupIds,
      })

      setProfile(updated)
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
      setFeedback({ type: "success", message: "Đã lưu thay đổi hồ sơ." })
    } catch (error) {
      console.error("[ui] Không thể cập nhật hồ sơ trong trang cài đặt:", error)
      setFeedback({ type: "error", message: "Không thể lưu thay đổi. Vui lòng thử lại." })
    } finally {
      setIsSaving(false)
    }
  }

  const hasChanges = useMemo(() => {
    if (!profile) return false
    const normalizedName = formValues.displayName.trim()
    const currentName = profile.displayName.trim()
    const normalizedPrimary = formValues.primaryGroupId ?? null
    const currentPrimary = profile.primaryGroupId ?? null

    const normalizeGroupList = (list?: string[]) => Array.from(new Set(list ?? [])).sort()
    const formGroupList = normalizeGroupList(formValues.groupIds)
    const profileGroupList = normalizeGroupList(profile.groupIds)

    const groupsChanged =
      formGroupList.length !== profileGroupList.length ||
      formGroupList.some((id, index) => id !== profileGroupList[index])

    return normalizedName !== currentName || normalizedPrimary !== currentPrimary || groupsChanged
  }, [formValues.displayName, formValues.groupIds, formValues.primaryGroupId, profile])

  const joinedDate = useMemo(() => {
    if (!profile?.createdAtUtc) {
      return "—"
    }

    try {
      const date = new Date(profile.createdAtUtc)
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
  }, [profile?.createdAtUtc])

  if (!profile) {
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
        <div className="flex items-center justify-center py-24">
          <div className="text-muted-foreground">Loading settings…</div>
        </div>
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
        <div className="mb-6 flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <h1 className="text-3xl font-bold">Settings</h1>
            <p className="text-muted-foreground">Manage your account settings and preferences</p>
          </div>
          <Button asChild variant="outline">
            <Link href="/hello">Open hello word page</Link>
          </Button>
        </div>

        <Tabs defaultValue="general" className="space-y-6">
          <TabsList className="grid w-full grid-cols-4">
            <TabsTrigger value="general">General</TabsTrigger>
            <TabsTrigger value="notifications">Notifications</TabsTrigger>
            <TabsTrigger value="security">Security</TabsTrigger>
            <TabsTrigger value="storage">Storage</TabsTrigger>
          </TabsList>

          <TabsContent value="general" className="space-y-4">
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <UserIcon className="h-5 w-5" />
                  Profile Information
                </CardTitle>
                <CardDescription>Update your basic profile details</CardDescription>
              </CardHeader>
              <CardContent className="space-y-4">
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
                <div className="grid gap-2">
                  <Label htmlFor="settings-name">Full Name</Label>
                  <Input
                    id="settings-name"
                    value={formValues.displayName}
                    onChange={handleDisplayNameChange}
                    placeholder="Your full name"
                  />
                </div>
                <div className="grid gap-2">
                  <Label htmlFor="settings-email">Email</Label>
                  <Input id="settings-email" type="email" value={profile.email} readOnly disabled />
                </div>
                <div className="grid gap-2">
                  <Label htmlFor="settings-primary-group">Primary group</Label>
                  <Select
                    value={formValues.primaryGroupId ?? undefined}
                    onValueChange={(value) => handlePrimaryGroupChange(value === "__none__" ? null : value)}
                    disabled={groups.length === 0}
                  >
                    <SelectTrigger id="settings-primary-group">
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
                  <Popover open={isGroupPickerOpen} onOpenChange={setGroupPickerOpen}>
                    <PopoverTrigger asChild>
                      <Button
                        type="button"
                        variant="outline"
                        role="combobox"
                        className={cn(
                          "w-full justify-between",
                          selectedGroupCount === 0 && "text-muted-foreground",
                        )}
                        disabled={groups.length === 0}
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
                  <p className="text-xs text-muted-foreground">
                    {selectedGroupNames.length > 0
                      ? selectedGroupNames.join(", ")
                      : selectedGroupCount > 0
                        ? `${selectedGroupCount} group${selectedGroupCount > 1 ? "s" : ""} selected`
                        : "Assign groups to organize your documents"}
                  </p>
                </div>
                <div className="grid gap-2">
                  <Label htmlFor="settings-joined">Joined</Label>
                  <Input id="settings-joined" value={joinedDate} readOnly disabled />
                </div>
                <div className="flex justify-end gap-2">
                  <Button variant="outline" onClick={handleReset} disabled={!hasChanges || isSaving}>
                    Reset
                  </Button>
                  <Button onClick={handleSave} disabled={!hasChanges || isSaving}>
                    {isSaving ? "Saving..." : "Save Changes"}
                  </Button>
                </div>
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <Palette className="h-5 w-5" />
                  Appearance
                </CardTitle>
                <CardDescription>Customize how the application looks</CardDescription>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="flex items-center justify-between">
                  <div className="space-y-0.5">
                    <Label>Theme</Label>
                    <p className="text-sm text-muted-foreground">Select your preferred theme</p>
                  </div>
                  <Select value={theme} onValueChange={(value) => setTheme(value as ThemeId)}>
                    <SelectTrigger className="w-40">
                      <SelectValue placeholder="Chọn theme" />
                    </SelectTrigger>
                    <SelectContent>
                      {themes.map((option) => (
                        <SelectItem key={option.id} value={option.id} title={option.description}>
                          {option.label}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <Globe className="h-5 w-5" />
                  Language & Region
                </CardTitle>
                <CardDescription>Set your language and regional preferences</CardDescription>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="grid gap-2">
                  <Label htmlFor="language">Language</Label>
                  <Select defaultValue="en">
                    <SelectTrigger id="language">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="en">English</SelectItem>
                      <SelectItem value="vi">Tiếng Việt</SelectItem>
                      <SelectItem value="es">Español</SelectItem>
                      <SelectItem value="fr">Français</SelectItem>
                    </SelectContent>
                  </Select>
                </div>
                <div className="grid gap-2">
                  <Label htmlFor="timezone">Timezone</Label>
                  <Select defaultValue="utc-8">
                    <SelectTrigger id="timezone">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="utc-8">Pacific Time (UTC-8)</SelectItem>
                      <SelectItem value="utc-5">Eastern Time (UTC-5)</SelectItem>
                      <SelectItem value="utc+7">Vietnam Time (UTC+7)</SelectItem>
                    </SelectContent>
                  </Select>
                </div>
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="notifications" className="space-y-4">
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <Bell className="h-5 w-5" />
                  Notification Preferences
                </CardTitle>
                <CardDescription>Choose how you want to be notified</CardDescription>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="flex items-center justify-between">
                  <div className="space-y-0.5">
                    <Label>Email Notifications</Label>
                    <p className="text-sm text-muted-foreground">Receive notifications via email</p>
                  </div>
                  <Switch checked={emailNotifications} onCheckedChange={setEmailNotifications} />
                </div>
                <Separator />
                <div className="flex items-center justify-between">
                  <div className="space-y-0.5">
                    <Label>Push Notifications</Label>
                    <p className="text-sm text-muted-foreground">Receive push notifications in browser</p>
                  </div>
                  <Switch checked={pushNotifications} onCheckedChange={setPushNotifications} />
                </div>
                <Separator />
                <div className="flex items-center justify-between">
                  <div className="space-y-0.5">
                    <Label>File Upload Notifications</Label>
                    <p className="text-sm text-muted-foreground">Get notified when files are uploaded</p>
                  </div>
                  <Switch defaultChecked />
                </div>
                <Separator />
                <div className="flex items-center justify-between">
                  <div className="space-y-0.5">
                    <Label>Workflow Updates</Label>
                    <p className="text-sm text-muted-foreground">Get notified about workflow changes</p>
                  </div>
                  <Switch defaultChecked />
                </div>
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="security" className="space-y-4">
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <Lock className="h-5 w-5" />
                  Password
                </CardTitle>
                <CardDescription>Change your password</CardDescription>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="grid gap-2">
                  <Label htmlFor="current-password">Current Password</Label>
                  <Input id="current-password" type="password" />
                </div>
                <div className="grid gap-2">
                  <Label htmlFor="new-password">New Password</Label>
                  <Input id="new-password" type="password" />
                </div>
                <div className="grid gap-2">
                  <Label htmlFor="confirm-password">Confirm New Password</Label>
                  <Input id="confirm-password" type="password" />
                </div>
                <Button>Update Password</Button>
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <Shield className="h-5 w-5" />
                  Two-Factor Authentication
                </CardTitle>
                <CardDescription>Add an extra layer of security to your account</CardDescription>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="flex items-center justify-between">
                  <div className="space-y-0.5">
                    <Label>Enable 2FA</Label>
                    <p className="text-sm text-muted-foreground">Require a code in addition to your password</p>
                  </div>
                  <Switch checked={twoFactorAuth} onCheckedChange={setTwoFactorAuth} />
                </div>
                {twoFactorAuth && (
                  <div className="pt-4">
                    <Button variant="outline">Configure 2FA</Button>
                  </div>
                )}
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="storage" className="space-y-4">
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <Database className="h-5 w-5" />
                  Storage Usage
                </CardTitle>
                <CardDescription>Manage your storage and file retention</CardDescription>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="space-y-2">
                  <div className="flex justify-between text-sm">
                    <span>Used Storage</span>
                    <span className="font-medium">45.2 GB of 100 GB</span>
                  </div>
                  <div className="h-2 bg-secondary rounded-full overflow-hidden">
                    <div className="h-full bg-primary" style={{ width: "45%" }} />
                  </div>
                </div>
                <Separator />
                <div className="space-y-2">
                  <Label>File Retention Policy</Label>
                  <Select defaultValue="90">
                    <SelectTrigger>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="30">30 days</SelectItem>
                      <SelectItem value="90">90 days</SelectItem>
                      <SelectItem value="180">180 days</SelectItem>
                      <SelectItem value="365">1 year</SelectItem>
                      <SelectItem value="forever">Forever</SelectItem>
                    </SelectContent>
                  </Select>
                  <p className="text-sm text-muted-foreground">Files will be automatically deleted after this period</p>
                </div>
                <Separator />
                <Button variant="outline">Clear Cache</Button>
              </CardContent>
            </Card>
          </TabsContent>
        </Tabs>
      </div>
    </div>
  )
}
