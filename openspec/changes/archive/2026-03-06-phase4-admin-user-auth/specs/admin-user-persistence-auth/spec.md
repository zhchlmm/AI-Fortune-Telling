## ADDED Requirements

### Requirement: 管理员登录必须基于数据库用户
系统 MUST 从数据库读取管理员用户并进行密码哈希校验。

#### Scenario: 有效用户登录成功
- **WHEN** 提交存在且启用的管理员用户名与正确密码
- **THEN** 返回 JWT token 与过期时间

#### Scenario: 无效凭据登录失败
- **WHEN** 用户不存在、被禁用或密码错误
- **THEN** 返回 401 Unauthorized
