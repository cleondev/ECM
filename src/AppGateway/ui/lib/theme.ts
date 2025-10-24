export type ThemeId = "daybreak" | "midnight" | "forest" | "sunset"

export type ThemeOption = {
  id: ThemeId
  label: string
  description: string
  isDark: boolean
}

export const THEME_STORAGE_KEY = "ecm-ui.theme"

export const THEME_OPTIONS: ThemeOption[] = [
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

export const DEFAULT_THEME = THEME_OPTIONS[0]

export const SUPPORTED_THEME_IDS = new Set<ThemeId>(THEME_OPTIONS.map((option) => option.id))

export const DARK_THEME_IDS = new Set<ThemeId>(THEME_OPTIONS.filter((option) => option.isDark).map((option) => option.id))

export function isSupportedTheme(themeId: string | null): themeId is ThemeId {
  return themeId != null && SUPPORTED_THEME_IDS.has(themeId as ThemeId)
}
