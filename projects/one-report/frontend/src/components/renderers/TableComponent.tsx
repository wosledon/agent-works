import { useState } from 'react';
import { ChevronLeft, ChevronRight, ChevronsLeft, ChevronsRight } from 'lucide-react';
import type { ComponentProps, ComponentStyle } from '~/types';

interface TableComponentProps {
  props: ComponentProps;
  style: ComponentStyle;
}

// 示例数据
const sampleData = [
  { name: '产品A', category: '类别1', value: 100, sales: 1000 },
  { name: '产品B', category: '类别2', value: 200, sales: 2000 },
  { name: '产品C', category: '类别1', value: 300, sales: 1500 },
  { name: '产品D', category: '类别3', value: 150, sales: 800 },
  { name: '产品E', category: '类别2', value: 250, sales: 2200 },
];

export function TableComponent({ props, style }: TableComponentProps) {
  const { title, columns: propColumns = [], data = [] } = props;
  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  
  // 使用实际数据或示例数据
  const tableData = data.length > 0 ? data : sampleData;
  
  // 自动生成列配置
  const autoColumns = () => {
    if (propColumns.length > 0) return propColumns;
    if (!tableData.length) return [];
    const firstRow = tableData[0] as Record<string, unknown>;
    return Object.keys(firstRow).map((key) => ({
      key,
      title: key.charAt(0).toUpperCase() + key.slice(1),
      dataIndex: key,
      align: 'left' as const,
    }));
  };
  
  const columns = autoColumns();
  
  // 分页
  const totalPages = Math.ceil(tableData.length / pageSize);
  const paginatedData = tableData.slice((currentPage - 1) * pageSize, currentPage * pageSize);
  
  const getShadowClass = () => {
    switch (style.shadow) {
      case 'small': return 'fluent-card-shadow-small';
      case 'medium': return 'fluent-card-shadow-medium';
      case 'large': return 'fluent-card-shadow-large';
      default: return '';
    }
  };

  const getAlignment = (align?: string) => {
    switch (align) {
      case 'center': return 'text-center';
      case 'right': return 'text-right';
      default: return 'text-left';
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
        className="flex-1 overflow-auto"
        style={{ 
          backgroundColor: style.backgroundColor || 'var(--fluent-neutral-0)',
        }}
      >
        <table className="w-full"
          style={{ fontSize: style.fontSize || 14 }}
        >
          <thead className="sticky top-0 z-10">
            <tr 
              className="border-b border-[var(--fluent-neutral-12)]"
              style={{ backgroundColor: 'var(--fluent-neutral-4)' }}
            >
              {columns.map((col) => (
                <th 
                  key={col.key} 
                  className={`py-3 px-4 font-semibold text-[var(--fluent-neutral-90)] ${getAlignment(col.align)}`}
                  style={{ 
                    width: col.width,
                    borderBottom: '1px solid var(--fluent-neutral-12)'
                  }}
                >
                  {col.title}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {paginatedData.map((row, idx) => (
              <tr 
                key={idx} 
                className="border-b border-[var(--fluent-neutral-8)] hover:bg-[var(--fluent-neutral-4)] transition-colors"
              >
                {columns.map((col) => (
                  <td 
                    key={col.key} 
                    className={`py-2.5 px-4 text-[var(--fluent-neutral-60)] ${getAlignment(col.align)}`}
                  >
                    {String((row as Record<string, unknown>)[col.dataIndex] ?? '')}
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
        
        {tableData.length === 0 && (
          <div className="flex flex-col items-center justify-center h-40 text-[var(--fluent-neutral-40)]">
            <svg className="w-10 h-10 mb-2 opacity-50" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M9 17v-2m3 2v-4m3 4v-6m2 10H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
            </svg>
            <p className="text-sm">暂无数据</p>
          </div>
        )}
      </div>
      
      {/* 分页 */}
      {tableData.length > 0 && (
        <div className="flex items-center justify-between px-4 py-2 border-t border-[var(--fluent-neutral-12)] bg-[var(--fluent-neutral-4)]"
          style={{ borderRadius: `0 0 ${style.borderRadius ?? 8}px ${style.borderRadius ?? 8}px` }}
        >
          <div className="flex items-center gap-2 text-xs text-[var(--fluent-neutral-50)]">
            <span>共 {tableData.length} 条</span>
            <select
              value={pageSize}
              onChange={(e) => { setPageSize(Number(e.target.value)); setCurrentPage(1); }}
              className="fluent-select text-xs py-1"
            >
              <option value={5}>5 条/页</option>
              <option value={10}>10 条/页</option>
              <option value={20}>20 条/页</option>
              <option value={50}>50 条/页</option>
            </select>
          </div>
          
          <div className="flex items-center gap-1">
            <button
              onClick={() => setCurrentPage(1)}
              disabled={currentPage === 1}
              className="p-1 rounded hover:bg-[var(--fluent-neutral-8)] disabled:opacity-30 disabled:cursor-not-allowed"
            >
              <ChevronsLeft className="w-4 h-4" />
            </button>
            <button
              onClick={() => setCurrentPage(p => Math.max(1, p - 1))}
              disabled={currentPage === 1}
              className="p-1 rounded hover:bg-[var(--fluent-neutral-8)] disabled:opacity-30 disabled:cursor-not-allowed"
            >
              <ChevronLeft className="w-4 h-4" />
            </button>
            
            <span className="text-xs text-[var(--fluent-neutral-60)] px-2">
              {currentPage} / {totalPages || 1}
            </span>
            
            <button
              onClick={() => setCurrentPage(p => Math.min(totalPages, p + 1))}
              disabled={currentPage >= totalPages}
              className="p-1 rounded hover:bg-[var(--fluent-neutral-8)] disabled:opacity-30 disabled:cursor-not-allowed"
            >
              <ChevronRight className="w-4 h-4" />
            </button>
            <button
              onClick={() => setCurrentPage(totalPages)}
              disabled={currentPage >= totalPages}
              className="p-1 rounded hover:bg-[var(--fluent-neutral-8)] disabled:opacity-30 disabled:cursor-not-allowed"
            >
              <ChevronsRight className="w-4 h-4" />
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
