'use client'
import { useEffect, useState } from 'react'
import { useRouter } from 'next/navigation'

import { FileManager } from '@/components/file-manager'

const LANDING_PAGE_ROUTE = '/landing'

export default function Home() {
  const router = useRouter()
  const [isAuthenticated, setIsAuthenticated] = useState<boolean | null>(null)

  useEffect(() => {
    let isMounted = true

    fetch('/api/profile', { credentials: 'include', cache: 'no-store' })
      .then(res => {
        if (!isMounted) return

        if (res.ok) {
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