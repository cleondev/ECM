"use client"

import { useCallback, useEffect, useMemo, useState } from "react"

type ThemeOption = {
  id: ThemeId
  label: string
  description: string
  isDark: boolean
}

type ThemeId = "daybreak" | "midnight" | "forest" | "sunset"

const THEME_STORAGE_KEY = "ecm-ui.theme"

const THEME_OPTIONS: ThemeOption[] = [
  {
    id: "daybreak",
    label: "Daybreak",
    description: "Sáng sủa, trung tính cho môi trường văn phòng",
    isDark: false,
  },
  {
    id: "midnight",
    label: "Midnight",
    description: "Tông tối hiện đại giúp tập trung",
    isDark: true,
  },
  {
    id: "forest",
    label: "Evergreen",
    description: "Tông xanh lá dịu mắt với điểm nhấn tự nhiên",
    isDark: false,
  },
  {
    id: "sunset",
    label: "Sunset",
    description: "Sắc cam ấm áp tạo cảm giác năng động",
    isDark: false,
  },
]

const DEFAULT_THEME = THEME_OPTIONS[0]

function applyTheme(themeId: ThemeId) {
  if (typeof document === "undefined") {
    return
  }

  const root = document.documentElement
  const theme = THEME_OPTIONS.find((item) => item.id === themeId) ?? DEFAULT_THEME

  root.setAttribute("data-theme", theme.id)

  if (theme.isDark) {
    root.classList.add("dark")
  } else {
    root.classList.remove("dark")
  }
}

function isSupportedTheme(themeId: string | null): themeId is ThemeId {
  return THEME_OPTIONS.some((item) => item.id === themeId)
}

export function useTheme() {
  const [theme, setTheme] = useState<ThemeId>(DEFAULT_THEME.id)

  useEffect(() => {
    if (typeof window === "undefined") {
      return
    }

    const stored = window.localStorage.getItem(THEME_STORAGE_KEY)
    const initialTheme = isSupportedTheme(stored) ? stored : DEFAULT_THEME.id

    setTheme(initialTheme)
    applyTheme(initialTheme)

    if (!stored) {
      window.localStorage.setItem(THEME_STORAGE_KEY, initialTheme)
    }
  }, [])

  const setAndPersistTheme = useCallback((nextTheme: ThemeId) => {
    setTheme(nextTheme)

    if (typeof window === "undefined") {
      return
    }

    applyTheme(nextTheme)
    window.localStorage.setItem(THEME_STORAGE_KEY, nextTheme)
  }, [])

  const availableThemes = useMemo(
    () =>
      THEME_OPTIONS.map((option) => ({
        id: option.id,
        label: option.label,
        description: option.description,
      })),
    [],
  )

  return {
    theme,
    setTheme: setAndPersistTheme,
    themes: availableThemes,
  }
}

export type { ThemeId }
