<script setup lang="ts">
import UiButton from './UiButton.vue'
import UiCard from './UiCard.vue'

const props = withDefaults(defineProps<{
  open: boolean
  title?: string
  description?: string
  confirmText?: string
  cancelText?: string
  loading?: boolean
}>(), {
  title: '确认操作',
  description: '',
  confirmText: '确认',
  cancelText: '取消',
  loading: false,
})

const emit = defineEmits<{
  (event: 'close'): void
  (event: 'confirm'): void
}>()
</script>

<template>
  <div
    v-if="open"
    class="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/50 p-4"
    @click.self="!loading && emit('close')"
  >
    <UiCard class="w-full max-w-md p-5">
      <h2 class="text-lg font-semibold">{{ title }}</h2>
      <p class="text-muted mt-2 text-sm">{{ description }}</p>
      <div class="mt-5 flex justify-end gap-2">
        <UiButton variant="secondary" :disabled="loading" @click="emit('close')">{{ cancelText }}</UiButton>
        <UiButton :disabled="loading" @click="emit('confirm')">
          {{ loading ? '处理中...' : confirmText }}
        </UiButton>
      </div>
    </UiCard>
  </div>
</template>
