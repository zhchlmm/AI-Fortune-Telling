"use strict";
const FORTUNE_TYPE_LABEL_MAP = {
    Tarot: '塔罗',
    Zodiac: '星座',
    Bazi: '八字',
    Palmistry: '手相',
    Physiognomy: '面相',
    FengShui: '风水',
};
const DEFAULT_FORTUNE_TYPE_OPTIONS = ['Tarot', 'Zodiac', 'Bazi', 'Palmistry', 'Physiognomy', 'FengShui'];
const MAX_IMAGE_SIZE_BYTES = 10 * 1024 * 1024;
function extractInputValue(event) {
    const detail = event?.detail;
    if (typeof detail === 'string') {
        return detail;
    }
    if (detail && typeof detail === 'object') {
        const detailValue = detail.value;
        if (typeof detailValue === 'string') {
            return detailValue;
        }
        const currentValue = detail.current;
        if (typeof currentValue === 'string') {
            return currentValue;
        }
    }
    const targetValue = event?.target?.value;
    if (typeof targetValue === 'string') {
        return targetValue;
    }
    return null;
}
Component({
    properties: {
        submitText: {
            type: String,
            value: '提交占卜',
        },
        questionPlaceholder: {
            type: String,
            value: '例如：最近换工作是否合适？',
        },
        maxLength: {
            type: Number,
            value: 200,
        },
        initialQuestion: {
            type: String,
            value: '',
        },
        initialFortuneType: {
            type: String,
            value: '',
        },
        loading: {
            type: Boolean,
            value: false,
        },
        uploadProgressVisible: {
            type: Boolean,
            value: false,
        },
        uploadProgressCurrent: {
            type: Number,
            value: 0,
        },
        uploadProgressTotal: {
            type: Number,
            value: 0,
        },
        uploadProgressPercent: {
            type: Number,
            value: 0,
        },
        canCancelUpload: {
            type: Boolean,
            value: false,
        },
        fortuneTypeOptions: {
            type: Array,
            value: DEFAULT_FORTUNE_TYPE_OPTIONS,
        },
        maxPhotoCount: {
            type: Number,
            value: 3,
        },
    },
    data: {
        fortuneTypeValueOptions: DEFAULT_FORTUNE_TYPE_OPTIONS,
        fortuneTypeDisplayOptions: ['塔罗', '星座', '八字', '手相', '面相', '风水'],
        fortuneTypeIndex: 0,
        fortuneType: 'Tarot',
        fortuneTypeLabel: '塔罗',
        userQuestion: '',
        photoPaths: [],
        failedPhotoPaths: [],
    },
    observers: {
        fortuneTypeOptions() {
            this.initFortuneTypeOptions();
        },
        initialQuestion() {
            this.applyInitialQuestion();
        },
        initialFortuneType() {
            this.applyInitialFortuneType();
        },
    },
    lifetimes: {
        attached() {
            this.initFortuneTypeOptions();
            this.applyInitialQuestion();
            this.applyInitialFortuneType();
        },
    },
    methods: {
        initFortuneTypeOptions() {
            const valueOptions = (this.properties.fortuneTypeOptions ?? []).filter((item) => typeof item === 'string' && item.trim().length > 0);
            const normalizedValueOptions = valueOptions.length > 0 ? valueOptions : DEFAULT_FORTUNE_TYPE_OPTIONS;
            const displayOptions = normalizedValueOptions.map((item) => FORTUNE_TYPE_LABEL_MAP[item] ?? item);
            const currentIndex = Math.min(this.data.fortuneTypeIndex, normalizedValueOptions.length - 1);
            this.setData({
                fortuneTypeValueOptions: normalizedValueOptions,
                fortuneTypeDisplayOptions: displayOptions,
                fortuneTypeIndex: currentIndex,
                fortuneType: normalizedValueOptions[currentIndex] ?? 'Tarot',
                fortuneTypeLabel: displayOptions[currentIndex] ?? '塔罗',
            });
            this.applyInitialFortuneType();
        },
        applyInitialQuestion() {
            const initialQuestion = String(this.properties.initialQuestion ?? '').trim();
            if (!initialQuestion) {
                return;
            }
            if (initialQuestion === this.data.userQuestion) {
                return;
            }
            this.setData({ userQuestion: initialQuestion });
        },
        applyInitialFortuneType() {
            const initialFortuneType = String(this.properties.initialFortuneType ?? '').trim();
            if (!initialFortuneType) {
                return;
            }
            const valueOptions = this.data.fortuneTypeValueOptions;
            const displayOptions = this.data.fortuneTypeDisplayOptions;
            const index = valueOptions.indexOf(initialFortuneType);
            if (index < 0) {
                return;
            }
            if (this.data.fortuneType === initialFortuneType && this.data.fortuneTypeIndex === index) {
                return;
            }
            this.setData({
                fortuneTypeIndex: index,
                fortuneType: valueOptions[index] ?? 'Tarot',
                fortuneTypeLabel: displayOptions[index] ?? '塔罗',
            });
        },
        onTypeChange(e) {
            const index = Number(e.detail.value);
            const valueOptions = this.data.fortuneTypeValueOptions;
            const displayOptions = this.data.fortuneTypeDisplayOptions;
            this.setData({
                fortuneTypeIndex: index,
                fortuneType: valueOptions[index] ?? 'Tarot',
                fortuneTypeLabel: displayOptions[index] ?? '塔罗',
            });
        },
        onQuestionInput(e) {
            const value = extractInputValue(e);
            if (value === null) {
                return;
            }
            this.setData({ userQuestion: value });
        },
        choosePhotos() {
            const remain = Math.max(this.properties.maxPhotoCount - this.data.photoPaths.length, 0);
            if (remain <= 0) {
                wx.showToast({ title: `最多上传${this.properties.maxPhotoCount}张`, icon: 'none' });
                return;
            }
            wx.chooseMedia({
                count: remain,
                mediaType: ['image'],
                sizeType: ['compressed'],
                sourceType: ['album', 'camera'],
                success: (res) => {
                    const tempFiles = res.tempFiles ?? [];
                    const validPaths = [];
                    let oversizedCount = 0;
                    let invalidTypeCount = 0;
                    for (const file of tempFiles) {
                        const filePath = file.tempFilePath;
                        if (!filePath) {
                            continue;
                        }
                        const fileType = (file.fileType ?? '').toLowerCase();
                        if (fileType && fileType !== 'image') {
                            invalidTypeCount += 1;
                            continue;
                        }
                        if (typeof file.size === 'number' && file.size > MAX_IMAGE_SIZE_BYTES) {
                            oversizedCount += 1;
                            continue;
                        }
                        validPaths.push(filePath);
                    }
                    if (oversizedCount > 0 || invalidTypeCount > 0) {
                        const messages = [];
                        if (oversizedCount > 0) {
                            messages.push(`${oversizedCount}张超过10MB`);
                        }
                        if (invalidTypeCount > 0) {
                            messages.push(`${invalidTypeCount}张不是图片`);
                        }
                        wx.showToast({ title: messages.join('，'), icon: 'none' });
                    }
                    if (validPaths.length === 0) {
                        return;
                    }
                    const added = validPaths;
                    const merged = this.data.photoPaths.concat(added).slice(0, this.properties.maxPhotoCount);
                    this.setData({
                        photoPaths: merged,
                        failedPhotoPaths: this.data.failedPhotoPaths.filter((path) => merged.includes(path)),
                    });
                },
            });
        },
        removePhoto(e) {
            const index = Number(e.currentTarget.dataset.index);
            if (Number.isNaN(index)) {
                return;
            }
            const next = this.data.photoPaths.filter((_, currentIndex) => currentIndex !== index);
            this.setData({
                photoPaths: next,
                failedPhotoPaths: this.data.failedPhotoPaths.filter((path) => next.includes(path)),
            });
        },
        previewPhoto(e) {
            const url = e.currentTarget.dataset.url;
            if (!url) {
                return;
            }
            wx.previewImage({
                current: url,
                urls: this.data.photoPaths,
            });
        },
        setFailedPhotoPaths(paths) {
            const current = this.data.photoPaths;
            const filtered = paths.filter((item) => current.includes(item));
            this.setData({ failedPhotoPaths: filtered });
        },
        clearFailedPhotoPaths() {
            this.setData({ failedPhotoPaths: [] });
        },
        onRetryFailedTap() {
            const question = this.data.userQuestion.trim();
            if (!question) {
                wx.showToast({ title: '请输入你的问题', icon: 'none' });
                return;
            }
            const failedPhotoPaths = this.data.failedPhotoPaths;
            if (failedPhotoPaths.length === 0) {
                wx.showToast({ title: '暂无失败图片', icon: 'none' });
                return;
            }
            const detail = {
                fortuneType: this.data.fortuneType,
                question,
                photoPaths: failedPhotoPaths,
                allPhotoPaths: this.data.photoPaths,
            };
            this.triggerEvent('retryfailed', detail);
        },
        onCancelUploadTap() {
            this.triggerEvent('cancelupload');
        },
        onSubmitTap() {
            if (this.data.loading) {
                return;
            }
            const question = this.data.userQuestion.trim();
            if (!question) {
                wx.showToast({ title: '请输入你的问题', icon: 'none' });
                return;
            }
            if (question.length < 4) {
                wx.showToast({ title: '问题至少 4 个字', icon: 'none' });
                return;
            }
            const detail = {
                fortuneType: this.data.fortuneType,
                question,
                photoPaths: this.data.photoPaths,
                allPhotoPaths: this.data.photoPaths,
            };
            this.triggerEvent('submit', detail);
        },
    },
});
