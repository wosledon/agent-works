import { useState } from 'react';
import { Settings, Plus, Trash2, Type, Hash, Calendar, AlignLeft } from 'lucide-react';
import type { DataMapping } from '~/types';

export interface FieldMappingConfig {
  field: string;
  label: string;
  type: 'x' | 'y' | 'category' | 'value' | 'name' | 'column';
  required?: boolean;
  allowMultiple?: boolean;
  format?: string;
}

interface FieldMappingPanelProps {
  chartType: 'bar' | 'line' | 'pie' | 'table';
  availableFields: string[];
  mapping: DataMapping[];
  onChange: (mapping: DataMapping[]) => void;
}

/**
 * 获取图表类型所需的字段映射配置
 */
function getFieldRequirements(chartType: string): FieldMappingConfig[] {
  switch (chartType) {
    case 'bar':
    case 'line':
      return [
        { field: 'x', label: 'X轴 / 分类字段', type: 'x', required: true },
        { field: 'y', label: 'Y轴 / 数值字段', type: 'y', required: true, allowMultiple: true },
      ];
    case 'pie':
      return [
        { field: 'name', label: '名称字段', type: 'name', required: true },
        { field: 'value', label: '数值字段', type: 'value', required: true },
      ];
    case 'table':
      return [
        { field: 'columns', label: '表格列', type: 'column', allowMultiple: true },
      ];
    default:
      return [];
  }
}

/**
 * 推断字段数据类型
 */
function inferFieldType(fieldName: string): 'string' | 'number' | 'date' | 'boolean' {
  const lower = fieldName.toLowerCase();
  if (lower.includes('date') || lower.includes('time')) return 'date';
  if (lower.includes('count') || lower.includes('num') || lower.includes('amount') || 
      lower.includes('price') || lower.includes('total') || lower.includes('qty')) return 'number';
  if (lower.startsWith('is_') || lower.startsWith('has_')) return 'boolean';
  return 'string';
}

/**
 * 获取类型图标
 */
function getTypeIcon(type: string) {
  switch (type) {
    case 'number':
      return <Hash className="w-3.5 h-3.5" />;
    case 'date':
      return <Calendar className="w-3.5 h-3.5" />;
    case 'string':
    default:
      return <Type className="w-3.5 h-3.5" />;
  }
}

export function FieldMappingPanel({ chartType, availableFields, mapping, onChange }: FieldMappingPanelProps) {
  const requirements = getFieldRequirements(chartType);

  // 处理单字段映射变更
  const handleSingleMappingChange = (fieldType: string, fieldName: string) => {
    const existingIndex = mapping.findIndex(m => m.field === fieldType);
    const fieldTypeInferred = inferFieldType(fieldName);
    
    const newMapping: DataMapping = {
      field: fieldType,
      label: fieldName,
      type: fieldTypeInferred,
    };

    if (existingIndex >= 0) {
      const updated = [...mapping];
      updated[existingIndex] = newMapping;
      onChange(updated);
    } else {
      onChange([...mapping, newMapping]);
    }
  };

  // 处理多字段映射变更（如 Y 轴多系列）
  const handleMultiMappingChange = (fieldType: string, fieldNames: string[]) => {
    // 移除该类型的现有映射
    const withoutType = mapping.filter(m => m.field !== fieldType);
    
    // 添加新的映射
    const newMappings = fieldNames.map((fieldName, index) => ({
      field: `${fieldType}_${index}`,
      label: fieldName,
      type: inferFieldType(fieldName),
    }));

    onChange([...withoutType, ...newMappings]);
  };

  // 获取当前选中的字段
  const getSelectedField = (fieldType: string): string => {
    return mapping.find(m => m.field === fieldType)?.label || '';
  };

  // 获取当前选中的多字段
  const getSelectedMultiFields = (fieldType: string): string[] => {
    return mapping
      .filter(m => m.field.startsWith(`${fieldType}_`))
      .map(m => m.label);
  };

  // 表格列选择器
  const handleTableColumnsChange = (fieldName: string, checked: boolean) => {
    const currentColumns = getSelectedMultiFields('columns');
    let newColumns: string[];
    
    if (checked) {
      newColumns = [...currentColumns, fieldName];
    } else {
      newColumns = currentColumns.filter(c => c !== fieldName);
    }
    
    handleMultiMappingChange('columns', newColumns);
  };

  // 渲染表格列选择
  if (chartType === 'table') {
    return (
      <div className="space-y-3">
        <div className="flex items-center gap-2 text-sm font-medium text-[var(--fluent-neutral-70)]">
          <Settings className="w-4 h-4" />
          <span>选择要显示的列</span>
        </div>
        
        <div className="space-y-2 max-h-[300px] overflow-y-auto">
          {availableFields.map((field) => {
            const isSelected = getSelectedMultiFields('columns').includes(field);
            const fieldType = inferFieldType(field);
            
            return (
              <label
                key={field}
                className="flex items-center gap-3 p-2 rounded-lg hover:bg-[var(--fluent-neutral-4)] cursor-pointer transition-colors"
              >
                <input
                  type="checkbox"
                  checked={isSelected}
                  onChange={(e) => handleTableColumnsChange(field, e.target.checked)}
                  className="w-4 h-4 rounded border-[var(--fluent-neutral-30)] text-[var(--fluent-primary)] focus:ring-[var(--fluent-primary)]"
                />
                <span className="text-[var(--fluent-neutral-40)]">{getTypeIcon(fieldType)}</span>
                <span className="text-sm text-[var(--fluent-neutral-90)] flex-1">{field}</span>
                <span className="text-xs text-[var(--fluent-neutral-50)]">{fieldType}</span>
              </label>
            );
          })}
        </div>
        
        {availableFields.length === 0 && (
          <div className="text-center py-4 text-[var(--fluent-neutral-50)]">
            <AlignLeft className="w-8 h-8 mx-auto mb-2 opacity-30" />
            <p className="text-sm">暂无可用字段</p>
            <p className="text-xs mt-1">请先执行查询获取数据</p>
          </div>
        )}
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {requirements.map((req) => (
        <div key={req.field} className="space-y-2">
          <label className="flex items-center gap-2 text-sm font-medium text-[var(--fluent-neutral-70)]">
            {req.type === 'x' && <AlignLeft className="w-4 h-4" />}
            {req.type === 'y' && <Hash className="w-4 h-4" />}
            {req.type === 'name' && <Type className="w-4 h-4" />}
            {req.type === 'value' && <Hash className="w-4 h-4" />}
            <span>{req.label}</span>
            {req.required && <span className="text-[var(--fluent-error)]">*</span>}
          </label>

          {req.allowMultiple ? (
            <MultiFieldSelect
              availableFields={availableFields}
              selectedFields={getSelectedMultiFields(req.field)}
              onChange={(fields) => handleMultiMappingChange(req.field, fields)}
              placeholder={`选择${req.label}`}
            />
          ) : (
            <select
              value={getSelectedField(req.field)}
              onChange={(e) => handleSingleMappingChange(req.field, e.target.value)}
              className="fluent-select w-full"
            >
              <option value="">-- 选择字段 --</option>
              {availableFields.map((field) => (
                <option key={field} value={field}>
                  {field} ({inferFieldType(field)})
                </option>
              ))}
            </select>
          )}
        </div>
      ))}

      {availableFields.length === 0 && (
        <div className="text-center py-4 text-[var(--fluent-neutral-50)]">
          <Settings className="w-8 h-8 mx-auto mb-2 opacity-30" />
          <p className="text-sm">暂无可用字段</p>
          <p className="text-xs mt-1">请先执行查询获取数据</p>
        </div>
      )}
    </div>
  );
}

interface MultiFieldSelectProps {
  availableFields: string[];
  selectedFields: string[];
  onChange: (fields: string[]) => void;
  placeholder?: string;
}

function MultiFieldSelect({ availableFields, selectedFields, onChange, placeholder }: MultiFieldSelectProps) {
  const [showDropdown, setShowDropdown] = useState(false);

  const handleAddField = (field: string) => {
    if (!selectedFields.includes(field)) {
      onChange([...selectedFields, field]);
    }
    setShowDropdown(false);
  };

  const handleRemoveField = (field: string) => {
    onChange(selectedFields.filter(f => f !== field));
  };

  const availableForAdd = availableFields.filter(f => !selectedFields.includes(f));

  return (
    <div className="space-y-2">
      {/* 已选择的字段标签 */}
      <div className="flex flex-wrap gap-2">
        {selectedFields.map((field) => (
          <div
            key={field}
            className="flex items-center gap-1.5 px-2.5 py-1.5 bg-[var(--fluent-primary-bg)] text-[var(--fluent-primary)] rounded-lg text-sm"
          >
            <span>{field}</span>
            <button
              onClick={() => handleRemoveField(field)}
              className="p-0.5 hover:bg-[var(--fluent-primary)]/20 rounded"
            >
              <Trash2 className="w-3.5 h-3.5" />
            </button>
          </div>
        ))}
        
        {/* 添加按钮 */}
        {availableForAdd.length > 0 && (
          <div className="relative">
            <button
              onClick={() => setShowDropdown(!showDropdown)}
              className="flex items-center gap-1 px-2.5 py-1.5 border border-dashed border-[var(--fluent-neutral-30)] text-[var(--fluent-neutral-50)] rounded-lg text-sm hover:border-[var(--fluent-primary)] hover:text-[var(--fluent-primary)] transition-colors"
            >
              <Plus className="w-3.5 h-3.5" />
              <span>添加</span>
            </button>
            
            {/* 下拉选择 */}
            {showDropdown && (
              <div className="absolute top-full left-0 mt-1 w-48 max-h-48 overflow-y-auto bg-[var(--fluent-neutral-0)] border border-[var(--fluent-neutral-12)] rounded-lg shadow-lg z-10">
                {availableForAdd.map((field) => (
                  <button
                    key={field}
                    onClick={() => handleAddField(field)}
                    className="w-full px-3 py-2 text-left text-sm hover:bg-[var(--fluent-neutral-4)] transition-colors"
                  >
                    <div className="flex items-center gap-2">
                      <span className="text-[var(--fluent-neutral-40)]">{getTypeIcon(inferFieldType(field))}</span>
                      <span>{field}</span>
                    </div>
                  </button>
                ))}
              </div>
            )}
          </div>
        )}
      </div>

      {/* 点击外部关闭下拉 */}
      {showDropdown && (
        <div
          className="fixed inset-0 z-0"
          onClick={() => setShowDropdown(false)}
        />
      )}
    </div>
  );
}

export default FieldMappingPanel;
