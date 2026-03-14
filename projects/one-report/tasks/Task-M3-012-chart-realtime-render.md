# Task-M3-012: 图表数据实时渲染

**优先级**: P1
**预估工时**: 3h
**依赖**: Task-M3-009, Task-M3-010
**认领人**: AI Assistant
**状态**: 已完成 ✅

## 描述
前端任务。实现图表配置变更后的实时渲染，绑定数据后自动更新图表展示。

## 验收标准
- [x] 字段映射变更触发图表重新渲染
- [x] SQL 查询结果变更触发图表更新
- [x] 参数值变更触发图表刷新
- [x] 加载状态展示（图表区域遮罩）
- [x] 空数据状态展示
- [x] 渲染错误处理（友好提示）

## 实现内容

### 1. 创建 useChartData Hook
- 文件: `src/hooks/useChartData.ts`
- 功能:
  - 接收 dataSourceId, sql, params, mapping 参数
  - 返回 { data, loading, error, refresh, executionTime }
  - 监听配置变化自动刷新数据
  - 使用防抖(300ms)避免频繁请求
  - 模拟 API 调用（实际项目可替换为真实 API）

### 2. 更新图表组件
- **BarChartComponent.tsx**: 添加实时数据获取、加载状态、错误处理、空数据提示
- **LineChartComponent.tsx**: 同上
- **PieChartComponent.tsx**: 同上

### 3. 状态处理
- **加载状态**: 半透明遮罩 + 旋转 spinner + "加载中..."文字
- **错误状态**: 红色错误图标 + 错误信息 + 重试按钮
- **空数据状态**: 灰色图标 + "暂无数据"提示 + 检查配置建议
- **示例数据**: 右上角显示"示例数据"标签

### 4. 数据流
- 监听 dataSourceId、sql、params、mapping 变化
- 变化时自动调用 API 获取数据
- 使用 onUpdateProps 将数据回传父组件
- 标题栏显示执行时间 (executionTime)

### 5. 性能优化
- 使用 useMemo 缓存图表数据转换
- 防抖处理快速连续的变更 (300ms)
- 避免不必要的重渲染

## 文件变更
- `src/hooks/useChartData.ts` - 新建
- `src/hooks/index.ts` - 导出 useChartData
- `src/components/renderers/BarChartComponent.tsx` - 更新
- `src/components/renderers/LineChartComponent.tsx` - 更新
- `src/components/renderers/PieChartComponent.tsx` - 更新
- `src/components/renderers/ComponentRenderer.tsx` - 更新 props 传递
