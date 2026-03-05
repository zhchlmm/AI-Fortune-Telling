<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import UiButton from './UiButton.vue'
import { useTheme, type ThemeColor, type ThemeMode } from '../../composables/useTheme'

const props = defineProps<{
  fullWidth?: boolean
}>()

const { themeMode, themeColor, resolvedTheme, setTheme, setThemeColor } = useTheme()
const open = ref(false)
const rootRef = ref<HTMLElement | null>(null)

const options: Array<{ label: string; value: ThemeMode }> = [
  { label: '跟随系统', value: 'system' },
  { label: '浅色', value: 'light' },
  { label: '深色', value: 'dark' },
]

const colorOptions: Array<{ label: string; value: ThemeColor; swatch: string }> = [
  { label: 'Default', value: 'default', swatch: '#0f172a' },
  { label: 'Blue', value: 'blue', swatch: '#2563eb' },
  { label: 'Violet', value: 'violet', swatch: '#7c3aed' },
]

const currentLabel = computed(() => {
  const current = options.find((item) => item.value === themeMode.value)
  return current?.label ?? '主题'
})

const triggerLabel = computed(() => {
  if (themeMode.value === 'system') {
    return `跟随系统（${resolvedTheme.value === 'dark' ? '深色' : '浅色'}）`
  }

  return currentLabel.value
})

function toggleOpen() {
  open.value = !open.value
}

function selectTheme(nextTheme: ThemeMode) {
  setTheme(nextTheme)
  open.value = false
}

function selectThemeColor(nextThemeColor: ThemeColor) {
  setThemeColor(nextThemeColor)
  open.value = false
}

function handleClickOutside(event: MouseEvent) {
  const target = event.target as Node | null
  if (!rootRef.value || !target) {
    return
  }

  if (!rootRef.value.contains(target)) {
    open.value = false
  }
}

function handleKeydown(event: KeyboardEvent) {
  if (event.key === 'Escape') {
    open.value = false
  }
}

onMounted(() => {
  document.addEventListener('mousedown', handleClickOutside)
  document.addEventListener('keydown', handleKeydown)
})

onBeforeUnmount(() => {
  document.removeEventListener('mousedown', handleClickOutside)
  document.removeEventListener('keydown', handleKeydown)
})
</script>

<template>
  <div ref="rootRef" class="relative" :class="props.fullWidth ? 'w-full' : 'inline-block'">
    <UiButton
      variant="secondary"
      :class="props.fullWidth ? 'w-full justify-between' : 'h-10 w-10 px-0'"
      :aria-label="`主题：${triggerLabel}`"
      aria-haspopup="menu"
      :aria-expanded="open"
      @click="toggleOpen"
    >
      <span class="inline-flex items-center gap-2">
        <svg
          v-if="resolvedTheme === 'dark'"
          xmlns="http://www.w3.org/2000/svg"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          stroke-width="1.8"
          class="h-4 w-4"
        >
          <path d="M21 12.8A9 9 0 1 1 11.2 3a7 7 0 0 0 9.8 9.8Z" />
        </svg>
        <svg
          v-else
          xmlns="http://www.w3.org/2000/svg"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          stroke-width="1.8"
          class="h-4 w-4"
        >
          <circle cx="12" cy="12" r="4" />
          <path d="M12 2v2.5M12 19.5V22M4.9 4.9l1.8 1.8M17.3 17.3l1.8 1.8M2 12h2.5M19.5 12H22M4.9 19.1l1.8-1.8M17.3 6.7l1.8-1.8" />
        </svg>
        <span v-if="props.fullWidth" class="text-sm">{{ triggerLabel }}</span>
      </span>
    </UiButton>

    <div
      v-if="open"
      class="ui-card absolute z-50 mt-2 min-w-52 overflow-hidden rounded-md p-1 shadow-md"
      :class="props.fullWidth ? 'left-0 right-0' : 'right-0'"
      role="menu"
      aria-label="主题菜单"
    >
      <div class="px-2 pb-1 pt-1.5 text-xs font-medium text-muted">模式</div>
      <button
        v-for="item in options"
        :key="item.value"
        type="button"
        class="w-full rounded-sm px-2 py-1.5 text-left text-sm transition"
        :style="{
          backgroundColor: themeMode === item.value ? 'var(--accent)' : 'transparent',
          color: 'var(--fg)',
          fontWeight: themeMode === item.value ? '600' : '400',
        }"
        role="menuitemradio"
        :aria-checked="themeMode === item.value"
        @click="selectTheme(item.value)"
      >
        {{ item.label }}
      </button>

      <div class="my-1 border-t" />
      <div class="px-2 pb-1 pt-1.5 text-xs font-medium text-muted">主题</div>
      <button
        v-for="item in colorOptions"
        :key="item.value"
        type="button"
        class="flex w-full items-center justify-between rounded-sm px-2 py-1.5 text-left text-sm transition"
        :style="{
          backgroundColor: themeColor === item.value ? 'var(--accent)' : 'transparent',
          color: 'var(--fg)',
          fontWeight: themeColor === item.value ? '600' : '400',
        }"
        role="menuitemradio"
        :aria-checked="themeColor === item.value"
        @click="selectThemeColor(item.value)"
      >
        <span>{{ item.label }}</span>
        <span
          class="h-3 w-3 rounded-full border"
          :style="{ backgroundColor: item.swatch }"
          aria-hidden="true"
        />
      </button>
    </div>
  </div>
</template>
