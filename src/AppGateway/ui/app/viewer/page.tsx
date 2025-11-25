import { ViewerPageClient } from "./viewer-page-client"

export const dynamic = "force-dynamic"
export const dynamicParams = false

export default function FileViewPage() {
  return <ViewerPageClient />
}
