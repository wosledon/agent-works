import { useState, useEffect, useMemo } from 'react';
import { Calendar, Hash, Type, List, AlertCircle } from 'lucide-react';

export interface QueryParamConfig {
  name: string;
  type: 'text' | 'number' | 'date' | 'datetime' | 'select' | 'boolean';
  label?: string;
  defaultValue?: string | number | boolean;
  required?: boolean;
  options?: { label: string; value: string | number }[];
  description?: string;
}

interface QueryParamsPanelProps {
  sql: string;
  values: Record<string, unknown>;
  onChange: (values: Record<string, unknown>) => void;
  configs?: QueryParamConfig[];
}

/**
 * 从 SQL 中提取参数占位符
 * 支持 {{paramName}} 和 :paramName 格式
 */
function extractParamsFromSql(sql: string): string[] {
  const params = new Set<string>();
  
  // 匹配 {{paramName}}
  const doubleBraceRegex = /\{\{(\w+)\}\}/g;
  let match;
  while ((match = doubleBraceRegex.exec(sql)) !== null) {
    params.add(match[1]);
  }
  
  // 匹配 :paramName（但排除 ::cast 语法）
  const colonRegex = /:(\w+)(?!:)/g;
  while ((match = colonRegex.exec(sql)) !== null) {
    params.add(match[1]);
  }
  
  return Array.from(params);
}

/**
 * 根据参数名推断类型
 */
function inferParamType(paramName: string): QueryParamConfig['type'] {
  const lower = paramName.toLowerCase();
  if (lower.startsWith('date_') || lower.endsWith('_date') || lower === 'date') {
    return 'date';
  }
  if (lower.startsWith('datetime_') || lower.endsWith('_datetime') || lower === 'datetime') {
    return 'datetime';
  }
  if (lower.startsWith('num_') || lower.startsWith('count_') || lower.startsWith('amount_') || 
      lower.endsWith('_num') || lower.endsWith('_count') || lower.endsWith('_amount') ||
      lower === 'num' || lower === 'count' || lower === 'amount') {
    return 'number';
  }
  if (lower.startsWith('is_') || lower.startsWith('has_') || lower === 'active' || lower === 'enabled') {
    return 'boolean';
  }
  if (lower.endsWith('_id') || lower === 'id') {
    return 'text';
  }
  return 'text';
}

/**
 * 获取类型图标
 */
function getTypeIcon(type: QueryParamConfig['type']) {
  switch (type) {
    case 'date':
    case 'datetime':
      return <Calendar className="w-4 h-4" />;
    case 'number':
      return <Hash className="w-4 h-4" />;
    case 'select':
      return <List className="w-4 h-4" />;
    default:
      return <Type className="w-4 h-4" />;
  }
}

/**
 * 获取类型标签
 */
function getTypeLabel(type: QueryParamConfig['type']): string {
  const labels: Record<string, string> = {
    text: '文本',
    number: '数字',
    date: '日期',
    datetime: '日期时间',
    select: '下拉选择',
    boolean: '开关',
  };
  return labels[type] || type;
}

export function QueryParamsPanel({ sql, values, onChange, configs = [] }: QueryParamsPanelProps) {
  const [localValues, setLocalValues] = useState<Record<string, unknown>>(values);

  // 同步外部值
  useEffect(() => {
    setLocalValues(values);
  }, [values]);

  // 提取 SQL 中的参数
  const extractedParams = useMemo(() => extractParamsFromSql(sql), [sql]);

  // 合并配置
  const paramConfigs = useMemo(() => {
    return extractedParams.map(paramName => {
      const existingConfig = configs.find(c => c.name === paramName);
      const inferredType = inferParamType(paramName);
      
      return {
        name: paramName,
        type: existingConfig?.type || inferredType,
        label: existingConfig?.label || paramName,
        defaultValue: existingConfig?.defaultValue,
        required: existingConfig?.required ?? false,
        options: existingConfig?.options,
        description: existingConfig?.description,
      } as QueryParamConfig;
    });
  }, [extractedParams, configs]);

  // 如果没有参数，显示提示
  if (paramConfigs.length === 0) {
    return (
      <div className="p-4 text-center text-[var(--fluent-neutral-50)]">
        <AlertCircle className="w-8 h-8 mx-auto mb-2 opacity-30" />
        <p className="text-sm">SQL 中没有发现参数</p>
        <p className="text-xs mt-1 opacity-60">
          使用 {'{{'}paramName{'}'}'} 或 :paramName 定义参数
        </p>
      </div>
    );
  }

  // 处理值变更
  const handleValueChange = (name: string, value: unknown) => {
    const newValues = { ...localValues, [name]: value };
    setLocalValues(newValues);
    onChange(newValues);
  };

  return (
    <div className="space-y-4">
      {paramConfigs.map((config) => (
        <ParamInput
          key={config.name}
          config={config}
          value={localValues[config.name]}
          onChange={(value) => handleValueChange(config.name, value)}
        />
      ))}
    </div>
  );
}

interface ParamInputProps {
  config: QueryParamConfig;
  value: unknown;
  onChange: (value: unknown) => void;
}

function ParamInput({ config, value, onChange }: ParamInputProps) {
  const { name, type, label, required, options, description, defaultValue } = config;
  const inputId = `param-${name}`;

  // 使用默认值
  useEffect(() => {
    if (value === undefined && defaultValue !== undefined) {
      onChange(defaultValue);
    }
  }, [value, defaultValue, onChange]);

  const renderInput = () => {
    switch (type) {
      case 'text':
        return (
          <input
            type="text"
            id={inputId}
            value={(value as string) || ''}
            onChange={(e) => onChange(e.target.value)}
            placeholder={`输入${label}`}
            className="fluent-input w-full"
          />
        );

      case 'number':
        return (
          <input
            type="number"
            id={inputId}
            value={(value as number) || ''}
            onChange={(e) => onChange(e.target.value === '' ? '' : Number(e.target.value))}
            placeholder={`输入${label}`}
            className="fluent-input w-full"
          />
        );

      case 'date':
        return (
          <input
            type="date"
            id={inputId}
            value={(value as string) || ''}
            onChange={(e) => onChange(e.target.value)}
            className="fluent-input w-full"
          />
        );

      case 'datetime':
        return (
          <input
            type="datetime-local"
            id={inputId}
            value={(value as string) || ''}
            onChange={(e) => onChange(e.target.value)}
            className="fluent-input w-full"
          />
        );

      case 'select':
        return (
          <select
            id={inputId}
            value={(value as string) || ''}
            onChange={(e) => onChange(e.target.value)}
            className="fluent-select w-full"
          >
            <option value="">-- 请选择 --</option>
            {options?.map((opt) => (
              <option key={opt.value} value={opt.value}>
                {opt.label}
              </option>
            ))}
          </select>
        );

      case 'boolean':
        return (
          <label className="flex items-center gap-3 cursor-pointer">
            <input
              type="checkbox"
              id={inputId}
              checked={(value as boolean) || false}
              onChange={(e) => onChange(e.target.checked)}
              className="w-4 h-4 rounded border-[var(--fluent-neutral-30)] text-[var(--fluent-primary)] focus:ring-[var(--fluent-primary)]"
            />
            <span className="text-sm text-[var(--fluent-neutral-60)]">启用</span>
          </label>
        );

      default:
        return null;
    }
  };

  return (
    <div className="space-y-1.5">
      <label htmlFor={inputId} className="flex items-center gap-2 text-sm font-medium text-[var(--fluent-neutral-70)]">
        {getTypeIcon(type)}
        <span>{label}</span>
        {required && <span className="text-[var(--fluent-error)]">*</span>}
        <span className="text-xs text-[var(--fluent-neutral-40)] ml-auto">
          {getTypeLabel(type)}
        </span>
      </label>
      {description && (
        <p className="text-xs text-[var(--fluent-neutral-50)]">{description}</p>
      )}
      {renderInput()}
    </div>
  );
}

export default QueryParamsPanel;
