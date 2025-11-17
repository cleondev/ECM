import { notFound } from "next/navigation"

import FileViewClient from "./file-view-client"
import { parseViewerPreference } from "@/lib/viewer-utils"

type FileViewPageProps = {
  searchParams?: {
    fileId?: string
    viewer?: string
    office?: string
  }
}

export const dynamic = "force-static"
export const dynamicParams = false

export default function FileViewPage({ searchParams }: FileViewPageProps) {
  const fileId = searchParams?.fileId?.trim()
  const preference = parseViewerPreference(searchParams?.viewer, searchParams?.office)

  if (!fileId) {
    notFound()
  }

  return <FileViewClient fileId={fileId} preference={preference} />
}
