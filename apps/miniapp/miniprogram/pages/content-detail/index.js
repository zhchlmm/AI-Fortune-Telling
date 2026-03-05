"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
const api_1 = require("../../services/api");
const datetime_1 = require("../../utils/datetime");
Page({
    data: {
        content: null,
        loading: false,
    },
    onLoad(query) {
        void this.loadContent(query.id);
    },
    async loadContent(id) {
        if (!id) {
            this.setData({ content: null });
            return;
        }
        this.setData({ loading: true });
        try {
            const target = await (0, api_1.getContentById)(id);
            this.setData({
                content: {
                    ...target,
                    displayTime: (0, datetime_1.formatBeijingTime)(target.publishedAt),
                },
            });
        }
        catch {
            wx.showToast({ title: '加载详情失败', icon: 'none' });
            this.setData({ content: null });
        }
        finally {
            this.setData({ loading: false });
        }
    },
});
