"use client"

import type React from "react"

import { useState, useEffect, useRef, useMemo } from "react"
import { useRouter } from "next/navigation"
import {
  ArrowLeft,
  Camera,
  Mail,
  Briefcase,
  Calendar,
  Users,
  ChevronDown,
  Check,
} from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Separator } from "@/components/ui/separator"
import { Badge } from "@/components/ui/badge"
import {
  fetchCurrentUserProfile,
  updateCurrentUserProfile,
  updateUserAvatar,
  fetchGroups,
  updateCurrentUserPassword,
} from "@/lib/api"
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

type PasswordFormState = {
  currentPassword: string
  newPassword: string
  confirmPassword: string
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
  const [passwordForm, setPasswordForm] = useState<PasswordFormState>({
    currentPassword: "",
    newPassword: "",
    confirmPassword: "",
  })
  const [isUpdatingPassword, setIsUpdatingPassword] = useState(false)
  const [passwordFeedback, setPasswordFeedback] = useState<{
    type: "success" | "error"
    message: string
  } | null>(null)
  const [isLoadingProfile, setIsLoadingProfile] = useState(() => !cachedSnapshot?.user)
  const [isRedirecting, setIsRedirecting] = useState(false)

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
    let mounted = true

    const loadProfile = async () => {
      console.debug("[profile] Bắt đầu tải hồ sơ người dùng trong trang profile.")

      if (mounted) {
        setIsLoadingProfile(true)
        setFeedback(null)
      }

      try {
        const profile = await fetchCurrentUserProfile()
        if (!mounted) return

        if (!profile) {
          console.warn("[profile] API trả về null, chuyển hướng tới trang đăng nhập.")
          setUser(null)
          setIsRedirecting(true)
          setIsLoadingProfile(false)
          router.replace(SIGN_IN_ROUTE)
          return
        }

        console.debug("[profile] Nhận được hồ sơ người dùng với id:", profile.id)
        setUser(profile)
        setPasswordForm({ currentPassword: "", newPassword: "", confirmPassword: "" })
        setPasswordFeedback(null)
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
      } finally {
        if (mounted) {
          setIsLoadingProfile(false)
        }
      }
    }

    loadProfile()

    return () => {
      mounted = false
    }
  }, [router])

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

  const handlePasswordFieldChange = (field: keyof PasswordFormState) => (
    event: React.ChangeEvent<HTMLInputElement>,
  ) => {
    const value = event.target.value
    setPasswordForm((prev) => ({ ...prev, [field]: value }))
    setPasswordFeedback(null)
  }

  const handleUpdatePassword = async () => {
    if (!user) return

    const requiresCurrentPassword = Boolean(user.hasPassword)
    const trimmedNewPassword = passwordForm.newPassword.trim()
    const trimmedConfirmPassword = passwordForm.confirmPassword.trim()

    if (requiresCurrentPassword && passwordForm.currentPassword.length === 0) {
      setPasswordFeedback({ type: "error", message: "Vui lòng nhập mật khẩu hiện tại." })
      return
    }

    if (trimmedNewPassword.length < 8) {
      setPasswordFeedback({ type: "error", message: "Mật khẩu mới phải có ít nhất 8 ký tự." })
      return
    }

    if (trimmedNewPassword !== trimmedConfirmPassword) {
      setPasswordFeedback({ type: "error", message: "Mật khẩu xác nhận không khớp." })
      return
    }

    setIsUpdatingPassword(true)
    setPasswordFeedback(null)

    try {
      await updateCurrentUserPassword({
        currentPassword: requiresCurrentPassword ? passwordForm.currentPassword : undefined,
        newPassword: trimmedNewPassword,
      })

      setPasswordFeedback({
        type: "success",
        message: requiresCurrentPassword
          ? "Mật khẩu của bạn đã được cập nhật."
          : "Mật khẩu của bạn đã được thiết lập.",
      })
      setPasswordForm({ currentPassword: "", newPassword: "", confirmPassword: "" })
      setUser((prev) => (prev ? { ...prev, hasPassword: true } : prev))
    } catch (error) {
      console.error("[ui] Không thể cập nhật mật khẩu:", error)
      setPasswordFeedback({
        type: "error",
        message: error instanceof Error ? error.message : "Không thể cập nhật mật khẩu. Vui lòng thử lại.",
      })
    } finally {
      setIsUpdatingPassword(false)
    }
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

  const requiresCurrentPassword = Boolean(user?.hasPassword)
  const trimmedNewPassword = passwordForm.newPassword.trim()
  const trimmedConfirmPassword = passwordForm.confirmPassword.trim()
  const canSubmitPassword =
    trimmedNewPassword.length >= 8 &&
    trimmedNewPassword === trimmedConfirmPassword &&
    (!requiresCurrentPassword || passwordForm.currentPassword.length > 0)

  if (isRedirecting) {
    return (
      <div className="flex items-center justify-center h-screen">
        <div className="text-muted-foreground">Đang chuyển hướng tới trang đăng nhập…</div>
      </div>
    )
  }

  if (isLoadingProfile && !user) {
    return (
      <div className="flex items-center justify-center h-screen">
        <div className="text-muted-foreground">Đang tải hồ sơ người dùng…</div>
      </div>
    )
  }

  if (!user) {
    return (
      <div className="flex h-screen flex-col items-center justify-center gap-4 px-4 text-center">
        <div className="text-base text-muted-foreground">
          Không thể tải hồ sơ người dùng. Vui lòng thử lại hoặc quay về trang chủ.
        </div>
        <Button onClick={() => router.replace(APP_HOME_ROUTE)}>Quay về trang quản lý tập tin</Button>
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
        <div className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle>Hồ sơ cá nhân</CardTitle>
            </CardHeader>
            <CardContent className="space-y-6">
            <div className="flex flex-col gap-6 md:flex-row md:items-center">
              <div className="relative">
                <Avatar className="h-24 w-24">
                  <AvatarImage src={avatarPreview || user.avatar || "/placeholder.svg"} alt={user.displayName} />
                  <AvatarFallback className="text-2xl">{user.displayName?.charAt(0) ?? "?"}</AvatarFallback>
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
                  title="Đổi ảnh đại diện"
                  onClick={() => fileInputRef.current?.click()}
                >
                  <Camera className="h-4 w-4" />
                </Button>
              </div>
              <div className="flex-1 space-y-2">
                <h2 className="text-2xl font-bold break-words">{user.displayName}</h2>
                <p className="text-sm text-muted-foreground break-words">{user.email}</p>
                {primaryGroupName ? (
                  <p className="text-xs text-muted-foreground">Đơn vị: {primaryGroupName}</p>
                ) : null}
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
                {isEditing ? "Hủy" : "Chỉnh sửa hồ sơ"}
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
                <Label htmlFor="name">Tên hiển thị</Label>
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
                <Label>Vai trò</Label>
                <div className="flex flex-wrap gap-2">
                  {user.roles.length > 0 ? (
                    user.roles.map((role, index) => (
                      <Badge key={`role-${role}-${index}`} variant="outline">
                        {role}
                      </Badge>
                    ))
                  ) : (
                    <span className="text-sm text-muted-foreground">Chưa được gán vai trò nào.</span>
                  )}
                </div>
              </div>

              <div className="grid gap-2">
                <Label>Trạng thái tài khoản</Label>
                <div className="flex flex-wrap items-center gap-2 text-sm">
                  <Badge variant={user.isActive ? "secondary" : "outline"}>
                    {user.isActive ? "Đang hoạt động" : "Đã vô hiệu hóa"}
                  </Badge>
                  <span className="text-muted-foreground">
                    {user.isActive
                      ? ""
                      : "Liên hệ quản trị viên để kích hoạt lại tài khoản."}
                  </span>
                </div>
              </div>

              <div className="grid gap-2">
                <Label htmlFor="primary-group">Đơn vị</Label>
                <div className="flex items-center gap-2">
                  <Briefcase className="h-4 w-4 text-muted-foreground" />
                  <Select
                    value={formValues.primaryGroupId ?? undefined}
                    onValueChange={(value) => handlePrimaryGroupChange(value === "__none__" ? null : value)}
                    disabled={!isEditing || groups.length === 0}
                  >
                    <SelectTrigger id="primary-group" className="w-full">
                      <SelectValue
                        placeholder={groups.length ? "Chọn đơn vị" : "Không có đơn vị nào"}
                      />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="__none__">Không có đơn vị</SelectItem>
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
                    ? "Bạn chưa được gán vào bất kỳ đơn vị nào."
                    : primaryGroupName
                      ? `Đơn vị hiện tại: ${primaryGroupName}`
                      : "Chọn một đơn vị làm đơn vị mặc định."}
                </p>
              </div>

              <div className="grid gap-2">
                <Label>Nhóm tham gia</Label>
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
                          ? `${selectedGroupCount} nhóm được chọn`
                          : "Chọn nhóm"}
                        <ChevronDown className="ml-2 h-4 w-4 opacity-50" />
                      </Button>
                    </PopoverTrigger>
                    <PopoverContent className="p-0 w-[280px]" align="start">
                      <Command>
                        <CommandInput placeholder="Tìm kiếm nhóm..." />
                        <CommandEmpty>Không tìm thấy nhóm.</CommandEmpty>
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
                      ? `Đã chọn ${selectedGroupCount} nhóm`
                      : "Chưa gán nhóm nào cho tài khoản này."}
                </p>
              </div>

              <div className="grid gap-2">
                <Label htmlFor="joined">Ngày tham gia</Label>
                <div className="flex items-center gap-2">
                  <Calendar className="h-4 w-4 text-muted-foreground" />
                  <Input id="joined" value={joinedDate} disabled readOnly />
                </div>
              </div>
            </div>

            {isEditing && (
              <>
                <Separator />
                <div className="flex justify-end gap-2">
                  <Button variant="outline" onClick={handleCancelEdit} disabled={isSaving}>
                    Hủy
                  </Button>
                  <Button onClick={handleSaveChanges} disabled={!hasChanges || isSaving}>
                    {isSaving ? "Đang lưu..." : "Lưu thay đổi"}
                  </Button>
                </div>
              </>
            )}
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Bảo mật tài khoản</CardTitle>
              <CardDescription>Thiết lập hoặc đổi mật khẩu đăng nhập của bạn.</CardDescription>
            </CardHeader>
            <CardContent className="space-y-6">
              <div className="flex flex-wrap items-center gap-2 text-sm">
                <Badge variant={requiresCurrentPassword ? "secondary" : "outline"}>
                  {requiresCurrentPassword ? "Đã thiết lập mật khẩu" : "Chưa có mật khẩu"}
                </Badge>
                <span className="text-muted-foreground">
                  {requiresCurrentPassword
                    ? "Bạn có thể đổi mật khẩu định kỳ để tăng bảo mật."
                    : "Đặt mật khẩu để đăng nhập trực tiếp vào hệ thống."}
                </span>
              </div>

              {passwordFeedback && (
                <div
                  className={
                    passwordFeedback.type === "error"
                      ? "text-sm text-destructive"
                      : "text-sm text-green-600"
                  }
                >
                  {passwordFeedback.message}
                </div>
              )}

              <div className="grid gap-4 md:grid-cols-2">
                {requiresCurrentPassword && (
                  <div className="grid gap-2 md:col-span-2">
                    <Label htmlFor="current-password">Mật khẩu hiện tại</Label>
                    <Input
                      id="current-password"
                      type="password"
                      value={passwordForm.currentPassword}
                      onChange={handlePasswordFieldChange("currentPassword")}
                      disabled={isUpdatingPassword}
                      autoComplete="current-password"
                    />
                  </div>
                )}
                <div className="grid gap-2">
                  <Label htmlFor="new-password">Mật khẩu mới</Label>
                  <Input
                    id="new-password"
                    type="password"
                    value={passwordForm.newPassword}
                    onChange={handlePasswordFieldChange("newPassword")}
                    disabled={isUpdatingPassword}
                    autoComplete="new-password"
                  />
                  <p
                    className={cn(
                      "text-xs",
                      trimmedNewPassword.length > 0 && trimmedNewPassword.length < 8
                        ? "text-destructive"
                        : "text-muted-foreground",
                    )}
                  >
                    Mật khẩu cần tối thiểu 8 ký tự.
                  </p>
                </div>
                <div className="grid gap-2">
                  <Label htmlFor="confirm-password">Xác nhận mật khẩu mới</Label>
                  <Input
                    id="confirm-password"
                    type="password"
                    value={passwordForm.confirmPassword}
                    onChange={handlePasswordFieldChange("confirmPassword")}
                    disabled={isUpdatingPassword}
                    autoComplete="new-password"
                  />
                  <p
                    className={cn(
                      "text-xs",
                      trimmedConfirmPassword.length > 0 && trimmedConfirmPassword !== trimmedNewPassword
                        ? "text-destructive"
                        : "text-muted-foreground",
                    )}
                  >
                    Nhập lại mật khẩu mới để xác nhận.
                  </p>
                </div>
              </div>

              <div className="flex justify-end">
                <Button onClick={handleUpdatePassword} disabled={!canSubmitPassword || isUpdatingPassword}>
                  {isUpdatingPassword
                    ? "Đang cập nhật..."
                    : requiresCurrentPassword
                      ? "Đổi mật khẩu"
                      : "Thiết lập mật khẩu"}
                </Button>
              </div>
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  )
}
