import type { FileDetail, FileDocumentPreviewPage } from "@/lib/types"
import { FileText, Film, Code2, ImageIcon } from "lucide-react"

function DocumentPage({ page }: { page: FileDocumentPreviewPage }): JSX.Element {
  return (
    <div className="flex flex-col justify-between rounded-xl border border-border bg-white/90 p-4 shadow-sm">
      <div
        className="mb-3 flex-1 rounded-lg bg-gradient-to-br from-slate-100 via-white to-slate-50 text-xs text-muted-foreground"
        style={{
          backgroundImage: page.thumbnail ? `url(${page.thumbnail})` : undefined,
          backgroundSize: "cover",
          backgroundPosition: "center",
        }}
      />
      <div>
        <p className="text-xs font-medium text-muted-foreground">Trang {page.number}</p>
        <p className="text-sm text-foreground">{page.excerpt}</p>
      </div>
    </div>
  )
}

export function FileViewer({ file }: { file: FileDetail }): JSX.Element {
  const preview = file.preview

  if (!preview) {
    return (
      <div className="flex min-h-[320px] items-center justify-center rounded-2xl border border-dashed border-border bg-muted/30">
        <div className="text-center text-sm text-muted-foreground">
          Không có dữ liệu xem trước cho tệp này.
        </div>
      </div>
    )
  }

  if (preview.kind === "image" || preview.kind === "design") {
    return (
      <div className="relative overflow-hidden rounded-2xl border border-border bg-muted/40">
        <img src={preview.url} alt={preview.alt ?? file.name} className="h-full w-full object-cover" />
        {preview.kind === "design" ? (
          <div className="pointer-events-none absolute inset-0 bg-gradient-to-t from-black/30 via-transparent to-transparent" />
        ) : null}
      </div>
    )
  }

  if (preview.kind === "video") {
    return (
      <div className="relative overflow-hidden rounded-2xl border border-border bg-black">
        <video
          className="h-full w-full"
          controls
          poster={preview.poster}
          src={preview.url}
        >
          Trình duyệt của bạn không hỗ trợ phần tử video.
        </video>
        <div className="pointer-events-none absolute inset-x-0 bottom-0 flex items-center gap-2 bg-gradient-to-t from-black/70 to-transparent p-4 text-sm font-medium text-white">
          <Film className="h-4 w-4" />
          Xem trước video trực tuyến
        </div>
      </div>
    )
  }

  if (preview.kind === "code") {
    return (
      <div className="overflow-hidden rounded-2xl border border-border bg-slate-950 text-slate-100">
        <div className="flex items-center gap-2 border-b border-slate-800 bg-slate-900/60 px-4 py-2 text-xs uppercase tracking-wide text-slate-400">
          <Code2 className="h-4 w-4" />
          {preview.language.toUpperCase()} snippet
        </div>
        <pre className="max-h-[420px] overflow-auto p-4 text-sm leading-relaxed">
          <code>{preview.content}</code>
        </pre>
      </div>
    )
  }

  if (preview.kind === "document") {
    return (
      <div className="space-y-4">
        <div className="flex items-center gap-2 text-sm text-muted-foreground">
          <FileText className="h-4 w-4" />
          {preview.summary}
        </div>
        <div className="grid gap-4 md:grid-cols-3">
          {preview.pages.map((page) => (
            <DocumentPage key={page.number} page={page} />
          ))}
        </div>
      </div>
    )
  }

  return (
    <div className="flex min-h-[320px] flex-col items-center justify-center gap-3 rounded-2xl border border-dashed border-border bg-muted/30 text-center">
      <ImageIcon className="h-6 w-6 text-muted-foreground" />
      <p className="text-sm text-muted-foreground">Xem trước đang được chuẩn bị cho định dạng này.</p>
    </div>
  )
}
