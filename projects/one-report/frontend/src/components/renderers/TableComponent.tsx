import type { ComponentProps, ComponentStyle } from '../../types';

interface TableComponentProps {
  props: ComponentProps;
  style: ComponentStyle;
}

export function TableComponent({ props, style }: TableComponentProps) {
  const { title, columns = [], data = [] } = props;
  
  // 示例数据
  const sampleData = data.length > 0 ? data : [
    { col1: '数据1', col2: '值1', col3: 100 },
    { col1: '数据2', col2: '值2', col3: 200 },
    { col1: '数据3', col2: '值3', col3: 300 },
  ];
  
  const sampleColumns = columns.length > 0 ? columns : [
    { key: 'col1', title: '列1', dataIndex: 'col1' },
    { key: 'col2', title: '列2', dataIndex: 'col2' },
    { key: 'col3', title: '列3', dataIndex: 'col3' },
  ];

  return (
    <div className="h-full flex flex-col bg-white rounded-lg overflow-hidden"
      style={{
        backgroundColor: style.backgroundColor || '#ffffff',
        borderRadius: style.borderRadius || 8,
      }}
    >
      {title && (
        <div className="px-4 py-3 border-b border-gray-200 bg-gray-50"
          style={{
            borderColor: style.borderColor || '#e5e7eb',
            borderWidth: style.borderWidth || 1,
          }}
        >
          <h3 className="text-sm font-medium text-gray-900"
            style={{ fontSize: style.fontSize || 14, color: style.color }}
          >
            {title}
          </h3>
        </div>
      )}
      <div className="flex-1 overflow-auto p-2"
        style={{
          borderColor: style.borderColor || '#e5e7eb',
          borderWidth: style.borderWidth || 1,
          borderStyle: 'solid',
        }}
      >
        <table className="w-full text-sm"
          style={{ fontSize: style.fontSize || 14, color: style.color }}
        >
          <thead>
            <tr className="border-b border-gray-200"
              style={{ borderColor: style.borderColor || '#e5e7eb' }}
            >
              {sampleColumns.map((col) => (
                <th 
                  key={col.key} 
                  className="text-left py-2 px-2 font-medium text-gray-700"
                  style={{ 
                    width: col.width, 
                    textAlign: col.align || 'left',
                  }}
                >
                  {col.title}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {sampleData.map((row, idx) => (
              <tr key={idx} className="border-b border-gray-100 hover:bg-gray-50"
                style={{ borderColor: style.borderColor || '#f3f4f6' }}
              >
                {sampleColumns.map((col) => (
                  <td 
                    key={col.key} 
                    className="py-2 px-2 text-gray-600"
                    style={{ textAlign: col.align || 'left' }}
                  >
                    {(row as Record<string, unknown>)[col.dataIndex] as string}
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
