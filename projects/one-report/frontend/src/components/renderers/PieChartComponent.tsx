import { useMemo, useEffect } from 'react';
import { PieChart, Pie, Cell, Tooltip, Legend, ResponsiveContainer } from 'recharts';
import { Loader2, AlertCircle, PieChart as PieChartIcon } from 'lucide-react';
import type { ComponentProps, ComponentStyle, DataMapping } from '~/types';
import { useChartData } from '~/hooks';

interface PieChartComponentProps {
  props: ComponentProps;
  style: ComponentStyle;
  dataSourceId?: string;
  dataMapping?: DataMapping[];
  onUpdateProps?: (props: Record<string, unknown>) => void;
}

// 示例数据
const sampleData = [
  { name: '类别A', value: 400 },
  { name: '类别B', value: 300 },
  { name: '类别C', value: 200 },
  { name: '类别D', value: 100 },
];

const COLORS = ['#0078d4', '#107c10', '#ffc107', '#d13438', '#8b5cf6', '#ec4899', '#00b7c3', '#ff8c00'];

export function PieChartComponent({ 
  props, 
  style, 
  dataSourceId: externalDataSourceId,
  dataMapping,
  onUpdateProps 
}: PieChartComponentProps) {
  const { 
    title, 
    data: propData = [], 
    nameField = 'name', 
    valueField = 'value',
    sqlQuery,
    params,
    dataSourceId: propDataSourceId,
  } = props;
  
  // 优先使用外部传入的 dataSourceId，其次使用 props 中的
  const effectiveDataSourceId = externalDataSourceId || propDataSourceId;
  
  // 使用 chart data hook 获取实时数据
  const { data: fetchedData, loading, error, refresh, executionTime } = useChartData({
    dataSourceId: effectiveDataSourceId,
    sql: sqlQuery,
    params: params as Record<string, unknown>,
    mapping: dataMapping,
  });
  
  // 当数据获取成功时，通过 onUpdateProps 回传数据给父组件
  useEffect(() => {
    if (fetchedData.length > 0 && onUpdateProps) {
      onUpdateProps({ data: fetchedData, executionTime });
    }
  }, [fetchedData, executionTime, onUpdateProps]);
  
  // 使用实际数据、获取的数据或示例数据
  const rawData = useMemo(() => {
    if (effectiveDataSourceId) {
      return fetchedData.length > 0 ? fetchedData : [];
    }
    return propData.length > 0 ? propData : sampleData;
  }, [effectiveDataSourceId, fetchedData, propData]);
  
  const nField = nameField || 'name';
  const vField = valueField || 'value';
  
  // 转换数据格式 - 使用 useMemo 缓存
  const formattedData = useMemo(() => {
    if (!rawData.length) return [];
    return rawData.map((item: unknown) => ({
      name: String((item as Record<string, unknown>)[nField] ?? ''),
      value: Number((item as Record<string, unknown>)[vField]) || 0,
    }));
  }, [rawData, nField, vField]);

  const getShadowClass = () => {
    switch (style.shadow) {
      case 'small': return 'fluent-card-shadow-small';
      case 'medium': return 'fluent-card-shadow-medium';
      case 'large': return 'fluent-card-shadow-large';
      default: return '';
    }
  };

  // 是否显示示例数据提示
  const isUsingSampleData = !effectiveDataSourceId && propData.length === 0;
  // 是否有真实数据（非示例数据）
  const hasRealData = effectiveDataSourceId 
    ? fetchedData.length > 0 
    : propData.length > 0;
  
  // 计算总值用于显示百分比
  const totalValue = useMemo(() => {
    return formattedData.reduce((sum, item) => sum + item.value, 0);
  }, [formattedData]);

  return (
    <div 
      className={`h-full flex flex-col bg-[var(--fluent-neutral-0)] ${getShadowClass()} relative`}
      style={{
        borderRadius: style.borderRadius ?? 8,
        border: style.borderWidth ? `${style.borderWidth}px solid ${style.borderColor || 'var(--fluent-neutral-12)'}` : '1px solid var(--fluent-neutral-12)',
      }}
    >
      {title && (
        <div className="px-4 py-3 border-b border-[var(--fluent-neutral-12)] flex items-center justify-between"
          style={{ 
            fontSize: style.fontSize ? style.fontSize + 2 : 16, 
            color: style.color || 'var(--fluent-neutral-90)',
            fontWeight: 600
          }}
        >
          <span>{title}</span>
          {executionTime && (
            <span className="text-xs text-[var(--fluent-neutral-50)] font-normal">
              {executionTime}ms
            </span>
          )}
        </div>
      )}
      
      <div 
        className="flex-1 min-h-0 p-4 relative"
        style={{ 
          backgroundColor: style.backgroundColor || 'var(--fluent-neutral-0)',
          borderRadius: `0 0 ${style.borderRadius ?? 8}px ${style.borderRadius ?? 8}px`,
        }}
      >
        {/* 加载遮罩 */}
        {loading && (
          <div className="absolute inset-0 z-10 flex flex-col items-center justify-center bg-[var(--fluent-neutral-0)]/80 backdrop-blur-sm"
            style={{ borderRadius: `0 0 ${style.borderRadius ?? 8}px ${style.borderRadius ?? 8}px` }}
          >
            <Loader2 className="w-8 h-8 text-[var(--fluent-primary)] animate-spin mb-2" />
            <span className="text-sm text-[var(--fluent-neutral-60)]">加载中...</span>
          </div>
        )}
        
        {/* 错误提示 */}
        {error && (
          <div className="absolute inset-0 z-10 flex flex-col items-center justify-center bg-[var(--fluent-error-bg)]/10 p-4"
            style={{ borderRadius: `0 0 ${style.borderRadius ?? 8}px ${style.borderRadius ?? 8}px` }}
          >
            <AlertCircle className="w-10 h-10 text-[var(--fluent-error)] mb-2" />
            <span className="text-sm text-[var(--fluent-error)] text-center mb-2">{error}</span>
            <button
              onClick={refresh}
              className="fluent-btn fluent-btn-secondary text-xs"
            >
              重试
            </button>
          </div>
        )}
        
        {/* 空数据提示 */}
        {!loading && !error && effectiveDataSourceId && !hasRealData && (
          <div className="absolute inset-0 z-10 flex flex-col items-center justify-center bg-[var(--fluent-neutral-4)]/50"
            style={{ borderRadius: `0 0 ${style.borderRadius ?? 8}px ${style.borderRadius ?? 8}px` }}
          >
            <PieChartIcon className="w-10 h-10 text-[var(--fluent-neutral-40)] mb-2" />
            <span className="text-sm text-[var(--fluent-neutral-50)]">暂无数据</span>
            <span className="text-xs text-[var(--fluent-neutral-40)] mt-1">请检查数据源配置</span>
          </div>
        )}
        
        {/* 示例数据提示 */}
        {isUsingSampleData && (
          <div className="absolute top-2 right-2 z-5 px-2 py-1 bg-[var(--fluent-neutral-8)] text-[var(--fluent-neutral-50)] text-xs rounded">
            示例数据
          </div>
        )}
        
        <ResponsiveContainer width="100%" height="100%">
          <PieChart>
            <Pie
              data={formattedData}
              cx="50%"
              cy="45%"
              labelLine={true}
              label={({ name, percent }) => totalValue > 0 ? `${name} ${((percent ?? 0) * 100).toFixed(0)}%` : ''}
              outerRadius={80}
              fill="#8884d8"
              dataKey="value"
            >
              {formattedData.map((_entry, index) => (
                <Cell 
                  key={`cell-${index}`} 
                  fill={COLORS[index % COLORS.length]}
                  stroke="var(--fluent-neutral-0)"
                  strokeWidth={2}
                />
              ))}
            </Pie>
            
            <Tooltip 
              contentStyle={{ 
                backgroundColor: 'var(--fluent-neutral-0)', 
                border: '1px solid var(--fluent-neutral-12)',
                borderRadius: 6,
                boxShadow: 'var(--fluent-shadow-8)'
              }}
              formatter={(value: number) => [value, '数值']}
            />
            
            <Legend 
              verticalAlign="bottom"
              height={36}
              wrapperStyle={{ paddingTop: 10 }}
            />
          </PieChart>
        </ResponsiveContainer>
      </div>
    </div>
  );
}
