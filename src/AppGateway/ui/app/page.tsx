'use client'
import { useEffect } from 'react'
import { useRouter } from 'next/navigation'

export default function Home() {
  const router = useRouter()

  useEffect(() => {
    // Gọi API backend để kiểm tra đăng nhập
    fetch('/api/profile', { credentials: 'include' })
      .then(res => {
        if (res.ok) {
          router.replace('/home') // Đã đăng nhập
        } else {
          router.replace('/landing') // Chưa đăng nhập
        }
      })
      .catch(() => router.replace('/landing'))
  }, [router])

  return null
}