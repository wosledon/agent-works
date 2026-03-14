import { useState, useEffect, useCallback, useRef } from 'react';
import type { DataSource, DataMapping } from '~/types';

interface UseChartDataOptions {
  dataSourceId?: string;
  sql?: string;
  params?: Record<string, unknown>;
  mapping?: DataMapping[];
  enabled?: boolean;
}

interface UseChartDataResult {
  data: unknown[];
  loading: boolean;
  error: string | null;
  refresh: () => Promise<void>;
  executionTime: number;
}

/**
 * 图表数据获取 Hook
 * 监听 dataSourceId、sql、params、mapping 变化，自动获取数据
 */
export function useChartData(
  dataSources: DataSource[],
  executeQuery: (dataSource: DataSource, params?: Record<string, unknown>) => Promise<{
    success: boolean;
    message: string;
    data?: unknown[];
  }>,
  options: UseChartDataOptions
): UseChartDataResult {
  const { dataSourceId, sql, params = {}, mapping, enabled = true } = options;

  const [data, setData] = useState<unknown[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [executionTime, setExecutionTime] = useState(0);

  // 用于防抖的 ref
  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const lastFetchRef = useRef<string>('');

  const fetchData = useCallback(async () => {
    if (!dataSourceId || !enabled) {
      setData([]);
      return;
    }

    const dataSource = dataSources.find(ds => ds.id === dataSourceId);
    if (!dataSource) {
      setError('数据源不存在');
      return;
    }

    // 生成缓存键，避免重复请求
    const fetchKey = JSON.stringify({ dataSourceId, sql, params, mapping });
    if (fetchKey === lastFetchRef.current && data.length > 0) {
      return;
    }

    setLoading(true);
    setError(null);
    const startTime = performance.now();

    try {
      // 如果有自定义 SQL，合并到数据源配置
      const effectiveDataSource = sql 
        ? { ...dataSource, config: { ...dataSource.config, sql } }
        : dataSource;

      const result = await executeQuery(effectiveDataSource, params);
      const endTime = performance.now();

      setExecutionTime(Math.round(endTime - startTime));

      if (result.success && result.data) {
        // 根据 mapping 转换数据
        const transformedData = transformDataByMapping(result.data, mapping);
        setData(transformedData);
        lastFetchRef.current = fetchKey;
      } else {
        setError(result.message || '获取数据失败');
        setData([]);
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : '获取数据时发生错误');
      setData([]);
    } finally {
      setLoading(false);
    }
  }, [dataSourceId, sql, params, mapping, dataSources, enabled, executeQuery, data.length]);

  // 防抖的数据获取
  useEffect(() => {
    if (debounceRef.current) {
      clearTimeout(debounceRef.current);
    }

    debounceRef.current = setTimeout(() => {
      fetchData();
    }, 300);

    return () => {
      if (debounceRef.current) {
        clearTimeout(debounceRef.current);
      }
    };
  }, [fetchData]);

  // 手动刷新
  const refresh = useCallback(async () => {
    lastFetchRef.current = '';
    await fetchData();
  }, [fetchData]);

  return {
    data,
    loading,
    error,
    refresh,
    executionTime,
  };
}

/**
 * 根据字段映射转换数据
 */
function transformDataByMapping(
  rawData: unknown[],
  mapping?: DataMapping[]
): unknown[] {
  if (!mapping || mapping.length === 0 || rawData.length === 0) {
    return rawData;
  }

  // 创建字段映射关系
  const fieldMap = new Map<string, string>();
  mapping.forEach(m => {
    fieldMap.set(m.field, m.label);
  });

  // 转换数据
  return rawData.map(row => {
    if (typeof row !== 'object' || row === null) return row;

    const transformed: Record<string, unknown> = {};
    const rowData = row as Record<string, unknown>;

    // 按照映射关系提取字段
    mapping.forEach(m => {
      if (m.label in rowData) {
        transformed[m.field] = rowData[m.label];
      }
    });

    // 保留未映射的原始字段
    Object.entries(rowData).forEach(([key, value]) => {
      if (!fieldMap.has(key)) {
        transformed[key] = value;
      }
    });

    return transformed;
  });
}

export default useChartData;
