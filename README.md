# AI Fortune Telling Monorepo

本仓库包含三端工程：

- `apps/miniapp`: 微信小程序（TypeScript）
- `apps/admin-web`: 管理后台前端（Vue + Vite）
- `services/admin-api`: 管理后台 API（.NET）

## 本地开发建议顺序

1. 启动 API 服务（当前开发端口示例 `http://localhost:5228`）
2. 启动管理后台前端（默认 `http://localhost:5173`）
3. 使用微信开发者工具打开 `apps/miniapp` 进行联调

## 环境变量

请复制 `.env.example` 到各端实际环境文件：

- 管理后台前端：`apps/admin-web/.env.development`
- 小程序：`apps/miniapp/env/dev.ts`
- API：`services/admin-api/AdminApi.Host/appsettings.Development.json`

## API 联调自检（PowerShell）

先在 `services/admin-api/AdminApi.Host` 启动 API：

`dotnet run`

### 1) 登录获取 JWT

`Invoke-RestMethod -Method Post -Uri "http://localhost:5228/api/v1/auth/login" -ContentType "application/json" -Body (@{ username='admin'; password='admin123' } | ConvertTo-Json) | ConvertTo-Json -Depth 5`

### 2) 鉴权访问模板列表

`$login = Invoke-RestMethod -Method Post -Uri "http://localhost:5228/api/v1/auth/login" -ContentType "application/json" -Body (@{ username='admin'; password='admin123' } | ConvertTo-Json); $headers = @{ Authorization = "Bearer $($login.token)" }; Invoke-RestMethod -Method Get -Uri "http://localhost:5228/api/v1/templates" -Headers $headers | ConvertTo-Json -Depth 6`

### 3) 创建会话并分页查询管理端会话

`Invoke-RestMethod -Method Post -Uri "http://localhost:5228/api/v1/fortune-sessions" -ContentType "application/json" -Body (@{ userId='demo-user'; fortuneType='Tarot'; parameters=@{ question='healthcheck' } } | ConvertTo-Json -Depth 5) | Out-Null; $login = Invoke-RestMethod -Method Post -Uri "http://localhost:5228/api/v1/auth/login" -ContentType "application/json" -Body (@{ username='admin'; password='admin123' } | ConvertTo-Json); $headers = @{ Authorization = "Bearer $($login.token)" }; Invoke-RestMethod -Method Get -Uri "http://localhost:5228/api/v1/admin/fortune-sessions?page=1&pageSize=10" -Headers $headers | ConvertTo-Json -Depth 6`

### 4) AI 成功路径审计验证（degraded=false）

先设置环境变量并启动 API（同一个终端会话内）：

`$env:OpenAiCompatible__Enabled='true'; $env:OpenAiCompatible__ApiKey='你的Key'; $env:OpenAiCompatible__BaseUrl='https://api.openai.com/v1'; $env:OpenAiCompatible__Model='gpt-4o-mini'; Set-Location "services/admin-api/AdminApi.Host"; dotnet run --urls http://localhost:5228`

再开另一个终端执行验证脚本：

`Set-Location "scripts"; .\verify-ai-audit.ps1 -BaseUrl "http://localhost:5228"`

## GitHub Copilot C# SDK 流式版本（WebSocket）

该版本与现有 REST 版本并存，不影响原有接口。

### 1) 启用 API 的 Copilot SDK 配置

在 `services/admin-api/AdminApi.Host/appsettings.Development.json` 中设置：

- `CopilotSdk:Enabled=true`
- `CopilotSdk:Model=gpt-5-mini`
- `CopilotSdk:GitHubToken=<你的GitHub Token>`

也可以使用环境变量覆盖：

`$env:CopilotSdk__Enabled='true'; $env:CopilotSdk__Model='gpt-5-mini'; $env:CopilotSdk__GitHubToken='你的Token'`

### 2) 启动 API（包含 WebSocket 端点）

`Set-Location "services/admin-api/AdminApi.Host"; dotnet run --urls http://localhost:5228`

WebSocket 端点：`ws://localhost:5228/ws/fortune-stream`

### 3) 小程序切换到 WebSocket 算命版本

在 `apps/miniapp/miniprogram/env/dev.ts`（和 `apps/miniapp/env/dev.ts`）中设置：

- `fortuneTransport: 'copilot-ws'`
- `copilotWsUrl: 'ws://localhost:5228/ws/fortune-stream'`

设置为 `fortuneTransport: 'rest'` 即恢复原版本。

### 4) 运行 WebSocket 流式联调脚本

`Set-Location "scripts"; .\verify-copilot-ws.ps1 -WsUrl "ws://localhost:5228/ws/fortune-stream"`
