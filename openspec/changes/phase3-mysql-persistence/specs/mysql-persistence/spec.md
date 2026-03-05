## ADDED Requirements

### Requirement: API必须支持MySQL持久化存储
系统 MUST 使用 MySQL 保存会话、模板、内容和订单数据，服务重启后数据可保留。

#### Scenario: 创建会话后重启服务仍可查询
- **WHEN** 用户创建占卜会话并重启服务
- **THEN** 会话可通过查询接口继续获取

### Requirement: 模板CRUD必须操作数据库
系统 MUST 在模板的列表、创建、更新、删除流程中直接读写数据库。

#### Scenario: 删除模板后列表不再返回
- **WHEN** 管理员删除某模板
- **THEN** 后续模板列表接口不返回该模板
