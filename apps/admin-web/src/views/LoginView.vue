<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import UiButton from '../components/ui/UiButton.vue'
import UiCard from '../components/ui/UiCard.vue'
import UiInput from '../components/ui/UiInput.vue'
import { showToast } from '../composables/useToast'
import { useAuthStore } from '../stores/auth'

const router = useRouter()
const authStore = useAuthStore()
const username = ref('admin')
const password = ref('admin123')

async function handleLogin() {
  try {
    await authStore.login(username.value, password.value)
    if (authStore.requirePasswordChange) {
      router.push('/change-password')
      return
    }
    router.push('/dashboard')
    showToast('登录成功', 'success')
  } catch (error) {
    showToast(error instanceof Error ? error.message : '登录失败', 'error')
  }
}
</script>

<template>
  <UiCard class="w-full max-w-md p-6">
    <h1 class="mb-1 text-2xl font-semibold">管理后台登录</h1>
    <p class="text-muted mb-6 text-sm">请输入管理员账号信息</p>
    <div class="space-y-4">
      <UiInput v-model="username" placeholder="用户名" />
      <UiInput v-model="password" type="password" placeholder="密码" />
      <UiButton class="w-full" @click="handleLogin">登录</UiButton>
    </div>
  </UiCard>
</template>
