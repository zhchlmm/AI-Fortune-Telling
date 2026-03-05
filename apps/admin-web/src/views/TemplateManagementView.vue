<script setup lang="ts">
import { onMounted, ref } from 'vue'
import UiButton from '../components/ui/UiButton.vue'
import UiCard from '../components/ui/UiCard.vue'
import UiConfirmDialog from '../components/ui/UiConfirmDialog.vue'
import TemplateEditorDialog from '../components/templates/TemplateEditorDialog.vue'
import { showToast } from '../composables/useToast'
import {
  createTemplate,
  deleteTemplate,
  fetchTemplates,
  type FortuneTemplateDto,
  updateTemplate,
} from '../api/client'

const templates = ref<FortuneTemplateDto[]>([])
const editingId = ref<string | null>(null)
const dialogVisible = ref(false)
const dialogMode = ref<'create' | 'edit'>('create')
const dialogSubmitting = ref(false)
const deleteConfirmVisible = ref(false)
const deleteSubmitting = ref(false)
const deletingTemplate = ref<FortuneTemplateDto | null>(null)
const dialogInitialValue = ref({
  name: '',
  fortuneType: 'Tarot',
  prompt: '',
  isEnabled: true,
})

async function load() {
  try {
    templates.value = await fetchTemplates()
  } catch {
    showToast('加载模板失败，请稍后重试', 'error')
  }
}

onMounted(async () => {
  await load()
})

function openCreateDialog() {
  dialogMode.value = 'create'
  editingId.value = null
  dialogInitialValue.value = {
    name: '',
    fortuneType: 'Tarot',
    prompt: '',
    isEnabled: true,
  }
  dialogVisible.value = true
}

function openEditDialog(item: FortuneTemplateDto) {
  dialogMode.value = 'edit'
  editingId.value = item.id
  dialogInitialValue.value = {
    name: item.name,
    fortuneType: item.fortuneType,
    prompt: item.prompt,
    isEnabled: item.isEnabled,
  }
  dialogVisible.value = true
}

function closeDialog() {
  dialogVisible.value = false
}

async function submitDialog(payload: {
  name: string
  fortuneType: string
  prompt: string
  isEnabled: boolean
}) {
  dialogSubmitting.value = true
  try
  {
    if (dialogMode.value == 'create')
    {
      await createTemplate(payload)
      showToast('模板创建成功', 'success')
    }
    else
    {
      if (!editingId.value)
      {
        return
      }

      await updateTemplate(editingId.value, payload)
      showToast('模板更新成功', 'success')
    }

    closeDialog()
    await load()
  }
  catch
  {
    showToast(dialogMode.value === 'create' ? '模板创建失败' : '模板更新失败', 'error')
  }
  finally
  {
    dialogSubmitting.value = false
  }
}

function requestDelete(item: FortuneTemplateDto) {
  deletingTemplate.value = item
  deleteConfirmVisible.value = true
}

function closeDeleteConfirm() {
  if (deleteSubmitting.value) {
    return
  }

  deleteConfirmVisible.value = false
  deletingTemplate.value = null
}

async function confirmDelete() {
  if (!deletingTemplate.value) {
    return
  }

  deleteSubmitting.value = true
  try {
    await deleteTemplate(deletingTemplate.value.id)
    if (editingId.value === deletingTemplate.value.id) {
      closeDialog()
    }
    showToast('模板删除成功', 'success')
    closeDeleteConfirm()
    await load()
  } catch {
    showToast('模板删除失败', 'error')
  } finally {
    deleteSubmitting.value = false
  }
}
</script>

<template>
  <main class="space-y-4">
    <header>
      <h1 class="text-2xl font-semibold">占卜模板管理</h1>
      <p class="text-sm text-slate-500">维护可用模板与提示词</p>
    </header>

    <div>
      <UiButton @click="openCreateDialog">新增模板</UiButton>
    </div>

    <UiCard class="overflow-hidden">
      <table class="w-full text-sm">
        <thead class="bg-slate-100 text-slate-600">
          <tr>
            <th class="px-4 py-3 text-left font-medium">名称</th>
            <th class="px-4 py-3 text-left font-medium">类型</th>
            <th class="px-4 py-3 text-left font-medium">操作</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="item in templates" :key="item.id" class="border-t border-slate-200">
            <td class="px-4 py-3 font-medium">{{ item.name }}</td>
            <td class="px-4 py-3 text-slate-600">{{ item.fortuneType }}</td>
            <td class="px-4 py-3">
              <div class="flex gap-2">
                <UiButton variant="secondary" @click="openEditDialog(item)">编辑</UiButton>
                <UiButton variant="secondary" @click="requestDelete(item)">删除</UiButton>
              </div>
            </td>
          </tr>
          <tr v-if="templates.length === 0">
            <td class="px-4 py-6 text-center text-slate-500" colspan="3">暂无模板数据</td>
          </tr>
        </tbody>
      </table>
    </UiCard>

    <TemplateEditorDialog
      :open="dialogVisible"
      :mode="dialogMode"
      :initial-value="dialogInitialValue"
      :submitting="dialogSubmitting"
      @close="closeDialog"
      @submit="submitDialog"
    />

    <UiConfirmDialog
      :open="deleteConfirmVisible"
      title="确认删除模板"
      :description="deletingTemplate ? `确认删除模板「${deletingTemplate.name}」吗？此操作不可撤销。` : ''"
      confirm-text="确认删除"
      cancel-text="取消"
      :loading="deleteSubmitting"
      @close="closeDeleteConfirm"
      @confirm="confirmDelete"
    />
  </main>
</template>
