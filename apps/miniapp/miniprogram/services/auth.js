"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.getStoredOpenId = getStoredOpenId;
exports.ensureMiniappLogin = ensureMiniappLogin;
const api_1 = require("./api");
const OPEN_ID_STORAGE_KEY = 'miniappOpenId';
let loginPromise = null;
function getStoredOpenId() {
    const cached = wx.getStorageSync(OPEN_ID_STORAGE_KEY);
    if (typeof cached === 'string' && cached.trim().length > 0) {
        return cached;
    }
    return '';
}
async function ensureMiniappLogin() {
    const app = getApp();
    const fromGlobal = app?.globalData?.openId?.trim() || app?.globalData?.userId?.trim() || '';
    if (fromGlobal) {
        return fromGlobal;
    }
    const fromStorage = getStoredOpenId();
    if (fromStorage) {
        app.globalData.openId = fromStorage;
        app.globalData.userId = fromStorage;
        return fromStorage;
    }
    if (!loginPromise) {
        loginPromise = loginByWxCode().finally(() => {
            loginPromise = null;
        });
    }
    return loginPromise;
}
async function loginByWxCode() {
    const loginRes = await wxLogin();
    const code = (loginRes.code || '').trim();
    if (!code) {
        throw new Error('获取微信登录 code 失败');
    }
    const payload = await (0, api_1.request)('/miniapp/users/login-by-code', 'POST', { code });
    if (!payload.openId || payload.openId.trim().length === 0) {
        throw new Error('登录响应缺少 openId');
    }
    const openId = payload.openId.trim();
    wx.setStorageSync(OPEN_ID_STORAGE_KEY, openId);
    const app = getApp();
    app.globalData.openId = openId;
    app.globalData.userId = openId;
    return openId;
}
function wxLogin() {
    return new Promise((resolve, reject) => {
        wx.login({
            success: (res) => resolve(res),
            fail: (err) => reject(err),
        });
    });
}
