"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
const tabbar_1 = require("../../utils/tabbar");
Page({
    data: {
        currentMemberType: '免费会员',
        plans: [
            {
                name: 'VIP会员',
                originPrice: '19.9元/月',
                salePrice: '9.9元/月',
                yearPrice: '99元',
                benefits: [
                    '免费AI算命',
                    '会员专享大模型（更准确）',
                    '生辰八字、看相、卜卦、星座、塔罗等',
                    '全站命理知识免费观看',
                    '大师直播参与权每季度1次',
                ],
            },
            {
                name: '黄金VIP会员',
                originPrice: '39.9元/月',
                salePrice: '19.9元/月',
                yearPrice: '199元',
                benefits: [
                    '免费AI算命',
                    '会员专享大模型（更准确）',
                    '生辰八字、看相、卜卦、星座、塔罗等',
                    '全站命理知识免费观看',
                    '大师直播参与权每月1次',
                ],
            },
            {
                name: '钻石VIP会员',
                originPrice: '99.9元/月',
                salePrice: '69.9元/月',
                yearPrice: '699元',
                benefits: [
                    '免费AI算命',
                    '会员专享大模型（更准确）',
                    '生辰八字、看相、卜卦、风水、星座、塔罗等',
                    '全站命理知识免费观看',
                    '大师直播参与权每月2次',
                    '大师1对1服务每年1次',
                ],
            },
        ],
    },
    onShow() {
        (0, tabbar_1.syncCustomTabBarSelected)(this, 2);
    },
    onSubscribe(e) {
        const plan = e.currentTarget.dataset.plan || '会员';
        const period = e.currentTarget.dataset.period || '包月';
        wx.showToast({ title: `${plan}${period}开通功能建设中`, icon: 'none' });
    },
});
