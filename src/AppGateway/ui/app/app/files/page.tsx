import { ViewerPageClient } from "./viewer-page-client"

export const dynamic = "force-static"
export const dynamicParams = false

export default function FileViewPage() {
  return <ViewerPageClient />
}
