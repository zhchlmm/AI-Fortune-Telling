import {
  createFortuneSession,
  createFortuneSessionByWebSocket,
  createUploadCancelController,
  isUploadCanceledError,
  UploadBatchError,
  uploadImages,
  type UploadCancelController,
} from '../../services/api'
import { env } from '../../env/dev'
import { ensureMiniappLogin } from '../../services/auth'

type FortuneSubmitDetail = {
  fortuneType: string
  question: string
  photoPaths: string[]
  allPhotoPaths: string[]
}

type AutoSubmitPayload = {
  fortuneType: string
  question: string
}

function safeDecode(value: string | undefined) {
  if (!value) {
    return ''
  }

  try {
    return decodeURIComponent(value)
  } catch {
    return value
  }
}

Page({
  data: {
    submitting: false,
    uploadedPhotoUrlMap: {} as Record<string, string>,
    uploadProgressVisible: false,
    uploadProgressCurrent: 0,
    uploadProgressTotal: 0,
    uploadProgressPercent: 0,
    canCancelUpload: false,
    prefillQuestion: '',
    prefillFortuneType: 'Tarot',
    streamResult: '',
    streamFinished: false,
    streamSessionId: '',
    streamError: '',
  },
  onLoad(options: Record<string, string>) {
    const question = safeDecode(options.question).trim()
    const fortuneType = safeDecode(options.fortuneType).trim() || 'Tarot'
    const autoSubmit = options.autoSubmit === '1' && question.length > 0

    this.setData({
      prefillQuestion: question,
      prefillFortuneType: fortuneType,
    })

    if (autoSubmit) {
      ;(this as unknown as { autoSubmitPayload?: AutoSubmitPayload }).autoSubmitPayload = {
        fortuneType,
        question,
      }
    }
  },
  onReady() {
    const payload = (this as unknown as { autoSubmitPayload?: AutoSubmitPayload }).autoSubmitPayload
    if (!payload) {
      return
    }

    ;(this as unknown as { autoSubmitPayload?: AutoSubmitPayload }).autoSubmitPayload = undefined
    this.onFortuneSubmit({
      detail: {
        fortuneType: payload.fortuneType,
        question: payload.question,
        photoPaths: [],
        allPhotoPaths: [],
      },
    } as unknown as WechatMiniprogram.CustomEvent<FortuneSubmitDetail>)
  },
  getFortuneFormComponent() {
    return this.selectComponent('#fortuneForm') as WechatMiniprogram.Component.TrivialInstance & {
      setFailedPhotoPaths?: (paths: string[]) => void
      clearFailedPhotoPaths?: () => void
    }
  },
  async onFortuneSubmit(e: WechatMiniprogram.CustomEvent<FortuneSubmitDetail>) {
    if (this.data.submitting) {
      return
    }

    const { fortuneType, question } = e.detail
    const photoPaths = e.detail.photoPaths ?? []
    const allPhotoPaths = e.detail.allPhotoPaths ?? photoPaths
    const uploadedPhotoUrlMap = { ...(this.data.uploadedPhotoUrlMap as Record<string, string>) }

    for (const key of Object.keys(uploadedPhotoUrlMap)) {
      if (!allPhotoPaths.includes(key)) {
        delete uploadedPhotoUrlMap[key]
      }
    }

    const pendingPhotoPaths = photoPaths.filter((path) => !uploadedPhotoUrlMap[path])
    const fortuneForm = this.getFortuneFormComponent()
    let uploadController: UploadCancelController | null = null

    this.setData({
      submitting: true,
      streamResult: '',
      streamFinished: false,
      streamSessionId: '',
      streamError: '',
    })
    try {
      const userId = await ensureMiniappLogin()
      const app = getApp<{ globalData: { userId: string } }>()
      app.globalData.userId = userId
      if (pendingPhotoPaths.length > 0) {
        uploadController = createUploadCancelController()
        ;(this as unknown as { uploadController?: UploadCancelController }).uploadController = uploadController
        this.setData({
          uploadProgressVisible: true,
          uploadProgressCurrent: 0,
          uploadProgressTotal: pendingPhotoPaths.length,
          uploadProgressPercent: 0,
          canCancelUpload: true,
        })
        wx.showLoading({ title: `上传照片 0/${pendingPhotoPaths.length}`, mask: true })
      }

      const uploadedUrls = await uploadImages(pendingPhotoPaths, {
        maxRetries: 1,
        concurrency: 2,
        controller: uploadController ?? undefined,
        onProgress: ({ current, total }) => {
          this.setData({
            uploadProgressCurrent: current,
            uploadProgressTotal: total,
            uploadProgressPercent: total > 0 ? Math.floor((current / total) * 100) : 0,
          })
          wx.showLoading({ title: `上传照片 ${current}/${total}`, mask: true })
        },
      })

      pendingPhotoPaths.forEach((path, index) => {
        const url = uploadedUrls[index]
        if (url) {
          uploadedPhotoUrlMap[path] = url
        }
      })

      this.setData({ uploadedPhotoUrlMap })

      const photoUrls = allPhotoPaths.map((path) => uploadedPhotoUrlMap[path]).filter((item): item is string => !!item)

      if (photoUrls.length !== allPhotoPaths.length) {
        const failed = allPhotoPaths.filter((path) => !uploadedPhotoUrlMap[path])
        fortuneForm?.setFailedPhotoPaths?.(failed)
        wx.hideLoading()
        this.setData({ uploadProgressVisible: false, canCancelUpload: false })
        wx.showToast({ title: '仍有失败图片，请重传失败项', icon: 'none' })
        return
      }

      wx.hideLoading()
      this.setData({ uploadProgressVisible: false, canCancelUpload: false })
      fortuneForm?.clearFailedPhotoPaths?.()
      const session =
        env.fortuneTransport === 'copilot-ws'
          ? await createFortuneSessionByWebSocket({
              userId: app.globalData.userId,
              fortuneType,
              question,
              onDelta: (_delta, fullText) => {
                this.setData({
                  streamResult: fullText,
                })
              },
            })
          : await createFortuneSession({
              userId: app.globalData.userId,
              fortuneType,
              parameters: {
                question,
                photoUrls: JSON.stringify(photoUrls),
              },
            })

      wx.hideLoading()

      this.setData({
        streamResult: session.resultSummary,
        streamFinished: true,
        streamSessionId: session.id,
      })
    } catch (error) {
      wx.hideLoading()
      this.setData({ uploadProgressVisible: false, canCancelUpload: false })
      if (isUploadCanceledError(error)) {
        wx.showToast({ title: '已取消上传', icon: 'none' })
        return
      }
      if (error instanceof UploadBatchError) {
        fortuneForm?.setFailedPhotoPaths?.([error.failedPath])
      }
      const message = error instanceof Error ? error.message : '提交失败'
      this.setData({ streamError: message })
      wx.showToast({ title: message, icon: 'none' })
    } finally {
      ;(this as unknown as { uploadController?: UploadCancelController }).uploadController = undefined
      this.setData({ submitting: false })
    }
  },
  async onRetryFailed(e: WechatMiniprogram.CustomEvent<FortuneSubmitDetail>) {
    await this.onFortuneSubmit(e)
  },
  onCancelUpload() {
    const controller = (this as unknown as { uploadController?: UploadCancelController }).uploadController
    controller?.cancel()
  },
})
