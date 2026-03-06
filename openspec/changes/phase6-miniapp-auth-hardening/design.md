## Context

已实现小程序自动登录与资料维护，但接口鉴权仍偏弱。需要在不影响现有用户体验的前提下，升级为 token 身份模型。

## Goals / Non-Goals

**Goals:**
- 登录返回 token，资料接口强制校验 token。
- token 绑定 openId，服务端以 token 身份为准。
- 客户端支持 token 过期自动重登。

**Non-Goals:**
- 不引入 refresh token 多端会话管理。
- 不实现复杂风控策略（设备指纹、IP 画像）。

## Decisions

- 复用现有 JWT 基础设施，新增 `MiniappJwtOptions` 与 `MiniappTokenService`。
- 小程序接口使用 `[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]`，并通过 claim `miniapp_openid` 获取身份。
- 登录响应增加 `expiresAt`，前端缓存 `miniapp_access_token` 与 `miniapp_openid`。
