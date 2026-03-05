import { syncCustomTabBarSelected } from '../../utils/tabbar'

type ContentListComponent = WechatMiniprogram.Component.TrivialInstance & {
  loadMore?: () => void | Promise<void>
}

type FortuneSubmitDetail = {
  fortuneType: string
  question: string
}

Page({
  data: {},
  onLoad() {},
  onShow() {
    syncCustomTabBarSelected(this, 0)
  },
  onReachBottom() {
    this.getContentListComponent()?.loadMore?.()
  },
  getContentListComponent() {
    return this.selectComponent('#contentList') as ContentListComponent | null
  },
  goContentDetail(e: WechatMiniprogram.CustomEvent<{ id: string }>) {
    const id = e.detail.id
    if (!id) {
      return
    }
    wx.navigateTo({ url: `/pages/content-detail/index?id=${id}` })
  },
  onFortuneSubmit(e: WechatMiniprogram.CustomEvent<FortuneSubmitDetail>) {
    const { fortuneType, question } = e.detail
    wx.navigateTo({
      url:
        `/pages/fortune-input/index?autoSubmit=1&fortuneType=${encodeURIComponent(fortuneType)}` +
        `&question=${encodeURIComponent(question)}`,
    })
  },
})
