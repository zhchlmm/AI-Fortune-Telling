"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
const tabbar_1 = require("../../utils/tabbar");
Page({
    data: {},
    onLoad() { },
    onShow() {
        (0, tabbar_1.syncCustomTabBarSelected)(this, 0);
    },
    onReachBottom() {
        this.getContentListComponent()?.loadMore?.();
    },
    getContentListComponent() {
        return this.selectComponent('#contentList');
    },
    goContentDetail(e) {
        const id = e.detail.id;
        if (!id) {
            return;
        }
        wx.navigateTo({ url: `/pages/content-detail/index?id=${id}` });
    },
    onFortuneSubmit(e) {
        const { fortuneType, question } = e.detail;
        wx.navigateTo({
            url: `/pages/fortune-input/index?autoSubmit=1&fortuneType=${encodeURIComponent(fortuneType)}` +
                `&question=${encodeURIComponent(question)}`,
        });
    },
});
