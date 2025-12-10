"use client"

import {
  Annotation,
  BookmarkView,
  Inject,
  LinkAnnotation,
  Magnification,
  Navigation,
  PdfViewerComponent,
  Print,
  TextSearch,
  TextSelection,
  ThumbnailView,
  Toolbar,
} from "@syncfusion/ej2-react-pdfviewer"

import "@syncfusion/ej2-base/styles/material.css"
import "@syncfusion/ej2-buttons/styles/material.css"
import "@syncfusion/ej2-dropdowns/styles/material.css"
import "@syncfusion/ej2-inputs/styles/material.css"
import "@syncfusion/ej2-navigations/styles/material.css"
import "@syncfusion/ej2-popups/styles/material.css"
import "@syncfusion/ej2-splitbuttons/styles/material.css"
import "@syncfusion/ej2-notifications/styles/material.css"
import "@syncfusion/ej2-react-pdfviewer/styles/material.css"

import { ensureSyncfusionLicense } from "@/lib/syncfusion"

ensureSyncfusionLicense()

type PdfViewerProps = {
  documentUrl: string
  serviceUrl?: string
  documentPath?: string
}

export function PdfViewer({ documentUrl, serviceUrl, documentPath }: PdfViewerProps) {
  const origin = typeof window !== "undefined" ? window.location.origin : ""

  // nếu có env thì lấy env, không thì ghép origin + path public
  const resourceUrl =
    process.env.NEXT_PUBLIC_PDFVIEWER_RESOURCE_URL ??
    `${origin}/ej2-pdfviewer-lib`

  const pdfServiceUrl = serviceUrl ?? process.env.NEXT_PUBLIC_PDFVIEWER_SERVICE_URL
  const documentPathValue = documentPath ?? documentUrl

  return (
    <div className="h-[75vh] min-h-[520px] w-full overflow-hidden rounded-lg border border-border bg-background">
      <PdfViewerComponent
        id="ecm-ej2-pdf-viewer"
        documentPath={documentPathValue}
        serviceUrl={pdfServiceUrl ?? undefined}
        resourceUrl={resourceUrl}
        height="100%"
        width="100%"
        toolbarSettings={{
          showTooltip: true,
          toolbarItems: [
            "PageNavigationTool",
            "MagnificationTool",
            "PanTool",
            "SelectionTool",
            "SearchOption",
            "DownloadOption",
          ],
        }}
      >
        <Inject
          services={[
            Toolbar,
            Magnification,
            Navigation,
            TextSelection,
            TextSearch,
            BookmarkView,
            ThumbnailView,
          ]}
        />
      </PdfViewerComponent>
    </div>
  )
}
