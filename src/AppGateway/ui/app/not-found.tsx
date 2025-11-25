import Link from "next/link"

export default function NotFound() {
  return (
    <div className="flex min-h-screen flex-col items-center justify-center gap-4 bg-background px-6 text-center text-foreground">
      <div className="text-3xl font-semibold">Trang bạn tìm không tồn tại</div>
      <p className="max-w-xl text-muted-foreground">
        Đường dẫn này có thể đã bị thay đổi hoặc không còn hoạt động. Vui lòng kiểm tra lại
        URL hoặc quay về trang chính.
      </p>
      <div className="flex flex-wrap items-center justify-center gap-3">
        <Link
          href="/"
          className="rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground shadow-sm transition hover:bg-primary/90"
        >
          Về trang chủ
        </Link>
        <Link
          href="/app/"
          className="rounded-md border border-border px-4 py-2 text-sm font-medium text-foreground transition hover:bg-muted"
        >
          Đi tới ứng dụng
        </Link>
      </div>
    </div>
  )
}
