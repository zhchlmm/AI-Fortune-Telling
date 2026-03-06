## ADDED Requirements

### Requirement: 手机号接口必须返回可区分错误码
系统 MUST 在手机号处理失败时返回明确业务错误码。

#### Scenario: sessionKey 缺失
- **WHEN** 用户未完成有效登录导致 sessionKey 缺失
- **THEN** 返回 400 且错误码为 `SESSION_KEY_MISSING`

#### Scenario: 解密失败
- **WHEN** encryptedData/iv 无法完成解密
- **THEN** 返回 400 且错误码为 `PHONE_DECRYPT_FAILED`

### Requirement: 手机号失败后必须支持业务降级
系统 MUST 在授权失败时允许用户继续手动输入手机号。

#### Scenario: 用户拒绝手机号授权
- **WHEN** 微信授权返回非 ok
- **THEN** 前端提示用户可手动输入并不阻断资料保存
