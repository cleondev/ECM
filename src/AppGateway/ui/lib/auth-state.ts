import type { User } from "./types"

type StoredAuthSnapshot = {
  isAuthenticated: boolean
  redirectPath: string | null
  user: User | null
  updatedAt: number
}

const STORAGE_KEY = "ecm.auth.snapshot"
const MAX_STALENESS_MS = 10 * 60 * 1000 // 10 minutes

function getSessionStorage(): Storage | null {
  if (typeof window === "undefined") {
    return null
  }

  try {
    return window.sessionStorage
  } catch (error) {
    console.warn("[auth] Không thể truy cập sessionStorage:", error)
    return null
  }
}

function isExpired(snapshot: StoredAuthSnapshot): boolean {
  if (!snapshot.isAuthenticated) {
    return false
  }

  const age = Date.now() - snapshot.updatedAt
  return Number.isFinite(age) && age > MAX_STALENESS_MS
}

function readSnapshot(): StoredAuthSnapshot | null {
  const storage = getSessionStorage()
  if (!storage) {
    return null
  }

  const raw = storage.getItem(STORAGE_KEY)
  if (!raw) {
    return null
  }

  try {
    const parsed = JSON.parse(raw) as StoredAuthSnapshot

    if (!parsed || typeof parsed !== "object") {
      storage.removeItem(STORAGE_KEY)
      return null
    }

    if (isExpired(parsed)) {
      storage.removeItem(STORAGE_KEY)
      return null
    }

    return parsed
  } catch (error) {
    console.warn("[auth] Không thể parse sessionStorage state:", error)
    storage.removeItem(STORAGE_KEY)
    return null
  }
}

function persistSnapshot(snapshot: StoredAuthSnapshot): void {
  const storage = getSessionStorage()
  if (!storage) {
    return
  }

  try {
    storage.setItem(STORAGE_KEY, JSON.stringify(snapshot))
  } catch (error) {
    console.warn("[auth] Không thể lưu trạng thái đăng nhập:", error)
  }
}

export function getCachedAuthSnapshot(): StoredAuthSnapshot | null {
  return readSnapshot()
}

export function updateCachedAuthSnapshot(data: {
  isAuthenticated: boolean
  redirectPath: string
  user: User | null
}): void {
  if (!data.isAuthenticated) {
    clearCachedAuthSnapshot()
    return
  }

  persistSnapshot({
    isAuthenticated: true,
    redirectPath: data.redirectPath,
    user: data.user,
    updatedAt: Date.now(),
  })
}

export function clearCachedAuthSnapshot(): void {
  const storage = getSessionStorage()
  if (!storage) {
    return
  }

  storage.removeItem(STORAGE_KEY)
}
