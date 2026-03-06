"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
const auth_1 = require("./services/auth");
App({
    globalData: {
        userId: '',
        openId: '',
    },
    async onLaunch() {
        const cachedOpenId = (0, auth_1.getStoredOpenId)();
        if (cachedOpenId) {
            this.globalData.openId = cachedOpenId;
            this.globalData.userId = cachedOpenId;
            return;
        }
        try {
            const openId = await (0, auth_1.ensureMiniappLogin)();
            this.globalData.openId = openId;
            this.globalData.userId = openId;
        }
        catch {
            // 登录失败时不阻断应用，页面内按需重试登录。
        }
    },
});
