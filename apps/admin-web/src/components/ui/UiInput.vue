<script setup lang="ts">
import { computed, useAttrs } from 'vue'

const modelValue = defineModel<string | number | null | undefined>()

defineOptions({
  inheritAttrs: false,
})

const attrs = useAttrs()
const mergedClass = computed(() => ['ui-input', attrs.class].filter(Boolean))

function handleInput(event: Event) {
  const target = event.target as HTMLInputElement | null
  modelValue.value = target?.value ?? ''
}
</script>

<template>
  <input
    v-bind="attrs"
    :class="mergedClass"
    :value="modelValue ?? ''"
    @input="handleInput"
  />
</template>
