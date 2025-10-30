import { clsx, type ClassValue } from "clsx"
import { twMerge } from "tailwind-merge"

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
}

function stripDiacritics(value: string): string {
  try {
    return value.normalize("NFD").replace(/[\u0300-\u036f]/g, "")
  } catch (error) {
    console.warn("[utils] Failed to normalize string for slugify:", error)
    return value
  }
}

export function slugify(value: string): string {
  if (!value) {
    return ""
  }

  const withoutDiacritics = stripDiacritics(value)

  return withoutDiacritics
    .toLowerCase()
    .trim()
    .replace(/[^a-z0-9\s_-]/g, "")
    .replace(/[\s_-]+/g, "-")
    .replace(/^-+|-+$/g, "")
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
