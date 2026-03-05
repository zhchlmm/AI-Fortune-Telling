import { createRouter, createWebHistory } from 'vue-router'
import DashboardView from '../views/DashboardView.vue'
import LoginView from '../views/LoginView.vue'
import ChangePasswordView from '../views/ChangePasswordView.vue'
import TemplateManagementView from '../views/TemplateManagementView.vue'
import ContentManagementView from '../views/ContentManagementView.vue'
import ContentCategoryManagementView from '../views/ContentCategoryManagementView.vue'
import SessionQueryView from '../views/SessionQueryView.vue'
import AuthAuditView from '../views/AuthAuditView.vue'
import AiAuditView from '../views/AiAuditView.vue'
import { useAuthStore } from '../stores/auth'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/login', component: LoginView },
    { path: '/change-password', component: ChangePasswordView },
    { path: '/dashboard', component: DashboardView },
    { path: '/templates', component: TemplateManagementView },
    { path: '/contents', component: ContentManagementView },
    { path: '/content-categories', component: ContentCategoryManagementView },
    { path: '/sessions', component: SessionQueryView },
    { path: '/auth-audits', component: AuthAuditView },
    { path: '/ai-audits', component: AiAuditView },
    { path: '/:pathMatch(.*)*', redirect: '/dashboard' },
  ],
})

router.beforeEach((to) => {
  const authStore = useAuthStore()
  if (to.path !== '/login' && !authStore.isLoggedIn) {
    return '/login'
  }

  if (
    authStore.isLoggedIn
    && authStore.requirePasswordChange
    && to.path !== '/change-password'
    && to.path !== '/login'
  ) {
    return '/change-password'
  }

  if (authStore.isLoggedIn && !authStore.requirePasswordChange && to.path === '/change-password') {
    return '/dashboard'
  }

  return true
})

export default router
