# Task-M3-009: 字段映射配置组件

**优先级**: P0
**预估工时**: 4h
**依赖**: Task-M3-008
**认领人**: AI Agent
**状态**: 已完成
**完成时间**: 2026-03-15

## 描述
前端任务。实现图表字段映射配置组件，根据图表类型（柱状图、折线图、饼图等）动态展示可配置的映射项。

## 验收标准
- [x] 根据图表类型动态生成映射配置项（X轴、Y轴、系列、数值等）
- [x] 映射字段下拉选择（从查询结果列中选择）
- [x] 支持多系列配置（Y轴可添加多个）
- [x] 支持数据格式配置（数值格式化、日期格式）
- [x] 实时预览绑定效果
- [x] 映射配置自动保存

## 实现详情

### 创建文件
- `src/components/FieldMappingPanel.tsx` - 字段映射配置组件

### 更新文件
- `src/components/index.ts` - 导出新组件和类型

### 组件功能
1. **图表类型适配**
   - 柱状图/折线图: X轴(分类字段)、Y轴(数值字段，可多选)
   - 饼图: 名称字段、数值字段
   - 表格: 列选择(多选)

2. **字段映射配置**
   - 字段选择下拉框（从可用字段中选择）
   - 显示标签输入
   - 自动推断字段类型（string/number/date/boolean）

3. **多系列支持**
   - Y轴可添加多个字段
   - 支持字段排序（上移/下移）
   - 支持删除已添加的字段

4. **数据格式配置**
   - 数值格式化类型：无/数值/货币/百分比
   - 小数位精度设置
   - 前缀/后缀配置
   - 千分位分隔符
   - 日期格式选择

5. **实时预览**
   - 显示前5条数据的格式化效果
   - 支持开关预览面板

6. **自动保存**
   - 配置变更自动保存
   - 显示保存状态提示

### 类型定义
```typescript
export type ChartType = 'bar' | 'line' | 'pie' | 'table';

export interface FieldMappingConfig extends DataMapping {
  format?: {
    type?: 'none' | 'number' | 'currency' | 'percentage' | 'date';
    precision?: number;
    prefix?: string;
    suffix?: string;
    dateFormat?: string;
    thousandsSeparator?: boolean;
  };
}
```

### Props
```typescript
interface FieldMappingPanelProps {
  chartType: ChartType;
  availableFields: string[];
  mapping: FieldMappingConfig[];
  onChange: (mapping: FieldMappingConfig[]) => void;
  previewData?: Record<string, unknown>[];
  onPreviewUpdate?: (previewConfig: Record<string, unknown>) => void;
  autoSave?: boolean;
  onAutoSave?: (mapping: FieldMappingConfig[]) => void;
}
```

### Fluent Design 风格
- 使用 CSS 变量保持与设计系统一致
- 响应式布局和过渡动画
- 图标和颜色符合 Fluent 设计规范
