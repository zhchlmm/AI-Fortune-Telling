import { env } from '../env/dev'
import { clearMiniappAuth, ensureMiniappLogin } from './auth'

type RequestMethod = 'GET' | 'POST' | 'PUT'

type RequestData = WechatMiniprogram.IAnyObject | string | ArrayBuffer

export function request<T>(url: string, method: RequestMethod, data?: RequestData) {
  return requestWithAuthRetry<T>(url, method, data, false)
}

function requestWithAuthRetry<T>(
  url: string,
  method: RequestMethod,
  data: RequestData | undefined,
  hasRetried: boolean,
) {
  return new Promise<T>((resolve, reject) => {
    const token = wx.getStorageSync('miniappAccessToken')
    const header: WechatMiniprogram.IAnyObject = {}
    if (typeof token === 'string' && token.trim().length > 0) {
      header.Authorization = `Bearer ${token}`
    }

    wx.request({
      url: `${env.apiBaseUrl}${url}`,
      method,
      data,
      header,
      success: (res) => {
        if (res.statusCode >= 200 && res.statusCode < 300) {
          resolve(res.data as T)
          return
        }

        const payload = (res.data ?? {}) as { message?: string; code?: string }
        const message = typeof payload.message === 'string' && payload.message.trim().length > 0
          ? payload.message
          : `Request failed: ${res.statusCode}`
        const error = new Error(message) as Error & { statusCode?: number; code?: string }
        error.statusCode = res.statusCode
        if (typeof payload.code === 'string') {
          error.code = payload.code
        }

        if (res.statusCode === 401 && !hasRetried && url !== '/miniapp/users/login-by-code') {
          clearMiniappAuth()
          ensureMiniappLogin()
            .then(() => requestWithAuthRetry<T>(url, method, data, true).then(resolve).catch(reject))
            .catch(() => reject(error))
          return
        }

        reject(error)
      },
      fail: (err) => reject(err),
    })
  })
}

export type FortuneSessionDto = {
  id: string
  userId: string
  fortuneType: string
  resultSummary: string
  createdAt: string
}

type FortuneWsInbound =
  | { type: 'connected'; message?: string }
  | { type: 'delta'; text: string }
  | { type: 'done'; id: string; result: string; fortuneType: string; model?: string }
  | { type: 'error'; message: string }

type StreamFortuneOptions = {
  userId: string
  fortuneType: string
  question: string
  onDelta?: (delta: string, fullText: string) => void
}

export type ContentItemDto = {
  id: string
  title: string
  summary: string
  content: string
  categoryId?: string
  categoryName: string
  publishedAt: string
}

export type ContentCategoryDto = {
  id: string
  name: string
  sortOrder: number
  isEnabled: boolean
  updatedAt: string
}

export type ContentQueryOptions = {
  categoryId?: string
  page?: number
  pageSize?: number
}

export type UploadImageResponse = {
  url: string
  fileName: string
  size: number
  contentType: string
}

export type UploadImagesOptions = {
  maxRetries?: number
  concurrency?: number
  compressQuality?: number
  onProgress?: (progress: { current: number; total: number }) => void
  controller?: UploadCancelController
}

export type MiniappUserProfileDto = {
  openId: string
  nickname?: string | null
  avatar?: string | null
  email?: string | null
  phoneNumber?: string | null
  updatedAt: string
}

export type UpdateMiniappUserProfilePayload = {
  nickname?: string
  avatar?: string
  email?: string
  phoneNumber?: string
}

export type UploadCancelController = {
  cancelled: boolean
  abortTasks: Set<WechatMiniprogram.UploadTask>
  cancel: () => void
}

export function createUploadCancelController(): UploadCancelController {
  const controller: UploadCancelController = {
    cancelled: false,
    abortTasks: new Set<WechatMiniprogram.UploadTask>(),
    cancel() {
      if (controller.cancelled) {
        return
      }

      controller.cancelled = true
      controller.abortTasks.forEach((task) => {
        try {
          task.abort()
        } catch {
          // ignore
        }
      })
      controller.abortTasks.clear()
    },
  }

  return controller
}

export function isUploadCanceledError(error: unknown) {
  return error instanceof Error && error.message === '上传已取消'
}

export class UploadBatchError extends Error {
  failedIndex: number
  failedPath: string

  constructor(failedIndex: number, failedPath: string, message: string) {
    super(message)
    this.name = 'UploadBatchError'
    this.failedIndex = failedIndex
    this.failedPath = failedPath
  }
}

function parseErrorMessage(payload: string | undefined, statusCode: number) {
  if (!payload) {
    return `上传失败（${statusCode}）`
  }

  try {
    const parsed = JSON.parse(payload) as { message?: string; title?: string }
    if (typeof parsed.message === 'string' && parsed.message.trim().length > 0) {
      return parsed.message
    }

    if (typeof parsed.title === 'string' && parsed.title.trim().length > 0) {
      return parsed.title
    }
  } catch {
    if (payload.trim().length > 0) {
      return payload
    }
  }

  return `上传失败（${statusCode}）`
}

function normalizeQuality(value: number | undefined) {
  if (typeof value !== 'number' || Number.isNaN(value)) {
    return 75
  }

  return Math.min(100, Math.max(40, Math.round(value)))
}

function compressImageForUpload(filePath: string, quality?: number) {
  return new Promise<string>((resolve) => {
    wx.compressImage({
      src: filePath,
      quality: normalizeQuality(quality),
      compressedWidth: 1600,
      compressedHeight: 1600,
      success: (res) => resolve(res.tempFilePath || filePath),
      fail: () => resolve(filePath),
    })
  })
}

export function createFortuneSession(payload: {
  userId: string
  fortuneType: string
  parameters: Record<string, string>
}) {
  return request<FortuneSessionDto>('/fortune-sessions', 'POST', payload)
}

export function createFortuneSessionByWebSocket(options: StreamFortuneOptions) {
  return new Promise<FortuneSessionDto>((resolve, reject) => {
    const socket = wx.connectSocket({
      url: env.copilotWsUrl,
      timeout: 120000,
    })

    let fullText = ''
    let closed = false

    const finishWithError = (message: string) => {
      if (closed) {
        return
      }
      closed = true
      try {
        socket.close({ code: 1000, reason: 'client-finish' })
      } catch {
        // ignore
      }
      reject(new Error(message))
    }

    socket.onOpen(() => {
      const payload = {
        type: 'fortune.start',
        userId: options.userId,
        fortuneType: options.fortuneType,
        question: options.question,
      }

      socket.send({
        data: JSON.stringify(payload),
      })
    })

    socket.onMessage((res) => {
      try {
        const raw = typeof res.data === 'string' ? res.data : ''
        const message = JSON.parse(raw) as FortuneWsInbound

        if (message.type === 'delta') {
          fullText += message.text
          options.onDelta?.(message.text, fullText)
          return
        }

        if (message.type === 'done') {
          if (closed) {
            return
          }

          closed = true
          try {
            socket.close({ code: 1000, reason: 'completed' })
          } catch {
            // ignore
          }

          resolve({
            id: message.id,
            userId: options.userId,
            fortuneType: message.fortuneType || options.fortuneType,
            resultSummary: message.result || fullText,
            createdAt: new Date().toISOString(),
          })
          return
        }

        if (message.type === 'error') {
          finishWithError(message.message || '算命服务异常')
        }
      } catch {
        finishWithError('算命服务响应解析失败')
      }
    })

    socket.onError(() => {
      finishWithError('WebSocket连接失败')
    })

    socket.onClose((res) => {
      if (!closed && res.code !== 1000) {
        finishWithError('WebSocket连接已关闭')
      }
    })
  })
}

export function getFortuneHistory(userId: string, page = 1, pageSize = 10) {
  return request<FortuneSessionDto[]>(
    `/fortune-sessions?userId=${encodeURIComponent(userId)}&page=${page}&pageSize=${pageSize}`,
    'GET',
  )
}

export function getMiniappUserProfile() {
  return request<MiniappUserProfileDto>('/miniapp/users/profile/me', 'GET')
}

export function updateMiniappUserProfile(payload: UpdateMiniappUserProfilePayload) {
  return request<MiniappUserProfileDto>('/miniapp/users/profile/me', 'PUT', payload)
}

export function updateMiniappUserPhoneByEncryptedData(payload: {
  encryptedData: string
  iv: string
}) {
  return request<MiniappUserProfileDto>('/miniapp/users/phone', 'POST', payload)
}

export function getContents(options?: string | ContentQueryOptions) {
  const query: string[] = []

  if (typeof options === 'string') {
    query.push(`categoryId=${encodeURIComponent(options)}`)
  } else if (options) {
    if (options.categoryId) {
      query.push(`categoryId=${encodeURIComponent(options.categoryId)}`)
    }
    if (typeof options.page === 'number') {
      query.push(`page=${options.page}`)
    }
    if (typeof options.pageSize === 'number') {
      query.push(`pageSize=${options.pageSize}`)
    }
  }

  const suffix = query.length > 0 ? `?${query.join('&')}` : ''
  return request<ContentItemDto[]>(`/contents${suffix}`, 'GET')
}

export function getContentById(id: string) {
  return request<ContentItemDto>(`/contents/${encodeURIComponent(id)}`, 'GET')
}

export function getContentCategories() {
  return request<ContentCategoryDto[]>('/content-categories', 'GET')
}

function uploadImageByPath(filePath: string, controller?: UploadCancelController) {
  return new Promise<UploadImageResponse>((resolve, reject) => {
    if (controller?.cancelled) {
      reject(new Error('上传已取消'))
      return
    }

    const task = wx.uploadFile({
      url: `${env.apiBaseUrl}/uploads/images`,
      filePath,
      name: 'file',
      success: (res) => {
        controller?.abortTasks.delete(task)
        if (res.statusCode < 200 || res.statusCode >= 300) {
          reject(new Error(parseErrorMessage(res.data, res.statusCode)))
          return
        }

        try {
          const parsed = JSON.parse(res.data) as UploadImageResponse
          resolve(parsed)
        } catch {
          reject(new Error('上传失败：响应格式错误'))
        }
      },
      fail: (err) => {
        controller?.abortTasks.delete(task)
        if (controller?.cancelled || (typeof err?.errMsg === 'string' && err.errMsg.includes('abort'))) {
          reject(new Error('上传已取消'))
          return
        }

        const message =
          typeof err?.errMsg === 'string' && err.errMsg.length > 0
            ? err.errMsg
            : '网络异常，无法连接上传服务'
        reject(new Error(message))
      },
    })

    controller?.abortTasks.add(task)
  })
}

export async function uploadImage(filePath: string, controller?: UploadCancelController, compressQuality?: number) {
  const compressedPath = await compressImageForUpload(filePath, compressQuality)
  return uploadImageByPath(compressedPath, controller)
}

async function uploadImageWithRetry(
  filePath: string,
  maxRetries: number,
  controller?: UploadCancelController,
  compressQuality?: number,
) {
  let lastError: unknown = null
  const compressedPath = await compressImageForUpload(filePath, compressQuality)

  for (let attempt = 0; attempt <= maxRetries; attempt += 1) {
    try {
      if (controller?.cancelled) {
        throw new Error('上传已取消')
      }

      return await uploadImageByPath(compressedPath, controller)
    } catch (error) {
      lastError = error
      if (isUploadCanceledError(error)) {
        throw error
      }

      if (attempt < maxRetries) {
        await new Promise((resolve) => setTimeout(resolve, 400))
      }
    }
  }

  throw lastError instanceof Error ? lastError : new Error('Upload failed')
}

export async function uploadImages(filePaths: string[], options?: UploadImagesOptions) {
  const result = new Array<string>(filePaths.length)
  const total = filePaths.length
  const maxRetries = options?.maxRetries ?? 1
  const concurrency = Math.max(1, options?.concurrency ?? 2)
  const compressQuality = options?.compressQuality

  if (total === 0) {
    return [] as string[]
  }

  let nextIndex = 0
  let completed = 0
  let firstError: Error | null = null

  const worker = async () => {
    while (nextIndex < total && !firstError) {
      if (options?.controller?.cancelled) {
        firstError = new Error('上传已取消')
        return
      }

      const index = nextIndex
      nextIndex += 1

      const filePath = filePaths[index]
      let uploaded: UploadImageResponse
      try {
        uploaded = await uploadImageWithRetry(filePath, maxRetries, options?.controller, compressQuality)
      } catch (error) {
        if (isUploadCanceledError(error)) {
          firstError = error instanceof Error ? error : new Error('上传已取消')
          return
        }

        const message = error instanceof Error ? error.message : '上传失败'
        firstError = new UploadBatchError(index, filePath, `第${index + 1}张图片上传失败：${message}`)
        return
      }

      result[index] = uploaded.url
      completed += 1
      options?.onProgress?.({ current: completed, total })
    }
  }

  const workers = Array.from({ length: Math.min(concurrency, total) }, () => worker())
  await Promise.all(workers)

  if (firstError) {
    throw firstError
  }

  return result
}
