import type React from "react"
import type { Metadata } from "next"
import { GeistSans } from "geist/font/sans"
import { GeistMono } from "geist/font/mono"
import { Suspense } from "react"
import "./globals.css"

export const metadata: Metadata = {
  title: "ECM - Enterprise Content Management",
  description:
    "Securely store, organize, and collaborate on documents with intelligent automation and enterprise-grade security.",
  generator: "v0.app",
}

export default function LandingLayout({
  children,
}: Readonly<{
  children: React.ReactNode
}>) {
  return (
    <div className={`dark ${GeistSans.variable} ${GeistMono.variable}`}>
      <div className="font-sans antialiased">
        <Suspense fallback={null}>{children}</Suspense>
      </div>
    </div>
  )
}
