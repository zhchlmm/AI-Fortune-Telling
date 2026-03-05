<script setup lang="ts">
import { onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import GlobalToast from './components/ui/GlobalToast.vue'
import UiButton from './components/ui/UiButton.vue'
import ThemeToggle from './components/ui/ThemeToggle.vue'
import { useAuthStore } from './stores/auth'
import { useTheme } from './composables/useTheme'

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()
const mobileMenuOpen = ref(false)
const { initializeTheme } = useTheme()

onMounted(() => {
  initializeTheme()
})

function logout() {
  authStore.logout()
  router.push('/login')
}

function toggleMobileMenu() {
  mobileMenuOpen.value = !mobileMenuOpen.value
}

function closeMobileMenu() {
  mobileMenuOpen.value = false
}

watch(() => route.path, () => {
  closeMobileMenu()
})
</script>

<template>
  <div class="min-h-screen">
    <GlobalToast />
    <div class="flex min-h-screen" v-if="route.path !== '/login' && route.path !== '/change-password'">
      <aside class="app-sidebar hidden w-64 md:block">
        <h2 class="mb-6 text-lg font-semibold">AI 占卜后台</h2>
        <nav class="space-y-1">
          <RouterLink
            to="/dashboard"
            class="nav-link"
            active-class="nav-link-active"
          >
            仪表盘
          </RouterLink>
          <RouterLink
            to="/templates"
            class="nav-link"
            active-class="nav-link-active"
          >
            模板管理
          </RouterLink>
          <RouterLink
            to="/contents"
            class="nav-link"
            active-class="nav-link-active"
          >
            内容管理
          </RouterLink>
          <RouterLink
            to="/content-categories"
            class="nav-link"
            active-class="nav-link-active"
          >
            分类管理
          </RouterLink>
          <RouterLink
            to="/sessions"
            class="nav-link"
            active-class="nav-link-active"
          >
            会话查询
          </RouterLink>
          <RouterLink
            to="/auth-audits"
            class="nav-link"
            active-class="nav-link-active"
          >
            登录审计
          </RouterLink>
          <RouterLink
            to="/ai-audits"
            class="nav-link"
            active-class="nav-link-active"
          >
            AI审计
          </RouterLink>
        </nav>
        <ThemeToggle class="mt-6" full-width />
        <UiButton variant="secondary" class="mt-2 w-full" @click="logout">退出登录</UiButton>
      </aside>

      <div
        v-if="mobileMenuOpen"
        class="fixed inset-0 z-40 bg-slate-900/40 md:hidden"
        @click="closeMobileMenu"
      />

      <aside
        class="app-sidebar fixed inset-y-0 left-0 z-50 w-64 transform transition-transform duration-200 md:hidden"
        :class="mobileMenuOpen ? 'translate-x-0' : '-translate-x-full'"
      >
        <div class="mb-4 flex items-center justify-between">
          <h2 class="text-lg font-semibold">AI 占卜后台</h2>
          <UiButton variant="secondary" class="px-3" @click="closeMobileMenu">关闭</UiButton>
        </div>
        <nav class="space-y-1">
          <RouterLink
            to="/dashboard"
            class="nav-link"
            active-class="nav-link-active"
            @click="closeMobileMenu"
          >
            仪表盘
          </RouterLink>
          <RouterLink
            to="/templates"
            class="nav-link"
            active-class="nav-link-active"
            @click="closeMobileMenu"
          >
            模板管理
          </RouterLink>
          <RouterLink
            to="/contents"
            class="nav-link"
            active-class="nav-link-active"
            @click="closeMobileMenu"
          >
            内容管理
          </RouterLink>
          <RouterLink
            to="/content-categories"
            class="nav-link"
            active-class="nav-link-active"
            @click="closeMobileMenu"
          >
            分类管理
          </RouterLink>
          <RouterLink
            to="/sessions"
            class="nav-link"
            active-class="nav-link-active"
            @click="closeMobileMenu"
          >
            会话查询
          </RouterLink>
          <RouterLink
            to="/auth-audits"
            class="nav-link"
            active-class="nav-link-active"
            @click="closeMobileMenu"
          >
            登录审计
          </RouterLink>
          <RouterLink
            to="/ai-audits"
            class="nav-link"
            active-class="nav-link-active"
            @click="closeMobileMenu"
          >
            AI审计
          </RouterLink>
        </nav>
        <ThemeToggle class="mt-6" full-width />
        <UiButton variant="secondary" class="mt-2 w-full" @click="logout">退出登录</UiButton>
      </aside>

      <section class="flex-1 p-4 md:p-6">
        <header class="mb-4 flex items-center justify-between md:hidden">
          <UiButton variant="secondary" @click="toggleMobileMenu">菜单</UiButton>
          <ThemeToggle />
        </header>
        <RouterView />
      </section>
    </div>
    <div v-else class="flex min-h-screen items-center justify-center p-6">
      <div class="fixed right-4 top-4">
        <ThemeToggle />
      </div>
      <RouterView />
    </div>
  </div>
</template>
