## Why

当前仓库仅有 OpenSpec 骨架，尚未建立微信小程序、管理后台 API 与管理后台前端的实现基础，无法支撑“AI 占卜算命”MVP 的开发与联调。
需要先建立可运行的三端基础工程与统一接口契约，确保后续占卜能力、内容运营、用户中心与订单能力可以并行迭代并稳定交付。

## What Changes

- 新建微信小程序基础工程（TypeScript + TDesign），落地首页、占卜流程页、历史记录页、用户中心页基础路由与 API 调用层。
- 新建管理后台 API 基础工程（ABP + .NET 10 + EF Core + MySQL + Redis），落地鉴权、占卜会话、内容资讯、订单与支付占位接口。
- 新建管理后台前端基础工程（Vue + Vite + Pinia + Tailwind + shadcn/ui），落地登录、仪表盘、内容管理、占卜模板管理、会话查询基础页面。
- 建立三端共享的数据契约与错误码规范，包含用户信息、占卜请求/响应、资讯列表、订单状态等核心 DTO。
- 建立首批开发运行文档与环境变量模板，支持本地启动、联调与后续测试扩展。

## Capabilities

### New Capabilities
- `miniapp-foundation`: 微信小程序基础工程、页面骨架、API 访问层与基础状态管理。
- `admin-api-foundation`: 管理后台 API 的模块化基础骨架、领域边界与核心接口占位。
- `admin-web-foundation`: 管理后台前端基础工程、鉴权壳层、运营核心页面骨架。
- `fortune-session-flow`: 占卜会话主流程（提交参数、生成结果、历史查询）的跨端契约与最小闭环。
- `content-and-order-baseline`: 资讯管理与订单能力的最小可用接口与后台入口。

### Modified Capabilities
- 无

## Impact

- 新增三端代码目录：`apps/miniapp`、`apps/admin-web`、`services/admin-api`。
- 新增跨端契约文档与接口约定，影响后续所有业务功能开发。
- 新增依赖：微信小程序工具链、Node.js 前端依赖、.NET SDK 与 ABP 相关包。
- 需要统一环境配置（数据库、Redis、JWT、小程序 AppId 与 API Base URL）。
