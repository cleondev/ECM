import { Suspense } from "react"

import { ViewerLoading, ViewerPageClient } from "./viewer-page-client"

export default function FileViewPage() {
  return (
    <Suspense fallback={<ViewerLoading />}>
      <ViewerPageClient />
    </Suspense>
  )
}
