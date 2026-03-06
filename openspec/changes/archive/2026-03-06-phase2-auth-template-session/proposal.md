## Why

第一阶段已完成三端基础骨架，但后台登录仍是 mock、会话查询缺少分页能力、模板管理仅为页面占位，无法支撑真实运营流程。
第二阶段需要补齐鉴权与核心运营接口，形成“可登录、可管理、可查询”的最小业务闭环。

## What Changes

- 在管理后台 API 增加 JWT 登录接口与鉴权中间件，保护管理端核心接口。
- 为占卜会话查询接口增加分页参数与分页返回结构。
- 新增占卜模板管理接口（列表、创建、更新、删除）并使用内存存储占位。
- 改造管理后台前端登录流程，持久化 token 并携带到 API 请求头。
- 改造会话查询页支持分页参数与分页显示，改造模板管理页接入 CRUD。

## Capabilities

### New Capabilities
- `admin-auth-jwt`: 管理后台 JWT 登录与接口鉴权能力。
- `session-pagination`: 占卜会话分页查询能力。
- `template-management-crud`: 占卜模板管理的基础 CRUD 能力。

### Modified Capabilities
- `admin-web-foundation`: 将 mock 登录改为真实 API 登录，并在受保护路由中使用 token 状态。
- `admin-api-foundation`: 在既有 API 基础上增加鉴权与可复用分页响应模型。

## Impact

- 影响 API 控制器、启动配置、模型定义与内存存储结构。
- 影响管理后台前端的鉴权状态管理、API 客户端拦截器与页面交互。
- 为后续 RBAC、模板版本化与高级查询提供演进基础。
