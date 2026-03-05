<script setup lang="ts">
import { computed, useAttrs } from 'vue'

type Variant = 'primary' | 'secondary'

const props = withDefaults(defineProps<{
  variant?: Variant
}>(), {
  variant: 'primary',
})

defineOptions({
  inheritAttrs: false,
})

const attrs = useAttrs()
const mergedClass = computed(() => [
  props.variant === 'primary' ? 'ui-btn-primary' : 'ui-btn-secondary',
  attrs.class,
].filter(Boolean))
</script>

<template>
  <button v-bind="attrs" :class="mergedClass">
    <slot />
  </button>
</template>
