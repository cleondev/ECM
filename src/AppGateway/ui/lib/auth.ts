const DEFAULT_SILENT_LOGIN_TIMEOUT = 10000

type SilentLoginOptions = {
  timeoutMs?: number
}

export function resolveGatewayUrl(path: string): string {
  const envBase = (process.env.NEXT_PUBLIC_GATEWAY_API_URL ?? "").replace(/\/$/, "")
  const runtimeBase = envBase || (typeof window !== "undefined" ? window.location.origin : "")

  if (!runtimeBase) {
    return path
  }

  try {
    return new URL(path, runtimeBase).toString()
  } catch (error) {
    console.warn("[auth] Không thể chuẩn hoá URL đăng nhập:", error)
    return path
  }
}

export async function attemptSilentLogin(
  loginUrl: string,
  options: SilentLoginOptions = {},
): Promise<boolean> {
  if (typeof window === "undefined" || typeof document === "undefined") {
    return false
  }

  const resolvedUrl = resolveGatewayUrl(loginUrl)
  const timeoutMs = Math.max(1000, options.timeoutMs ?? DEFAULT_SILENT_LOGIN_TIMEOUT)

  return new Promise<boolean>((resolve) => {
    const iframe = document.createElement("iframe")
    iframe.src = resolvedUrl
    iframe.style.display = "none"
    iframe.style.position = "absolute"
    iframe.style.width = "0"
    iframe.style.height = "0"
    iframe.setAttribute("aria-hidden", "true")
    iframe.tabIndex = -1

    let finished = false

    const cleanup = (result: boolean) => {
      if (finished) {
        return
      }
      finished = true

      window.clearTimeout(timeoutId)

      iframe.removeEventListener("load", handleLoad)
      iframe.removeEventListener("error", handleError)

      if (iframe.parentNode) {
        iframe.parentNode.removeChild(iframe)
      }

      resolve(result)
    }

    const handleLoad = () => {
      cleanup(true)
    }

    const handleError = () => {
      cleanup(false)
    }

    const timeoutId = window.setTimeout(() => {
      console.warn("[auth] Silent login iframe timed out.")
      cleanup(false)
    }, timeoutMs)

    iframe.addEventListener("load", handleLoad)
    iframe.addEventListener("error", handleError)

    document.body.appendChild(iframe)
  })
}
