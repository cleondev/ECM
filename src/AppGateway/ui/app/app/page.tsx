"use client"
import { useEffect, useState } from "react"
import { useRouter } from "next/navigation"

import { FileManager } from "@/components/file-manager"
import { checkLogin } from "@/lib/api"

const LANDING_PAGE_ROUTE = "/"
const MAIN_APP_ROUTE = "/app"

export default function AppHomePage() {
  const router = useRouter()
  const [isAuthenticated, setIsAuthenticated] = useState<boolean | null>(null)

  useEffect(() => {
    let isMounted = true

    checkLogin(MAIN_APP_ROUTE)
      .then((result) => {
        if (!isMounted) return

        if (result.isAuthenticated) {
          setIsAuthenticated(true)
        } else {
          router.replace(LANDING_PAGE_ROUTE)
        }
      })
      .catch(() => {
        if (!isMounted) return
        router.replace(LANDING_PAGE_ROUTE)
      })

    return () => {
      isMounted = false
    }
  }, [router])

  if (isAuthenticated) {
    return <FileManager />
  }

  return null
}
