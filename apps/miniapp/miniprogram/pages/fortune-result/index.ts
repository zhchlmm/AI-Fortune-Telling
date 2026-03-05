const RESULT_FORTUNE_TYPE_LABEL_MAP: Record<string, string> = {
  Tarot: '塔罗',
  Zodiac: '星座',
  Bazi: '八字',
  Palmistry: '手相',
  Physiognomy: '面相',
  FengShui: '风水',
}

function formatFortuneType(value: string) {
  return RESULT_FORTUNE_TYPE_LABEL_MAP[value] ?? value
}

Page({
  data: {
    id: '',
    result: '',
    fortuneType: '',
    displayFortuneType: '',
  },
  onLoad(query: Record<string, string>) {
    const fortuneType = query.fortuneType ?? ''
    this.setData({
      id: query.id ?? '',
      result: decodeURIComponent(query.result ?? ''),
      fortuneType,
      displayFortuneType: formatFortuneType(fortuneType),
    })
  },
  goAgain() {
    wx.redirectTo({ url: '/pages/fortune-input/index' })
  },
  goHistory() {
    wx.switchTab({ url: '/pages/history/index' })
  },
  goHome() {
    wx.switchTab({ url: '/pages/home/index' })
  },
})
