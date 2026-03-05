import { syncCustomTabBarSelected } from '../../utils/tabbar'

type ContentListComponent = WechatMiniprogram.Component.TrivialInstance & {
  loadMore?: () => void | Promise<void>
}

Page({
  data: {},
  onLoad() {},
  onShow() {
    syncCustomTabBarSelected(this, 1)
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
})
