"use client"

import { useEffect, useRef, useState } from "react"

import { SpreadsheetComponent } from "@syncfusion/ej2-react-spreadsheet"
import "@syncfusion/ej2-base/styles/material.css"
import "@syncfusion/ej2-buttons/styles/material.css"
import "@syncfusion/ej2-dropdowns/styles/material.css"
import "@syncfusion/ej2-grids/styles/material.css"
import "@syncfusion/ej2-inputs/styles/material.css"
import "@syncfusion/ej2-lists/styles/material.css"
import "@syncfusion/ej2-navigations/styles/material.css"
import "@syncfusion/ej2-popups/styles/material.css"
import "@syncfusion/ej2-react-spreadsheet/styles/material.css"
import "@syncfusion/ej2-splitbuttons/styles/material.css"

import type { FileDetail } from "@/lib/types"
import { ensureSyncfusionLicense } from "@/lib/syncfusion"
import { UnsupportedViewer } from "./UnsupportedViewer"

ensureSyncfusionLicense()

type ExcelViewerProps = {
  file: FileDetail
  excelJsonUrl?: string | null
}

export function ExcelViewer({ file, excelJsonUrl }: ExcelViewerProps) {
  const spreadsheetRef = useRef<SpreadsheetComponent>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!excelJsonUrl) {
      setError("Không có nội dung bảng tính để hiển thị.")
      return
    }

    let cancelled = false
    setLoading(true)
    setError(null)

    fetch(excelJsonUrl, { credentials: "include" })
      .then(async (response) => {
        if (!response.ok) {
          throw new Error(`Viewer endpoint returned status ${response.status}`)
        }

        const payload = await response.json()

        if (!cancelled) {
          spreadsheetRef.current?.openFromJson({ file: payload })
        }
      })
      .catch((err) => {
        console.error("[viewer] Failed to load spreadsheet viewer content", err)
        if (!cancelled) {
          setError("Không thể tải dữ liệu bảng tính từ máy chủ.")
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
  }, [excelJsonUrl])

  if (error) {
    return <UnsupportedViewer file={file} message={error} />
  }

  if (!excelJsonUrl) {
    return <UnsupportedViewer file={file} />
  }

  return (
    <div className="space-y-3">
      {loading ? (
        <div className="rounded-lg border border-border bg-muted/60 p-3 text-sm text-muted-foreground">
          Đang tải nội dung bảng tính…
        </div>
      ) : null}
      <div className="overflow-hidden rounded-lg border border-border bg-background">
        <SpreadsheetComponent 
        ref={spreadsheetRef} 
        height={"75vh"} 
        allowEditing={false} 
        showRibbon={false}
        showAggregateBar={false}
        />
      </div>
    </div>
  )
}
