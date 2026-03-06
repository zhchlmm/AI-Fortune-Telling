## Why

目前管理后台无法查看与管理小程序用户，运营侧缺少用户画像与封禁控制，影响风险治理与客服处理效率。

## What Changes

- 管理后台 API 新增小程序用户管理接口（分页、搜索、详情、封禁/解封）。
- Admin Web 新增“小程序用户管理”页面与路由。
- 支持按 openId、昵称、手机号、封禁状态筛选。

## Capabilities

### New Capabilities
- `admin-miniapp-user-management`: 管理端小程序用户运营管理能力。

### Modified Capabilities
- `admin-web-foundation`: 新增用户管理导航与页面。

## Impact

- 影响 `AdminApi.Host/Controllers`、模型定义与管理端路由/页面。
- 与 miniapp 用户实体产生管理端读写联动。
