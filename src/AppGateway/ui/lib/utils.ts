import { clsx, type ClassValue } from "clsx"
import { twMerge } from "tailwind-merge"

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
}

export function slugify(value: string): string {
  return value
    .toLowerCase()
    .trim()
    .replace(/[^\w\s-]/g, "")
    .replace(/\s+/g, "-")
    .replace(/-+/g, "-")
}

type MaybeString = string | null | undefined

function splitPathAndSuffix(path: string): [string, string] {
  const queryIndex = path.indexOf("?")
  const hashIndex = path.indexOf("#")

  const cutIndex =
    queryIndex >= 0 && hashIndex >= 0
      ? Math.min(queryIndex, hashIndex)
      : queryIndex >= 0
        ? queryIndex
        : hashIndex

  if (cutIndex === -1) {
    return [path, ""]
  }

  return [path.slice(0, cutIndex), path.slice(cutIndex)]
}

function normalizeCandidatePath(candidate: MaybeString): string | null {
  if (!candidate) {
    return null
  }

  const trimmed = candidate.trim()

  if (!trimmed || !trimmed.startsWith("/") || trimmed.startsWith("//")) {
    return null
  }

  const [pathname, suffix] = splitPathAndSuffix(trimmed)

  if (pathname === "/") {
    return null
  }

  const normalizedPathname = pathname.endsWith("/") ? pathname : `${pathname}/`
  return `${normalizedPathname}${suffix}`
}

export function normalizeRedirectTarget(
  candidate: MaybeString,
  fallback: string = "/app/",
): string {
  const normalizedFallback = normalizeCandidatePath(fallback) ?? "/app/"
  const normalizedCandidate = normalizeCandidatePath(candidate)

  return normalizedCandidate ?? normalizedFallback
}

export function normalizeRedirectTargetWithDiagnostics(
  candidate: MaybeString,
  fallback: string = "/app/",
): { normalized: string; normalizedCandidate: string | null; normalizedFallback: string } {
  const normalizedFallback = normalizeCandidatePath(fallback) ?? "/app/"
  const normalizedCandidate = normalizeCandidatePath(candidate)

  return {
    normalized: normalizedCandidate ?? normalizedFallback,
    normalizedCandidate,
    normalizedFallback,
  }
}

export function createSignInRedirectPath(
  candidate: MaybeString,
  fallback: string = "/app/",
): string {
  const normalizedTarget = normalizeRedirectTarget(candidate, fallback)
  return `/signin/?redirectUri=${encodeURIComponent(normalizedTarget)}`
}
