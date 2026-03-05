<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import UiButton from '../components/ui/UiButton.vue'
import UiCard from '../components/ui/UiCard.vue'
import UiInput from '../components/ui/UiInput.vue'
import UiPagination from '../components/ui/UiPagination.vue'
import { showToast } from '../composables/useToast'
import { fetchPagedFortuneSessions, type FortuneSessionDto } from '../api/client'
import { formatBeijingDateTime, toUtcTimestamp } from '../utils/datetime'

const sessions = ref<FortuneSessionDto[]>([])
const userId = ref('')
const page = ref(1)
const pageSize = ref(10)
const total = ref(0)
const sortBy = ref<'createdAt' | 'userId' | 'fortuneType'>('createdAt')
const sortOrder = ref<'asc' | 'desc'>('desc')
const previewVisible = ref(false)
const previewUrl = ref('')

const FORTUNE_TYPE_LABEL_MAP: Record<string, string> = {
  Tarot: '塔罗',
  Zodiac: '星座',
  Bazi: '八字',
  Palmistry: '手相',
  Physiognomy: '面相',
  FengShui: '风水',
}

const sortedSessions = computed(() => {
  const items = [...sessions.value]
  items.sort((left, right) => {
    let compare = 0
    if (sortBy.value === 'createdAt') {
      compare = toUtcTimestamp(left.createdAt) - toUtcTimestamp(right.createdAt)
    } else if (sortBy.value === 'userId') {
      compare = left.userId.localeCompare(right.userId)
    } else {
      compare = left.fortuneType.localeCompare(right.fortuneType)
    }

    return sortOrder.value === 'asc' ? compare : -compare
  })

  return items
})

async function load() {
  try {
    const data = await fetchPagedFortuneSessions({
      userId: userId.value || undefined,
      page: page.value,
      pageSize: pageSize.value,
    })

    sessions.value = data.items
    total.value = data.total
  } catch {
    showToast('加载会话数据失败，请稍后重试', 'error')
  }
}

onMounted(async () => {
  await load()
})

async function performSearch() {
  page.value = 1
  await load()
}

async function resetFilters() {
  userId.value = ''
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

function toggleSort(column: 'createdAt' | 'userId' | 'fortuneType') {
  if (sortBy.value === column) {
    sortOrder.value = sortOrder.value === 'asc' ? 'desc' : 'asc'
    return
  }

  sortBy.value = column
  sortOrder.value = column === 'createdAt' ? 'desc' : 'asc'
}

function sortLabel(column: 'createdAt' | 'userId' | 'fortuneType') {
  if (sortBy.value !== column) {
    return ''
  }

  return sortOrder.value === 'asc' ? '↑' : '↓'
}

function formatDateTime(value: string) {
  return formatBeijingDateTime(value)
}

function formatFortuneType(value: string) {
  return FORTUNE_TYPE_LABEL_MAP[value] ?? value
}

function openPreview(url: string) {
  previewUrl.value = url
  previewVisible.value = true
}

function closePreview() {
  previewVisible.value = false
  previewUrl.value = ''
}

function parseInputSummary(inputSummary: string) {
  const summary: Record<string, string> = {}
  const segments = inputSummary.split(';').map((item) => item.trim()).filter(Boolean)

  for (const segment of segments) {
    const separatorIndex = segment.indexOf('=')
    if (separatorIndex <= 0) {
      continue
    }

    const key = segment.slice(0, separatorIndex).trim()
    const value = segment.slice(separatorIndex + 1).trim()
    summary[key] = value
  }

  return summary
}

function getQuestion(inputSummary: string) {
  const parsed = parseInputSummary(inputSummary)
  return parsed.question ?? '-'
}

function getPhotoUrls(inputSummary: string) {
  const parsed = parseInputSummary(inputSummary)
  const raw = parsed.photoUrls
  if (!raw) {
    return [] as string[]
  }

  try {
    const list = JSON.parse(raw) as unknown
    if (Array.isArray(list)) {
      return list.filter((item): item is string => typeof item === 'string' && item.length > 0)
    }
  } catch {
    return []
  }

  return []
}
</script>

<template>
  <main class="space-y-4">
    <header>
      <h1 class="text-2xl font-semibold">会话查询</h1>
      <p class="text-muted text-sm">按用户查询占卜会话</p>
    </header>

    <UiCard class="p-4">
      <div class="flex flex-wrap items-center gap-2">
        <UiInput
          v-model="userId"
          class="max-w-xs"
          placeholder="按用户ID筛选（可选）"
          @keyup.enter="performSearch"
        />
        <UiButton @click="performSearch">查询</UiButton>
        <UiButton variant="secondary" @click="resetFilters">重置</UiButton>
      </div>
    </UiCard>

    <UiCard class="overflow-hidden">
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
              <button class="text-left font-medium" @click="toggleSort('userId')">
                用户ID {{ sortLabel('userId') }}
              </button>
            </th>
            <th class="px-4 py-3 text-left font-medium">
              <button class="text-left font-medium" @click="toggleSort('fortuneType')">
                类型 {{ sortLabel('fortuneType') }}
              </button>
            </th>
            <th class="px-4 py-3 text-left font-medium">提问</th>
            <th class="px-4 py-3 text-left font-medium">照片</th>
            <th class="px-4 py-3 text-left font-medium">结果摘要</th>
          </tr>
          </thead>
          <tbody>
            <tr v-for="session in sortedSessions" :key="session.id" class="border-t border-slate-200">
              <td class="px-4 py-3 text-slate-600">{{ formatDateTime(session.createdAt) }}</td>
              <td class="px-4 py-3 font-medium">{{ session.userId }}</td>
              <td class="px-4 py-3 text-slate-600">{{ formatFortuneType(session.fortuneType) }}</td>
              <td class="px-4 py-3 text-slate-600">{{ getQuestion(session.inputSummary) }}</td>
              <td class="px-4 py-3">
                <div class="flex flex-wrap gap-2" v-if="getPhotoUrls(session.inputSummary).length > 0">
                  <button
                    v-for="url in getPhotoUrls(session.inputSummary)"
                    :key="url"
                    type="button"
                    class="block"
                    @click="openPreview(url)"
                  >
                    <img :src="url" alt="会话照片" class="h-12 w-12 rounded object-cover border border-slate-200" />
                  </button>
                </div>
                <span v-else class="text-slate-400">-</span>
              </td>
              <td class="px-4 py-3">{{ session.resultSummary }}</td>
            </tr>
            <tr v-if="sortedSessions.length === 0">
              <td class="px-4 py-6 text-center text-slate-500" colspan="6">暂无会话数据</td>
            </tr>
          </tbody>
        </table>
      </div>

      <div class="space-y-3 p-3 md:hidden" v-if="sortedSessions.length > 0">
        <div v-for="session in sortedSessions" :key="session.id" class="rounded-lg border p-3">
          <p class="text-xs text-slate-500">{{ formatDateTime(session.createdAt) }}</p>
          <p class="mt-1 text-sm font-medium">{{ session.userId }} · {{ formatFortuneType(session.fortuneType) }}</p>
          <p class="mt-1 text-xs text-slate-500">问题：{{ getQuestion(session.inputSummary) }}</p>
          <div class="mt-2 flex flex-wrap gap-2" v-if="getPhotoUrls(session.inputSummary).length > 0">
            <button
              v-for="url in getPhotoUrls(session.inputSummary)"
              :key="url"
              type="button"
              @click="openPreview(url)"
            >
              <img :src="url" alt="会话照片" class="h-12 w-12 rounded object-cover border border-slate-200" />
            </button>
          </div>
          <p class="mt-2 text-sm">{{ session.resultSummary }}</p>
        </div>
      </div>

      <p v-else class="p-4 text-sm text-slate-500 md:hidden">暂无会话数据</p>
    </UiCard>

    <UiPagination
      class="justify-stretch md:justify-end"
      :page="page"
      :page-size="pageSize"
      :total="total"
      @change-page="handlePageChange"
      @change-page-size="handlePageSizeChange"
    />

    <div
      v-if="previewVisible"
      class="fixed inset-0 z-50 flex items-center justify-center bg-black/70 p-4"
      @click="closePreview"
    >
      <img
        :src="previewUrl"
        alt="预览图片"
        class="max-h-full max-w-full rounded-lg bg-white"
        @click.stop
      />
      <button
        type="button"
        class="absolute right-4 top-4 rounded-full bg-white/90 px-3 py-1 text-sm"
        @click.stop="closePreview"
      >
        关闭
      </button>
    </div>
  </main>
</template>
