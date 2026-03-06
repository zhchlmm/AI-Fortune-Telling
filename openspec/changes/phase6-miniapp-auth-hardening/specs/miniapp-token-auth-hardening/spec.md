## ADDED Requirements

### Requirement: 小程序资料接口必须基于 token 鉴权
系统 MUST 要求客户端携带有效 Bearer token 才可访问和更新个人资料。

#### Scenario: 未携带 token 访问资料接口
- **WHEN** 客户端请求资料接口但未携带 Authorization
- **THEN** 系统返回 401 Unauthorized

#### Scenario: token 与资料归属一致
- **WHEN** 客户端携带有效 token 调用资料更新
- **THEN** 系统仅更新 token 对应 openId 的资料

### Requirement: 小程序登录必须返回可用 token
系统 MUST 在微信 code 登录成功后返回访问 token 与过期时间。

#### Scenario: 登录成功返回 token
- **WHEN** code 可正常换取 openId
- **THEN** 返回 `openId`、`accessToken`、`expiresAt`
