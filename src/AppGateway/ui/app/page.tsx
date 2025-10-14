'use client'
import { useEffect } from 'react'
import { useRouter } from 'next/navigation'

const LANDING_PAGE_ROUTE = '/landing'
const MAIN_APP_ROUTE = '/profile'

export default function Home() {
  const router = useRouter()

  useEffect(() => {
    // Gọi API backend để kiểm tra đăng nhập
    fetch('/api/profile', { credentials: 'include', cache: 'no-store' })
      .then(res => {
        if (res.ok) {
          router.replace(MAIN_APP_ROUTE) // Đã đăng nhập
        } else {
          router.replace(LANDING_PAGE_ROUTE) // Chưa đăng nhập
        }
      })
      .catch(() => router.replace(LANDING_PAGE_ROUTE))
  }, [router])

  return null
}