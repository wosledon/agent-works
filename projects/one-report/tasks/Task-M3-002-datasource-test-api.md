# Task-M3-002: 数据源连接测试 API

**优先级**: P0
**预估工时**: 2h
**依赖**: Task-M3-001
**认领人**: BackendDev
**状态**: 已完成
**实际工时**: 1.5h

## 描述
后端任务。实现数据源连接测试接口，验证数据库连接配置是否正确，返回连接成功/失败状态及错误信息。

## 验收标准
- [x] 实现 POST /api/datasource/:id/test 测试已有数据源连接
- [x] 实现 POST /api/datasource/test 测试新配置（无需保存）
- [x] 支持 MySQL、PostgreSQL、ClickHouse 连接测试
- [x] 连接失败返回详细错误信息
- [x] 连接超时处理（默认 10s）

## 实际完成情况
**代码位置**: `backend/src/OneReport/Controllers/DataSourcesController.cs`
- TestConnection 和 TestConnectionById 两个端点已实现
- 支持多种数据库类型
- 详细的错误处理和超时控制
