import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer } from 'recharts';
import type { ComponentProps, ComponentStyle } from '~/types';

interface LineChartComponentProps {
  props: ComponentProps;
  style: ComponentStyle;
}

// 示例数据
const sampleData = [
  { date: '01/01', value: 400, value2: 240 },
  { date: '01/02', value: 300, value2: 139 },
  { date: '01/03', value: 500, value2: 380 },
  { date: '01/04', value: 278, value2: 390 },
  { date: '01/05', value: 189, value2: 480 },
  { date: '01/06', value: 239, value2: 380 },
  { date: '01/07', value: 349, value2: 430 },
];

export function LineChartComponent({ props, style }: LineChartComponentProps) {
  const { 
    title, 
    data = [], 
    categoryField = 'date', 
    valueField = 'value',
    xAxis = 'date',
    yAxis = 'value'
  } = props;
  
  // 使用实际数据或示例数据
  const chartData = data.length > 0 ? data : sampleData;
  const xField = categoryField || xAxis || 'date';
  const yField = valueField || yAxis || 'value';
  
  // 获取所有数值字段（用于多系列）
  const getNumericFields = () => {
    if (!chartData.length) return [yField];
    const firstRow = chartData[0] as Record<string, unknown>;
    return Object.entries(firstRow)
      .filter(([key, val]) => key !== xField && typeof val === 'number')
      .map(([key]) => key);
  };
  
  const numericFields = getNumericFields();
  const colors = ['#0078d4', '#107c10', '#ffc107', '#d13438', '#8b5cf6', '#ec4899'];

  const getShadowClass = () => {
    switch (style.shadow) {
      case 'small': return 'fluent-card-shadow-small';
      case 'medium': return 'fluent-card-shadow-medium';
      case 'large': return 'fluent-card-shadow-large';
      default: return '';
    }
  };

  return (
    <div 
      className={`h-full flex flex-col bg-[var(--fluent-neutral-0)] ${getShadowClass()}`}
      style={{
        borderRadius: style.borderRadius ?? 8,
        border: style.borderWidth ? `${style.borderWidth}px solid ${style.borderColor || 'var(--fluent-neutral-12)'}` : '1px solid var(--fluent-neutral-12)',
      }}
    >
      {title && (
        <div className="px-4 py-3 border-b border-[var(--fluent-neutral-12)]"
          style={{ 
            fontSize: style.fontSize ? style.fontSize + 2 : 16, 
            color: style.color || 'var(--fluent-neutral-90)',
            fontWeight: 600
          }}
        >
          {title}
        </div>
      )}
      
      <div 
        className="flex-1 min-h-0 p-4"
        style={{ 
          backgroundColor: style.backgroundColor || 'var(--fluent-neutral-0)',
          borderRadius: `0 0 ${style.borderRadius ?? 8}px ${style.borderRadius ?? 8}px`,
        }}
      >
        <ResponsiveContainer width="100%" height="100%">
          <LineChart data={chartData as Record<string, unknown>[]}>
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
                <Line 
                  key={field}
                  type="monotone"
                  dataKey={field} 
                  stroke={colors[index % colors.length]}
                  strokeWidth={2}
                  dot={{ fill: colors[index % colors.length], strokeWidth: 2, r: 4 }}
                  activeDot={{ r: 6 }}
                  name={field}
                />
              ))
            ) : (
              <Line 
                type="monotone"
                dataKey={yField} 
                stroke="#0078d4"
                strokeWidth={2}
                dot={{ fill: '#0078d4', strokeWidth: 2, r: 4 }}
                activeDot={{ r: 6 }}
              />
            )}
          </LineChart>
        </ResponsiveContainer>
      </div>
    </div>
  );
}
