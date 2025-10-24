"use client"

import { Palette } from "lucide-react"

import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { cn } from "@/lib/utils"
import { useTheme } from "@/hooks/use-theme"
import type { ThemeId } from "@/lib/theme"

type ThemeSwitcherProps = {
  className?: string
}

const THEME_ACCENT: Record<ThemeId, string> = {
  daybreak: "bg-sky-400",
  midnight: "bg-indigo-500",
  forest: "bg-emerald-500",
  sunset: "bg-orange-500",
}

export function ThemeSwitcher({ className }: ThemeSwitcherProps) {
  const { theme, setTheme, themes } = useTheme()

  return (
    <Select value={theme} onValueChange={(value) => setTheme(value as ThemeId)}>
      <SelectTrigger size="sm" className={cn("w-[160px] gap-2 pl-2", className)} aria-label="Change theme">
        <span className={cn("inline-flex size-3 rounded-full", THEME_ACCENT[theme])} aria-hidden="true" />
        <Palette className="size-4 text-muted-foreground" aria-hidden="true" />
        <SelectValue placeholder="Chọn theme" />
      </SelectTrigger>
      <SelectContent>
        {themes.map((option) => (
          <SelectItem key={option.id} value={option.id} title={option.description}>
            <div className="flex items-center gap-2">
              <span className={cn("inline-flex size-3 rounded-full", THEME_ACCENT[option.id as ThemeId])} aria-hidden="true" />
              <span>{option.label}</span>
            </div>
          </SelectItem>
        ))}
      </SelectContent>
    </Select>
  )
}
