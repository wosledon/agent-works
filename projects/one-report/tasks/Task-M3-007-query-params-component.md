# Task-M3-007: 查询参数输入组件

**优先级**: P1
**预估工时**: 2h
**依赖**: Task-M3-005
**认领人**: AI Agent
**状态**: 已完成
**完成时间**: 2026-03-15

## 描述
前端任务。实现动态查询参数输入组件，根据 SQL 中的参数定义自动生成对应的输入控件。

## 验收标准
- [x] 解析 SQL 中的参数占位符（如 {{paramName}}）
- [x] 根据参数类型生成对应输入组件（文本、数字、日期、下拉）
- [x] 参数值变化触发重新查询
- [x] 支持参数默认值配置
- [x] 必填参数校验

## 实现详情

### 创建文件
- `src/components/QueryParamsPanel.tsx` - 查询参数输入组件

### 更新文件
- `src/components/index.ts` - 导出 QueryParamsPanel 组件及其类型

### 功能特性
1. **SQL 参数解析**: 支持 `{{paramName}}`, `:paramName`, `@paramName`, `${paramName}` 多种占位符格式
2. **智能类型推断**: 
   - `date_`, `dt_` 前缀或包含 `date` + 时间相关词汇 → 日期选择器
   - `datetime_`, `time_` 前缀 → 日期时间选择器
   - `num_`, `int_`, `count_`, `amount_`, `price_`, `qty_` 等前缀或 `_id`, `_num` 后缀 → 数字输入
   - `select_`, `enum_`, `status_`, `type_` 等前缀 → 下拉选择
   - 其他 → 文本输入
3. **参数值变化通知**: 通过 `onChange(values, isValid)` 回调实时通知父组件
4. **默认值支持**: 通过 `paramConfigs` 配置或自动推断应用默认值
5. **必填校验**: 支持 `required` 属性校验，实时显示错误提示

### Props 接口
```typescript
interface QueryParamsPanelProps {
  sql: string;                          // SQL 查询字符串
  paramConfigs?: QueryParamConfig[];    // 参数配置元数据（可选）
  values?: QueryParamValues;            // 当前参数值
  onChange: (values: QueryParamValues, isValid: boolean) => void;
  onReset?: () => void;                 // 重置回调
  title?: string;                       // 面板标题
  className?: string;                   // 自定义类名
}
```

### 类型定义
```typescript
export type QueryParamType = 'text' | 'number' | 'date' | 'select' | 'datetime';

export interface QueryParamConfig {
  name: string;
  type: QueryParamType;
  label?: string;
  defaultValue?: string | number;
  required?: boolean;
  options?: { label: string; value: string | number }[];
  placeholder?: string;
  min?: number;
  max?: number;
}

export type QueryParamValues = Record<string, string | number>;
```

### 使用示例
```tsx
import { QueryParamsPanel, QueryParamConfig } from '~/components';

const sql = `SELECT * FROM orders 
  WHERE created_at >= {{start_date}} 
    AND status = {{status}}
    AND amount > {{min_amount}}`;

const paramConfigs: QueryParamConfig[] = [
  { name: 'start_date', type: 'date', required: true, defaultValue: '2024-01-01' },
  { name: 'status', type: 'select', options: [{ label: '已完成', value: 'completed' }, { label: '待处理', value: 'pending' }] },
  { name: 'min_amount', type: 'number', defaultValue: 0, min: 0 },
];

function MyComponent() {
  const handleParamsChange = (values, isValid) => {
    if (isValid) {
      // 触发重新查询
      executeQuery(sql, values);
    }
  };

  return (
    <QueryParamsPanel
      sql={sql}
      paramConfigs={paramConfigs}
      onChange={handleParamsChange}
      title="筛选条件"
    />
  );
}
```

### 样式特点
- 使用 Fluent Design 风格
- 图标前缀输入框（Hash、Calendar、Type、ChevronDown）
- 错误状态边框高亮
- 必填标记（红色星号）
- 底部状态栏显示校验状态和填充进度
