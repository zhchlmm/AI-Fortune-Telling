## Context

手机号能力与微信授权体系强相关，失败原因多样，当前统一报错“手机号解析失败”不利于用户理解与运维排障。

## Goals / Non-Goals

**Goals:**
- 细分错误类型：用户拒绝、参数缺失、sessionKey 缺失、解密失败。
- 小程序端提供重试与手动输入 fallback 引导。
- 增加端到端单测覆盖关键分支。

**Non-Goals:**
- 不引入外部消息队列重试。
- 不增加客服工单系统集成。

## Decisions

- 后端返回统一结构 `{ code, message }`，code 如 `PHONE_DECRYPT_FAILED`。
- 前端根据 code 显示精确 toast 并建议改手动输入。
- 单测集中在 `MiniappUsersControllerTests`，复用 InMemory Db。
