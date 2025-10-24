import Script from "next/script"

import {
  DARK_THEME_IDS,
  DEFAULT_THEME,
  SUPPORTED_THEME_IDS,
  THEME_STORAGE_KEY,
} from "@/lib/theme"

const supportedThemes = Array.from(SUPPORTED_THEME_IDS)
const darkThemes = Array.from(DARK_THEME_IDS)

const themeInitializer = `(() => {
  try {
    const storageKey = ${JSON.stringify(THEME_STORAGE_KEY)}
    const supported = new Set(${JSON.stringify(supportedThemes)})
    const darkThemes = new Set(${JSON.stringify(darkThemes)})
    const root = document.documentElement
    const stored = window.localStorage.getItem(storageKey)
    const fallback = ${JSON.stringify(DEFAULT_THEME.id)}
    const theme = stored && supported.has(stored) ? stored : fallback

    root.setAttribute('data-theme', theme)

    if (darkThemes.has(theme)) {
      root.classList.add('dark')
    } else {
      root.classList.remove('dark')
    }

    if (!stored) {
      window.localStorage.setItem(storageKey, theme)
    }
  } catch (error) {
    console.warn('[theme] Failed to apply stored theme', error)
  }
})();`

export function ThemeScript() {
  return <Script id="theme-script" strategy="beforeInteractive" dangerouslySetInnerHTML={{ __html: themeInitializer }} />
}
