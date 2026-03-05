<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import ContentEditorDialog from '../components/contents/ContentEditorDialog.vue'
import UiButton from '../components/ui/UiButton.vue'
import UiCard from '../components/ui/UiCard.vue'
import UiConfirmDialog from '../components/ui/UiConfirmDialog.vue'
import UiInput from '../components/ui/UiInput.vue'
import UiPagination from '../components/ui/UiPagination.vue'
import UiSelect from '../components/ui/UiSelect.vue'
import { showToast } from '../composables/useToast'
import {
  formatBeijingCompactDateTime,
  formatBeijingDateTime,
  toUtcTimestamp,
} from '../utils/datetime'
import {
  createContent,
  deleteContent,
  fetchAdminContentDetail,
  fetchContentCategories,
  fetchAdminContents,
  type ContentCategoryDto,
  type ContentItemDto,
  updateContent,
} from '../api/client'

const contents = ref<ContentItemDto[]>([])
const categories = ref<ContentCategoryDto[]>([])
const keyword = ref('')
const publishFilter = ref<'all' | 'published' | 'unpublished'>('all')
const categoryFilter = ref('all')
const page = ref(1)
const pageSize = ref(10)
const sortBy = ref<'publishedAt' | 'title' | 'isPublished'>('publishedAt')
const sortOrder = ref<'asc' | 'desc'>('desc')
const dialogVisible = ref(false)
const dialogMode = ref<'create' | 'edit'>('create')
const dialogSubmitting = ref(false)
const editingId = ref<string | null>(null)
const dialogInitialValue = ref({
  title: '',
  summary: '',
  content: '',
  categoryId: '',
  isPublished: true,
})

const detailVisible = ref(false)
const detailLoading = ref(false)
const detailItem = ref<ContentItemDto | null>(null)

const deleteConfirmVisible = ref(false)
const deleteSubmitting = ref(false)
const deletingItem = ref<ContentItemDto | null>(null)

const categoryOptions = computed(() => categories.value.map((item) => ({ id: item.id, name: item.name })))

const sortedContents = computed(() => {
  const items = [...contents.value]
  items.sort((left, right) => {
    let compare = 0
    if (sortBy.value === 'publishedAt') {
      compare = toUtcTimestamp(left.publishedAt) - toUtcTimestamp(right.publishedAt)
    } else if (sortBy.value === 'title') {
      compare = left.title.localeCompare(right.title)
    } else {
      compare = Number(left.isPublished) - Number(right.isPublished)
    }

    return sortOrder.value === 'asc' ? compare : -compare
  })

  return items
})

const pagedContents = computed(() => {
  const start = (page.value - 1) * pageSize.value
  return sortedContents.value.slice(start, start + pageSize.value)
})

async function load() {
  try {
    contents.value = await fetchAdminContents({
      keyword: keyword.value.trim() || undefined,
      categoryId: categoryFilter.value === 'all' ? undefined : categoryFilter.value,
      isPublished: publishFilter.value === 'all'
        ? undefined
        : publishFilter.value === 'published',
    })
  } catch {
    showToast('加载内容失败，请稍后重试', 'error')
  }
}

async function loadCategories() {
  try {
    categories.value = await fetchContentCategories()
    if (categoryFilter.value !== 'all' && !categories.value.some((x) => x.id === categoryFilter.value)) {
      categoryFilter.value = 'all'
    }
  } catch {
    showToast('加载分类失败，请稍后重试', 'error')
  }
}

onMounted(async () => {
  await Promise.all([loadCategories(), load()])
})

watch([sortBy, sortOrder], () => {
  page.value = 1
})

function openCreateDialog() {
  dialogMode.value = 'create'
  editingId.value = null
  dialogInitialValue.value = {
    title: '',
    summary: '',
    content: '',
    categoryId: categories.value[0]?.id ?? '',
    isPublished: true,
  }
  dialogVisible.value = true
}

async function search() {
  page.value = 1
  await load()
}

async function resetFilters() {
  keyword.value = ''
  publishFilter.value = 'all'
  categoryFilter.value = 'all'
  page.value = 1
  await load()
}

function openEditDialog(item: ContentItemDto) {
  dialogMode.value = 'edit'
  editingId.value = item.id
  dialogInitialValue.value = {
    title: item.title,
    summary: item.summary,
    content: item.content,
    categoryId: item.categoryId ?? '',
    isPublished: item.isPublished,
  }
  dialogVisible.value = true
}

function closeDialog() {
  dialogVisible.value = false
}

async function submitDialog(payload: {
  title: string
  summary: string
  content: string
  categoryId: string
  isPublished: boolean
}) {
  dialogSubmitting.value = true
  try {
    if (dialogMode.value === 'create') {
      await createContent(payload)
      showToast('内容创建成功', 'success')
    } else {
      if (!editingId.value) {
        return
      }

      await updateContent(editingId.value, payload)
      showToast('内容更新成功', 'success')
    }

    closeDialog()
    await load()
  } catch {
    showToast(dialogMode.value === 'create' ? '内容创建失败' : '内容更新失败', 'error')
  } finally {
    dialogSubmitting.value = false
  }
}

function requestDelete(item: ContentItemDto) {
  deletingItem.value = item
  deleteConfirmVisible.value = true
}

function closeDeleteConfirm() {
  if (deleteSubmitting.value) {
    return
  }

  deletingItem.value = null
  deleteConfirmVisible.value = false
}

async function confirmDelete() {
  if (!deletingItem.value) {
    return
  }

  deleteSubmitting.value = true
  try {
    await deleteContent(deletingItem.value.id)
    showToast('内容删除成功', 'success')
    closeDeleteConfirm()
    await load()
  } catch {
    showToast('内容删除失败', 'error')
  } finally {
    deleteSubmitting.value = false
  }
}

function openDetail(item: ContentItemDto) {
  detailVisible.value = true
  void loadDetail(item.id)
}

function closeDetail() {
  detailVisible.value = false
  detailItem.value = null
}

async function loadDetail(id: string) {
  detailLoading.value = true
  try {
    detailItem.value = await fetchAdminContentDetail(id)
  } catch {
    showToast('加载详情失败', 'error')
  } finally {
    detailLoading.value = false
  }
}

function formatDateTime(value: string) {
  return formatBeijingDateTime(value)
}

function formatCompactDateTime(value: string) {
  return formatBeijingCompactDateTime(value)
}

function toggleSort(column: 'publishedAt' | 'title' | 'isPublished') {
  if (sortBy.value === column) {
    sortOrder.value = sortOrder.value === 'asc' ? 'desc' : 'asc'
    return
  }

  sortBy.value = column
  sortOrder.value = column === 'publishedAt' ? 'desc' : 'asc'
}

function sortLabel(column: 'publishedAt' | 'title' | 'isPublished') {
  if (sortBy.value !== column) {
    return ''
  }

  return sortOrder.value === 'asc' ? '↑' : '↓'
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
      <h1 class="text-2xl font-semibold">内容管理</h1>
      <p class="text-sm text-slate-500">资讯发布与运营配置</p>
    </header>

    <div class="flex flex-wrap gap-2">
      <UiButton @click="openCreateDialog">新增内容</UiButton>
      <RouterLink to="/content-categories">
        <UiButton variant="secondary">前往分类管理</UiButton>
      </RouterLink>
    </div>

    <UiCard class="p-4">
      <div class="grid gap-2 md:flex md:flex-wrap md:items-center">
        <UiInput
          v-model="keyword"
          class="w-full md:max-w-xs"
          placeholder="按标题或摘要搜索"
          @keyup.enter="search"
        />
        <UiSelect v-model="publishFilter" class="w-full md:w-auto">
          <option value="all">全部状态</option>
          <option value="published">仅已发布</option>
          <option value="unpublished">仅未发布</option>
        </UiSelect>
        <UiSelect v-model="categoryFilter" class="w-full md:w-auto">
          <option value="all">全部分类</option>
          <option v-for="item in categories" :key="item.id" :value="item.id">{{ item.name }}</option>
        </UiSelect>
        <div class="grid grid-cols-2 gap-2 md:flex md:items-center">
          <UiButton class="w-full md:w-auto" @click="search">查询</UiButton>
          <UiButton class="w-full md:w-auto" variant="secondary" @click="resetFilters">重置</UiButton>
        </div>
      </div>
    </UiCard>

    <UiCard class="p-4 md:hidden">
      <div class="grid grid-cols-1 gap-2 sm:grid-cols-2">
        <label class="space-y-1">
          <span class="text-xs text-slate-500">排序字段</span>
          <UiSelect v-model="sortBy" class="w-full">
            <option value="publishedAt">发布时间</option>
            <option value="title">标题</option>
            <option value="isPublished">发布状态</option>
          </UiSelect>
        </label>
        <label class="space-y-1">
          <span class="text-xs text-slate-500">排序方向</span>
          <UiSelect v-model="sortOrder" class="w-full">
            <option value="desc">降序</option>
            <option value="asc">升序</option>
          </UiSelect>
        </label>
      </div>
    </UiCard>

    <UiCard class="overflow-hidden">
      <div class="md:hidden divide-y divide-slate-200">
        <div
          v-for="item in pagedContents"
          :key="item.id"
          class="space-y-2 p-3 md:space-y-3 md:p-4"
        >
          <div class="flex items-start">
            <button
              class="min-w-0 flex-1 truncate text-left text-sm font-semibold text-slate-900 hover:text-blue-600"
              @click="openDetail(item)"
            >
              {{ item.title }}
            </button>
          </div>
          <p class="overflow-hidden text-sm text-slate-600 [display:-webkit-box] [-webkit-box-orient:vertical] [-webkit-line-clamp:2]">
            {{ item.summary }}
          </p>
          <div class="flex flex-wrap items-center gap-2 text-xs text-slate-500">
            <span class="ui-badge-warning">{{ item.categoryName }}</span>
            <span :class="item.isPublished ? 'ui-badge-success' : 'ui-badge-danger'">
              {{ item.isPublished ? '已发布' : '未发布' }}
            </span>
            <span class="ml-auto whitespace-nowrap">发布时间：{{ formatCompactDateTime(item.publishedAt) }}</span>
          </div>
          <div class="grid grid-cols-2 gap-2">
            <UiButton class="w-full" variant="secondary" @click="openEditDialog(item)">编辑</UiButton>
            <UiButton class="w-full" variant="secondary" @click="requestDelete(item)">删除</UiButton>
          </div>
        </div>
        <div v-if="pagedContents.length === 0" class="p-4">
          <div class="rounded-lg border border-dashed border-slate-300 bg-slate-50 px-4 py-8 text-center text-sm text-slate-500">
            暂无内容数据
          </div>
        </div>
      </div>

      <table class="hidden w-full text-sm md:table">
        <thead class="bg-slate-100 text-slate-600">
          <tr>
            <th class="px-4 py-3 text-left font-medium">
              <button class="text-left font-medium" @click="toggleSort('title')">
                标题 {{ sortLabel('title') }}
              </button>
            </th>
            <th class="px-4 py-3 text-left font-medium">摘要</th>
            <th class="px-4 py-3 text-left font-medium">分类</th>
            <th class="px-4 py-3 text-left font-medium">
              <button class="text-left font-medium" @click="toggleSort('isPublished')">
                发布状态 {{ sortLabel('isPublished') }}
              </button>
            </th>
            <th class="px-4 py-3 text-left font-medium">
              <button class="text-left font-medium" @click="toggleSort('publishedAt')">
                发布时间 {{ sortLabel('publishedAt') }}
              </button>
            </th>
            <th class="px-4 py-3 text-left font-medium">操作</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="item in pagedContents" :key="item.id" class="border-t border-slate-200">
            <td class="px-4 py-3 font-medium">
              <button class="text-left hover:text-blue-600" @click="openDetail(item)">{{ item.title }}</button>
            </td>
            <td class="px-4 py-3 text-slate-600">{{ item.summary }}</td>
            <td class="px-4 py-3 text-slate-600">{{ item.categoryName }}</td>
            <td class="px-4 py-3">
              <span :class="item.isPublished ? 'ui-badge-success' : 'ui-badge-danger'">
                {{ item.isPublished ? '已发布' : '未发布' }}
              </span>
            </td>
            <td class="px-4 py-3 text-slate-600">{{ formatDateTime(item.publishedAt) }}</td>
            <td class="px-4 py-3">
              <div class="flex gap-2">
                <UiButton variant="secondary" @click="openEditDialog(item)">编辑</UiButton>
                <UiButton variant="secondary" @click="requestDelete(item)">删除</UiButton>
              </div>
            </td>
          </tr>
          <tr v-if="pagedContents.length === 0">
            <td class="px-4 py-6 text-center text-slate-500" colspan="6">暂无内容数据</td>
          </tr>
        </tbody>
      </table>
    </UiCard>

    <UiPagination
      class="justify-stretch md:justify-end"
      :page="page"
      :page-size="pageSize"
      :total="sortedContents.length"
      @change-page="handlePageChange"
      @change-page-size="handlePageSizeChange"
    />

    <ContentEditorDialog
      :open="dialogVisible"
      :mode="dialogMode"
      :categories="categoryOptions"
      :initial-value="dialogInitialValue"
      :submitting="dialogSubmitting"
      @close="closeDialog"
      @submit="submitDialog"
    />

    <div
      v-if="detailVisible"
      class="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/50 p-4"
      @click.self="closeDetail"
    >
      <UiCard class="w-full max-w-3xl p-5">
        <header class="mb-3 flex items-center justify-between">
          <h2 class="text-lg font-semibold">内容详情</h2>
          <UiButton variant="secondary" @click="closeDetail">关闭</UiButton>
        </header>
        <div v-if="detailLoading" class="py-12 text-center text-slate-500">加载中...</div>
        <div v-else-if="detailItem" class="space-y-3">
          <h3 class="text-xl font-semibold">{{ detailItem.title }}</h3>
          <div class="text-sm text-slate-500">分类：{{ detailItem.categoryName }} · 发布时间：{{ formatDateTime(detailItem.publishedAt) }}</div>
          <p class="rounded-md bg-slate-50 p-3 text-sm text-slate-700">{{ detailItem.summary }}</p>
          <article class="rounded-md border border-slate-200 p-4 text-sm leading-7 text-slate-800" v-html="detailItem.content"></article>
        </div>
      </UiCard>
    </div>

    <UiConfirmDialog
      :open="deleteConfirmVisible"
      title="确认删除内容"
      :description="deletingItem ? `确认删除内容「${deletingItem.title}」吗？此操作不可撤销。` : ''"
      confirm-text="确认删除"
      cancel-text="取消"
      :loading="deleteSubmitting"
      @close="closeDeleteConfirm"
      @confirm="confirmDelete"
    />
  </main>
</template>
