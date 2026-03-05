<script setup lang="ts">
import { ref, watch } from 'vue'
import UiButton from '../ui/UiButton.vue'
import UiCard from '../ui/UiCard.vue'
import UiInput from '../ui/UiInput.vue'
import UiSelect from '../ui/UiSelect.vue'
import UiTextarea from '../ui/UiTextarea.vue'

type ContentEditorMode = 'create' | 'edit'

type ContentEditorPayload = {
  title: string
  summary: string
  content: string
  categoryId: string
  isPublished: boolean
}

type CategoryOption = {
  id: string
  name: string
}

const props = withDefaults(defineProps<{
  open: boolean
  mode: ContentEditorMode
  categories: CategoryOption[]
  initialValue?: ContentEditorPayload
  submitting?: boolean
}>(), {
  submitting: false,
})

const emit = defineEmits<{
  (event: 'close'): void
  (event: 'submit', payload: ContentEditorPayload): void
}>()

const title = ref('')
const summary = ref('')
const content = ref('')
const categoryId = ref('')
const isPublished = ref(true)
const titleError = ref('')
const summaryError = ref('')
const contentError = ref('')
const categoryError = ref('')

watch(
  () => [props.open, props.initialValue, props.mode],
  () => {
    if (!props.open) {
      return
    }

    title.value = props.initialValue?.title ?? ''
    summary.value = props.initialValue?.summary ?? ''
    content.value = props.initialValue?.content ?? ''
    categoryId.value = props.initialValue?.categoryId ?? props.categories[0]?.id ?? ''
    isPublished.value = props.initialValue?.isPublished ?? true
    titleError.value = ''
    summaryError.value = ''
    contentError.value = ''
    categoryError.value = ''
  },
  { immediate: true },
)

watch(title, () => {
  if (titleError.value && title.value.trim()) {
    titleError.value = ''
  }
})

watch(summary, () => {
  if (summaryError.value && summary.value.trim()) {
    summaryError.value = ''
  }
})

watch(content, () => {
  if (contentError.value && content.value.trim()) {
    contentError.value = ''
  }
})

watch(categoryId, () => {
  if (categoryError.value && categoryId.value) {
    categoryError.value = ''
  }
})

function handleSubmit() {
  titleError.value = ''
  summaryError.value = ''
  contentError.value = ''
  categoryError.value = ''

  if (!title.value.trim()) {
    titleError.value = '标题不能为空'
  }

  if (!summary.value.trim()) {
    summaryError.value = '摘要不能为空'
  }

  if (!content.value.trim()) {
    contentError.value = '正文不能为空'
  }

  if (!categoryId.value) {
    categoryError.value = '请选择分类'
  }

  if (titleError.value || summaryError.value || contentError.value || categoryError.value) {
    return
  }

  emit('submit', {
    title: title.value.trim(),
    summary: summary.value.trim(),
    content: content.value.trim(),
    categoryId: categoryId.value,
    isPublished: isPublished.value,
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
        <h2 class="text-lg font-semibold">{{ mode === 'create' ? '新增内容' : '编辑内容' }}</h2>
        <p class="text-muted text-sm">维护内容标题、摘要与发布状态</p>
      </header>

      <UiInput v-model="title" placeholder="内容标题" />
      <p v-if="titleError" class="mt-2 text-sm text-rose-600">{{ titleError }}</p>

      <UiTextarea v-model="summary" class="mt-3" placeholder="内容摘要" />
      <p v-if="summaryError" class="mt-2 text-sm text-rose-600">{{ summaryError }}</p>

      <div class="mt-3">
        <UiSelect v-model="categoryId">
          <option disabled value="">请选择分类</option>
          <option v-for="item in categories" :key="item.id" :value="item.id">{{ item.name }}</option>
        </UiSelect>
      </div>
      <p v-if="categoryError" class="mt-2 text-sm text-rose-600">{{ categoryError }}</p>

      <UiTextarea v-model="content" class="mt-3 min-h-56" placeholder="请输入富文本HTML内容，例如：&lt;p&gt;正文&lt;/p&gt;" />
      <p v-if="contentError" class="mt-2 text-sm text-rose-600">{{ contentError }}</p>

      <div class="mt-3 rounded-lg border border-slate-200 bg-slate-50 p-3">
        <p class="mb-2 text-xs text-slate-500">正文预览</p>
        <div class="prose prose-sm max-w-none" v-html="content"></div>
      </div>

      <div class="mt-3">
        <UiSelect v-model="isPublished">
          <option :value="true">已发布</option>
          <option :value="false">未发布</option>
        </UiSelect>
      </div>

      <footer class="mt-5 flex justify-end gap-2">
        <UiButton variant="secondary" :disabled="submitting" @click="emit('close')">取消</UiButton>
        <UiButton :disabled="submitting" @click="handleSubmit">
          {{ submitting ? '处理中...' : mode === 'create' ? '创建内容' : '保存修改' }}
        </UiButton>
      </footer>
    </UiCard>
  </div>
</template>
