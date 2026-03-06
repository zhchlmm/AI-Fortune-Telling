## Why

小程序资料字段已可编辑，但缺少服务端格式校验与变更审计，存在数据质量和追踪能力不足问题。

## What Changes

- 为昵称、邮箱、手机号增加后端输入校验与长度限制。
- 新增小程序资料变更审计表，记录字段变更前后值（敏感信息脱敏）。
- 资料更新接口返回标准化错误信息，便于前端提示。

## Capabilities

### New Capabilities
- `miniapp-profile-validation-audit`: 小程序资料校验与变更审计能力。

### Modified Capabilities
- `miniapp-user-profile-optional-fields`: 在保持非必填前提下增加格式约束。

## Impact

- 影响 `MiniappUsersController`、实体与迁移。
- 影响小程序 profile 保存交互与错误提示。
