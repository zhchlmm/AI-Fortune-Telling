## ADDED Requirements

### Requirement: 系统必须提供占卜模板CRUD接口
系统 MUST 提供模板列表、创建、更新、删除接口，模板包含名称、占卜类型、提示词与启用状态。

#### Scenario: 创建模板成功
- **WHEN** 管理员提交有效模板数据
- **THEN** 系统返回创建后的模板对象

#### Scenario: 更新模板成功
- **WHEN** 管理员提交已有模板ID和更新数据
- **THEN** 系统返回更新后的模板对象

#### Scenario: 删除模板成功
- **WHEN** 管理员请求删除已有模板
- **THEN** 系统返回 204 NoContent
