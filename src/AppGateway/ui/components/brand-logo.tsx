import Image from "next/image"

import { cn } from "@/lib/utils"

export type BrandLogoProps = {
  className?: string
  imageClassName?: string
  textClassName?: string
  showText?: boolean
  size?: number
  priority?: boolean
}

export function BrandLogo({
  className,
  imageClassName,
  textClassName,
  showText = true,
  size = 32,
  priority = false,
}: BrandLogoProps) {
  return (
    <span className={cn("inline-flex items-center gap-2", className)}>
      <Image
        src="/logo/logo_256x256.png"
        alt="ECM logo"
        width={size}
        height={size}
        priority={priority}
        className={cn("h-8 w-8", imageClassName)}
      />
      {showText && (
        <span className={cn("font-semibold text-lg text-foreground", textClassName)}>ECM</span>
      )}
    </span>
  )
}
