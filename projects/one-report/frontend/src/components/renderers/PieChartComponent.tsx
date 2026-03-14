import { PieChart, Pie, Cell, Tooltip, Legend, ResponsiveContainer } from 'recharts';
import type { ComponentProps, ComponentStyle } from '~/types';

interface PieChartComponentProps {
  props: ComponentProps;
  style: ComponentStyle;
}

const COLORS = ['#3b82f6', '#10b981', '#f59e0b', '#ef4444', '#8b5cf6', '#ec4899'];

export function PieChartComponent({ props, style }: PieChartComponentProps) {
  const { title, data = [] } = props;
  
  // 示例数据
  const sampleData = data.length > 0 ? data : [
    { name: '类别A', value: 400 },
    { name: '类别B', value: 300 },
    { name: '类别C', value: 200 },
    { name: '类别D', value: 100 },
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
          <PieChart>
            <Pie
              data={sampleData}
              cx="50%"
              cy="50%"
              labelLine={false}
              label={({ name, percent }) => `${name} ${((percent ?? 0) * 100).toFixed(0)}%`}
              outerRadius={80}
              fill="#8884d8"
              dataKey="value"
            >
              {sampleData.map((_entry, index) => (
                <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
              ))}
            </Pie>
            <Tooltip 
              contentStyle={{ 
                backgroundColor: '#fff', 
                border: '1px solid #e5e7eb',
                borderRadius: 6,
                boxShadow: '0 4px 6px -1px rgba(0, 0, 0, 0.1)'
              }}
            />
            <Legend />
          </PieChart>
        </ResponsiveContainer>
      </div>
    </div>
  );
}
