"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.formatBeijingTime = formatBeijingTime;
function formatBeijingTime(value) {
    const normalized = /(Z|[+-]\d{2}:\d{2})$/i.test(value) ? value : `${value}Z`;
    const date = new Date(normalized);
    if (Number.isNaN(date.getTime())) {
        return '-';
    }
    const beijing = new Date(date.getTime() + 8 * 60 * 60 * 1000);
    const year = beijing.getUTCFullYear();
    const month = `${beijing.getUTCMonth() + 1}`.padStart(2, '0');
    const day = `${beijing.getUTCDate()}`.padStart(2, '0');
    const hours = `${beijing.getUTCHours()}`.padStart(2, '0');
    const minutes = `${beijing.getUTCMinutes()}`.padStart(2, '0');
    return `${year}-${month}-${day} ${hours}:${minutes}`;
}
