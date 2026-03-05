## Why

当前登录仍基于固定账号常量，无法支持账号持久化与安全维护。
需要将管理员账号纳入数据库并使用密码哈希校验。

## What Changes

- 新增管理员用户实体与 `AdminUsers` 数据表。
- 登录接口改为数据库用户校验，不再依赖硬编码账号。
- 增加密码哈希服务与初始化种子管理员。
- 生成并应用对应 EF Core migration。

## Capabilities

### New Capabilities
- `admin-user-persistence-auth`: 管理员用户持久化与哈希登录校验。

### Modified Capabilities
- `admin-auth-jwt`: 登录凭据来源从固定常量改为数据库。

## Impact

- 影响认证控制器、数据模型、种子逻辑与迁移文件。
