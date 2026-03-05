<script setup lang="ts">
import { useToast } from '../../composables/useToast'

const { toastState } = useToast()
</script>

<template>
  <div
    v-if="toastState.visible"
    class="fixed right-4 top-4 z-[100] rounded-md px-4 py-2 text-sm shadow-lg"
    :class="toastState.type === 'success' ? 'bg-emerald-600 text-white' : 'bg-rose-600 text-white'"
  >
    {{ toastState.message }}
  </div>
</template>
