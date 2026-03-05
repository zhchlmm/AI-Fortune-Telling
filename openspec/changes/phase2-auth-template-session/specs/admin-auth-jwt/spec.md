## ADDED Requirements

### Requirement: 管理后台必须支持JWT登录
系统 MUST 提供管理员登录接口并返回 JWT token，错误凭证必须返回 401。

#### Scenario: 登录成功
- **WHEN** 管理员提交正确用户名和密码
- **THEN** 系统返回 token 与过期时间

#### Scenario: 登录失败
- **WHEN** 管理员提交错误用户名或密码
- **THEN** 系统返回 401 Unauthorized

### Requirement: 管理端核心接口必须受鉴权保护
系统 MUST 要求请求携带 Bearer token 才能访问模板管理与会话查询接口。

#### Scenario: 未携带token访问模板列表
- **WHEN** 客户端未携带 Authorization 请求头
- **THEN** 系统返回 401 Unauthorized
