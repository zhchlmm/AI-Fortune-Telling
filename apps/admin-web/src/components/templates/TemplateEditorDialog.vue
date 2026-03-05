<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import UiButton from '../ui/UiButton.vue'
import UiCard from '../ui/UiCard.vue'
import UiInput from '../ui/UiInput.vue'
import UiSelect from '../ui/UiSelect.vue'
import UiTextarea from '../ui/UiTextarea.vue'

type TemplateEditorMode = 'create' | 'edit'

type TemplateEditorPayload = {
  name: string
  fortuneType: string
  prompt: string
  isEnabled: boolean
}

const props = withDefaults(defineProps<{
  open: boolean
  mode: TemplateEditorMode
  initialValue?: TemplateEditorPayload
  submitting?: boolean
}>(), {
  submitting: false,
})

const emit = defineEmits<{
  (event: 'close'): void
  (event: 'submit', payload: TemplateEditorPayload): void
}>()

const name = ref('')
const fortuneType = ref('Tarot')
const prompt = ref('')
const isEnabled = ref(true)
const nameError = ref('')
const promptError = ref('')

const submitLabel = computed(() => {
  if (props.mode === 'create') {
    return props.submitting ? '创建中...' : '创建模板'
  }

  return props.submitting ? '保存中...' : '保存修改'
})

function handleKeydown(event: KeyboardEvent) {
  if (event.key !== 'Escape') {
    return
  }

  if (!props.open || props.submitting) {
    return
  }

  emit('close')
}

onMounted(() => {
  window.addEventListener('keydown', handleKeydown)
})

onBeforeUnmount(() => {
  window.removeEventListener('keydown', handleKeydown)
})

watch(
  () => [props.open, props.initialValue, props.mode],
  () => {
    if (!props.open) {
      return
    }

    name.value = props.initialValue?.name ?? ''
    fortuneType.value = props.initialValue?.fortuneType ?? 'Tarot'
    prompt.value = props.initialValue?.prompt ?? ''
    isEnabled.value = props.initialValue?.isEnabled ?? true
    nameError.value = ''
    promptError.value = ''
  },
  { immediate: true },
)

watch(name, () => {
  if (nameError.value && name.value.trim()) {
    nameError.value = ''
  }
})

watch(prompt, () => {
  if (promptError.value && prompt.value.trim()) {
    promptError.value = ''
  }
})

function handleSubmit() {
  nameError.value = ''
  promptError.value = ''

  if (!name.value.trim()) {
    nameError.value = '模板名称不能为空'
  }

  if (!prompt.value.trim()) {
    promptError.value = '提示词不能为空'
  }

  if (nameError.value || promptError.value) {
    return
  }

  emit('submit', {
    name: name.value.trim(),
    fortuneType: fortuneType.value,
    prompt: prompt.value.trim(),
    isEnabled: isEnabled.value,
  })
}
</script>

<template>
  <div
    v-if="open"
    class="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/50 p-4"
    @click.self="!submitting && emit('close')"
  >
    <UiCard class="w-full max-w-2xl p-5">
      <header class="mb-4">
        <h2 class="text-lg font-semibold">{{ mode === 'create' ? '新增模板' : '编辑模板' }}</h2>
        <p class="text-muted text-sm">维护模板信息与提示词内容</p>
      </header>

      <div class="grid gap-3 md:grid-cols-2">
        <UiInput v-model="name" placeholder="模板名称" />
        <UiSelect v-model="fortuneType">
          <option value="Tarot">Tarot</option>
          <option value="Zodiac">Zodiac</option>
        </UiSelect>
      </div>
      <p v-if="nameError" class="mt-2 text-sm text-rose-600">{{ nameError }}</p>

      <UiTextarea v-model="prompt" class="mt-3" placeholder="提示词" />
      <p v-if="promptError" class="mt-2 text-sm text-rose-600">{{ promptError }}</p>

      <div class="mt-3">
        <UiSelect v-model="isEnabled">
          <option :value="true">启用</option>
          <option :value="false">停用</option>
        </UiSelect>
      </div>

      <footer class="mt-5 flex justify-end gap-2">
        <UiButton variant="secondary" :disabled="submitting" @click="emit('close')">取消</UiButton>
        <UiButton :disabled="submitting" @click="handleSubmit">
          {{ submitLabel }}
        </UiButton>
      </footer>
    </UiCard>
  </div>
</template>
