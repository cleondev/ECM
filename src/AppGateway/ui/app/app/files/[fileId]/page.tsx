import FileViewClient from "./file-view-client"
type FileViewPageProps = { params: { fileId: string } }

export default function FileViewPage({ params }: FileViewPageProps) {
  return <FileViewClient params={params} />
}
