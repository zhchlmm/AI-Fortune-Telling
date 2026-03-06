<script setup lang="ts">
import { onMounted, ref } from 'vue'
import {
  blockMiniappUser,
  fetchMiniappUserDetail,
  fetchPagedMiniappUsers,
  unblockMiniappUser,
  type AdminMiniappUserDetailDto,
  type AdminMiniappUserDto,
} from '../api/client'
import UiButton from '../components/ui/UiButton.vue'
import UiCard from '../components/ui/UiCard.vue'
import UiConfirmDialog from '../components/ui/UiConfirmDialog.vue'
import UiInput from '../components/ui/UiInput.vue'
import UiPagination from '../components/ui/UiPagination.vue'
import UiSelect from '../components/ui/UiSelect.vue'
import { showToast } from '../composables/useToast'
import { formatBeijingDateTime } from '../utils/datetime'

const users = ref<AdminMiniappUserDto[]>([])
const detail = ref<AdminMiniappUserDetailDto | null>(null)
const detailLoading = ref(false)
const tableLoading = ref(false)
const actionLoading = ref(false)

const keyword = ref('')
const blockFilter = ref<'all' | 'blocked' | 'normal'>('all')
const page = ref(1)
const pageSize = ref(10)
const total = ref(0)

const confirmOpen = ref(false)
const confirmText = ref('')
const targetUserId = ref('')
const targetAction = ref<'block' | 'unblock'>('block')

function toBlockedQueryValue() {
  if (blockFilter.value === 'blocked') {
    return true
  }

  if (blockFilter.value === 'normal') {
    return false
  }

  return undefined
}

async function loadUsers() {
  tableLoading.value = true
  try {
    const data = await fetchPagedMiniappUsers({
      keyword: keyword.value || undefined,
      isBlocked: toBlockedQueryValue(),
      page: page.value,
      pageSize: pageSize.value,
    })
    users.value = data.items
    total.value = data.total
  } catch {
    showToast('加载小程序用户失败，请稍后重试', 'error')
  } finally {
    tableLoading.value = false
  }
}

async function performSearch() {
  page.value = 1
  await loadUsers()
}

async function resetFilters() {
  keyword.value = ''
  blockFilter.value = 'all'
  page.value = 1
  await loadUsers()
}

async function handlePageChange(nextPage: number) {
  page.value = nextPage
  await loadUsers()
}

async function handlePageSizeChange(nextPageSize: number) {
  pageSize.value = nextPageSize
  page.value = 1
  await loadUsers()
}

async function showDetail(id: string) {
  detailLoading.value = true
  try {
    detail.value = await fetchMiniappUserDetail(id)
  } catch {
    showToast('加载用户详情失败', 'error')
  } finally {
    detailLoading.value = false
  }
}

function openBlockConfirm(id: string) {
  targetUserId.value = id
  targetAction.value = 'block'
  confirmText.value = '确认封禁该小程序用户？封禁后用户无法修改资料。'
  confirmOpen.value = true
}

function openUnblockConfirm(id: string) {
  targetUserId.value = id
  targetAction.value = 'unblock'
  confirmText.value = '确认解封该小程序用户？解封后用户可继续更新资料。'
  confirmOpen.value = true
}

function closeConfirm() {
  if (actionLoading.value) {
    return
  }

  confirmOpen.value = false
  targetUserId.value = ''
}

async function submitAction() {
  if (!targetUserId.value) {
    return
  }

  actionLoading.value = true
  try {
    if (targetAction.value === 'block') {
      await blockMiniappUser(targetUserId.value)
      showToast('封禁成功', 'success')
    } else {
      await unblockMiniappUser(targetUserId.value)
      showToast('解封成功', 'success')
    }

    confirmOpen.value = false
    await loadUsers()

    if (detail.value?.id === targetUserId.value) {
      await showDetail(targetUserId.value)
    }
  } catch {
    showToast('操作失败，请稍后重试', 'error')
  } finally {
    actionLoading.value = false
  }
}

function formatDateTime(value?: string) {
  if (!value) {
    return '-'
  }

  return formatBeijingDateTime(value)
}

onMounted(async () => {
  await loadUsers()
})
</script>

<template>
  <main class="space-y-4">
    <header>
      <h1 class="text-2xl font-semibold">小程序用户管理</h1>
      <p class="text-muted text-sm">查询、查看并封禁或解封小程序用户</p>
    </header>

    <UiCard class="p-4">
      <div class="flex flex-wrap items-center gap-2">
        <UiInput
          v-model="keyword"
          class="max-w-xs"
          placeholder="按 openId / 昵称 / 手机号 / 邮箱筛选"
          @keyup.enter="performSearch"
        />
        <UiSelect v-model="blockFilter" class="w-36">
          <option value="all">全部状态</option>
          <option value="normal">正常</option>
          <option value="blocked">已封禁</option>
        </UiSelect>
        <UiButton @click="performSearch">查询</UiButton>
        <UiButton variant="secondary" @click="resetFilters">重置</UiButton>
      </div>
    </UiCard>

    <div class="grid gap-4 lg:grid-cols-[2fr_1fr]">
      <UiCard class="overflow-hidden">
        <div class="max-h-[560px] overflow-auto hidden md:block">
          <table class="w-full text-sm">
            <thead class="sticky top-0 z-10 bg-slate-100 text-slate-600">
              <tr>
                <th class="px-4 py-3 text-left font-medium">OpenId</th>
                <th class="px-4 py-3 text-left font-medium">昵称</th>
                <th class="px-4 py-3 text-left font-medium">手机号</th>
                <th class="px-4 py-3 text-left font-medium">状态</th>
                <th class="px-4 py-3 text-left font-medium">最近更新</th>
                <th class="px-4 py-3 text-left font-medium">操作</th>
              </tr>
            </thead>
            <tbody>
              <tr v-if="tableLoading">
                <td class="px-4 py-6 text-center text-slate-500" colspan="6">加载中...</td>
              </tr>
              <tr v-for="item in users" :key="item.id" class="border-t border-slate-200">
                <td class="px-4 py-3 font-mono text-xs">{{ item.openId }}</td>
                <td class="px-4 py-3">{{ item.nickname || '-' }}</td>
                <td class="px-4 py-3">{{ item.phoneNumber || '-' }}</td>
                <td class="px-4 py-3">
                  <span :class="item.isBlocked ? 'text-rose-600' : 'text-emerald-600'">
                    {{ item.isBlocked ? '已封禁' : '正常' }}
                  </span>
                </td>
                <td class="px-4 py-3 text-slate-600">{{ formatDateTime(item.updatedAt) }}</td>
                <td class="px-4 py-3">
                  <div class="flex flex-wrap gap-2">
                    <UiButton variant="secondary" class="px-3" @click="showDetail(item.id)">详情</UiButton>
                    <UiButton
                      v-if="!item.isBlocked"
                      variant="secondary"
                      class="px-3"
                      @click="openBlockConfirm(item.id)"
                    >
                      封禁
                    </UiButton>
                    <UiButton
                      v-else
                      class="px-3"
                      @click="openUnblockConfirm(item.id)"
                    >
                      解封
                    </UiButton>
                  </div>
                </td>
              </tr>
              <tr v-if="!tableLoading && users.length === 0">
                <td class="px-4 py-6 text-center text-slate-500" colspan="6">暂无用户数据</td>
              </tr>
            </tbody>
          </table>
        </div>

        <div class="space-y-3 p-3 md:hidden" v-if="users.length > 0">
          <div v-for="item in users" :key="item.id" class="rounded-lg border p-3">
            <p class="font-mono text-xs text-slate-500">{{ item.openId }}</p>
            <p class="mt-1 text-sm">昵称：{{ item.nickname || '-' }}</p>
            <p class="mt-1 text-sm">手机号：{{ item.phoneNumber || '-' }}</p>
            <p class="mt-1 text-sm">状态：{{ item.isBlocked ? '已封禁' : '正常' }}</p>
            <p class="mt-1 text-xs text-slate-500">更新：{{ formatDateTime(item.updatedAt) }}</p>
            <div class="mt-2 flex gap-2">
              <UiButton variant="secondary" class="px-3" @click="showDetail(item.id)">详情</UiButton>
              <UiButton
                v-if="!item.isBlocked"
                variant="secondary"
                class="px-3"
                @click="openBlockConfirm(item.id)"
              >
                封禁
              </UiButton>
              <UiButton v-else class="px-3" @click="openUnblockConfirm(item.id)">解封</UiButton>
            </div>
          </div>
        </div>

        <p v-else class="p-4 text-sm text-slate-500 md:hidden">暂无用户数据</p>
      </UiCard>

      <UiCard class="p-4">
        <h2 class="text-lg font-semibold">用户详情</h2>
        <p class="text-muted mt-1 text-sm">点击列表中的“详情”查看</p>

        <div v-if="detailLoading" class="mt-4 text-sm text-slate-500">详情加载中...</div>

        <dl v-else-if="detail" class="mt-4 space-y-2 text-sm">
          <div>
            <dt class="text-slate-500">OpenId</dt>
            <dd class="font-mono text-xs break-all">{{ detail.openId }}</dd>
          </div>
          <div>
            <dt class="text-slate-500">昵称</dt>
            <dd>{{ detail.nickname || '-' }}</dd>
          </div>
          <div>
            <dt class="text-slate-500">邮箱</dt>
            <dd>{{ detail.email || '-' }}</dd>
          </div>
          <div>
            <dt class="text-slate-500">手机号</dt>
            <dd>{{ detail.phoneNumber || '-' }}</dd>
          </div>
          <div>
            <dt class="text-slate-500">头像</dt>
            <dd class="break-all">{{ detail.avatar || '-' }}</dd>
          </div>
          <div>
            <dt class="text-slate-500">状态</dt>
            <dd :class="detail.isBlocked ? 'text-rose-600' : 'text-emerald-600'">
              {{ detail.isBlocked ? '已封禁' : '正常' }}
            </dd>
          </div>
          <div>
            <dt class="text-slate-500">封禁时间</dt>
            <dd>{{ formatDateTime(detail.blockedAt) }}</dd>
          </div>
          <div>
            <dt class="text-slate-500">创建时间</dt>
            <dd>{{ formatDateTime(detail.createdAt) }}</dd>
          </div>
          <div>
            <dt class="text-slate-500">更新时间</dt>
            <dd>{{ formatDateTime(detail.updatedAt) }}</dd>
          </div>
        </dl>

        <p v-else class="mt-4 text-sm text-slate-500">暂未选择用户</p>
      </UiCard>
    </div>

    <UiPagination
      class="justify-stretch md:justify-end"
      :page="page"
      :page-size="pageSize"
      :total="total"
      @change-page="handlePageChange"
      @change-page-size="handlePageSizeChange"
    />

    <UiConfirmDialog
      :open="confirmOpen"
      :description="confirmText"
      :loading="actionLoading"
      :confirm-text="targetAction === 'block' ? '确认封禁' : '确认解封'"
      @close="closeConfirm"
      @confirm="submitAction"
    />
  </main>
</template>
