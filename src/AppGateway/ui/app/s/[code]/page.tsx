"use client"

export const dynamic = "force-static"

export function generateStaticParams(): Array<{ code: string }> {
  return []
}

export { default } from "../page"
