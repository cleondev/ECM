import * as React from 'react'

const MOBILE_BREAKPOINT = 768

const getIsMobile = () => {
  if (typeof window === 'undefined') {
    return undefined
  }

  return window.innerWidth < MOBILE_BREAKPOINT
}

export function useIsMobile() {
  const [isMobile, setIsMobile] = React.useState<boolean | undefined>(() => getIsMobile())

  React.useEffect(() => {
    if (typeof window === 'undefined') {
      return
    }

    const mql = window.matchMedia(`(max-width: ${MOBILE_BREAKPOINT - 1}px)`)
    const onChange = (event: MediaQueryListEvent) => {
      setIsMobile(event.matches)
    }

    setIsMobile(mql.matches)
    mql.addEventListener('change', onChange)
    return () => mql.removeEventListener('change', onChange)
  }, [])

  return isMobile
}
