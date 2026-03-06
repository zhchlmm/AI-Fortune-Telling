import { env } from '../env/dev'

type LoginByCodeResponse = {
  openId: string
  accessToken: string
  expiresAt: string
}

const OPEN_ID_STORAGE_KEY = 'miniappOpenId'
const ACCESS_TOKEN_STORAGE_KEY = 'miniappAccessToken'
let loginPromise: Promise<string> | null = null

export function getStoredOpenId() {
  const cached = wx.getStorageSync(OPEN_ID_STORAGE_KEY)
  if (typeof cached === 'string' && cached.trim().length > 0) {
    return cached
  }

  return ''
}

export function getStoredAccessToken() {
  const cached = wx.getStorageSync(ACCESS_TOKEN_STORAGE_KEY)
  if (typeof cached === 'string' && cached.trim().length > 0) {
    return cached
  }

  return ''
}

export function clearMiniappAuth() {
  wx.removeStorageSync(OPEN_ID_STORAGE_KEY)
  wx.removeStorageSync(ACCESS_TOKEN_STORAGE_KEY)

  const app = getApp<{ globalData: { userId: string; openId: string; accessToken?: string } }>()
  app.globalData.openId = ''
  app.globalData.userId = ''
  app.globalData.accessToken = ''
}

export async function ensureMiniappLogin() {
  const app = getApp<{ globalData: { userId: string; openId: string; accessToken?: string } }>()
  const fromGlobal = app?.globalData?.openId?.trim() || app?.globalData?.userId?.trim() || ''
  const tokenFromGlobal = app?.globalData?.accessToken?.trim() || ''
  if (fromGlobal && tokenFromGlobal) {
    return fromGlobal
  }

  const fromStorage = getStoredOpenId()
  const tokenFromStorage = getStoredAccessToken()
  if (fromStorage && tokenFromStorage) {
    app.globalData.openId = fromStorage
    app.globalData.userId = fromStorage
    app.globalData.accessToken = tokenFromStorage
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

  const payload = await requestLoginByCode({ code })
  if (!payload.openId || payload.openId.trim().length === 0) {
    throw new Error('登录响应缺少 openId')
  }

  if (!payload.accessToken || payload.accessToken.trim().length === 0) {
    throw new Error('登录响应缺少 accessToken')
  }

  const openId = payload.openId.trim()
  const accessToken = payload.accessToken.trim()
  wx.setStorageSync(OPEN_ID_STORAGE_KEY, openId)
  wx.setStorageSync(ACCESS_TOKEN_STORAGE_KEY, accessToken)

  const app = getApp<{ globalData: { userId: string; openId: string; accessToken?: string } }>()
  app.globalData.openId = openId
  app.globalData.userId = openId
  app.globalData.accessToken = accessToken

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

function requestLoginByCode(payload: { code: string }) {
  return new Promise<LoginByCodeResponse>((resolve, reject) => {
    wx.request({
      url: `${env.apiBaseUrl}/miniapp/users/login-by-code`,
      method: 'POST',
      data: payload,
      success: (res) => {
        if (res.statusCode >= 200 && res.statusCode < 300) {
          resolve(res.data as LoginByCodeResponse)
          return
        }

        const data = (res.data ?? {}) as { message?: string }
        const message = typeof data.message === 'string' && data.message.trim().length > 0
          ? data.message
          : `Request failed: ${res.statusCode}`
        reject(new Error(message))
      },
      fail: (err) => reject(err),
    })
  })
}
