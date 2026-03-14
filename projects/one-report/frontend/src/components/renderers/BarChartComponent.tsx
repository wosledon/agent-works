import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer } from 'recharts';
import type { ComponentProps, ComponentStyle } from '~/types';

interface BarChartComponentProps {
  props: ComponentProps;
  style: ComponentStyle;
}

export function BarChartComponent({ props, style }: BarChartComponentProps) {
  const { title, xAxis = 'name', yAxis = 'value', data = [] } = props;
  
  // 示例数据
  const sampleData = data.length > 0 ? data : [
    { name: 'A', value: 400 },
    { name: 'B', value: 300 },
    { name: 'C', value: 200 },
    { name: 'D', value: 278 },
    { name: 'E', value: 189 },
  ];

  return (
    <div className="h-full flex flex-col bg-white rounded-lg p-4"
      style={{
        backgroundColor: style.backgroundColor || '#ffffff',
        borderRadius: style.borderRadius || 8,
        borderColor: style.borderColor || '#e5e7eb',
        borderWidth: style.borderWidth || 1,
        borderStyle: 'solid',
      }}
    >
      {title && (
        <h3 className="text-sm font-medium text-gray-900 mb-3"
          style={{ fontSize: style.fontSize || 14, color: style.color }}
        >
          {title}
        </h3>
      )}
      <div className="flex-1 min-h-0"
        style={{ padding: style.padding || 0 }}
      >
        <ResponsiveContainer width="100%" height="100%">
          <BarChart data={sampleData}>
            <CartesianGrid strokeDasharray="3 3" stroke="#e5e7eb" />
            <XAxis 
              dataKey={xAxis} 
              tick={{ fill: '#6b7280', fontSize: 12 }}
              axisLine={{ stroke: '#e5e7eb' }}
            />
            <YAxis 
              tick={{ fill: '#6b7280', fontSize: 12 }}
              axisLine={{ stroke: '#e5e7eb' }}
            />
            <Tooltip 
              contentStyle={{ 
                backgroundColor: '#fff', 
                border: '1px solid #e5e7eb',
                borderRadius: 6,
                boxShadow: '0 4px 6px -1px rgba(0, 0, 0, 0.1)'
              }}
            />
            <Legend />
            <Bar dataKey={yAxis} fill="#3b82f6" radius={[4, 4, 0, 0]} />
          </BarChart>
        </ResponsiveContainer>
      </div>
    </div>
  );
}
