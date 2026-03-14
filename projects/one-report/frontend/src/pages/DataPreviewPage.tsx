import { useState, useCallback } from 'react';
import { 
  Play, 
  Maximize2, 
  Minimize2, 
  Database, 
  Clock, 
  Rows3,
  AlertCircle,
  Loader2,
  ChevronDown,
  RefreshCw
} from 'lucide-react';
import type { DataSource } from '~/types';
import { DataPreviewTable } from '~/components/DataPreviewTable';
import { QueryParamsPanel } from '~/components/QueryParamsPanel';

interface DataPreviewPageProps {
  dataSources: DataSource[];
  onExecuteQuery: (dataSource: DataSource, params?: Record<string, unknown>) => Promise<{
    success: boolean;
    message: string;
    data?: unknown[];
    executionTime?: number;
  }>;
}

export function DataPreviewPage({ dataSources, onExecuteQuery }: DataPreviewPageProps) {
  // 状态
  const [selectedDataSourceId, setSelectedDataSourceId] = useState<string>('');
  const [queryParams, setQueryParams] = useState<Record<string, unknown>>({});
  const [data, setData] = useState<unknown[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [executionTime, setExecutionTime] = useState<number>(0);
  const [isFullscreen, setIsFullscreen] = useState(false);
  const [showParams, setShowParams] = useState(true);

  const selectedDataSource = dataSources.find(ds => ds.id === selectedDataSourceId);

  // 切换数据源时清空数据
  const handleDataSourceChange = (dataSourceId: string) => {
    setSelectedDataSourceId(dataSourceId);
    setData([]);
    setError(null);
    setExecutionTime(0);
    setQueryParams({});
  };

  // 执行查询
  const handleExecute = useCallback(async () => {
    if (!selectedDataSource) return;

    setLoading(true);
    setError(null);
    const startTime = performance.now();

    try {
      const result = await onExecuteQuery(selectedDataSource, queryParams);
      const endTime = performance.now();
      
      setExecutionTime(Math.round(endTime - startTime));
      
      if (result.success && result.data) {
        setData(result.data);
      } else {
        setError(result.message || '查询失败');
        setData([]);
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : '执行查询时发生错误');
      setData([]);
      setExecutionTime(0);
    } finally {
      setLoading(false);
    }
  }, [selectedDataSource, queryParams, onExecuteQuery]);

  // 获取 SQL 内容（用于参数解析）
  const sqlContent = selectedDataSource?.config?.sql || '';

  // 判断是否有参数
  const hasParams = sqlContent.includes('{{') || sqlContent.includes(':');

  return (
    <div className={`flex flex-col bg-[var(--fluent-neutral-0)] ${isFullscreen ? 'fixed inset-0 z-50' : 'h-full'}`}>
      {/* 顶部工具栏 */}
      <div className="flex items-center justify-between px-6 py-4 border-b border-[var(--fluent-neutral-12)] bg-[var(--fluent-neutral-0)]">
        <div className="flex items-center gap-4">
          <div className="flex items-center gap-2">
            <Database className="w-5 h-5 text-[var(--fluent-primary)]" />
            <span className="font-medium text-[var(--fluent-neutral-90)]">数据预览</span>
          </div>
          
          {/* 数据源选择 */}
          <div className="relative">
            <select
              value={selectedDataSourceId}
              onChange={(e) => handleDataSourceChange(e.target.value)}
              className="fluent-select min-w-[200px]"
            >
              <option value="">-- 选择数据源 --</option>
              {dataSources.map((ds) => (
                <option key={ds.id} value={ds.id}>
                  {ds.name} ({ds.type})
                </option>
              ))}
            </select>
            <ChevronDown className="absolute right-3 top-1/2 -translate-y-1/2 w-4 h-4 text-[var(--fluent-neutral-40)] pointer-events-none" />
          </div>

          {/* 参数面板切换 */}
          {hasParams && selectedDataSource && (
            <button
              onClick={() => setShowParams(!showParams)}
              className={`text-sm px-3 py-1.5 rounded-lg transition-colors ${
                showParams 
                  ? 'bg-[var(--fluent-primary-bg)] text-[var(--fluent-primary)]' 
                  : 'text-[var(--fluent-neutral-60)] hover:bg-[var(--fluent-neutral-4)]'
              }`}
            >
              参数设置
            </button>
          )}
        </div>

        <div className="flex items-center gap-3">
          {/* 刷新按钮 */}
          <button
            onClick={handleExecute}
            disabled={!selectedDataSource || loading}
            className="fluent-btn fluent-btn-primary flex items-center gap-2"
          >
            {loading ? (
              <>
                <Loader2 className="w-4 h-4 animate-spin" />
                <span>执行中...</span>
              </>
            ) : (
              <>
                <Play className="w-4 h-4" />
                <span>执行查询</span>
              </>
            )}
          </button>

          {/* 全屏切换 */}
          <button
            onClick={() => setIsFullscreen(!isFullscreen)}
            className="p-2 hover:bg-[var(--fluent-neutral-4)] rounded-lg transition-colors"
            title={isFullscreen ? '退出全屏' : '全屏预览'}
          >
            {isFullscreen ? (
              <Minimize2 className="w-5 h-5 text-[var(--fluent-neutral-60)]" />
            ) : (
              <Maximize2 className="w-5 h-5 text-[var(--fluent-neutral-60)]" />
            )}
          </button>
        </div>
      </div>

      {/* 统计信息栏 */}
      {(data.length > 0 || executionTime > 0) && (
        <div className="flex items-center gap-6 px-6 py-2 border-b border-[var(--fluent-neutral-12)] bg-[var(--fluent-neutral-4)]">
          <div className="flex items-center gap-2 text-sm text-[var(--fluent-neutral-60)]">
            <Rows3 className="w-4 h-4" />
            <span>{data.length} 条记录</span>
          </div>
          <div className="flex items-center gap-2 text-sm text-[var(--fluent-neutral-60)]">
            <Clock className="w-4 h-4" />
            <span>执行时间: {executionTime}ms</span>
          </div>
          {selectedDataSource && (
            <div className="flex items-center gap-2 text-sm text-[var(--fluent-neutral-50)]">
              <span>数据源: {selectedDataSource.name}</span>
            </div>
          )}
        </div>
      )}

      {/* 错误提示 */}
      {error && (
        <div className="mx-6 mt-4 p-4 bg-[var(--fluent-error-bg)] border border-[var(--fluent-error)]/20 rounded-lg">
          <div className="flex items-start gap-3">
            <AlertCircle className="w-5 h-5 text-[var(--fluent-error)] flex-shrink-0 mt-0.5" />
            <div>
              <p className="font-medium text-[var(--fluent-error)]">查询失败</p>
              <p className="text-sm text-[var(--fluent-error)]/80 mt-1">{error}</p>
            </div>
          </div>
        </div>
      )}

      {/* 主体内容 */}
      <div className="flex-1 overflow-hidden flex">
        {/* 参数面板 */}
        {showParams && hasParams && selectedDataSource && (
          <div className="w-72 border-r border-[var(--fluent-neutral-12)] p-4 overflow-y-auto">
            <div className="mb-4">
              <h3 className="text-sm font-medium text-[var(--fluent-neutral-90)]">查询参数</h3>
              <p className="text-xs text-[var(--fluent-neutral-50)] mt-1">
                设置 SQL 中的参数值
              </p>
            </div>
            <QueryParamsPanel
              sql={sqlContent}
              values={queryParams}
              onChange={setQueryParams}
            />
          </div>
        )}

        {/* 数据表格 */}
        <div className="flex-1 overflow-hidden">
          <DataPreviewTable 
            data={data} 
            loading={loading}
          />
        </div>
      </div>
    </div>
  );
}

export default DataPreviewPage;
