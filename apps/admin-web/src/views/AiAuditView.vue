<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import UiButton from '../components/ui/UiButton.vue'
import UiCard from '../components/ui/UiCard.vue'
import UiInput from '../components/ui/UiInput.vue'
import UiPagination from '../components/ui/UiPagination.vue'
import UiSelect from '../components/ui/UiSelect.vue'
import { showToast } from '../composables/useToast'
import {
  fetchPagedAiAudits,
  fetchAiAuditTypeDistribution,
  type AiAuditDto,
  type AiAuditTypeDistributionDto,
} from '../api/client'
import { formatBeijingDateTime, toUtcTimestamp } from '../utils/datetime'

const audits = ref<AiAuditDto[]>([])
const fortuneType = ref('')
const status = ref<'all' | 'normal' | 'degraded'>('all')
const fromTime = ref('')
const toTime = ref('')
const page = ref(1)
const pageSize = ref(20)
const total = ref(0)
const typeDistribution = ref<AiAuditTypeDistributionDto[]>([])
const sortBy = ref<'createdAt' | 'fortuneType' | 'elapsedMs'>('createdAt')
const sortOrder = ref<'asc' | 'desc'>('desc')

const sortedAudits = computed(() => {
  const items = [...audits.value]
  items.sort((left, right) => {
    let compare = 0
    if (sortBy.value === 'createdAt') {
      compare = toUtcTimestamp(left.createdAt) - toUtcTimestamp(right.createdAt)
    } else if (sortBy.value === 'fortuneType') {
      compare = left.fortuneType.localeCompare(right.fortuneType)
    } else {
      compare = left.elapsedMs - right.elapsedMs
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

function resolveDegradedFilter() {
  if (status.value === 'degraded') {
    return true
  }
  if (status.value === 'normal') {
    return false
  }
  return undefined
}

async function load() {
  const params = {
    fortuneType: fortuneType.value || undefined,
    degraded: resolveDegradedFilter(),
    fromUtc: toUtcIso(fromTime.value),
    toUtc: toUtcIso(toTime.value),
  }

  try {
    const data = await fetchPagedAiAudits({
      ...params,
      page: page.value,
      pageSize: pageSize.value,
    })

    audits.value = data.items
    total.value = data.total

    const distribution = await fetchAiAuditTypeDistribution(params)
    typeDistribution.value = distribution
  } catch {
    showToast('加载AI审计失败，请稍后重试', 'error')
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
  fortuneType.value = ''
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

function formatDateTime(value: string) {
  return formatBeijingDateTime(value)
}

function toggleSort(column: 'createdAt' | 'fortuneType' | 'elapsedMs') {
  if (sortBy.value === column) {
    sortOrder.value = sortOrder.value === 'asc' ? 'desc' : 'asc'
    return
  }

  sortBy.value = column
  sortOrder.value = column === 'createdAt' ? 'desc' : 'asc'
}

function sortLabel(column: 'createdAt' | 'fortuneType' | 'elapsedMs') {
  if (sortBy.value !== column) {
    return ''
  }

  return sortOrder.value === 'asc' ? '↑' : '↓'
}
</script>

<template>
  <main class="space-y-4">
    <header>
      <h1 class="text-2xl font-semibold">AI审计日志</h1>
      <p class="text-muted text-sm">查看模型调用、耗时与降级情况</p>
    </header>

    <UiCard class="p-4">
      <div class="flex flex-wrap items-center gap-2">
        <UiInput
          v-model="fortuneType"
          class="max-w-xs"
          placeholder="按类型筛选（如 Tarot）"
          @keyup.enter="search"
        />
        <UiSelect v-model="status">
          <option value="all">全部状态</option>
          <option value="normal">仅正常</option>
          <option value="degraded">仅降级</option>
        </UiSelect>
        <UiInput v-model="fromTime" class="w-auto" type="datetime-local" @keyup.enter="search" />
        <UiInput v-model="toTime" class="w-auto" type="datetime-local" @keyup.enter="search" />
        <UiButton @click="search">查询</UiButton>
        <UiButton variant="secondary" @click="resetFilters">重置</UiButton>
      </div>
    </UiCard>

    <UiCard class="overflow-hidden" v-if="typeDistribution.length > 0">
      <div class="overflow-auto hidden md:block">
        <table class="w-full text-sm">
          <thead class="bg-slate-100 text-slate-600">
            <tr>
              <th class="px-4 py-3 text-left font-medium">类型</th>
              <th class="px-4 py-3 text-left font-medium">调用量</th>
              <th class="px-4 py-3 text-left font-medium">降级量</th>
              <th class="px-4 py-3 text-left font-medium">降级率</th>
              <th class="px-4 py-3 text-left font-medium">平均耗时(ms)</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="item in typeDistribution" :key="item.fortuneType" class="border-t border-slate-200">
              <td class="px-4 py-3 font-medium">{{ item.fortuneType }}</td>
              <td class="px-4 py-3">{{ item.totalCalls }}</td>
              <td class="px-4 py-3">{{ item.degradedCalls }}</td>
              <td class="px-4 py-3">{{ item.degradedRate.toFixed(2) }}%</td>
              <td class="px-4 py-3">{{ item.avgElapsedMs.toFixed(2) }}</td>
            </tr>
          </tbody>
        </table>
      </div>

      <div class="space-y-2 p-3 md:hidden">
        <div v-for="item in typeDistribution" :key="item.fortuneType" class="rounded-lg border p-3">
          <p class="text-sm font-medium">{{ item.fortuneType }}</p>
          <p class="mt-1 text-xs text-slate-500">
            调用 {{ item.totalCalls }} / 降级 {{ item.degradedCalls }} / 降级率 {{ item.degradedRate.toFixed(2) }}% / 均耗时 {{ item.avgElapsedMs.toFixed(2) }}ms
          </p>
        </div>
      </div>
    </UiCard>

    <UiCard class="overflow-hidden" v-if="audits.length > 0">
      <div class="max-h-130 overflow-auto hidden md:block">
        <table class="w-full text-sm">
          <thead class="sticky top-0 z-10 bg-slate-100 text-slate-600">
            <tr>
              <th class="px-4 py-3 text-left font-medium">
                <button class="text-left font-medium" @click="toggleSort('createdAt')">
                  时间 {{ sortLabel('createdAt') }}
                </button>
              </th>
              <th class="px-4 py-3 text-left font-medium">
                <button class="text-left font-medium" @click="toggleSort('fortuneType')">
                  类型 {{ sortLabel('fortuneType') }}
                </button>
              </th>
              <th class="px-4 py-3 text-left font-medium">模型</th>
              <th class="px-4 py-3 text-left font-medium">状态</th>
              <th class="px-4 py-3 text-left font-medium">
                <button class="text-left font-medium" @click="toggleSort('elapsedMs')">
                  耗时(ms) {{ sortLabel('elapsedMs') }}
                </button>
              </th>
              <th class="px-4 py-3 text-left font-medium">原因</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="audit in sortedAudits" :key="audit.id" class="border-t border-slate-200">
              <td class="px-4 py-3 text-slate-600">{{ formatDateTime(audit.createdAt) }}</td>
              <td class="px-4 py-3 font-medium">{{ audit.fortuneType }}</td>
              <td class="px-4 py-3 text-slate-600">{{ audit.model || '-' }}</td>
              <td class="px-4 py-3">
                <span :class="audit.degraded ? 'ui-badge-danger' : 'ui-badge-success'">
                  {{ audit.degraded ? '降级' : '正常' }}
                </span>
              </td>
              <td class="px-4 py-3">{{ audit.elapsedMs }}</td>
              <td class="px-4 py-3">{{ audit.reason }}</td>
            </tr>
          </tbody>
        </table>
      </div>

      <div class="space-y-3 p-3 md:hidden" v-if="sortedAudits.length > 0">
        <div v-for="audit in sortedAudits" :key="audit.id" class="rounded-lg border p-3">
          <p class="text-xs text-slate-500">{{ formatDateTime(audit.createdAt) }}</p>
          <p class="mt-1 text-sm font-medium">{{ audit.fortuneType }} · {{ audit.model || '-' }}</p>
          <p class="mt-2 text-sm">原因：{{ audit.reason }}</p>
          <div class="mt-2 flex items-center justify-between">
            <span :class="audit.degraded ? 'ui-badge-danger' : 'ui-badge-success'">
              {{ audit.degraded ? '降级' : '正常' }}
            </span>
            <span class="text-xs text-slate-500">{{ audit.elapsedMs }}ms</span>
          </div>
        </div>
      </div>
    </UiCard>
    <p v-else class="ui-card p-5 text-sm text-slate-500">暂无AI审计数据</p>

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
