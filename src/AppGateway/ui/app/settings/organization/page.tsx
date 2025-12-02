"use client"

import { useEffect, useMemo, useState } from "react"
import Link from "next/link"
import {
  Building2,
  FolderCog,
  LayoutGrid,
  Tags,
  UserCheck,
} from "lucide-react"

import { Button } from "@/components/ui/button"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Separator } from "@/components/ui/separator"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { Badge } from "@/components/ui/badge"
import { ScrollArea } from "@/components/ui/scroll-area"
import { useAuthGuard } from "@/hooks/use-auth-guard"
import { fetchCurrentUserProfile, fetchTags } from "@/lib/api"
import { getCachedAuthSnapshot } from "@/lib/auth-state"
import type { TagNode, User } from "@/lib/types"
import { cn } from "@/lib/utils"

const ORG_SETTINGS_ROUTE = "/app/settings/organization/"

const adminActionItems = [
  {
    title: "Vai trò & phân quyền",
    description: "Thiết lập vai trò chuẩn, phân nhóm quyền chi tiết và đảm bảo người dùng đúng phạm vi truy cập.",
  },
  {
    title: "Chính sách bảo mật",
    description: "Bật kiểm duyệt tài liệu, yêu cầu MFA và cấu hình nhật ký hoạt động để theo dõi thay đổi.",
  },
  {
    title: "Quy tắc tuân thủ",
    description: "Đồng bộ yêu cầu tuân thủ nội bộ (ISO/TCVN) cho toàn bộ tổ chức từ một nơi duy nhất.",
  },
]

const departmentPlaybooks = [
  {
    title: "Sơ đồ đơn vị",
    description: "Tổ chức cây phòng ban, nhóm dự án và đơn vị trực thuộc để áp dụng quyền truy cập theo cấu trúc.",
  },
  {
    title: "Luồng phê duyệt",
    description: "Định nghĩa quy trình phê duyệt tài liệu theo phòng ban để giảm phụ thuộc cấu hình thủ công.",
  },
  {
    title: "Đồng bộ nhân sự",
    description: "Tích hợp với HR hoặc danh bạ nội bộ để tự động cập nhật thành viên và vai trò.",
  },
]

const documentTypePolicies = [
  {
    title: "Danh mục loại tài liệu",
    description: "Chuẩn hóa danh sách loại hồ sơ, tài liệu nghiệp vụ và biểu mẫu sử dụng trong toàn hệ thống.",
  },
  {
    title: "Mẫu metadata",
    description: "Định nghĩa trường bắt buộc, nhãn, và validation cho từng loại tài liệu để tránh nhập thiếu.",
  },
  {
    title: "Vòng đời & lưu trữ",
    description: "Thiết lập thời gian lưu trữ, nhắc gia hạn và quy tắc hủy cho từng loại tài liệu quan trọng.",
  },
]

function isAdminUser(user: User | null): boolean {
  if (!user?.roles?.length) return false
  return user.roles.some((role) => role.toLowerCase().includes("admin"))
}

function TagTree({ tags }: { tags: TagNode[] }) {
  if (!tags.length) {
    return <p className="text-sm text-muted-foreground">Chưa có tag hoặc namespace nào được cấu hình.</p>
  }

  const renderNode = (node: TagNode, level = 0) => {
    const isNamespace = node.kind === "namespace"
    const paddingLeft = `${level * 16}px`
    return (
      <div key={node.id} className={cn("rounded-md border p-3", isNamespace ? "bg-muted/60" : "bg-background/80")}>
        <div className="flex items-start justify-between gap-3" style={{ paddingLeft }}>
          <div className="space-y-1">
            <div className="flex items-center gap-2 text-sm font-semibold">
              {isNamespace ? <Tags className="h-4 w-4" /> : <LayoutGrid className="h-4 w-4" />}
              <span>{node.name}</span>
              {isNamespace && node.namespaceLabel ? (
                <Badge variant="secondary" className="text-xs">
                  Namespace
                </Badge>
              ) : null}
            </div>
            <p className="text-xs text-muted-foreground">
              {isNamespace
                ? `Phạm vi: ${node.namespaceScope ?? "user"}`
                : node.namespaceLabel || "Nằm trong namespace mặc định"}
            </p>
          </div>
          {node.color ? (
            <span
              className="h-4 w-4 rounded-full border"
              style={{ backgroundColor: node.color, borderColor: node.color }}
              aria-label="Tag color"
            />
          ) : null}
        </div>
        {node.children?.length ? (
          <div className="mt-3 space-y-2">
            {node.children.map((child) => (
              <div key={child.id} className="space-y-2">
                {renderNode(child, level + 1)}
              </div>
            ))}
          </div>
        ) : null}
      </div>
    )
  }

  return <div className="space-y-2">{tags.map((node) => renderNode(node))}</div>
}

export default function OrganizationSettingsPage() {
  const { isAuthenticated, isChecking } = useAuthGuard(ORG_SETTINGS_ROUTE)
  const [user, setUser] = useState<User | null>(() => getCachedAuthSnapshot()?.user ?? null)
  const [isAuthorizing, setIsAuthorizing] = useState(true)
  const [authorizationError, setAuthorizationError] = useState<string | null>(null)
  const [tags, setTags] = useState<TagNode[]>([])
  const [isLoadingTags, setIsLoadingTags] = useState(false)

  const isAdmin = useMemo(() => isAdminUser(user), [user])

  useEffect(() => {
    let active = true

    const loadUser = async () => {
      if (!isAuthenticated) {
        setIsAuthorizing(false)
        return
      }

      try {
        setAuthorizationError(null)
        const profile = await fetchCurrentUserProfile()
        if (!active) return
        setUser(profile)
      } catch (error) {
        console.error("[org-settings] Unable to load profile:", error)
        if (!active) return
        setAuthorizationError("Không thể tải thông tin người dùng. Vui lòng thử lại.")
      } finally {
        if (active) {
          setIsAuthorizing(false)
        }
      }
    }

    loadUser()

    return () => {
      active = false
    }
  }, [isAuthenticated])

  useEffect(() => {
    let active = true
    if (!isAuthenticated || !isAdmin) {
      return undefined
    }

    const loadTags = async () => {
      try {
        setIsLoadingTags(true)
        const data = await fetchTags()
        if (!active) return
        setTags(data)
      } catch (error) {
        console.error("[org-settings] Unable to load tags:", error)
        if (active) {
          setTags([])
        }
      } finally {
        if (active) {
          setIsLoadingTags(false)
        }
      }
    }

    loadTags()

    return () => {
      active = false
    }
  }, [isAuthenticated, isAdmin])

  if (isChecking || isAuthorizing) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-muted/40 text-muted-foreground">
        <div className="space-y-3 text-center">
          <p className="text-lg font-semibold">Đang kiểm tra quyền truy cập…</p>
          <p className="text-sm">Vui lòng chờ trong giây lát.</p>
        </div>
      </div>
    )
  }

  if (!isAuthenticated) {
    return null
  }

  if (!isAdmin) {
    return (
      <div className="mx-auto flex min-h-screen max-w-3xl flex-col justify-center gap-6 px-6 py-10 text-center">
        <Card>
          <CardHeader>
            <CardTitle>Bạn không có quyền truy cập</CardTitle>
            <CardDescription>
              Trang Organization Settings chỉ dành cho tài khoản quản trị. Vui lòng liên hệ quản trị viên để được cấp quyền.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <div className="flex flex-wrap items-center justify-center gap-3">
              <Button asChild variant="outline">
                <Link href="/app/">Quay lại trang chính</Link>
              </Button>
              <Button asChild>
                <Link href="/settings">Cập nhật hồ sơ cá nhân</Link>
              </Button>
            </div>
          </CardContent>
        </Card>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-background">
      <div className="mx-auto max-w-6xl px-6 py-10">
        <div className="flex items-center justify-between gap-4">
          <div className="space-y-2">
            <p className="text-sm uppercase tracking-[0.14em] text-muted-foreground">Quản lý tổ chức</p>
            <h1 className="text-3xl font-bold">Organization Settings</h1>
            <p className="text-sm text-muted-foreground">
              Cấu hình phạm vi toàn tổ chức: người dùng, phòng ban, tag/namespace và loại tài liệu.
            </p>
          </div>
          <div className="flex flex-wrap gap-2">
            <Button asChild variant="outline">
              <Link href="/app/">Quay lại app</Link>
            </Button>
            <Button asChild>
              <Link href="/settings">Cá nhân hóa tài khoản</Link>
            </Button>
          </div>
        </div>

        {authorizationError ? (
          <Card className="mt-6 border-destructive/40 bg-destructive/5">
            <CardHeader>
              <CardTitle className="text-destructive">Không thể tải thông tin</CardTitle>
              <CardDescription className="text-destructive">
                {authorizationError}
              </CardDescription>
            </CardHeader>
          </Card>
        ) : null}

        <Separator className="my-8" />

        <Tabs defaultValue="users" className="space-y-6">
          <TabsList className="grid w-full grid-cols-4">
            <TabsTrigger value="users" className="text-sm">
              <UserCheck className="mr-2 h-4 w-4" /> Người dùng
            </TabsTrigger>
            <TabsTrigger value="departments" className="text-sm">
              <Building2 className="mr-2 h-4 w-4" /> Phòng ban
            </TabsTrigger>
            <TabsTrigger value="tags" className="text-sm">
              <Tags className="mr-2 h-4 w-4" /> Tag & Namespace
            </TabsTrigger>
            <TabsTrigger value="doc-types" className="text-sm">
              <FolderCog className="mr-2 h-4 w-4" /> Loại tài liệu
            </TabsTrigger>
          </TabsList>

          <TabsContent value="users" className="space-y-4">
            <Card>
              <CardHeader>
                <CardTitle>Quản trị người dùng</CardTitle>
                <CardDescription>
                  Kiểm soát tài khoản, vai trò và bảo mật đăng nhập cho toàn bộ tổ chức.
                </CardDescription>
              </CardHeader>
              <CardContent className="grid gap-4 md:grid-cols-3">
                {adminActionItems.map((item) => (
                  <div key={item.title} className="rounded-lg border bg-muted/30 p-4">
                    <h3 className="font-semibold">{item.title}</h3>
                    <p className="mt-2 text-sm text-muted-foreground">{item.description}</p>
                  </div>
                ))}
              </CardContent>
            </Card>
            <Card>
              <CardHeader>
                <CardTitle>Trạng thái phiên đăng nhập</CardTitle>
                <CardDescription>
                  Xem nhanh thông tin tài khoản đang dùng để đảm bảo thao tác đúng phân quyền.
                </CardDescription>
              </CardHeader>
              <CardContent className="flex flex-wrap items-center gap-3 text-sm">
                <Badge variant="secondary">User: {user?.displayName ?? "--"}</Badge>
                <Badge variant="secondary">Email: {user?.email ?? "--"}</Badge>
                <Badge variant="outline">Roles: {user?.roles?.join(", ") || "Chưa có"}</Badge>
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="departments" className="space-y-4">
            <Card>
              <CardHeader>
                <CardTitle>Quản trị phòng ban</CardTitle>
                <CardDescription>
                  Thiết kế cấu trúc tổ chức và gán quyền kế thừa theo từng đơn vị.
                </CardDescription>
              </CardHeader>
              <CardContent className="grid gap-4 md:grid-cols-3">
                {departmentPlaybooks.map((item) => (
                  <div key={item.title} className="rounded-lg border bg-muted/30 p-4">
                    <h3 className="font-semibold">{item.title}</h3>
                    <p className="mt-2 text-sm text-muted-foreground">{item.description}</p>
                  </div>
                ))}
              </CardContent>
            </Card>
            <Card>
              <CardHeader>
                <CardTitle>Gợi ý triển khai</CardTitle>
                <CardDescription>
                  Bắt đầu bằng các phòng ban cấp 1, sau đó thêm nhóm dự án/đơn vị trực thuộc để áp dụng quyền nhanh hơn.
                </CardDescription>
              </CardHeader>
            </Card>
          </TabsContent>

          <TabsContent value="tags" className="space-y-4">
            <Card>
              <CardHeader>
                <CardTitle>Quản trị tag & namespace</CardTitle>
                <CardDescription>
                  Xem toàn bộ cây tag và namespace (không giới hạn như thanh bên phải) để chuẩn hóa phân loại tài liệu.
                </CardDescription>
              </CardHeader>
              <CardContent className="space-y-4">
                {isLoadingTags ? (
                  <p className="text-sm text-muted-foreground">Đang tải cây tag…</p>
                ) : (
                  <ScrollArea className="h-[520px] rounded-md border p-4">
                    <TagTree tags={tags} />
                  </ScrollArea>
                )}
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="doc-types" className="space-y-4">
            <Card>
              <CardHeader>
                <CardTitle>Quản trị loại tài liệu</CardTitle>
                <CardDescription>
                  Chuẩn hóa loại tài liệu, metadata và vòng đời lưu trữ cho mọi bộ phận.
                </CardDescription>
              </CardHeader>
              <CardContent className="grid gap-4 md:grid-cols-3">
                {documentTypePolicies.map((item) => (
                  <div key={item.title} className="rounded-lg border bg-muted/30 p-4">
                    <h3 className="font-semibold">{item.title}</h3>
                    <p className="mt-2 text-sm text-muted-foreground">{item.description}</p>
                  </div>
                ))}
              </CardContent>
            </Card>
            <Card>
              <CardHeader>
                <CardTitle>Checklist triển khai</CardTitle>
                <CardDescription>
                  Xác định loại tài liệu ưu tiên, thêm metadata bắt buộc và gắn tag/namespace mặc định trước khi mở rộng.
                </CardDescription>
              </CardHeader>
            </Card>
          </TabsContent>
        </Tabs>
      </div>
    </div>
  )
}
