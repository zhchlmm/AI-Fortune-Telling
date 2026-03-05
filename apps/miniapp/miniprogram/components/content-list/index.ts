import { getContentCategories, getContents, type ContentCategoryDto, type ContentItemDto } from '../../services/api'
import { formatBeijingTime } from '../../utils/datetime'

type CategoryView = {
  id: string
  name: string
}

type ContentView = ContentItemDto & {
  displayTime: string
}

Component({
  properties: {
    emptyText: {
      type: String,
      value: '暂无资讯',
    },
    pageSize: {
      type: Number,
      value: 10,
    },
  },
  data: {
    categories: [{ id: 'all', name: '全部' }] as CategoryView[],
    activeCategoryId: 'all',
    contents: [] as ContentView[],
    loading: false,
    loadingMore: false,
    page: 1,
    hasMore: true,
  },
  lifetimes: {
    attached() {
      void this.initialize()
    },
  },
  methods: {
    async initialize() {
      await this.loadCategories()
      await this.loadContents(true)
    },
    async refresh() {
      this.setData({
        page: 1,
        hasMore: true,
        contents: [],
      })
      await this.loadContents(true)
    },
    async loadCategories() {
      try {
        const list = await getContentCategories()
        const categories = [
          { id: 'all', name: '全部' },
          ...list.map((item: ContentCategoryDto) => ({ id: item.id, name: item.name })),
        ]
        this.setData({ categories })
      } catch {
        this.setData({ categories: [{ id: 'all', name: '全部' }] })
      }
    },
    async loadContents(reset = true) {
      if (this.data.loading || this.data.loadingMore) {
        return
      }

      const nextPage = reset ? 1 : this.data.page
      const categoryId = this.data.activeCategoryId === 'all' ? undefined : this.data.activeCategoryId

      this.setData({
        loading: reset,
        loadingMore: !reset,
      })

      try {
        const list = await getContents({
          categoryId,
          page: nextPage,
          pageSize: this.properties.pageSize,
        })

        const mapped = list.map((item) => ({
          ...item,
          displayTime: formatBeijingTime(item.publishedAt),
        }))

        const merged = reset ? mapped : this.data.contents.concat(mapped)
        const hasMore = mapped.length === this.properties.pageSize

        this.setData({
          contents: merged,
          page: nextPage + 1,
          hasMore,
        })
      } catch {
        wx.showToast({ title: '加载资讯失败', icon: 'none' })
      } finally {
        this.setData({ loading: false, loadingMore: false })
      }
    },
    async loadMore() {
      if (!this.data.hasMore) {
        return
      }

      await this.loadContents(false)
    },
    async onCategoryChange(e: WechatMiniprogram.CustomEvent<{ categoryId: string }>) {
      const categoryId = e.currentTarget.dataset.categoryId ?? 'all'
      this.setData({
        activeCategoryId: categoryId,
        page: 1,
        hasMore: true,
        contents: [],
      })
      await this.loadContents(true)
    },
    onOpenDetail(e: WechatMiniprogram.CustomEvent<{ id: string }>) {
      const id = e.detail.id
      if (!id) {
        return
      }

      this.triggerEvent('open', { id })
    },
  },
})
