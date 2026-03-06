## Why

手机号授权链路对微信错误码、解密失败和用户拒绝场景处理不够细，容易造成用户体验波动和问题排查困难。

## What Changes

- 后端手机号接口增加错误分类与可观测日志。
- 小程序端增加手机号授权失败分级提示与重试引导。
- 补充手机号相关测试：成功、拒绝、解密失败、sessionKey 缺失。

## Capabilities

### New Capabilities
- `miniapp-phone-reliability`: 微信手机号获取可靠性与错误分级能力。

### Modified Capabilities
- `wechat-phone-binding`: 增强失败处理与重试行为。

## Impact

- 影响 `MiniappUsersController`、测试工程。
- 影响小程序 profile 手机号授权交互提示。
