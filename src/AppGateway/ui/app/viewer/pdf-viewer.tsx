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

type PdfViewerProps = {
  documentUrl: string
}

export function PdfViewer({ documentUrl }: PdfViewerProps) {
  return (
    <div className="h-[75vh] min-h-[520px] w-full overflow-hidden rounded-lg border border-border bg-background">
      <PdfViewerComponent
        id="ecm-ej2-pdf-viewer"
        documentPath={documentUrl}
        serviceUrl="https://services.syncfusion.com/react/production/api/pdfviewer"
        enableToolbar
        height="100%"
        width="100%"
      >
        <Inject
          services={[
            Toolbar,
            Magnification,
            Navigation,
            TextSelection,
            TextSearch,
            Print,
            Annotation,
            LinkAnnotation,
            BookmarkView,
            ThumbnailView,
          ]}
        />
      </PdfViewerComponent>
    </div>
  )
}
