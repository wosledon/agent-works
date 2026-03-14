import { useState, useMemo, useRef, useCallback } from 'react';
import { ArrowUpDown, ArrowUp, ArrowDown, Loader2, Copy, Check } from 'lucide-react';

interface DataPreviewTableProps {
  data: unknown[];
  loading?: boolean;
  pageSize?: number;
}

type SortDirection = 'asc' | 'desc' | null;

interface SortState {
  key: string | null;
  direction: SortDirection;
}

export function DataPreviewTable({ 
  data, 
  loading = false,
  pageSize = 100 
}: DataPreviewTableProps) {
  const [sort, setSort] = useState<SortState>({ key: null, direction: null });
  const [currentPage, setCurrentPage] = useState(1);
  const [copiedCell, setCopiedCell] = useState<string | null>(null);
  const [columnWidths, setColumnWidths] = useState<Record<string, number>>({});
  const resizingRef = useRef<{ key: string; startX: number; startWidth: number } | null>(null);
  const tableRef = useRef<HTMLTableElement>(null);

  // 获取列定义
  const columns = useMemo(() => {
    if (data.length === 0) return [];
    return Object.keys(data[0] as Record<string, unknown>).map(key => ({
      key,
      title: key,
      width: columnWidths[key] || 150,
    }));
  }, [data, columnWidths]);

  // 排序数据
  const sortedData = useMemo(() => {
    if (!sort.key || !sort.direction) return data;
    
    return [...data].sort((a, b) => {
      const aVal = (a as Record<string, unknown>)[sort.key!];
      const bVal = (b as Record<string, unknown>)[sort.key!];
      
      // 处理 null/undefined
      if (aVal == null && bVal == null) return 0;
      if (aVal == null) return sort.direction === 'asc' ? -1 : 1;
      if (bVal == null) return sort.direction === 'asc' ? 1 : -1;
      
      // 数字比较
      if (typeof aVal === 'number' && typeof bVal === 'number') {
        return sort.direction === 'asc' ? aVal - bVal : bVal - aVal;
      }
      
      // 字符串比较
      const aStr = String(aVal);
      const bStr = String(bVal);
      const comparison = aStr.localeCompare(bStr);
      return sort.direction === 'asc' ? comparison : -comparison;
    });
  }, [data, sort]);

  // 分页数据
  const totalPages = Math.ceil(sortedData.length / pageSize);
  const paginatedData = useMemo(() => {
    const start = (currentPage - 1) * pageSize;
    return sortedData.slice(start, start + pageSize);
  }, [sortedData, currentPage, pageSize]);

  // 处理排序
  const handleSort = (key: string) => {
    setSort(prev => {
      if (prev.key !== key) {
        return { key, direction: 'asc' };
      }
      if (prev.direction === 'asc') {
        return { key, direction: 'desc' };
      }
      return { key: null, direction: null };
    });
    setCurrentPage(1);
  };

  // 获取排序图标
  const getSortIcon = (key: string) => {
    if (sort.key !== key) {
      return <ArrowUpDown className="w-3.5 h-3.5 text-[var(--fluent-neutral-30)]" />;
    }
    if (sort.direction === 'asc') {
      return <ArrowUp className="w-3.5 h-3.5 text-[var(--fluent-primary)]" />;
    }
    return <ArrowDown className="w-3.5 h-3.5 text-[var(--fluent-primary)]" />;
  };

  // 复制单元格内容
  const handleCopyCell = async (value: unknown, cellId: string) => {
    try {
      await navigator.clipboard.writeText(String(value));
      setCopiedCell(cellId);
      setTimeout(() => setCopiedCell(null), 1500);
    } catch {
      // 复制失败静默处理
    }
  };

  // 列宽调整
  const handleResizeStart = (e: React.MouseEvent, key: string) => {
    e.preventDefault();
    const currentWidth = columnWidths[key] || 150;
    resizingRef.current = { key, startX: e.clientX, startWidth: currentWidth };
    document.addEventListener('mousemove', handleResizeMove);
    document.addEventListener('mouseup', handleResizeEnd);
  };

  const handleResizeMove = useCallback((e: MouseEvent) => {
    if (!resizingRef.current) return;
    const { key, startX, startWidth } = resizingRef.current;
    const newWidth = Math.max(80, startWidth + e.clientX - startX);
    setColumnWidths(prev => ({ ...prev, [key]: newWidth }));
  }, []);

  const handleResizeEnd = useCallback(() => {
    resizingRef.current = null;
    document.removeEventListener('mousemove', handleResizeMove);
    document.removeEventListener('mouseup', handleResizeEnd);
  }, [handleResizeMove]);

  // 格式化单元格值
  const formatValue = (value: unknown): string => {
    if (value == null) return '-';
    if (typeof value === 'boolean') return value ? '是' : '否';
    if (typeof value === 'object') return JSON.stringify(value).slice(0, 50);
    return String(value);
  };

  // 获取值类型样式
  const getValueTypeClass = (value: unknown): string => {
    if (value == null) return 'text-[var(--fluent-neutral-30)] italic';
    if (typeof value === 'number') return 'text-[var(--fluent-primary)] font-mono text-right';
    if (typeof value === 'boolean') return 'text-[var(--fluent-success)]';
    return 'text-[var(--fluent-neutral-90)]';
  };

  if (loading) {
    return (
      <div className="flex flex-col items-center justify-center h-64 text-[var(--fluent-neutral-50)]">
        <Loader2 className="w-8 h-8 animate-spin mb-3" />
        <p>加载数据中...</p>
      </div>
    );
  }

  if (data.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center h-64 text-[var(--fluent-neutral-50)]">
        <p className="text-lg">暂无数据</p>
      </div>
    );
  }

  return (
    <div className="flex flex-col h-full">
      {/* 表格 */}
      <div className="flex-1 overflow-auto">
        <table ref={tableRef} className="fluent-table w-full">
          <thead className="sticky top-0 z-10">
            <tr>
              <th className="w-12 text-center bg-[var(--fluent-neutral-4)]">#</th>
              {columns.map(col => (
                <th 
                  key={col.key}
                  className="relative bg-[var(--fluent-neutral-4)] group"
                  style={{ width: col.width, minWidth: col.width }}
                >
                  <div className="flex items-center justify-between"
                  >
                    <button
                      onClick={() => handleSort(col.key)}
                      className="flex items-center gap-1.5 flex-1 text-left hover:text-[var(--fluent-primary)] transition-colors"
                    >
                      {col.title}
                      {getSortIcon(col.key)}
                    </button>
                  </div>
                  {/* 拖拽调整列宽 */}
                  <div
                    className="absolute right-0 top-0 h-full w-1 cursor-col-resize hover:bg-[var(--fluent-primary)] opacity-0 group-hover:opacity-100 transition-opacity"
                    onMouseDown={(e) => handleResizeStart(e, col.key)}
                  />
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {paginatedData.map((row, rowIndex) => {
              const actualIndex = (currentPage - 1) * pageSize + rowIndex + 1;
              return (
                <tr key={rowIndex} className="hover:bg-[var(--fluent-neutral-4)] group">
                  <td className="text-center text-[var(--fluent-neutral-40)] text-sm">
                    {actualIndex}
                  </td>
                  {columns.map(col => {
                    const value = (row as Record<string, unknown>)[col.key];
                    const cellId = `${rowIndex}-${col.key}`;
                    return (
                      <td 
                        key={col.key}
                        className="relative group/cell"
                        style={{ width: col.width }}
                      >
                        <span className={`${getValueTypeClass(value)} truncate block`}>
                          {formatValue(value)}
                        </span>
                        {/* 复制按钮 */}
                        <button
                          onClick={() => handleCopyCell(value, cellId)}
                          className="absolute right-1 top-1/2 -translate-y-1/2 p-1 bg-[var(--fluent-neutral-0)] shadow-sm rounded opacity-0 group-hover/cell:opacity-100 hover:bg-[var(--fluent-primary-bg)] transition-all"
                          title="复制"
                        >
                          {copiedCell === cellId ? (
                            <Check className="w-3 h-3 text-[var(--fluent-success)]" />
                          ) : (
                            <Copy className="w-3 h-3 text-[var(--fluent-neutral-50)]" />
                          )}
                        </button>
                      </td>
                    );
                  })}
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>

      {/* 分页 */}
      {totalPages > 1 && (
        <div className="flex items-center justify-between px-4 py-3 border-t border-[var(--fluent-neutral-12)]">
          <div className="text-sm text-[var(--fluent-neutral-50)]">
            显示 {(currentPage - 1) * pageSize + 1} - {Math.min(currentPage * pageSize, sortedData.length)} 条，
            共 {sortedData.length} 条
          </div>
          <div className="flex items-center gap-1">
            <button
              onClick={() => setCurrentPage(1)}
              disabled={currentPage === 1}
              className="px-2 py-1 text-sm hover:bg-[var(--fluent-neutral-4)] rounded disabled:opacity-50 transition-colors"
            >
              首页
            </button>
            <button
              onClick={() => setCurrentPage(p => Math.max(1, p - 1))}
              disabled={currentPage === 1}
              className="px-2 py-1 text-sm hover:bg-[var(--fluent-neutral-4)] rounded disabled:opacity-50 transition-colors"
            >
              上一页
            </button>
            <span className="text-sm text-[var(--fluent-neutral-60)] px-2">
              {currentPage} / {totalPages}
            </span>
            <button
              onClick={() => setCurrentPage(p => Math.min(totalPages, p + 1))}
              disabled={currentPage === totalPages}
              className="px-2 py-1 text-sm hover:bg-[var(--fluent-neutral-4)] rounded disabled:opacity-50 transition-colors"
            >
              下一页
            </button>
            <button
              onClick={() => setCurrentPage(totalPages)}
              disabled={currentPage === totalPages}
              className="px-2 py-1 text-sm hover:bg-[var(--fluent-neutral-4)] rounded disabled:opacity-50 transition-colors"
            >
              末页
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
