<script setup lang="ts">
import { computed } from 'vue'
import UiButton from './UiButton.vue'

const props = withDefaults(defineProps<{
  page: number
  pageSize: number
  total: number
  pageSizeOptions?: number[]
}>(), {
  pageSizeOptions: () => [10, 20, 50],
})

const emit = defineEmits<{
  (event: 'change-page', value: number): void
  (event: 'change-page-size', value: number): void
}>()

const totalPages = computed(() => Math.max(1, Math.ceil(props.total / props.pageSize)))
const canPrev = computed(() => props.page > 1)
const canNext = computed(() => props.page < totalPages.value)

function goPrev() {
  if (!canPrev.value) {
    return
  }
  emit('change-page', props.page - 1)
}

function goNext() {
  if (!canNext.value) {
    return
  }
  emit('change-page', props.page + 1)
}

function onPageSizeChange(event: Event) {
  const target = event.target as HTMLSelectElement | null
  if (!target) {
    return
  }

  const next = Number.parseInt(target.value, 10)
  if (Number.isNaN(next)) {
    return
  }

  emit('change-page-size', next)
}
</script>

<template>
  <div class="grid gap-2 sm:flex sm:flex-wrap sm:items-center" v-bind="$attrs">
    <div class="flex items-center justify-between gap-2 sm:justify-start">
      <div class="flex items-center gap-2">
        <span class="text-sm text-slate-500">每页</span>
        <select class="ui-select h-9 w-20" :value="pageSize" @change="onPageSizeChange">
          <option v-for="option in pageSizeOptions" :key="option" :value="option">{{ option }}</option>
        </select>
      </div>
      <span class="text-xs text-slate-500 whitespace-nowrap sm:text-sm">第 {{ page }} 页 / 共 {{ total }} 条</span>
    </div>
    <div class="grid grid-cols-2 gap-2 sm:flex sm:items-center">
      <UiButton class="w-full sm:w-auto" variant="secondary" @click="goPrev" :disabled="!canPrev">上一页</UiButton>
      <UiButton class="w-full sm:w-auto" variant="secondary" @click="goNext" :disabled="!canNext">下一页</UiButton>
    </div>
  </div>
</template>
