import type { Metadata } from "next"
import { GeistSans } from "geist/font/sans"
import { GeistMono } from "geist/font/mono"

import { ThemeScript } from "@/components/theme-script"
import { SyncfusionLicenseLoader } from "@/components/syncfusion-license-loader.tsx"


import "file-icon-vectors/dist/file-icon-vectors.css"
import "file-icon-vectors/dist/file-icon-square-o.min.css"
import "./globals.css"

const appDescription =
  "Securely store, organize, and collaborate on documents with intelligent automation and enterprise-grade security."

export const metadata: Metadata = {
  title: {
    default: "ECM | Enterprise Content Management",
    template: "%s | ECM",
  },
  description: appDescription,
  generator: "v0.app",
  icons: {
    icon: [
      { url: "/favicon.ico" },
      { url: "/logo/logo_256x256.png", sizes: "256x256", type: "image/png" },
    ],
    apple: [{ url: "/logo/logo_256x256.png", sizes: "256x256" }],
  },
}

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode
}>) {
  return (
    <html lang="en" suppressHydrationWarning>
      <body suppressHydrationWarning className={`font-sans ${GeistSans.variable} ${GeistMono.variable}`}>
        <ThemeScript />
        <SyncfusionLicenseLoader />
        {children}
      </body>
    </html>
  )
}
