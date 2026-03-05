import { defineStore } from 'pinia'
import { AUTH_TOKEN_KEY, changePassword as apiChangePassword, login } from '../api/client'

const REQUIRE_PASSWORD_CHANGE_KEY = 'require_password_change'
const ADMIN_USERNAME_KEY = 'admin_username'

export const useAuthStore = defineStore('auth', {
  state: () => ({
    token: localStorage.getItem(AUTH_TOKEN_KEY) ?? '',
    username: localStorage.getItem(ADMIN_USERNAME_KEY) ?? '',
    requirePasswordChange: localStorage.getItem(REQUIRE_PASSWORD_CHANGE_KEY) === 'true',
  }),
  getters: {
    isLoggedIn: (state) => state.token.length > 0,
  },
  actions: {
    async login(username: string, password: string) {
      const result = await login(username, password)
      this.token = result.token
      this.username = username
      this.requirePasswordChange = result.requirePasswordChange
      localStorage.setItem(AUTH_TOKEN_KEY, result.token)
      localStorage.setItem(ADMIN_USERNAME_KEY, username)
      localStorage.setItem(REQUIRE_PASSWORD_CHANGE_KEY, String(result.requirePasswordChange))
    },
    async changePasswordAndRefresh(currentPassword: string, newPassword: string) {
      if (!this.username) {
        throw new Error('missing username in auth state')
      }

      await apiChangePassword({
        currentPassword,
        newPassword,
      })

      await this.login(this.username, newPassword)
    },
    clearRequirePasswordChange() {
      this.requirePasswordChange = false
      localStorage.setItem(REQUIRE_PASSWORD_CHANGE_KEY, 'false')
    },
    logout() {
      this.token = ''
      this.username = ''
      this.requirePasswordChange = false
      localStorage.removeItem(AUTH_TOKEN_KEY)
      localStorage.removeItem(ADMIN_USERNAME_KEY)
      localStorage.removeItem(REQUIRE_PASSWORD_CHANGE_KEY)
    },
  },
})
