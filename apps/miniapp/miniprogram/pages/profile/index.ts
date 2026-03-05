import { syncCustomTabBarSelected } from '../../utils/tabbar'

function resolveMemberMeta(level: string) {
  if (level.includes('钻石')) {
    return { icon: 'user-vip', hint: '钻石权益已开通', color: '#60A5FA' }
  }
  if (level.includes('黄金')) {
    return { icon: 'user-vip', hint: '黄金权益已开通', color: '#D4AF37' }
  }
  if (level.includes('VIP')) {
    return { icon: 'user-vip', hint: 'VIP权益已开通', color: '#0052D9' }
  }
  return { icon: 'user', hint: '开通会员解锁更多能力', color: '#9CA3AF' }
}

Page({
  data: {
    userId: '',
    memberLevel: 'VIP',
    memberIcon: 'user-vip',
    memberIconColor: '#0052D9',
    memberNameColor: '#0052D9',
    memberHint: '查看更多会员能力',
  },
  onShow() {
    const app = getApp<{ globalData: { userId: string } }>()
    const memberLevel = wx.getStorageSync('memberLevel') || '免费会员'
    const memberMeta = resolveMemberMeta(String(memberLevel))
    this.setData({
      userId: app.globalData.userId,
      memberLevel,
      memberIcon: memberMeta.icon,
      memberIconColor: memberMeta.color,
      memberNameColor: memberMeta.color,
      memberHint: memberMeta.hint,
    })
    syncCustomTabBarSelected(this, 3)
  },
  goHistory() {
    wx.navigateTo({ url: '/pages/history/index' })
  },
  goFortune() {
    wx.navigateTo({ url: '/pages/fortune-input/index' })
  },
  goAbout() {
    wx.navigateTo({ url: '/pages/about/index' })
  },
  goMemberCenter() {
    wx.switchTab({ url: '/pages/member/index' })
  },
  goHome() {
    wx.switchTab({ url: '/pages/home/index' })
  },
})
