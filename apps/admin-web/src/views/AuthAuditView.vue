<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import UiButton from '../components/ui/UiButton.vue'
import UiCard from '../components/ui/UiCard.vue'
import UiInput from '../components/ui/UiInput.vue'
import UiPagination from '../components/ui/UiPagination.vue'
import UiSelect from '../components/ui/UiSelect.vue'
import { showToast } from '../composables/useToast'
import { exportLoginAuditsCsv, fetchPagedLoginAudits, type LoginAuditDto } from '../api/client'
import { formatBeijingDateTime, toUtcTimestamp } from '../utils/datetime'

const audits = ref<LoginAuditDto[]>([])
const username = ref('')
const status = ref<'all' | 'success' | 'failed'>('all')
const fromTime = ref('')
const toTime = ref('')
const page = ref(1)
const pageSize = ref(20)
const total = ref(0)
const sortBy = ref<'createdAt' | 'username' | 'isSuccess'>('createdAt')
const sortOrder = ref<'asc' | 'desc'>('desc')

const sortedAudits = computed(() => {
  const items = [...audits.value]
  items.sort((left, right) => {
    let compare = 0
    if (sortBy.value === 'createdAt') {
      compare = toUtcTimestamp(left.createdAt) - toUtcTimestamp(right.createdAt)
    } else if (sortBy.value === 'username') {
      compare = left.username.localeCompare(right.username)
    } else {
      compare = Number(left.isSuccess) - Number(right.isSuccess)
    }

    return sortOrder.value === 'asc' ? compare : -compare
  })

  return items
})

function toUtcIso(value: string) {
  if (!value) {
    return undefined
  }

  const date = new Date(value)
  if (Number.isNaN(date.getTime())) {
    return undefined
  }

  return date.toISOString()
}

function resolveIsSuccessFilter() {
  if (status.value === 'success') {
    return true
  }
  if (status.value === 'failed') {
    return false
  }
  return undefined
}

function getFilterParams() {
  return {
    username: username.value || undefined,
    isSuccess: resolveIsSuccessFilter(),
    fromUtc: toUtcIso(fromTime.value),
    toUtc: toUtcIso(toTime.value),
  }
}

async function load() {
  try {
    const data = await fetchPagedLoginAudits({
      ...getFilterParams(),
      page: page.value,
      pageSize: pageSize.value,
    })

    audits.value = data.items
    total.value = data.total
  } catch {
    showToast('加载审计数据失败，请稍后重试', 'error')
  }
}

onMounted(async () => {
  await load()
})

async function search() {
  page.value = 1
  await load()
}

async function resetFilters() {
  username.value = ''
  status.value = 'all'
  fromTime.value = ''
  toTime.value = ''
  page.value = 1
  await load()
}

async function handlePageChange(nextPage: number) {
  page.value = nextPage
  await load()
}

async function handlePageSizeChange(nextPageSize: number) {
  pageSize.value = nextPageSize
  page.value = 1
  await load()
}

async function exportCsv() {
  try {
    const blob = await exportLoginAuditsCsv(getFilterParams())
    const url = window.URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url
    link.download = `login-audits-${new Date().toISOString().replace(/[:.]/g, '-')}.csv`
    document.body.appendChild(link)
    link.click()
    document.body.removeChild(link)
    window.URL.revokeObjectURL(url)
    showToast('审计CSV导出成功', 'success')
  } catch {
    showToast('审计CSV导出失败，请稍后重试', 'error')
  }
}

function formatDateTime(value: string) {
  return formatBeijingDateTime(value)
}

function toggleSort(column: 'createdAt' | 'username' | 'isSuccess') {
  if (sortBy.value === column) {
    sortOrder.value = sortOrder.value === 'asc' ? 'desc' : 'asc'
    return
  }

  sortBy.value = column
  sortOrder.value = column === 'createdAt' ? 'desc' : 'asc'
}

function sortLabel(column: 'createdAt' | 'username' | 'isSuccess') {
  if (sortBy.value !== column) {
    return ''
  }

  return sortOrder.value === 'asc' ? '↑' : '↓'
}
</script>

<template>
  <main class="space-y-4">
    <header>
      <h1 class="text-2xl font-semibold">登录审计日志</h1>
      <p class="text-muted text-sm">查询与导出管理员登录行为</p>
    </header>

    <UiCard class="p-4">
      <div class="flex flex-wrap items-center gap-2">
        <UiInput
          v-model="username"
          class="max-w-xs"
          placeholder="按用户名筛选（可选）"
          @keyup.enter="search"
        />
        <UiSelect v-model="status">
        <option value="all">全部结果</option>
        <option value="success">仅成功</option>
        <option value="failed">仅失败</option>
      </UiSelect>
        <UiInput v-model="fromTime" class="w-auto" type="datetime-local" @keyup.enter="search" />
        <UiInput v-model="toTime" class="w-auto" type="datetime-local" @keyup.enter="search" />
        <UiButton @click="search">查询</UiButton>
        <UiButton variant="secondary" @click="resetFilters">重置</UiButton>
        <UiButton variant="secondary" @click="exportCsv">导出 CSV</UiButton>
      </div>
    </UiCard>

    <UiCard class="overflow-hidden" v-if="audits.length > 0">
      <div class="max-h-[520px] overflow-auto hidden md:block">
        <table class="w-full text-sm">
          <thead class="sticky top-0 z-10 bg-slate-100 text-slate-600">
            <tr>
              <th class="px-4 py-3 text-left font-medium">
                <button class="text-left font-medium" @click="toggleSort('createdAt')">
                  时间 {{ sortLabel('createdAt') }}
                </button>
              </th>
              <th class="px-4 py-3 text-left font-medium">
                <button class="text-left font-medium" @click="toggleSort('username')">
                  用户名 {{ sortLabel('username') }}
                </button>
              </th>
              <th class="px-4 py-3 text-left font-medium">
                <button class="text-left font-medium" @click="toggleSort('isSuccess')">
                  状态 {{ sortLabel('isSuccess') }}
                </button>
              </th>
              <th class="px-4 py-3 text-left font-medium">原因</th>
              <th class="px-4 py-3 text-left font-medium">IP</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="audit in sortedAudits" :key="audit.id" class="border-t border-slate-200">
              <td class="px-4 py-3 text-slate-600">{{ formatDateTime(audit.createdAt) }}</td>
              <td class="px-4 py-3 font-medium">{{ audit.username }}</td>
              <td class="px-4 py-3">
                <span :class="audit.isSuccess ? 'ui-badge-success' : 'ui-badge-danger'">
                  {{ audit.isSuccess ? '成功' : '失败' }}
                </span>
              </td>
              <td class="px-4 py-3">{{ audit.reason }}</td>
              <td class="px-4 py-3 text-slate-600">{{ audit.ipAddress }}</td>
            </tr>
          </tbody>
        </table>
      </div>

      <div class="space-y-3 p-3 md:hidden" v-if="sortedAudits.length > 0">
        <div v-for="audit in sortedAudits" :key="audit.id" class="rounded-lg border p-3">
          <p class="text-xs text-slate-500">{{ formatDateTime(audit.createdAt) }}</p>
          <p class="mt-1 text-sm font-medium">{{ audit.username }}</p>
          <p class="mt-2 text-sm">{{ audit.reason }}</p>
          <div class="mt-2 flex items-center justify-between">
            <span :class="audit.isSuccess ? 'ui-badge-success' : 'ui-badge-danger'">
              {{ audit.isSuccess ? '成功' : '失败' }}
            </span>
            <span class="text-xs text-slate-500">{{ audit.ipAddress }}</span>
          </div>
        </div>
      </div>
    </UiCard>
    <p v-else class="ui-card p-5 text-sm text-slate-500">暂无审计数据</p>

    <UiPagination
      class="justify-stretch md:justify-end"
      :page="page"
      :page-size="pageSize"
      :total="total"
      @change-page="handlePageChange"
      @change-page-size="handlePageSizeChange"
    />
  </main>
</template>
