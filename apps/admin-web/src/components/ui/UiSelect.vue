<script setup lang="ts">
import { computed, useAttrs } from 'vue'

const modelValue = defineModel<unknown>()

defineOptions({
  inheritAttrs: false,
})

const attrs = useAttrs()
const mergedClass = computed(() => ['ui-select', attrs.class].filter(Boolean))

function handleChange(event: Event) {
  const target = event.target as HTMLSelectElement | null
  if (!target) {
    return
  }

  const option = target.selectedOptions?.[0] as (HTMLOptionElement & { _value?: unknown }) | undefined
  modelValue.value = option?._value ?? target.value
}
</script>

<template>
  <select
    v-bind="attrs"
    :class="mergedClass"
    :value="modelValue as string | number | undefined"
    @change="handleChange"
  >
    <slot />
  </select>
</template>
