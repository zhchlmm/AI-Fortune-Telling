## Context

第二阶段已完成 JWT、分页查询与模板 CRUD，但数据仍在内存中。第三阶段聚焦“存储实现替换”，保证外部接口不变。

## Goals / Non-Goals

**Goals:**
- 引入 EF Core MySQL Provider 并配置 DbContext。
- 完成会话、模板、内容、订单实体持久化。
- 控制器改造后保持现有 API 路由和响应结构稳定。

**Non-Goals:**
- 不在本阶段引入复杂仓储抽象或 DDD 聚合重构。
- 不在本阶段实现数据库迁移脚本自动化流水线。

## Decisions

- 采用单 DbContext 管理当前核心表，降低阶段复杂度。
- 使用启动时轻量种子逻辑确保内容与模板初始数据可用。
- 控制器层直接使用 DbContext，后续阶段再抽象应用服务层。

## Risks / Trade-offs

- [风险] 开发机无 MySQL 时接口不可用 → [缓解] 提供清晰连接串配置与说明。
- [风险] 无迁移脚本会导致手工建表差异 → [缓解] 先用 EnsureCreated 保障开发联调。
