<script setup lang="ts">
import { computed, useAttrs } from 'vue'

const modelValue = defineModel<string | null | undefined>()

defineOptions({
  inheritAttrs: false,
})

const attrs = useAttrs()
const mergedClass = computed(() => ['ui-textarea', attrs.class].filter(Boolean))

function handleInput(event: Event) {
  const target = event.target as HTMLTextAreaElement | null
  modelValue.value = target?.value ?? ''
}
</script>

<template>
  <textarea
    v-bind="attrs"
    :class="mergedClass"
    :value="modelValue ?? ''"
    @input="handleInput"
  ></textarea>
</template>
