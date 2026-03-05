## ADDED Requirements

### Requirement: 占卜会话查询必须支持分页
系统 MUST 支持 `page` 与 `pageSize` 参数，并返回 `total` 与 `items`。

#### Scenario: 分页查询第一页
- **WHEN** 客户端请求 page=1,pageSize=10
- **THEN** 系统返回最多10条会话和总数total

#### Scenario: 请求超出范围页码
- **WHEN** 客户端请求超出可用范围的页码
- **THEN** 系统返回空 items 且 total 保持正确
