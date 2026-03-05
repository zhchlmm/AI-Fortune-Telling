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

const currentPassword = ref('')
const newPassword = ref('')
const confirmPassword = ref('')

async function handleSubmit() {
  if (newPassword.value.length < 6) {
    showToast('新密码长度不能少于6位', 'error')
    return
  }

  if (newPassword.value !== confirmPassword.value) {
    showToast('两次输入的新密码不一致', 'error')
    return
  }

  try {
    await authStore.changePasswordAndRefresh(currentPassword.value, newPassword.value)
    showToast('密码修改成功，正在跳转...', 'success')
    setTimeout(() => {
      router.push('/dashboard')
    }, 600)
  } catch {
    showToast('改密失败，请检查当前密码', 'error')
  }
}
</script>

<template>
  <UiCard class="w-full max-w-md p-6">
    <h1 class="mb-1 text-2xl font-semibold">首次登录请修改密码</h1>
    <p class="text-muted mb-6 text-sm">修改后将自动返回仪表盘</p>
    <div class="space-y-4">
      <UiInput v-model="currentPassword" type="password" placeholder="当前密码" />
      <UiInput v-model="newPassword" type="password" placeholder="新密码（至少6位）" />
      <UiInput v-model="confirmPassword" type="password" placeholder="确认新密码" />
      <UiButton class="w-full" @click="handleSubmit">提交</UiButton>
    </div>
  </UiCard>
</template>
