"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.syncCustomTabBarSelected = syncCustomTabBarSelected;
function syncCustomTabBarSelected(page, selected) {
    const tabBar = page.getTabBar?.();
    tabBar?.setData?.({ selected });
}
