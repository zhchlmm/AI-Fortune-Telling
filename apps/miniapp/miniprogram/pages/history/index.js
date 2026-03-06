"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
const api_1 = require("../../services/api");
const auth_1 = require("../../services/auth");
const datetime_1 = require("../../utils/datetime");
const FORTUNE_TYPE_LABEL_MAP = {
    Tarot: '塔罗',
    Zodiac: '星座',
    Bazi: '八字',
    Palmistry: '手相',
    Physiognomy: '面相',
    FengShui: '风水',
};
function formatFortuneType(value) {
    return FORTUNE_TYPE_LABEL_MAP[value] ?? value;
}
Page({
    data: {
        history: [],
        loading: false,
        loadingMore: false,
        page: 1,
        pageSize: 10,
        hasMore: true,
    },
    onShow() {
        void this.loadHistory(true);
    },
    onReachBottom() {
        void this.loadHistory(false);
    },
    async loadHistory(reset = true) {
        if (this.data.loading || this.data.loadingMore) {
            return;
        }
        if (!reset && !this.data.hasMore) {
            return;
        }
        const nextPage = reset ? 1 : this.data.page;
        this.setData({
            loading: reset,
            loadingMore: !reset,
        });
        try {
            const userId = await (0, auth_1.ensureMiniappLogin)();
            const app = getApp();
            app.globalData.userId = userId;
            const history = await (0, api_1.getFortuneHistory)(app.globalData.userId, nextPage, this.data.pageSize);
            const mapped = history.map((item) => ({
                ...item,
                displayTime: (0, datetime_1.formatBeijingTime)(item.createdAt),
                displayFortuneType: formatFortuneType(item.fortuneType),
            }));
            const merged = reset ? mapped : this.data.history.concat(mapped);
            const hasMore = mapped.length === this.data.pageSize;
            this.setData({
                history: merged,
                page: nextPage + 1,
                hasMore,
            });
        }
        catch {
            wx.showToast({ title: '加载历史失败', icon: 'none' });
        }
        finally {
            this.setData({ loading: false, loadingMore: false });
        }
    },
    viewDetail(e) {
        const { id, result, fortuneType } = e.currentTarget.dataset;
        if (!id) {
            return;
        }
        wx.navigateTo({
            url: `/pages/fortune-result/index?id=${id}&result=${encodeURIComponent(result ?? '')}&fortuneType=${encodeURIComponent(fortuneType ?? '')}`,
        });
    },
});
