## Context

小程序用户数据已入库，但只有小程序端能访问自己的资料。管理端需要可检索、可查看、可执行风控动作。

## Goals / Non-Goals

**Goals:**
- 新增管理端用户列表和详情查看。
- 支持用户封禁/解封，封禁后小程序资料修改受限。
- 页面具备基础搜索与分页。

**Non-Goals:**
- 不实现复杂 RBAC 到按钮级权限。
- 不实现批量导入导出。

## Decisions

- 在 `MiniappUserEntity` 增加 `IsBlocked` 与 `BlockedAt` 字段。
- 新增 `/api/v1/admin/miniapp-users` 控制器并复用现有管理员 JWT 鉴权。
- Admin Web 新增 `MiniappUserManagementView.vue`，采用与会话页一致的分页模式。
