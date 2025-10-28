"use client"

import type React from "react"

import { useState, useEffect, useRef, useMemo } from "react"
import { useRouter } from "next/navigation"
import { ArrowLeft, Camera, Mail, Briefcase, MapPin, Phone, Calendar } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Separator } from "@/components/ui/separator"
import { useAuthGuard } from "@/hooks/use-auth-guard"
import { fetchCurrentUserProfile, updateCurrentUserProfile, updateUserAvatar } from "@/lib/api"
import { getCachedAuthSnapshot } from "@/lib/auth-state"
import type { User } from "@/lib/types"

const APP_HOME_ROUTE = "/app/"
const PROFILE_ROUTE = "/profile/"
const SIGN_IN_ROUTE = `/signin/?redirectUri=${encodeURIComponent(PROFILE_ROUTE)}`

export default function ProfilePage() {
  const router = useRouter()
  const cachedSnapshot = useMemo(() => getCachedAuthSnapshot(), [])
  const [user, setUser] = useState<User | null>(() => cachedSnapshot?.user ?? null)
  const [isEditing, setIsEditing] = useState(false)
  const [avatarPreview, setAvatarPreview] = useState<string | null>(null)
  const fileInputRef = useRef<HTMLInputElement>(null)
  const [formValues, setFormValues] = useState({
    displayName: cachedSnapshot?.user?.displayName ?? "",
    department: cachedSnapshot?.user?.department ?? "",
  })
  const [isSaving, setIsSaving] = useState(false)
  const [feedback, setFeedback] = useState<{ type: "success" | "error"; message: string } | null>(null)
  const { isAuthenticated, isChecking } = useAuthGuard(PROFILE_ROUTE)

  useEffect(() => {
    if (!isAuthenticated || isChecking) {
      return
    }

    let mounted = true

    const loadProfile = async () => {
      try {
        const profile = await fetchCurrentUserProfile()
        if (!mounted) return

        if (!profile) {
          router.replace(SIGN_IN_ROUTE)
          return
        }

        setUser(profile)
        setFormValues({
          displayName: profile.displayName,
          department: profile.department ?? "",
        })
      } catch (error) {
        console.error("[ui] Không thể tải hồ sơ người dùng:", error)
        if (!mounted) return

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

  const handleInputChange = (field: "displayName" | "department") =>
    (event: React.ChangeEvent<HTMLInputElement>) => {
      const value = event.target.value
      setFormValues((prev) => ({ ...prev, [field]: value }))
    }

  const handleCancelEdit = () => {
    if (user) {
      setFormValues({
        displayName: user.displayName,
        department: user.department ?? "",
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
        department: formValues.department,
      })

      setUser(updated)
      setFormValues({
        displayName: updated.displayName,
        department: updated.department ?? "",
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
    const normalizedDepartment = formValues.department.trim()
    const currentDepartment = (user.department ?? "").trim()

    return (
      normalizedName !== currentName ||
      normalizedDepartment !== currentDepartment
    )
  }, [formValues.displayName, formValues.department, user])

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
                  onChange={handleInputChange("displayName")}
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
                <Label htmlFor="department">Department</Label>
                <div className="flex items-center gap-2">
                  <Briefcase className="h-4 w-4 text-muted-foreground" />
                  <Input
                    id="department"
                    value={formValues.department}
                    onChange={handleInputChange("department")}
                    disabled={!isEditing}
                  />
                </div>
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
