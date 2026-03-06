import { ensureMiniappLogin, getStoredOpenId } from './services/auth'

App({
  globalData: {
    userId: '',
    openId: '',
  },
  async onLaunch() {
    const cachedOpenId = getStoredOpenId()
    if (cachedOpenId) {
      this.globalData.openId = cachedOpenId
      this.globalData.userId = cachedOpenId
      return
    }

    try {
      const openId = await ensureMiniappLogin()
      this.globalData.openId = openId
      this.globalData.userId = openId
    } catch {
      // 登录失败时不阻断应用，页面内按需重试登录。
    }
  },
})
