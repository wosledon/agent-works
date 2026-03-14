import { PieChart, Pie, Cell, Tooltip, Legend, ResponsiveContainer } from 'recharts';
import type { ComponentProps, ComponentStyle } from '~/types';

interface PieChartComponentProps {
  props: ComponentProps;
  style: ComponentStyle;
}

// 示例数据
const sampleData = [
  { name: '类别A', value: 400 },
  { name: '类别B', value: 300 },
  { name: '类别C', value: 200 },
  { name: '类别D', value: 100 },
];

const COLORS = ['#0078d4', '#107c10', '#ffc107', '#d13438', '#8b5cf6', '#ec4899', '#00b7c3', '#ff8c00'];

export function PieChartComponent({ props, style }: PieChartComponentProps) {
  const { 
    title, 
    data = [], 
    nameField = 'name', 
    valueField = 'value'
  } = props;
  
  // 使用实际数据或示例数据
  const chartData = data.length > 0 ? data : sampleData;
  const nField = nameField || 'name';
  const vField = valueField || 'value';
  
  // 转换数据格式
  const formattedData = chartData.map((item: unknown) => ({
    name: (item as Record<string, string>)[nField] || '',
    value: Number((item as Record<string, number>)[vField]) || 0,
  }));

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
          <PieChart>
            <Pie
              data={formattedData}
              cx="50%"
              cy="50%"
              labelLine={true}
              label={({ name, percent }) => `${name} ${((percent ?? 0) * 100).toFixed(0)}%`}
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
