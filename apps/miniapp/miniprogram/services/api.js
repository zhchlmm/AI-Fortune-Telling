"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.UploadBatchError = void 0;
exports.request = request;
exports.createUploadCancelController = createUploadCancelController;
exports.isUploadCanceledError = isUploadCanceledError;
exports.createFortuneSession = createFortuneSession;
exports.createFortuneSessionByWebSocket = createFortuneSessionByWebSocket;
exports.getFortuneHistory = getFortuneHistory;
exports.getMiniappUserProfile = getMiniappUserProfile;
exports.updateMiniappUserProfile = updateMiniappUserProfile;
exports.updateMiniappUserPhoneByEncryptedData = updateMiniappUserPhoneByEncryptedData;
exports.getContents = getContents;
exports.getContentById = getContentById;
exports.getContentCategories = getContentCategories;
exports.uploadImage = uploadImage;
exports.uploadImages = uploadImages;
const dev_1 = require("../env/dev");
function request(url, method, data) {
    return new Promise((resolve, reject) => {
        wx.request({
            url: `${dev_1.env.apiBaseUrl}${url}`,
            method,
            data,
            success: (res) => {
                if (res.statusCode >= 200 && res.statusCode < 300) {
                    resolve(res.data);
                    return;
                }
                reject(new Error(`Request failed: ${res.statusCode}`));
            },
            fail: (err) => reject(err),
        });
    });
}
function createUploadCancelController() {
    const controller = {
        cancelled: false,
        abortTasks: new Set(),
        cancel() {
            if (controller.cancelled) {
                return;
            }
            controller.cancelled = true;
            controller.abortTasks.forEach((task) => {
                try {
                    task.abort();
                }
                catch {
                    // ignore
                }
            });
            controller.abortTasks.clear();
        },
    };
    return controller;
}
function isUploadCanceledError(error) {
    return error instanceof Error && error.message === '上传已取消';
}
class UploadBatchError extends Error {
    constructor(failedIndex, failedPath, message) {
        super(message);
        this.name = 'UploadBatchError';
        this.failedIndex = failedIndex;
        this.failedPath = failedPath;
    }
}
exports.UploadBatchError = UploadBatchError;
function parseErrorMessage(payload, statusCode) {
    if (!payload) {
        return `上传失败（${statusCode}）`;
    }
    try {
        const parsed = JSON.parse(payload);
        if (typeof parsed.message === 'string' && parsed.message.trim().length > 0) {
            return parsed.message;
        }
        if (typeof parsed.title === 'string' && parsed.title.trim().length > 0) {
            return parsed.title;
        }
    }
    catch {
        if (payload.trim().length > 0) {
            return payload;
        }
    }
    return `上传失败（${statusCode}）`;
}
function normalizeQuality(value) {
    if (typeof value !== 'number' || Number.isNaN(value)) {
        return 75;
    }
    return Math.min(100, Math.max(40, Math.round(value)));
}
function compressImageForUpload(filePath, quality) {
    return new Promise((resolve) => {
        wx.compressImage({
            src: filePath,
            quality: normalizeQuality(quality),
            compressedWidth: 1600,
            compressedHeight: 1600,
            success: (res) => resolve(res.tempFilePath || filePath),
            fail: () => resolve(filePath),
        });
    });
}
function createFortuneSession(payload) {
    return request('/fortune-sessions', 'POST', payload);
}
function createFortuneSessionByWebSocket(options) {
    return new Promise((resolve, reject) => {
        const socket = wx.connectSocket({
            url: dev_1.env.copilotWsUrl,
            timeout: 120000,
        });
        let fullText = '';
        let closed = false;
        const finishWithError = (message) => {
            if (closed) {
                return;
            }
            closed = true;
            try {
                socket.close({ code: 1000, reason: 'client-finish' });
            }
            catch {
                // ignore
            }
            reject(new Error(message));
        };
        socket.onOpen(() => {
            const payload = {
                type: 'fortune.start',
                userId: options.userId,
                fortuneType: options.fortuneType,
                question: options.question,
            };
            socket.send({
                data: JSON.stringify(payload),
            });
        });
        socket.onMessage((res) => {
            try {
                const raw = typeof res.data === 'string' ? res.data : '';
                const message = JSON.parse(raw);
                if (message.type === 'delta') {
                    fullText += message.text;
                    options.onDelta?.(message.text, fullText);
                    return;
                }
                if (message.type === 'done') {
                    if (closed) {
                        return;
                    }
                    closed = true;
                    try {
                        socket.close({ code: 1000, reason: 'completed' });
                    }
                    catch {
                        // ignore
                    }
                    resolve({
                        id: message.id,
                        userId: options.userId,
                        fortuneType: message.fortuneType || options.fortuneType,
                        resultSummary: message.result || fullText,
                        createdAt: new Date().toISOString(),
                    });
                    return;
                }
                if (message.type === 'error') {
                    finishWithError(message.message || '算命服务异常');
                }
            }
            catch {
                finishWithError('算命服务响应解析失败');
            }
        });
        socket.onError(() => {
            finishWithError('WebSocket连接失败');
        });
        socket.onClose((res) => {
            if (!closed && res.code !== 1000) {
                finishWithError('WebSocket连接已关闭');
            }
        });
    });
}
function getFortuneHistory(userId, page = 1, pageSize = 10) {
    return request(`/fortune-sessions?userId=${encodeURIComponent(userId)}&page=${page}&pageSize=${pageSize}`, 'GET');
}
function getMiniappUserProfile(openId) {
    return request(`/miniapp/users/profile?openId=${encodeURIComponent(openId)}`, 'GET');
}
function updateMiniappUserProfile(payload) {
    return request('/miniapp/users/profile', 'PUT', payload);
}
function updateMiniappUserPhoneByEncryptedData(payload) {
    return request('/miniapp/users/phone', 'POST', payload);
}
function getContents(options) {
    const query = [];
    if (typeof options === 'string') {
        query.push(`categoryId=${encodeURIComponent(options)}`);
    }
    else if (options) {
        if (options.categoryId) {
            query.push(`categoryId=${encodeURIComponent(options.categoryId)}`);
        }
        if (typeof options.page === 'number') {
            query.push(`page=${options.page}`);
        }
        if (typeof options.pageSize === 'number') {
            query.push(`pageSize=${options.pageSize}`);
        }
    }
    const suffix = query.length > 0 ? `?${query.join('&')}` : '';
    return request(`/contents${suffix}`, 'GET');
}
function getContentById(id) {
    return request(`/contents/${encodeURIComponent(id)}`, 'GET');
}
function getContentCategories() {
    return request('/content-categories', 'GET');
}
function uploadImageByPath(filePath, controller) {
    return new Promise((resolve, reject) => {
        if (controller?.cancelled) {
            reject(new Error('上传已取消'));
            return;
        }
        const task = wx.uploadFile({
            url: `${dev_1.env.apiBaseUrl}/uploads/images`,
            filePath,
            name: 'file',
            success: (res) => {
                controller?.abortTasks.delete(task);
                if (res.statusCode < 200 || res.statusCode >= 300) {
                    reject(new Error(parseErrorMessage(res.data, res.statusCode)));
                    return;
                }
                try {
                    const parsed = JSON.parse(res.data);
                    resolve(parsed);
                }
                catch {
                    reject(new Error('上传失败：响应格式错误'));
                }
            },
            fail: (err) => {
                controller?.abortTasks.delete(task);
                if (controller?.cancelled || (typeof err?.errMsg === 'string' && err.errMsg.includes('abort'))) {
                    reject(new Error('上传已取消'));
                    return;
                }
                const message = typeof err?.errMsg === 'string' && err.errMsg.length > 0
                    ? err.errMsg
                    : '网络异常，无法连接上传服务';
                reject(new Error(message));
            },
        });
        controller?.abortTasks.add(task);
    });
}
async function uploadImage(filePath, controller, compressQuality) {
    const compressedPath = await compressImageForUpload(filePath, compressQuality);
    return uploadImageByPath(compressedPath, controller);
}
async function uploadImageWithRetry(filePath, maxRetries, controller, compressQuality) {
    let lastError = null;
    const compressedPath = await compressImageForUpload(filePath, compressQuality);
    for (let attempt = 0; attempt <= maxRetries; attempt += 1) {
        try {
            if (controller?.cancelled) {
                throw new Error('上传已取消');
            }
            return await uploadImageByPath(compressedPath, controller);
        }
        catch (error) {
            lastError = error;
            if (isUploadCanceledError(error)) {
                throw error;
            }
            if (attempt < maxRetries) {
                await new Promise((resolve) => setTimeout(resolve, 400));
            }
        }
    }
    throw lastError instanceof Error ? lastError : new Error('Upload failed');
}
async function uploadImages(filePaths, options) {
    const result = new Array(filePaths.length);
    const total = filePaths.length;
    const maxRetries = options?.maxRetries ?? 1;
    const concurrency = Math.max(1, options?.concurrency ?? 2);
    const compressQuality = options?.compressQuality;
    if (total === 0) {
        return [];
    }
    let nextIndex = 0;
    let completed = 0;
    let firstError = null;
    const worker = async () => {
        while (nextIndex < total && !firstError) {
            if (options?.controller?.cancelled) {
                firstError = new Error('上传已取消');
                return;
            }
            const index = nextIndex;
            nextIndex += 1;
            const filePath = filePaths[index];
            let uploaded;
            try {
                uploaded = await uploadImageWithRetry(filePath, maxRetries, options?.controller, compressQuality);
            }
            catch (error) {
                if (isUploadCanceledError(error)) {
                    firstError = error instanceof Error ? error : new Error('上传已取消');
                    return;
                }
                const message = error instanceof Error ? error.message : '上传失败';
                firstError = new UploadBatchError(index, filePath, `第${index + 1}张图片上传失败：${message}`);
                return;
            }
            result[index] = uploaded.url;
            completed += 1;
            options?.onProgress?.({ current: completed, total });
        }
    };
    const workers = Array.from({ length: Math.min(concurrency, total) }, () => worker());
    await Promise.all(workers);
    if (firstError) {
        throw firstError;
    }
    return result;
}
