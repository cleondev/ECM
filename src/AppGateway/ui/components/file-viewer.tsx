import type { FileDetail, FileDocumentPreviewPage } from "@/lib/types"
import type { ViewerConfig } from "@/lib/viewer-utils"
import { Code2, FileSpreadsheet, FileText, Film, ImageIcon, Presentation } from "lucide-react"

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
        <p className="text-xs font-medium text-muted-foreground">Page {page.number}</p>
        <p className="text-sm text-foreground">{page.excerpt}</p>
      </div>
    </div>
  )
}

function VideoPreview({ preview }: { preview?: FileDetail["preview"] }) {
  if (preview?.kind !== "video") {
    return <ViewerFallback message="Unable to play video from this file." />
  }

  return (
    <div className="relative overflow-hidden rounded-2xl border border-border bg-black">
      <video className="h-full w-full" controls poster={preview.poster} src={preview.url}>
        Your browser does not support the video element.
      </video>
      <div className="pointer-events-none absolute inset-x-0 bottom-0 flex items-center gap-2 bg-gradient-to-t from-black/70 to-transparent p-4 text-sm font-medium text-white">
        <Film className="h-4 w-4" />
        Streaming video preview
      </div>
    </div>
  )
}

function ImagePreview({ preview, alt }: { preview?: FileDetail["preview"]; alt: string }) {
  if (preview?.kind !== "image" && preview?.kind !== "design") {
    return <ViewerFallback message="No image available to display." />
  }

  return (
    <div className="relative overflow-hidden rounded-2xl border border-border bg-muted/40">
      <img src={preview.url} alt={preview.alt ?? alt} className="h-full w-full object-cover" />
      {preview.kind === "design" ? (
        <div className="pointer-events-none absolute inset-0 bg-gradient-to-t from-black/30 via-transparent to-transparent" />
      ) : null}
    </div>
  )
}

function CodePreview({ preview }: { preview?: FileDetail["preview"] }) {
  if (preview?.kind !== "code") {
    return <ViewerFallback message="No snippet attached." />
  }

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

function PdfPreview({ preview }: { preview?: FileDetail["preview"] }) {
  if (preview?.kind !== "document") {
    return (
      <div className="space-y-4">
        <div className="flex items-center gap-2 text-sm text-muted-foreground">
        <FileText className="h-4 w-4" />
          PDF viewer
        </div>
        <ViewerFallback message="No PDF content available for preview." />
      </div>
    )
  }

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

function ViewerFallback({ message }: { message: string }) {
  return (
    <div className="flex min-h-[320px] flex-col items-center justify-center gap-3 rounded-2xl border border-dashed border-border bg-muted/30 text-center">
      <ImageIcon className="h-6 w-6 text-muted-foreground" />
      <p className="text-sm text-muted-foreground">{message}</p>
    </div>
  )
}

function WordPreview({ file }: { file: FileDetail }) {
  return (
    <div className="rounded-2xl border border-border bg-white shadow-sm">
      <div className="flex items-center gap-2 border-b border-border/60 bg-blue-50/80 px-4 py-3 text-sm font-semibold text-blue-700">
        <FileText className="h-4 w-4" />
        Quick Word document preview
      </div>
      <div className="space-y-4 px-6 py-5 text-sm leading-relaxed text-slate-700">
        <p className="font-medium text-slate-900">{file.name}</p>
        <p>
          This is a simulated preview that helps you quickly scan the main content before downloading the document.
        </p>
        <div className="space-y-3 rounded-xl bg-slate-50 p-4">
          <p className="text-slate-900">
            “{file.description ?? "Summary content is being synchronized"}”.
          </p>
          <p className="text-xs uppercase tracking-wide text-slate-500">FORMAT: Word</p>
        </div>
      </div>
    </div>
  )
}

function ExcelPreview() {
  return (
    <div className="overflow-hidden rounded-2xl border border-border bg-white shadow-sm">
      <div className="flex items-center gap-2 border-b border-border/60 bg-emerald-50/80 px-4 py-3 text-sm font-semibold text-emerald-700">
        <FileSpreadsheet className="h-4 w-4" />
        Interactive spreadsheet
      </div>
      <div className="overflow-x-auto p-4">
        <table className="min-w-[480px] border-collapse text-sm">
          <thead>
            <tr>
              {Array.from({ length: 5 }, (_, index) => (
                <th key={index} className="border border-border/60 bg-emerald-100/70 px-3 py-2 text-left font-semibold text-emerald-900">
                  Column {String.fromCharCode(65 + index)}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {Array.from({ length: 4 }, (_, row) => (
              <tr key={row}>
                {Array.from({ length: 5 }, (_, col) => (
                  <td key={col} className="border border-border/60 px-3 py-2 text-slate-700">
                    {(row + 1) * (col + 2)}
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}

function PowerPointPreview() {
  return (
    <div className="space-y-4 rounded-2xl border border-border bg-white p-4 shadow-sm">
      <div className="flex items-center gap-2 text-sm font-semibold text-orange-600">
        <Presentation className="h-4 w-4" />
        Featured slide deck
      </div>
      <div className="grid gap-4 md:grid-cols-3">
        {Array.from({ length: 3 }, (_, index) => (
          <div
            key={index}
            className="rounded-xl border border-border/70 bg-gradient-to-br from-orange-100 via-white to-orange-50 p-4 shadow-inner"
          >
            <p className="text-xs font-medium uppercase text-orange-500">Slide {index + 1}</p>
            <p className="mt-2 text-sm font-semibold text-slate-900">Presentation title</p>
            <p className="text-xs text-slate-600">Quick notes to outline the key talking points.</p>
          </div>
        ))}
      </div>
    </div>
  )
}

type FileViewerProps = {
  file: FileDetail
  viewerConfig: ViewerConfig
}

export function FileViewer({ file, viewerConfig }: FileViewerProps): JSX.Element {
  const { category, officeKind } = viewerConfig
  const preview = file.preview

  if (!preview && category !== "pdf") {
    return <ViewerFallback message="No preview data available for this file." />
  }

  switch (category) {
    case "video":
      return <VideoPreview preview={preview} />
    case "image":
      return <ImagePreview preview={preview} alt={file.name} />
    case "code":
      return <CodePreview preview={preview} />
    case "pdf":
      if (officeKind === "word") {
        return <WordPreview file={file} />
      }

      if (officeKind === "excel") {
        return <ExcelPreview />
      }

      if (officeKind === "powerpoint") {
        return <PowerPointPreview />
      }

      return <PdfPreview preview={preview} />
    case "unsupported":
    default:
      return <ViewerFallback message="This format is not supported for inline viewing yet." />
  }
}
