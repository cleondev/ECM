import FileViewClient from "./file-view-client"
import { mockFiles } from "@/lib/mock-data"

type FileViewPageProps = { params: { fileId: string } }

export function generateStaticParams() {
  return mockFiles.map((file) => ({ fileId: file.id }))
}

export default function FileViewPage({ params }: FileViewPageProps) {
  return <FileViewClient params={params} />
}
