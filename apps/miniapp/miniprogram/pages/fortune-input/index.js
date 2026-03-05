"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
const api_1 = require("../../services/api");
const dev_1 = require("../../env/dev");
function safeDecode(value) {
    if (!value) {
        return '';
    }
    try {
        return decodeURIComponent(value);
    }
    catch {
        return value;
    }
}
Page({
    data: {
        submitting: false,
        uploadedPhotoUrlMap: {},
        uploadProgressVisible: false,
        uploadProgressCurrent: 0,
        uploadProgressTotal: 0,
        uploadProgressPercent: 0,
        canCancelUpload: false,
        prefillQuestion: '',
        prefillFortuneType: 'Tarot',
        streamResult: '',
        streamFinished: false,
        streamSessionId: '',
        streamError: '',
    },
    onLoad(options) {
        const question = safeDecode(options.question).trim();
        const fortuneType = safeDecode(options.fortuneType).trim() || 'Tarot';
        const autoSubmit = options.autoSubmit === '1' && question.length > 0;
        this.setData({
            prefillQuestion: question,
            prefillFortuneType: fortuneType,
        });
        if (autoSubmit) {
            ;
            this.autoSubmitPayload = {
                fortuneType,
                question,
            };
        }
    },
    onReady() {
        const payload = this.autoSubmitPayload;
        if (!payload) {
            return;
        }
        ;
        this.autoSubmitPayload = undefined;
        this.onFortuneSubmit({
            detail: {
                fortuneType: payload.fortuneType,
                question: payload.question,
                photoPaths: [],
                allPhotoPaths: [],
            },
        });
    },
    getFortuneFormComponent() {
        return this.selectComponent('#fortuneForm');
    },
    async onFortuneSubmit(e) {
        if (this.data.submitting) {
            return;
        }
        const { fortuneType, question } = e.detail;
        const photoPaths = e.detail.photoPaths ?? [];
        const allPhotoPaths = e.detail.allPhotoPaths ?? photoPaths;
        const uploadedPhotoUrlMap = { ...this.data.uploadedPhotoUrlMap };
        for (const key of Object.keys(uploadedPhotoUrlMap)) {
            if (!allPhotoPaths.includes(key)) {
                delete uploadedPhotoUrlMap[key];
            }
        }
        const pendingPhotoPaths = photoPaths.filter((path) => !uploadedPhotoUrlMap[path]);
        const fortuneForm = this.getFortuneFormComponent();
        let uploadController = null;
        this.setData({
            submitting: true,
            streamResult: '',
            streamFinished: false,
            streamSessionId: '',
            streamError: '',
        });
        try {
            const app = getApp();
            if (pendingPhotoPaths.length > 0) {
                uploadController = (0, api_1.createUploadCancelController)();
                this.uploadController = uploadController;
                this.setData({
                    uploadProgressVisible: true,
                    uploadProgressCurrent: 0,
                    uploadProgressTotal: pendingPhotoPaths.length,
                    uploadProgressPercent: 0,
                    canCancelUpload: true,
                });
                wx.showLoading({ title: `上传照片 0/${pendingPhotoPaths.length}`, mask: true });
            }
            const uploadedUrls = await (0, api_1.uploadImages)(pendingPhotoPaths, {
                maxRetries: 1,
                concurrency: 2,
                controller: uploadController ?? undefined,
                onProgress: ({ current, total }) => {
                    this.setData({
                        uploadProgressCurrent: current,
                        uploadProgressTotal: total,
                        uploadProgressPercent: total > 0 ? Math.floor((current / total) * 100) : 0,
                    });
                    wx.showLoading({ title: `上传照片 ${current}/${total}`, mask: true });
                },
            });
            pendingPhotoPaths.forEach((path, index) => {
                const url = uploadedUrls[index];
                if (url) {
                    uploadedPhotoUrlMap[path] = url;
                }
            });
            this.setData({ uploadedPhotoUrlMap });
            const photoUrls = allPhotoPaths.map((path) => uploadedPhotoUrlMap[path]).filter((item) => !!item);
            if (photoUrls.length !== allPhotoPaths.length) {
                const failed = allPhotoPaths.filter((path) => !uploadedPhotoUrlMap[path]);
                fortuneForm?.setFailedPhotoPaths?.(failed);
                wx.hideLoading();
                this.setData({ uploadProgressVisible: false, canCancelUpload: false });
                wx.showToast({ title: '仍有失败图片，请重传失败项', icon: 'none' });
                return;
            }
            wx.hideLoading();
            this.setData({ uploadProgressVisible: false, canCancelUpload: false });
            fortuneForm?.clearFailedPhotoPaths?.();
            const session = dev_1.env.fortuneTransport === 'copilot-ws'
                ? await (0, api_1.createFortuneSessionByWebSocket)({
                    userId: app.globalData.userId,
                    fortuneType,
                    question,
                    onDelta: (_delta, fullText) => {
                        this.setData({
                            streamResult: fullText,
                        });
                    },
                })
                : await (0, api_1.createFortuneSession)({
                    userId: app.globalData.userId,
                    fortuneType,
                    parameters: {
                        question,
                        photoUrls: JSON.stringify(photoUrls),
                    },
                });
            wx.hideLoading();
            this.setData({
                streamResult: session.resultSummary,
                streamFinished: true,
                streamSessionId: session.id,
            });
        }
        catch (error) {
            wx.hideLoading();
            this.setData({ uploadProgressVisible: false, canCancelUpload: false });
            if ((0, api_1.isUploadCanceledError)(error)) {
                wx.showToast({ title: '已取消上传', icon: 'none' });
                return;
            }
            if (error instanceof api_1.UploadBatchError) {
                fortuneForm?.setFailedPhotoPaths?.([error.failedPath]);
            }
            const message = error instanceof Error ? error.message : '提交失败';
            this.setData({ streamError: message });
            wx.showToast({ title: message, icon: 'none' });
        }
        finally {
            ;
            this.uploadController = undefined;
            this.setData({ submitting: false });
        }
    },
    async onRetryFailed(e) {
        await this.onFortuneSubmit(e);
    },
    onCancelUpload() {
        const controller = this.uploadController;
        controller?.cancel();
    },
});
