"use client"

import { useCallback, useEffect, useMemo, useState } from "react"

import {
  DEFAULT_THEME,
  THEME_OPTIONS,
  THEME_STORAGE_KEY,
  isSupportedTheme,
} from "@/lib/theme"

import type { ThemeId } from "@/lib/theme"

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
