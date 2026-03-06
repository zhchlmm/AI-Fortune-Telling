import { syncCustomTabBarSelected } from '../../utils/tabbar'
import {
  getMiniappUserProfile,
  updateMiniappUserPhoneByEncryptedData,
  updateMiniappUserProfile,
  type MiniappUserProfileDto,
} from '../../services/api'
import { clearMiniappAuth, ensureMiniappLogin } from '../../services/auth'

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
    nickname: '未设置',
    email: '未设置',
    phoneNumber: '未设置',
    avatarUrl: '',
    editMode: false,
    draftNickname: '',
    draftEmail: '',
    draftPhoneNumber: '',
    draftAvatarUrl: '',
    savingProfile: false,
    memberLevel: 'VIP',
    memberIcon: 'user-vip',
    memberIconColor: '#0052D9',
    memberNameColor: '#0052D9',
    memberHint: '查看更多会员能力',
  },
  async onShow() {
    const userId = await ensureMiniappLogin()
    const app = getApp<{ globalData: { userId: string } }>()
    app.globalData.userId = userId
    const memberLevel = wx.getStorageSync('memberLevel') || '免费会员'
    const memberMeta = resolveMemberMeta(String(memberLevel))
    this.setData({
      userId,
      memberLevel,
      memberIcon: memberMeta.icon,
      memberIconColor: memberMeta.color,
      memberNameColor: memberMeta.color,
      memberHint: memberMeta.hint,
    })

    await this.loadProfile(userId)
    syncCustomTabBarSelected(this, 3)
  },
  async loadProfile(_openId: string) {
    try {
      const profile = await getMiniappUserProfile()
      this.setProfileData(profile)
    } catch (error) {
      const message = this.resolveErrorMessage(error, '加载个人资料失败')
      this.setData({
        nickname: '未设置',
        email: '未设置',
        phoneNumber: '未设置',
      })
      wx.showToast({ title: message, icon: 'none' })
    }
  },
  setProfileData(profile: MiniappUserProfileDto) {
    const nickname = (profile.nickname || '').trim()
    const email = (profile.email || '').trim()
    const phoneNumber = (profile.phoneNumber || '').trim()
    const avatarUrl = (profile.avatar || '').trim()

    this.setData({
      nickname: nickname || '未设置',
      email: email || '未设置',
      phoneNumber: phoneNumber || '未设置',
      avatarUrl,
      draftNickname: nickname,
      draftEmail: email,
      draftPhoneNumber: phoneNumber,
      draftAvatarUrl: avatarUrl,
    })
  },
  toggleEditMode() {
    const nextEditMode = !this.data.editMode
    this.setData({
      editMode: nextEditMode,
      draftNickname: this.data.nickname === '未设置' ? '' : this.data.nickname,
      draftEmail: this.data.email === '未设置' ? '' : this.data.email,
      draftPhoneNumber: this.data.phoneNumber === '未设置' ? '' : this.data.phoneNumber,
      draftAvatarUrl: this.data.avatarUrl,
    })
  },
  onNicknameInput(e: WechatMiniprogram.CustomEvent<{ value?: string }>) {
    this.setData({ draftNickname: e.detail?.value ?? '' })
  },
  onEmailInput(e: WechatMiniprogram.CustomEvent<{ value?: string }>) {
    this.setData({ draftEmail: e.detail?.value ?? '' })
  },
  onPhoneInput(e: WechatMiniprogram.CustomEvent<{ value?: string }>) {
    this.setData({ draftPhoneNumber: e.detail?.value ?? '' })
  },
  onChooseAvatar(e: WechatMiniprogram.CustomEvent<{ avatarUrl?: string }>) {
    const avatarUrl = (e.detail?.avatarUrl || '').trim()
    if (!avatarUrl) {
      return
    }

    this.setData({ draftAvatarUrl: avatarUrl })
  },
  async useWechatNickname() {
    try {
      const res = await this.getWechatUserInfo()
      const nickname = (res.userInfo?.nickName || '').trim()
      const avatarUrl = (res.userInfo?.avatarUrl || '').trim()

      this.setData({
        draftNickname: nickname || this.data.draftNickname,
        draftAvatarUrl: avatarUrl || this.data.draftAvatarUrl,
      })
    } catch {
      wx.showToast({ title: '获取微信昵称失败', icon: 'none' })
    }
  },
  async saveProfile() {
    if (this.data.savingProfile) {
      return
    }

    this.setData({ savingProfile: true })
    try {
      const updated = await updateMiniappUserProfile({
        nickname: this.data.draftNickname,
        email: this.data.draftEmail,
        phoneNumber: this.data.draftPhoneNumber,
        avatar: this.data.draftAvatarUrl,
      })

      this.setProfileData(updated)
      this.setData({ editMode: false })
      wx.showToast({ title: '保存成功', icon: 'success' })
    } catch (error) {
      wx.showToast({ title: this.resolveErrorMessage(error, '保存失败'), icon: 'none' })
    } finally {
      this.setData({ savingProfile: false })
    }
  },
  async onGetPhoneNumber(
    e: WechatMiniprogram.CustomEvent<{
      errMsg?: string
      encryptedData?: string
      iv?: string
    }>,
  ) {
    if (!e.detail?.errMsg?.includes('ok')) {
      wx.showToast({ title: '未授权手机号', icon: 'none' })
      return
    }

    if (!e.detail.encryptedData || !e.detail.iv) {
      wx.showToast({ title: '手机号数据缺失', icon: 'none' })
      return
    }

    try {
      const updated = await updateMiniappUserPhoneByEncryptedData({
        encryptedData: e.detail.encryptedData,
        iv: e.detail.iv,
      })

      this.setProfileData(updated)
      wx.showToast({ title: '手机号已更新', icon: 'success' })
    } catch (error) {
      wx.showToast({ title: this.resolveErrorMessage(error, '手机号更新失败'), icon: 'none' })
    }
  },
  resolveErrorMessage(error: unknown, fallback: string) {
    const err = error as { message?: string; code?: string; statusCode?: number }
    const code = err?.code ?? ''
    if (code === 'PHONE_DECRYPT_FAILED') {
      return '微信手机号解析失败，请重试或手动输入'
    }

    if (code === 'SESSION_KEY_MISSING') {
      clearMiniappAuth()
      return '登录态已失效，请重试'
    }

    if (code === 'USER_BLOCKED') {
      return '账号已受限，暂不可修改资料'
    }

    if (code === 'INVALID_PHONE_NUMBER') {
      return '手机号格式不正确'
    }

    if (code === 'INVALID_EMAIL') {
      return '邮箱格式不正确'
    }

    if (typeof err?.message === 'string' && err.message.trim().length > 0) {
      return err.message
    }

    return fallback
  },
  getWechatUserInfo() {
    return new Promise<WechatMiniprogram.GetUserInfoSuccessCallbackResult>((resolve, reject) => {
      if (wx.getUserProfile) {
        wx.getUserProfile({
          desc: '用于自动填充昵称与头像',
          success: (res) => resolve(res),
          fail: (err) => reject(err),
        })
        return
      }

      wx.getUserInfo({
        success: (res) => resolve(res),
        fail: (err) => reject(err),
      })
    })
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
