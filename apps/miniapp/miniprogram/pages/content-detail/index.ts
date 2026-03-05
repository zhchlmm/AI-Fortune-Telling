import { getContentById, type ContentItemDto } from '../../services/api'
import { formatBeijingTime } from '../../utils/datetime'

type ContentView = ContentItemDto & {
  displayTime: string
}

Page({
  data: {
    content: null as ContentView | null,
    loading: false,
  },
  onLoad(query: Record<string, string | undefined>) {
    void this.loadContent(query.id)
  },
  async loadContent(id?: string) {
    if (!id) {
      this.setData({ content: null })
      return
    }

    this.setData({ loading: true })
    try {
      const target = await getContentById(id)
      this.setData({
        content: {
          ...target,
          displayTime: formatBeijingTime(target.publishedAt),
        },
      })
    } catch {
      wx.showToast({ title: '加载详情失败', icon: 'none' })
      this.setData({ content: null })
    } finally {
      this.setData({ loading: false })
    }
  },
})
