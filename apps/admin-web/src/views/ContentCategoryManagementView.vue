<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import UiButton from '../components/ui/UiButton.vue'
import UiCard from '../components/ui/UiCard.vue'
import UiConfirmDialog from '../components/ui/UiConfirmDialog.vue'
import UiInput from '../components/ui/UiInput.vue'
import UiPagination from '../components/ui/UiPagination.vue'
import UiSelect from '../components/ui/UiSelect.vue'
import { showToast } from '../composables/useToast'
import {
  createContentCategory,
  deleteContentCategory,
  fetchContentCategories,
  type ContentCategoryDto,
  updateContentCategory,
} from '../api/client'

const categories = ref<ContentCategoryDto[]>([])
const keyword = ref('')
const statusFilter = ref<'all' | 'enabled' | 'disabled'>('all')
const page = ref(1)
const pageSize = ref(10)

const nameInput = ref('')
const sortOrderInput = ref('10')
const enabledInput = ref(true)
const editingId = ref<string | null>(null)
const submitting = ref(false)
const editorVisible = ref(false)

const deleteConfirmVisible = ref(false)
const deletingItem = ref<ContentCategoryDto | null>(null)
const deleteSubmitting = ref(false)

const filteredCategories = computed(() => {
  return categories.value.filter((item) => {
    const byKeyword = !keyword.value.trim() || item.name.includes(keyword.value.trim())
    const byStatus = statusFilter.value === 'all'
      || (statusFilter.value === 'enabled' ? item.isEnabled : !item.isEnabled)
    return byKeyword && byStatus
  })
})

const pagedCategories = computed(() => {
  const start = (page.value - 1) * pageSize.value
  return filteredCategories.value.slice(start, start + pageSize.value)
})

async function loadCategories() {
  try {
    categories.value = await fetchContentCategories()
  } catch {
    showToast('加载分类失败，请稍后重试', 'error')
  }
}

onMounted(async () => {
  await loadCategories()
})

function resetEditor() {
  editingId.value = null
  nameInput.value = ''
  sortOrderInput.value = '10'
  enabledInput.value = true
}

function openCreateDialog() {
  resetEditor()
  editorVisible.value = true
}

function openEdit(item: ContentCategoryDto) {
  editingId.value = item.id
  nameInput.value = item.name
  sortOrderInput.value = String(item.sortOrder)
  enabledInput.value = item.isEnabled
  editorVisible.value = true
}

function closeEditor() {
  if (submitting.value) {
    return
  }

  editorVisible.value = false
}

async function submit() {
  const name = nameInput.value.trim()
  const sortOrder = Number(sortOrderInput.value)

  if (!name) {
    showToast('分类名称不能为空', 'error')
    return
  }

  if (!Number.isInteger(sortOrder)) {
    showToast('排序值必须为整数', 'error')
    return
  }

  submitting.value = true
  try {
    if (editingId.value) {
      await updateContentCategory(editingId.value, {
        name,
        sortOrder,
        isEnabled: enabledInput.value,
      })
      showToast('分类更新成功', 'success')
    } else {
      await createContentCategory({
        name,
        sortOrder,
        isEnabled: enabledInput.value,
      })
      showToast('分类创建成功', 'success')
    }

    editorVisible.value = false
    resetEditor()
    await loadCategories()
  } catch {
    showToast(editingId.value ? '分类更新失败' : '分类创建失败', 'error')
  } finally {
    submitting.value = false
  }
}

function requestDelete(item: ContentCategoryDto) {
  deletingItem.value = item
  deleteConfirmVisible.value = true
}

function closeDeleteConfirm() {
  if (deleteSubmitting.value) {
    return
  }

  deleteConfirmVisible.value = false
  deletingItem.value = null
}

async function confirmDelete() {
  if (!deletingItem.value) {
    return
  }

  deleteSubmitting.value = true
  try {
    await deleteContentCategory(deletingItem.value.id)
    showToast('分类删除成功', 'success')
    closeDeleteConfirm()
    if (editingId.value === deletingItem.value.id) {
      resetEditor()
    }
    await loadCategories()
  } catch {
    showToast('分类删除失败（可能已被内容引用）', 'error')
  } finally {
    deleteSubmitting.value = false
  }
}

function search() {
  page.value = 1
}

function resetFilters() {
  keyword.value = ''
  statusFilter.value = 'all'
  page.value = 1
}

function handlePageChange(nextPage: number) {
  page.value = nextPage
}

function handlePageSizeChange(nextPageSize: number) {
  pageSize.value = nextPageSize
  page.value = 1
}
</script>

<template>
  <main class="space-y-4">
    <header>
      <h1 class="text-2xl font-semibold">内容分类管理</h1>
      <p class="text-sm text-slate-500">维护资讯分类，用于内容归档和前端筛选展示</p>
    </header>

    <UiCard class="p-4 space-y-3">
      <div class="flex items-center justify-between">
        <h2 class="text-base font-semibold">分类编辑</h2>
        <UiButton variant="secondary" @click="openCreateDialog">新建分类</UiButton>
      </div>
      <p class="text-sm text-slate-500">点击“新建分类”或列表“编辑”按钮，在弹窗中维护分类信息。</p>
    </UiCard>

    <UiCard class="p-4">
      <div class="grid gap-2 md:flex md:flex-wrap md:items-center">
        <UiInput
          v-model="keyword"
          class="w-full md:max-w-xs"
          placeholder="按分类名称搜索"
          @keyup.enter="search"
        />
        <UiSelect v-model="statusFilter" class="w-full md:w-auto">
          <option value="all">全部状态</option>
          <option value="enabled">仅启用</option>
          <option value="disabled">仅停用</option>
        </UiSelect>
        <div class="grid grid-cols-2 gap-2 md:flex md:items-center">
          <UiButton class="w-full md:w-auto" @click="search">查询</UiButton>
          <UiButton class="w-full md:w-auto" variant="secondary" @click="resetFilters">重置</UiButton>
        </div>
      </div>
    </UiCard>

    <UiCard class="overflow-hidden">
      <table class="w-full text-sm">
        <thead class="bg-slate-100 text-slate-600">
          <tr>
            <th class="px-4 py-3 text-left font-medium">分类名称</th>
            <th class="px-4 py-3 text-left font-medium">排序</th>
            <th class="px-4 py-3 text-left font-medium">状态</th>
            <th class="px-4 py-3 text-left font-medium">操作</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="item in pagedCategories" :key="item.id" class="border-t border-slate-200">
            <td class="px-4 py-3 font-medium">{{ item.name }}</td>
            <td class="px-4 py-3 text-slate-600">{{ item.sortOrder }}</td>
            <td class="px-4 py-3">
              <span :class="item.isEnabled ? 'ui-badge-success' : 'ui-badge-danger'">
                {{ item.isEnabled ? '启用' : '停用' }}
              </span>
            </td>
            <td class="px-4 py-3">
              <div class="flex gap-2">
                <UiButton variant="secondary" @click="openEdit(item)">编辑</UiButton>
                <UiButton variant="secondary" @click="requestDelete(item)">删除</UiButton>
              </div>
            </td>
          </tr>
          <tr v-if="pagedCategories.length === 0">
            <td class="px-4 py-6 text-center text-slate-500" colspan="4">暂无分类数据</td>
          </tr>
        </tbody>
      </table>
    </UiCard>

    <UiPagination
      class="justify-stretch md:justify-end"
      :page="page"
      :page-size="pageSize"
      :total="filteredCategories.length"
      @change-page="handlePageChange"
      @change-page-size="handlePageSizeChange"
    />

    <UiConfirmDialog
      :open="deleteConfirmVisible"
      title="确认删除分类"
      :description="deletingItem ? `确认删除分类「${deletingItem.name}」吗？` : ''"
      confirm-text="确认删除"
      cancel-text="取消"
      :loading="deleteSubmitting"
      @close="closeDeleteConfirm"
      @confirm="confirmDelete"
    />

    <div
      v-if="editorVisible"
      class="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/50 p-4"
      @click.self="closeEditor"
    >
      <UiCard class="w-full max-w-xl p-5">
        <header class="mb-4">
          <h2 class="text-lg font-semibold">{{ editingId ? '编辑分类' : '新增分类' }}</h2>
          <p class="text-sm text-slate-500">维护分类名称、排序和启用状态</p>
        </header>
        <div class="space-y-3">
          <UiInput v-model="nameInput" placeholder="分类名称" />
          <UiInput v-model="sortOrderInput" placeholder="排序值（整数）" />
          <UiSelect v-model="enabledInput">
            <option :value="true">启用</option>
            <option :value="false">停用</option>
          </UiSelect>
        </div>
        <footer class="mt-5 flex justify-end gap-2">
          <UiButton variant="secondary" :disabled="submitting" @click="closeEditor">取消</UiButton>
          <UiButton :disabled="submitting" @click="submit">
            {{ submitting ? '处理中...' : editingId ? '保存分类' : '创建分类' }}
          </UiButton>
        </footer>
      </UiCard>
    </div>
  </main>
</template>
