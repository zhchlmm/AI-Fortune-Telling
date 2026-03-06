import { request } from './api'

type LoginByCodeResponse = {
  openId: string
}

const OPEN_ID_STORAGE_KEY = 'miniappOpenId'
let loginPromise: Promise<string> | null = null

export function getStoredOpenId() {
  const cached = wx.getStorageSync(OPEN_ID_STORAGE_KEY)
  if (typeof cached === 'string' && cached.trim().length > 0) {
    return cached
  }

  return ''
}

export async function ensureMiniappLogin() {
  const app = getApp<{ globalData: { userId: string; openId: string } }>()
  const fromGlobal = app?.globalData?.openId?.trim() || app?.globalData?.userId?.trim() || ''
  if (fromGlobal) {
    return fromGlobal
  }

  const fromStorage = getStoredOpenId()
  if (fromStorage) {
    app.globalData.openId = fromStorage
    app.globalData.userId = fromStorage
    return fromStorage
  }

  if (!loginPromise) {
    loginPromise = loginByWxCode().finally(() => {
      loginPromise = null
    })
  }

  return loginPromise
}

async function loginByWxCode() {
  const loginRes = await wxLogin()
  const code = (loginRes.code || '').trim()
  if (!code) {
    throw new Error('获取微信登录 code 失败')
  }

  const payload = await request<LoginByCodeResponse>('/miniapp/users/login-by-code', 'POST', { code })
  if (!payload.openId || payload.openId.trim().length === 0) {
    throw new Error('登录响应缺少 openId')
  }

  const openId = payload.openId.trim()
  wx.setStorageSync(OPEN_ID_STORAGE_KEY, openId)

  const app = getApp<{ globalData: { userId: string; openId: string } }>()
  app.globalData.openId = openId
  app.globalData.userId = openId

  return openId
}

function wxLogin() {
  return new Promise<WechatMiniprogram.LoginSuccessCallbackResult>((resolve, reject) => {
    wx.login({
      success: (res) => resolve(res),
      fail: (err) => reject(err),
    })
  })
}
