import { redirect } from "next/navigation"

import LandingPage from "./landing/page"
import { checkLogin } from "@/lib/api"

export default async function RootPage() {
  try {
    const result = await checkLogin("/app")

    if (result.isAuthenticated) {
      redirect(result.redirectPath ?? "/app")
    }
  } catch (error) {
    console.error("[app] Failed to verify login status", error)
  }

  return <LandingPage />
}
