<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { fetchAiAuditSummary, fetchHealth } from '../api/client'

const health = ref('loading')
const aiTotalCalls = ref('-')
const aiDegradedRate = ref('-')
const aiAvgElapsed = ref('-')

onMounted(async () => {
  try {
    const data = await fetchHealth()
    health.value = `${data.status} @ ${data.timestamp}`
  } catch {
    health.value = 'unavailable'
  }

  try {
    const summary = await fetchAiAuditSummary(60)
    aiTotalCalls.value = String(summary.totalCalls)
    aiDegradedRate.value = `${summary.degradedRate.toFixed(2)}%`
    aiAvgElapsed.value = `${summary.avgElapsedMs.toFixed(2)} ms`
  } catch {
    aiTotalCalls.value = 'unavailable'
    aiDegradedRate.value = 'unavailable'
    aiAvgElapsed.value = 'unavailable'
  }
})
</script>

<template>
  <main class="space-y-4">
    <header>
      <h1 class="text-2xl font-semibold">仪表盘</h1>
      <p class="text-sm text-slate-500">后台系统概览</p>
    </header>
    <section class="grid gap-4 md:grid-cols-2">
      <article class="ui-card p-5">
        <h2 class="text-sm text-slate-500">API 健康状态</h2>
        <p class="mt-2 text-base font-medium">{{ health }}</p>
      </article>
      <article class="ui-card p-5">
        <h2 class="text-sm text-slate-500">当前模块</h2>
        <p class="mt-2 text-base font-medium">认证 / 模板 / 会话 / 审计</p>
      </article>
    </section>

    <section class="grid gap-4 md:grid-cols-3">
      <article class="ui-card p-5">
        <h2 class="text-sm text-slate-500">近1小时 AI 调用量</h2>
        <p class="mt-2 text-base font-medium">{{ aiTotalCalls }}</p>
      </article>
      <article class="ui-card p-5">
        <h2 class="text-sm text-slate-500">近1小时降级率</h2>
        <p class="mt-2 text-base font-medium">{{ aiDegradedRate }}</p>
      </article>
      <article class="ui-card p-5">
        <h2 class="text-sm text-slate-500">近1小时平均耗时</h2>
        <p class="mt-2 text-base font-medium">{{ aiAvgElapsed }}</p>
      </article>
    </section>
  </main>
</template>
