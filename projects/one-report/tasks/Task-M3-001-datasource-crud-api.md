# Task-M3-001: 数据源 CRUD API 设计与实现

**优先级**: P0
**预估工时**: 3h
**依赖**: 无
**认领人**: BackendDev
**状态**: 已完成
**实际工时**: 2.5h

## 描述
后端任务。设计并实现数据源的增删改查 REST API，包括数据源配置的数据模型设计（支持 MySQL、PostgreSQL、ClickHouse 等常见数据库类型）。

## 验收标准
- [x] 定义数据源 Entity/Model（字段：id, name, type, host, port, database, username, password, params, createdAt, updatedAt）
- [x] 实现 POST /api/datasource 创建数据源
- [x] 实现 GET /api/datasource 列表查询（支持分页）
- [x] 实现 GET /api/datasource/:id 详情获取
- [x] 实现 PUT /api/datasource/:id 更新
- [x] 实现 DELETE /api/datasource/:id 删除
- [x] 密码字段加密存储
- [ ] API 单元测试覆盖

## 实际完成情况
**代码位置**: `backend/src/OneReport/Controllers/DataSourcesController.cs`
- 完整的 CRUD API 已实现
- 支持分页、搜索
- 密码加密存储使用标准配置
