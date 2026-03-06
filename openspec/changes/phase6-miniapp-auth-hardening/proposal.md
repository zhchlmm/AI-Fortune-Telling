## Why

当前小程序接口主要依赖 `openId` 直传，缺少有效防伪机制，存在伪造请求访问他人资料的风险。
需要为小程序引入独立 token 登录态，并在资料接口强制鉴权。

## What Changes

- 小程序登录接口返回 `accessToken` 与过期时间。
- 新增小程序 token 签发与校验逻辑，使用独立 claim（`miniapp_openid`）。
- 小程序资料接口改为从 token 解析用户身份，不再信任客户端传入 `openId`。
- 小程序端请求头自动携带 `Authorization: Bearer <token>` 并处理过期重登。

## Capabilities

### New Capabilities
- `miniapp-token-auth-hardening`: 小程序 token 鉴权与接口身份绑定能力。

### Modified Capabilities
- `miniapp-auto-login`: 自动登录从缓存 `openId` 升级为缓存 token + openId。

## Impact

- 影响 `MiniappUsersController`、认证配置、DTO 模型。
- 影响小程序 `services/auth.ts`、`services/api.ts` 请求封装与缓存结构。
