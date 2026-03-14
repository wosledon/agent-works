import { useState, useMemo } from 'react';
import { 
  Database, 
  Settings, 
  Eye, 
  ChevronDown, 
  ChevronRight,
  Play,
  Loader2,
  CheckCircle,
  XCircle,
  BarChart3,
  LineChart,
  PieChart,
  Table2,
  Code,
  X
} from 'lucide-react';
import type { DataSource, ReportComponent, DataMapping } from '~/types';
import { DataPreviewTable } from './DataPreviewTable';

interface ChartBindingPanelProps {
  component: ReportComponent;
  dataSources: DataSource[];
  onUpdateDataSource: (dataSourceId: string) => void;
  onUpdateDataMapping: (mapping: DataMapping[]) => void;
  onUpdateProps: (props: Record<string, unknown>) => void;
  onTestDataSource: (dataSource: DataSource) => Promise<{ success: boolean; message: string; data?: unknown[] }>;
  onClose?: () => void;
}

export function ChartBindingPanel({ 
  component, 
  dataSources, 
  onUpdateDataSource, 
  onUpdateDataMapping,
  onUpdateProps,
  onTestDataSource,
  onClose 
}: ChartBindingPanelProps) {
  const [isExpanded, setIsExpanded] = useState(true);
  const [activeSection, setActiveSection] = useState<'data' | 'mapping' | 'preview'>('data');
  const [sqlQuery, setSqlQuery] = useState(component.props.sqlQuery as string || '');
  const [testData, setTestData] = useState<unknown[] | null>(null);
  const [testStatus, setTestStatus] = useState<'idle' | 'testing' | 'success' | 'error'>('idle');
  const [testMessage, setTestMessage] = useState('');
  
  const selectedDataSource = dataSources.find(ds => ds.id === component.dataSourceId);
  
  // 获取可用字段
  const availableFields = useMemo(() => {
    if (!testData || testData.length === 0) return [];
    return Object.keys(testData[0] as Record<string, unknown>);
  }, [testData]);

  // 获取字段类型
  const getFieldType = (field: string): 'string' | 'number' | 'date' | 'boolean' => {
    if (!testData || testData.length === 0) return 'string';
    const value = (testData[0] as Record<string, unknown>)?.[field];
    if (typeof value === 'number') return 'number';
    if (typeof value === 'boolean') return 'boolean';
    if (value instanceof Date || /\d{4}-\d{2}-\d{2}/.test(String(value))) return 'date';
    return 'string';
  };

  // 执行查询
  const handleExecuteQuery = async () => {
    if (!selectedDataSource) return;
    
    setTestStatus('testing');
    try {
      const result = await onTestDataSource({
        ...selectedDataSource,
        config: {
          ...selectedDataSource.config,
          sql: sqlQuery || selectedDataSource.config.sql
        }
      });
      
      setTestStatus(result.success ? 'success' : 'error');
      setTestMessage(result.message);
      if (result.data) {
        setTestData(result.data);
        // 更新组件数据
        onUpdateProps({ data: result.data });
      }
    } catch (error) {
      setTestStatus('error');
      setTestMessage(error instanceof Error ? error.message : '查询失败');
    }
  };

  // 更新字段映射
  const handleFieldMapping = (fieldType: 'x' | 'y' | 'category' | 'value' | 'name', field: string) => {
    const currentMapping = component.dataMapping || [];
    const existingIndex = currentMapping.findIndex(m => m.field === fieldType);
    
    if (existingIndex >= 0) {
      const updated = [...currentMapping];
      updated[existingIndex] = { 
        field: fieldType, 
        label: field, 
        type: getFieldType(field) 
      };
      onUpdateDataMapping(updated);
    } else {
      onUpdateDataMapping([
        ...currentMapping,
        { field: fieldType, label: field, type: getFieldType(field) }
      ]);
    }
    
    // 同时更新组件 props
    const propsUpdate: Record<string, string> = {};
    if (fieldType === 'x') {
      propsUpdate.xAxis = field;
      propsUpdate.categoryField = field;
    } else if (fieldType === 'y') {
      propsUpdate.yAxis = field;
      propsUpdate.valueField = field;
    } else if (fieldType === 'category') {
      propsUpdate.categoryField = field;
    } else if (fieldType === 'value') {
      propsUpdate.valueField = field;
    } else if (fieldType === 'name') {
      propsUpdate.nameField = field;
    }
    onUpdateProps(propsUpdate);
  };

  // 获取当前选中的字段
  const getSelectedField = (fieldType: string): string => {
    const mapping = component.dataMapping?.find(m => m.field === fieldType);
    return mapping?.label || '';
  };

  const getChartIcon = () => {
    switch (component.type) {
      case 'chart-bar': return <BarChart3 className="w-5 h-5" />;
      case 'chart-line': return <LineChart className="w-5 h-5" />;
      case 'chart-pie': return <PieChart className="w-5 h-5" />;
      case 'table': return <Table2 className="w-5 h-5" />;
      default: return <BarChart3 className="w-5 h-5" />;
    }
  };

  const getFieldRequirements = () => {
    switch (component.type) {
      case 'chart-bar':
      case 'chart-line':
        return [
          { key: 'x', label: 'X轴 / 分类字段', required: true },
          { key: 'y', label: 'Y轴 / 数值字段', required: true },
        ];
      case 'chart-pie':
        return [
          { key: 'name', label: '名称字段', required: true },
          { key: 'value', label: '数值字段', required: true },
        ];
      case 'table':
        return [];
      default:
        return [];
    }
  };

  if (!isExpanded) {
    return (
      <div 
        className="w-12 bg-[var(--fluent-neutral-4)] border-l border-[var(--fluent-neutral-12)] flex flex-col items-center py-4 cursor-pointer hover:bg-[var(--fluent-neutral-8)] transition-colors"
        onClick={() => setIsExpanded(true)}
      >
        <Database className="w-5 h-5 text-[var(--fluent-primary)] mb-2" />
        <ChevronLeft className="w-4 h-4 text-[var(--fluent-neutral-50)]" />
      </div>
    );
  }

  return (
    <div className="w-80 bg-[var(--fluent-neutral-0)] border-l border-[var(--fluent-neutral-12)] flex flex-col">
      {/* 头部 */}
      <div className="flex items-center justify-between px-4 py-3 border-b border-[var(--fluent-neutral-12)]">
        <div className="flex items-center gap-2">
          {getChartIcon()}
          <span className="font-medium text-[var(--fluent-neutral-90)]">数据绑定</span>
        </div>
        <button 
          onClick={() => {
            setIsExpanded(false);
            onClose?.();
          }}
          className="p-1 hover:bg-[var(--fluent-neutral-4)] rounded transition-colors"
        >
          <X className="w-4 h-4 text-[var(--fluent-neutral-50)]" />
        </button>
      </div>

      {/* 标签页 */}
      <div className="flex border-b border-[var(--fluent-neutral-12)]">
        {[
          { id: 'data', label: '数据源', icon: Database },
          { id: 'mapping', label: '字段映射', icon: Settings },
          { id: 'preview', label: '预览', icon: Eye },
        ].map(({ id, label, icon: Icon }) => (
          <button
            key={id}
            onClick={() => setActiveSection(id as typeof activeSection)}
            className={`flex-1 px-3 py-2.5 text-xs font-medium flex items-center justify-center gap-1 transition-colors ${
              activeSection === id
                ? 'text-[var(--fluent-primary)] border-b-2 border-[var(--fluent-primary)]'
                : 'text-[var(--fluent-neutral-50)] hover:text-[var(--fluent-neutral-90)] hover:bg-[var(--fluent-neutral-4)]'
            }`}
          >
            <Icon className="w-3.5 h-3.5" />
            {label}
          </button>
        ))}
      </div>

      {/* 内容区 */}
      <div className="flex-1 overflow-y-auto p-4">
        {/* 数据源选择 */}
        {activeSection === 'data' && (
          <div className="space-y-4">
            <div>
              <label className="block text-xs font-medium text-[var(--fluent-neutral-60)] mb-2">
                选择数据源
              </label>
              <select
                value={component.dataSourceId || ''}
                onChange={(e) => onUpdateDataSource(e.target.value)}
                className="fluent-select w-full"
              >
                <option value="">-- 请选择数据源 --</option>
                {dataSources.map((ds) => (
                  <option key={ds.id} value={ds.id}>
                    {ds.name} ({ds.type})
                  </option>
                ))}
              </select>
            </div>

            {selectedDataSource && (
              <div className="p-3 bg-[var(--fluent-neutral-4)] rounded-lg">
                <div className="flex items-center justify-between mb-2">
                  <span className="text-sm font-medium text-[var(--fluent-neutral-90)]">
                    {selectedDataSource.name}
                  </span>
                  <span className={`text-xs px-2 py-0.5 rounded ${
                    selectedDataSource.testStatus === 'success' 
                      ? 'bg-[var(--fluent-success-bg)] text-[var(--fluent-success)]'
                      : selectedDataSource.testStatus === 'error'
                      ? 'bg-[var(--fluent-error-bg)] text-[var(--fluent-error)]'
                      : 'bg-[var(--fluent-neutral-8)] text-[var(--fluent-neutral-50)]'
                  }`}>
                    {selectedDataSource.testStatus === 'success' ? '正常' : 
                     selectedDataSource.testStatus === 'error' ? '异常' : '未测试'}
                  </span>
                </div>
                <p className="text-xs text-[var(--fluent-neutral-50)]">
                  类型: {selectedDataSource.type}
                </p>
                {selectedDataSource.lastTested && (
                  <p className="text-xs text-[var(--fluent-neutral-50)]">
                    最后测试: {new Date(selectedDataSource.lastTested).toLocaleString('zh-CN')}
                  </p>
                )}
              </div>
            )}

            {/* SQL 编辑器 */}
            {selectedDataSource?.type === 'database' && (
              <div className="space-y-2">
                <label className="block text-xs font-medium text-[var(--fluent-neutral-60)]">
                  <Code className="w-3 h-3 inline mr-1" />
                  SQL 查询 (可选)
                </label>
                <textarea
                  value={sqlQuery}
                  onChange={(e) => setSqlQuery(e.target.value)}
                  rows={4}
                  className="fluent-textarea w-full font-mono text-xs"
                  placeholder="自定义 SQL 查询，留空使用数据源默认查询"
                />
                <button
                  onClick={handleExecuteQuery}
                  disabled={testStatus === 'testing'}
                  className="fluent-btn fluent-btn-primary w-full text-xs"
                >
                  {testStatus === 'testing' ? (
                    <>
                      <Loader2 className="w-3 h-3 animate-spin mr-1" />
                      执行中...
                    </>
                  ) : (
                    <>
                      <Play className="w-3 h-3 mr-1" />
                      执行查询
                    </>
                  )}
                </button>
                
                {testStatus !== 'idle' && (
                  <div className={`flex items-center gap-1.5 text-xs ${
                    testStatus === 'success' ? 'text-[var(--fluent-success)]' : 'text-[var(--fluent-error)]'
                  }`}>
                    {testStatus === 'success' ? (
                      <CheckCircle className="w-3.5 h-3.5" />
                    ) : (
                      <XCircle className="w-3.5 h-3.5" />
                    )}
                    {testMessage}
                  </div>
                )}
              </div>
            )}
          </div>
        )}

        {/* 字段映射 */}
        {activeSection === 'mapping' && (
          <div className="space-y-4">
            {!selectedDataSource ? (
              <div className="text-center py-8 text-[var(--fluent-neutral-50)]">
                <Database className="w-10 h-10 mx-auto mb-2 opacity-30" />
                <p className="text-sm">请先选择数据源</p>
              </div>
            ) : availableFields.length === 0 ? (
              <div className="text-center py-8 text-[var(--fluent-neutral-50)]">
                <Play className="w-10 h-10 mx-auto mb-2 opacity-30" />
                <p className="text-sm">请先执行查询获取数据</p>
              </div>
            ) : (
              <>
                <div className="text-xs text-[var(--fluent-neutral-50)] mb-3">
                  可用字段: {availableFields.join(', ')}
                </div>
                
                {getFieldRequirements().map(({ key, label, required }) => (
                  <div key={key}>
                    <label className="block text-xs font-medium text-[var(--fluent-neutral-60)] mb-1.5">
                      {label} {required && <span className="text-[var(--fluent-error)]">*</span>}
                    </label>
                    <select
                      value={getSelectedField(key)}
                      onChange={(e) => handleFieldMapping(key as 'x' | 'y' | 'category' | 'value' | 'name', e.target.value)}
                      className="fluent-select w-full"
                    >
                      <option value="">-- 选择字段 --</option>
                      {availableFields.map((field) => (
                        <option key={field} value={field}>
                          {field} ({getFieldType(field)})
                        </option>
                      ))}
                    </select>
                  </div>
                ))}

                {component.type === 'table' && (
                  <div className="p-3 bg-[var(--fluent-primary-bg)] rounded-lg">
                    <p className="text-xs text-[var(--fluent-primary)]">
                      表格组件会自动显示所有字段
                    </p>
                  </div>
                )}
              </>
            )}
          </div>
        )}

        {/* 数据预览 */}
        {activeSection === 'preview' && (
          <div className="space-y-4">
            {!selectedDataSource ? (
              <div className="text-center py-8 text-[var(--fluent-neutral-50)]">
                <Database className="w-10 h-10 mx-auto mb-2 opacity-30" />
                <p className="text-sm">请先选择数据源</p>
              </div>
            ) : !testData && !component.props.data ? (
              <div className="text-center py-8 text-[var(--fluent-neutral-50)]">
                <Play className="w-10 h-10 mx-auto mb-2 opacity-30" />
                <p className="text-sm">请先执行查询预览数据</p>
              </div>
            ) : (
              <div className="h-[300px] border border-[var(--fluent-neutral-12)] rounded-lg overflow-hidden">
                <DataPreviewTable 
                  data={(testData || component.props.data || []) as unknown[]} 
                  pageSize={10}
                />
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  );
}

// 缺少的 ChevronLeft 图标
function ChevronLeft({ className }: { className?: string }) {
  return (
    <svg className={className} width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
      <path d="M15 18l-6-6 6-6" />
    </svg>
  );
}
