export function syncCustomTabBarSelected(page: WechatMiniprogram.Page.TrivialInstance, selected: number) {
  const tabBar = (page as WechatMiniprogram.Page.TrivialInstance & {
    getTabBar?: () => WechatMiniprogram.Component.TrivialInstance | null
  }).getTabBar?.()

  tabBar?.setData?.({ selected })
}
