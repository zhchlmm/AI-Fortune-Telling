## 1. 后端 token 能力

- [x] 1.1 新增小程序 token 选项与签发服务
- [x] 1.2 登录接口返回 token 与过期时间
- [x] 1.3 资料接口改为 token 鉴权并移除 `openId` 直传信任

## 2. 小程序端接入

- [x] 2.1 缓存 token/openId 并在请求头自动附带 Bearer token
- [x] 2.2 token 失效时自动清理并触发重新登录

## 3. 验证

- [x] 3.1 后端构建 + 关键接口鉴权验证
- [x] 3.2 小程序 typecheck/build 验证
