## Why

当前 API 使用内存存储，服务重启后数据丢失，无法满足后台管理的持续运营需求。
需要将核心数据迁移到 MySQL 持久化层，并保持现有接口契约稳定。

## What Changes

- 在管理后台 API 引入 EF Core + MySQL Provider。
- 新增 DbContext 与实体映射，覆盖会话、模板、内容、订单。
- 将控制器从 InMemoryStore 改造为 DbContext 读写。
- 增加开发环境连接串配置与基础种子数据初始化。

## Capabilities

### New Capabilities
- `mysql-persistence`: 管理后台 API 的 MySQL 持久化能力。

### Modified Capabilities
- `admin-api-foundation`: 数据存储实现从内存变为 MySQL。
- `template-management-crud`: 模板 CRUD 持久化实现。
- `session-pagination`: 分页会话查询改为数据库分页查询。

## Impact

- 影响 API 数据访问层与控制器实现。
- 增加 MySQL 与 EF Core 依赖。
- 本地开发需要可用 MySQL 实例或可替代连接配置。
