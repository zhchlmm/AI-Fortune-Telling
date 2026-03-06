import { ensureMiniappLogin, getStoredAccessToken, getStoredOpenId } from './services/auth'

App({
  globalData: {
    userId: '',
    openId: '',
    accessToken: '',
  },
  async onLaunch() {
    const cachedOpenId = getStoredOpenId()
    if (cachedOpenId) {
      const cachedToken = getStoredAccessToken()
      this.globalData.openId = cachedOpenId
      this.globalData.userId = cachedOpenId
      this.globalData.accessToken = cachedToken
      return
    }

    try {
      const openId = await ensureMiniappLogin()
      const token = getStoredAccessToken()
      this.globalData.openId = openId
      this.globalData.userId = openId
      this.globalData.accessToken = token
    } catch {
      // 登录失败时不阻断应用，页面内按需重试登录。
    }
  },
})
