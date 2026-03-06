"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
const tabbar_1 = require("../../utils/tabbar");
const api_1 = require("../../services/api");
const auth_1 = require("../../services/auth");
function resolveMemberMeta(level) {
    if (level.includes('钻石')) {
        return { icon: 'user-vip', hint: '钻石权益已开通', color: '#60A5FA' };
    }
    if (level.includes('黄金')) {
        return { icon: 'user-vip', hint: '黄金权益已开通', color: '#D4AF37' };
    }
    if (level.includes('VIP')) {
        return { icon: 'user-vip', hint: 'VIP权益已开通', color: '#0052D9' };
    }
    return { icon: 'user', hint: '开通会员解锁更多能力', color: '#9CA3AF' };
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
        const userId = await (0, auth_1.ensureMiniappLogin)();
        const app = getApp();
        app.globalData.userId = userId;
        const memberLevel = wx.getStorageSync('memberLevel') || '免费会员';
        const memberMeta = resolveMemberMeta(String(memberLevel));
        this.setData({
            userId,
            memberLevel,
            memberIcon: memberMeta.icon,
            memberIconColor: memberMeta.color,
            memberNameColor: memberMeta.color,
            memberHint: memberMeta.hint,
        });
        await this.loadProfile(userId);
        (0, tabbar_1.syncCustomTabBarSelected)(this, 3);
    },
    async loadProfile(openId) {
        try {
            const profile = await (0, api_1.getMiniappUserProfile)(openId);
            this.setProfileData(profile);
        }
        catch {
            this.setData({
                nickname: '未设置',
                email: '未设置',
                phoneNumber: '未设置',
            });
        }
    },
    setProfileData(profile) {
        const nickname = (profile.nickname || '').trim();
        const email = (profile.email || '').trim();
        const phoneNumber = (profile.phoneNumber || '').trim();
        const avatarUrl = (profile.avatar || '').trim();
        this.setData({
            nickname: nickname || '未设置',
            email: email || '未设置',
            phoneNumber: phoneNumber || '未设置',
            avatarUrl,
            draftNickname: nickname,
            draftEmail: email,
            draftPhoneNumber: phoneNumber,
            draftAvatarUrl: avatarUrl,
        });
    },
    toggleEditMode() {
        const nextEditMode = !this.data.editMode;
        this.setData({
            editMode: nextEditMode,
            draftNickname: this.data.nickname === '未设置' ? '' : this.data.nickname,
            draftEmail: this.data.email === '未设置' ? '' : this.data.email,
            draftPhoneNumber: this.data.phoneNumber === '未设置' ? '' : this.data.phoneNumber,
            draftAvatarUrl: this.data.avatarUrl,
        });
    },
    onNicknameInput(e) {
        this.setData({ draftNickname: e.detail?.value ?? '' });
    },
    onEmailInput(e) {
        this.setData({ draftEmail: e.detail?.value ?? '' });
    },
    onPhoneInput(e) {
        this.setData({ draftPhoneNumber: e.detail?.value ?? '' });
    },
    onChooseAvatar(e) {
        const avatarUrl = (e.detail?.avatarUrl || '').trim();
        if (!avatarUrl) {
            return;
        }
        this.setData({ draftAvatarUrl: avatarUrl });
    },
    async useWechatNickname() {
        try {
            const res = await this.getWechatUserInfo();
            const nickname = (res.userInfo?.nickName || '').trim();
            const avatarUrl = (res.userInfo?.avatarUrl || '').trim();
            this.setData({
                draftNickname: nickname || this.data.draftNickname,
                draftAvatarUrl: avatarUrl || this.data.draftAvatarUrl,
            });
        }
        catch {
            wx.showToast({ title: '获取微信昵称失败', icon: 'none' });
        }
    },
    async saveProfile() {
        if (this.data.savingProfile) {
            return;
        }
        this.setData({ savingProfile: true });
        try {
            const updated = await (0, api_1.updateMiniappUserProfile)({
                openId: this.data.userId,
                nickname: this.data.draftNickname,
                email: this.data.draftEmail,
                phoneNumber: this.data.draftPhoneNumber,
                avatar: this.data.draftAvatarUrl,
            });
            this.setProfileData(updated);
            this.setData({ editMode: false });
            wx.showToast({ title: '保存成功', icon: 'success' });
        }
        catch {
            wx.showToast({ title: '保存失败', icon: 'none' });
        }
        finally {
            this.setData({ savingProfile: false });
        }
    },
    async onGetPhoneNumber(e) {
        if (!e.detail?.errMsg?.includes('ok')) {
            wx.showToast({ title: '未授权手机号', icon: 'none' });
            return;
        }
        if (!e.detail.encryptedData || !e.detail.iv) {
            wx.showToast({ title: '手机号数据缺失', icon: 'none' });
            return;
        }
        try {
            const updated = await (0, api_1.updateMiniappUserPhoneByEncryptedData)({
                openId: this.data.userId,
                encryptedData: e.detail.encryptedData,
                iv: e.detail.iv,
            });
            this.setProfileData(updated);
            wx.showToast({ title: '手机号已更新', icon: 'success' });
        }
        catch {
            wx.showToast({ title: '手机号更新失败', icon: 'none' });
        }
    },
    getWechatUserInfo() {
        return new Promise((resolve, reject) => {
            if (wx.getUserProfile) {
                wx.getUserProfile({
                    desc: '用于自动填充昵称与头像',
                    success: (res) => resolve(res),
                    fail: (err) => reject(err),
                });
                return;
            }
            wx.getUserInfo({
                success: (res) => resolve(res),
                fail: (err) => reject(err),
            });
        });
    },
    goHistory() {
        wx.navigateTo({ url: '/pages/history/index' });
    },
    goFortune() {
        wx.navigateTo({ url: '/pages/fortune-input/index' });
    },
    goAbout() {
        wx.navigateTo({ url: '/pages/about/index' });
    },
    goMemberCenter() {
        wx.switchTab({ url: '/pages/member/index' });
    },
    goHome() {
        wx.switchTab({ url: '/pages/home/index' });
    },
});
