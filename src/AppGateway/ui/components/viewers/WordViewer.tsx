"use client"

import { useEffect, useRef, useState } from "react"

import {
  DocumentEditorContainerComponent,
  Inject as DocumentEditorInject,
  Toolbar as DocumentEditorToolbar,
} from "@syncfusion/ej2-react-documenteditor"
import "@syncfusion/ej2-base/styles/material.css"
import "@syncfusion/ej2-buttons/styles/material.css"
import "@syncfusion/ej2-dropdowns/styles/material.css"
import "@syncfusion/ej2-inputs/styles/material.css"
import "@syncfusion/ej2-navigations/styles/material.css"
import "@syncfusion/ej2-popups/styles/material.css"
import "@syncfusion/ej2-react-documenteditor/styles/material.css"
import "@syncfusion/ej2-splitbuttons/styles/material.css"

import type { FileDetail } from "@/lib/types"
import { ensureSyncfusionLicense } from "@/lib/syncfusion"
import { UnsupportedViewer } from "./UnsupportedViewer"

ensureSyncfusionLicense()

type WordViewerProps = {
  file: FileDetail
  sfdtUrl?: string | null
}

export function WordViewer({ file, sfdtUrl }: WordViewerProps) {
  const editorRef = useRef<DocumentEditorContainerComponent>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!sfdtUrl) {
      setError("Không có nội dung Word để hiển thị.")
      return
    }

    let cancelled = false
    setLoading(true)
    setError(null)

    fetch(sfdtUrl, { credentials: "include" })
      .then(async (response) => {
        if (!response.ok) {
          throw new Error(`Viewer endpoint returned status ${response.status}`)
        }

        const content = await response.text()

        if (!cancelled) {
          editorRef.current?.documentEditor?.open(content, "Sfdt")
        }
      })
      .catch((err) => {
        console.error("[viewer] Failed to load Word viewer content", err)
        if (!cancelled) {
          setError("Không thể tải nội dung Word từ máy chủ.")
        }
      })
      .finally(() => {
        if (!cancelled) {
          setLoading(false)
        }
      })

    return () => {
      cancelled = true
    }
  }, [sfdtUrl])

  if (error) {
    return <UnsupportedViewer file={file} message={error} />
  }

  if (!sfdtUrl) {
    return <UnsupportedViewer file={file} />
  }

  return (
    <div className="space-y-3">
      {loading ? (
        <div className="rounded-lg border border-border bg-muted/60 p-3 text-sm text-muted-foreground">
          Đang tải nội dung tài liệu…
        </div>
      ) : null}
      <DocumentEditorContainerComponent
        ref={editorRef}
        enableToolbar={false}
        showPropertiesPane={false}
        height="75vh"
        serviceUrl="https://services.syncfusion.com/react/production/api/documenteditor/"
        style={{ border: "1px solid hsl(var(--border))" }}
        documentEditorSettings={{ isReadOnly: true }}
      >
        <DocumentEditorInject services={[DocumentEditorToolbar]} />
      </DocumentEditorContainerComponent>
    </div>
  )
}
