## ADDED Requirements

### Requirement: 管理后台必须提供小程序用户分页管理
系统 MUST 允许管理员分页查询小程序用户并按关键字段筛选。

#### Scenario: 按昵称筛选用户
- **WHEN** 管理员输入昵称关键字并查询
- **THEN** 返回匹配用户分页列表

### Requirement: 管理后台必须支持封禁与解封用户
系统 MUST 允许管理员对小程序用户执行封禁与解封操作。

#### Scenario: 封禁用户后限制资料更新
- **WHEN** 用户被管理员封禁
- **THEN** 小程序资料更新接口返回 403 Forbidden

#### Scenario: 解封后恢复更新
- **WHEN** 用户被解除封禁
- **THEN** 小程序资料更新接口恢复可用
