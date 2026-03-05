import { computed, ref } from 'vue'

type ThemeMode = 'light' | 'dark' | 'system'
type ResolvedTheme = 'light' | 'dark'
type ThemeColor = 'default' | 'blue' | 'violet'

const THEME_KEY = 'admin_theme_mode'
const THEME_COLOR_KEY = 'admin_theme_color'
const mode = ref<ThemeMode>('system')
const color = ref<ThemeColor>('default')
const initialized = ref(false)
let mediaQuery: MediaQueryList | null = null

const resolvedTheme = computed<ResolvedTheme>(() => {
  if (mode.value === 'system') {
    return mediaQuery?.matches ? 'dark' : 'light'
  }

  return mode.value
})

function applyResolvedTheme(nextTheme: ResolvedTheme) {
  document.documentElement.classList.toggle('dark', nextTheme === 'dark')
}

function applyThemeColor(nextColor: ThemeColor) {
  document.documentElement.setAttribute('data-theme', nextColor)
}

function setTheme(nextMode: ThemeMode) {
  mode.value = nextMode
  applyResolvedTheme(resolvedTheme.value)
  localStorage.setItem(THEME_KEY, nextMode)
}

function setThemeColor(nextColor: ThemeColor) {
  color.value = nextColor
  applyThemeColor(nextColor)
  localStorage.setItem(THEME_COLOR_KEY, nextColor)
}

function initializeTheme() {
  if (initialized.value || typeof window === 'undefined') {
    return
  }

  mediaQuery = window.matchMedia('(prefers-color-scheme: dark)')

  const saved = localStorage.getItem(THEME_KEY)
  if (saved === 'light' || saved === 'dark' || saved === 'system') {
    mode.value = saved
  }

  const savedColor = localStorage.getItem(THEME_COLOR_KEY)
  if (savedColor === 'default' || savedColor === 'blue' || savedColor === 'violet') {
    color.value = savedColor
  } else if (savedColor === 'neutral' || savedColor === 'zinc' || savedColor === 'slate' || savedColor === 'stone') {
    color.value = 'default'
    localStorage.setItem(THEME_COLOR_KEY, 'default')
  }

  const handleSystemThemeChange = () => {
    if (mode.value === 'system') {
      applyResolvedTheme(resolvedTheme.value)
    }
  }

  if (mediaQuery.addEventListener) {
    mediaQuery.addEventListener('change', handleSystemThemeChange)
  } else {
    mediaQuery.addListener(handleSystemThemeChange)
  }

  applyResolvedTheme(resolvedTheme.value)
  applyThemeColor(color.value)
  initialized.value = true
}

export function useTheme() {
  return {
    themeMode: mode,
    themeColor: color,
    resolvedTheme,
    setTheme,
    setThemeColor,
    initializeTheme,
  }
}

export type { ThemeMode, ResolvedTheme, ThemeColor }
