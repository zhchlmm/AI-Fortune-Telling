type TabItem = {
  pagePath: string
  text: string
  icon: string
}

Component({
  data: {
    selected: 0,
    color: '#808695',
    selectedColor: '#7a5af8',
    list: [
      {
        pagePath: 'pages/home/index',
        text: '首页',
        icon: 'home',
      },
      {
        pagePath: 'pages/info/index',
        text: '资讯',
        icon: 'app',
      },
      {
        pagePath: 'pages/member/index',
        text: '会员',
        icon: 'user-vip',
      },
      {
        pagePath: 'pages/profile/index',
        text: '我的',
        icon: 'user',
      },
    ] as TabItem[],
  },
  pageLifetimes: {
    show() {
      const pages = getCurrentPages()
      const current = pages[pages.length - 1]
      const currentRoute = current?.route ?? ''
      const selected = this.data.list.findIndex((item) => item.pagePath === currentRoute)
      if (selected >= 0) {
        this.setData({ selected })
      }
    },
  },
  methods: {
    switchTab(e: WechatMiniprogram.BaseEvent) {
      const index = Number(e.currentTarget.dataset.index)
      const item = this.data.list[index]
      if (!item) {
        return
      }

      if (this.data.selected === index) {
        return
      }

      this.setData({ selected: index })
      wx.switchTab({ url: `/${item.pagePath}` })
    },
  },
})
