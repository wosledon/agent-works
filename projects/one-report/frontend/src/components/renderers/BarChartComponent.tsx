import { useMemo, useEffect } from 'react';
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer } from 'recharts';
import { Loader2, AlertCircle, BarChart3 } from 'lucide-react';
import type { ComponentProps, ComponentStyle, DataMapping } from '~/types';
import { useChartData } from '~/hooks';

interface BarChartComponentProps {
  props: ComponentProps;
  style: ComponentStyle;
  dataSourceId?: string;
  dataMapping?: DataMapping[];
  onUpdateProps?: (props: Record<string, unknown>) => void;
}

// 示例数据
const sampleData = [
  { category: '一月', value: 400 },
  { category: '二月', value: 300 },
  { category: '三月', value: 500 },
  { category: '四月', value: 278 },
  { category: '五月', value: 189 },
];

export function BarChartComponent({ 
  props, 
  style, 
  dataSourceId: externalDataSourceId,
  dataMapping,
  onUpdateProps 
}: BarChartComponentProps) {
  const { 
    title, 
    data: propData = [], 
    categoryField = 'category', 
    valueField = 'value',
    xAxis = 'category',
    yAxis = 'value',
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
  const chartData = useMemo(() => {
    if (effectiveDataSourceId) {
      return fetchedData.length > 0 ? fetchedData : [];
    }
    return propData.length > 0 ? propData : sampleData;
  }, [effectiveDataSourceId, fetchedData, propData]);
  
  const xField = categoryField || xAxis || 'category';
  const yField = valueField || yAxis || 'value';
  
  // 获取所有数值字段（用于多系列）
  const numericFields = useMemo(() => {
    if (!chartData.length) return [yField];
    const firstRow = chartData[0] as Record<string, unknown>;
    return Object.entries(firstRow)
      .filter(([key, val]) => key !== xField && typeof val === 'number')
      .map(([key]) => key);
  }, [chartData, xField, yField]);
  
  const colors = ['#0078d4', '#107c10', '#ffc107', '#d13438', '#8b5cf6', '#ec4899'];

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
            <BarChart3 className="w-10 h-10 text-[var(--fluent-neutral-40)] mb-2" />
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
          <BarChart data={chartData as Record<string, unknown>[]}>
            <CartesianGrid strokeDasharray="3 3" stroke="var(--fluent-neutral-12)" />
            <XAxis 
              dataKey={xField}
              tick={{ fill: 'var(--fluent-neutral-60)', fontSize: 12 }}
              axisLine={{ stroke: 'var(--fluent-neutral-20)' }}
              tickLine={{ stroke: 'var(--fluent-neutral-20)' }}
            />
            <YAxis 
              tick={{ fill: 'var(--fluent-neutral-60)', fontSize: 12 }}
              axisLine={{ stroke: 'var(--fluent-neutral-20)' }}
              tickLine={{ stroke: 'var(--fluent-neutral-20)' }}
            />
            <Tooltip 
              contentStyle={{ 
                backgroundColor: 'var(--fluent-neutral-0)', 
                border: '1px solid var(--fluent-neutral-12)',
                borderRadius: 6,
                boxShadow: 'var(--fluent-shadow-8)'
              }}
              labelStyle={{ color: 'var(--fluent-neutral-90)', fontWeight: 600 }}
              itemStyle={{ color: 'var(--fluent-neutral-60)' }}
            />
            
            <Legend 
              wrapperStyle={{ paddingTop: 10 }}
            />
            
            {numericFields.length > 0 ? (
              numericFields.map((field, index) => (
                <Bar 
                  key={field}
                  dataKey={field} 
                  fill={colors[index % colors.length]} 
                  radius={[4, 4, 0, 0]}
                  name={field}
                />
              ))
            ) : (
              <Bar 
                dataKey={yField} 
                fill="#0078d4" 
                radius={[4, 4, 0, 0]}
              />
            )}
          </BarChart>
        </ResponsiveContainer>
      </div>
    </div>
  );
}
