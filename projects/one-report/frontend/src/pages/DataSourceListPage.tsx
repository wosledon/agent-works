import { useState, useMemo } from 'react';
import { 
  Search, 
  Plus, 
  Edit2, 
  Trash2, 
  TestTube, 
  Database, 
  Globe, 
  FileText,
  CheckCircle,
  XCircle,
  HelpCircle,
  Loader2,
  ChevronLeft,
  ChevronRight,
  AlertTriangle,
  RefreshCw
} from 'lucide-react';
import type { DataSource } from '~/types';
import { DataSourceModal } from '~/components/DataSourceModal';
import { DataPreviewTable } from '~/components/DataPreviewTable';

interface DataSourceListPageProps {
  dataSources: DataSource[];
  onAdd: (dataSource: Omit<DataSource, 'id'>) => void;
  onUpdate: (id: string, dataSource: Partial<DataSource>) => void;
  onRemove: (id: string) => void;
  onTest: (dataSource: DataSource) => Promise<{ success: boolean; message: string; data?: unknown[] }>;
  onBack?: () => void;
}

const typeIcons: Record<DataSource['type'], React.ComponentType<{ className?: string }>> = {
  api: Globe,
  static: FileText,
  file: FileText,
  database: Database,
};

const typeLabels: Record<DataSource['type'], string> = {
  api: 'API 接口',
  static: '静态数据',
  file: '文件',
  database: '数据库',
};

export function DataSourceListPage({ 
  dataSources, 
  onAdd, 
  onUpdate, 
  onRemove, 
  onTest,
  onBack 
}: DataSourceListPageProps) {
  // 搜索和分页状态
  const [searchQuery, setSearchQuery] = useState('');
  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize] = useState(10);
  
  // 模态框状态
  const [showModal, setShowModal] = useState(false);
  const [editingDataSource, setEditingDataSource] = useState<DataSource | null>(null);
  const [showPreview, setShowPreview] = useState(false);
  const [previewDataSource, setPreviewDataSource] = useState<DataSource | null>(null);
  const [previewData, setPreviewData] = useState<unknown[] | null>(null);
  
  // 删除确认状态
  const [showDeleteConfirm, setShowDeleteConfirm] = useState<string | null>(null);
  
  // 测试状态
  const [testingId, setTestingId] = useState<string | null>(null);

  // 过滤和分页
  const filteredDataSources = useMemo(() => {
    if (!searchQuery.trim()) return dataSources;
    const query = searchQuery.toLowerCase();
    return dataSources.filter(ds => 
      ds.name.toLowerCase().includes(query) ||
      typeLabels[ds.type].toLowerCase().includes(query)
    );
  }, [dataSources, searchQuery]);

  const totalPages = Math.ceil(filteredDataSources.length / pageSize);
  const paginatedDataSources = useMemo(() => {
    const start = (currentPage - 1) * pageSize;
    return filteredDataSources.slice(start, start + pageSize);
  }, [filteredDataSources, currentPage, pageSize]);

  // 处理添加
  const handleAdd = () => {
    setEditingDataSource(null);
    setShowModal(true);
  };

  // 处理编辑
  const handleEdit = (ds: DataSource) => {
    setEditingDataSource(ds);
    setShowModal(true);
  };

  // 处理保存
  const handleSave = (dataSource: Omit<DataSource, 'id'>) => {
    if (editingDataSource) {
      onUpdate(editingDataSource.id, dataSource);
    } else {
      onAdd(dataSource);
    }
    setShowModal(false);
  };

  // 处理删除
  const handleDelete = (id: string) => {
    onRemove(id);
    setShowDeleteConfirm(null);
  };

  // 处理测试连接
  const handleTest = async (ds: DataSource) => {
    setTestingId(ds.id);
    try {
      const result = await onTest(ds);
      onUpdate(ds.id, { 
        testStatus: result.success ? 'success' : 'error',
        testMessage: result.message,
        lastTested: new Date().toISOString()
      });
    } finally {
      setTestingId(null);
    }
  };

  // 处理预览数据
  const handlePreview = async (ds: DataSource) => {
    setPreviewDataSource(ds);
    setShowPreview(true);
    const result = await onTest(ds);
    if (result.success && result.data) {
      setPreviewData(result.data);
    } else {
      setPreviewData([]);
    }
  };

  // 获取状态图标
  const getStatusIcon = (status?: DataSource['testStatus']) => {
    switch (status) {
      case 'success':
        return <CheckCircle className="w-4 h-4 text-[var(--fluent-success)]" />;
      case 'error':
        return <XCircle className="w-4 h-4 text-[var(--fluent-error)]" />;
      case 'testing':
        return <Loader2 className="w-4 h-4 text-[var(--fluent-primary)] animate-spin" />;
      default:
        return <HelpCircle className="w-4 h-4 text-[var(--fluent-neutral-40)]" />;
    }
  };

  // 获取状态文本
  const getStatusText = (status?: DataSource['testStatus']) => {
    switch (status) {
      case 'success':
        return '连接正常';
      case 'error':
        return '连接失败';
      case 'testing':
        return '测试中';
      default:
        return '未测试';
    }
  };

  return (
    <div className="flex flex-col h-full bg-[var(--fluent-neutral-0)]">
      {/* 头部 */}
      <div className="flex items-center justify-between px-6 py-4 border-b border-[var(--fluent-neutral-12)]">
        <div className="flex items-center gap-4">
          {onBack && (
            <button 
              onClick={onBack}
              className="p-2 hover:bg-[var(--fluent-neutral-4)] rounded-lg transition-colors"
            >
              <ChevronLeft className="w-5 h-5 text-[var(--fluent-neutral-60)]" />
            </button>
          )}
          <div>
            <h1 className="text-xl font-semibold text-[var(--fluent-neutral-90)]">数据源管理</h1>
            <p className="text-sm text-[var(--fluent-neutral-50)] mt-0.5">
              共 {dataSources.length} 个数据源
            </p>
          </div>
        </div>
        <button 
          onClick={handleAdd}
          className="fluent-btn fluent-btn-primary flex items-center gap-2"
        >
          <Plus className="w-4 h-4" />
          添加数据源
        </button>
      </div>

      {/* 搜索栏 */}
      <div className="px-6 py-4 border-b border-[var(--fluent-neutral-12)]">
        <div className="relative max-w-md">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-[var(--fluent-neutral-40)]" />
          <input
            type="text"
            value={searchQuery}
            onChange={(e) => {
              setSearchQuery(e.target.value);
              setCurrentPage(1);
            }}
            placeholder="搜索数据源名称..."
            className="fluent-input w-full pl-10"
          />
          {searchQuery && (
            <button
              onClick={() => {
                setSearchQuery('');
                setCurrentPage(1);
              }}
              className="absolute right-3 top-1/2 -translate-y-1/2 text-[var(--fluent-neutral-40)] hover:text-[var(--fluent-neutral-60)]"
            >
              ×
            </button>
          )}
        </div>
      </div>

      {/* 表格 */}
      <div className="flex-1 overflow-auto p-6">
        {paginatedDataSources.length === 0 ? (
          <div className="flex flex-col items-center justify-center h-full text-[var(--fluent-neutral-50)]">
            <Database className="w-16 h-16 mb-4 opacity-30" />
            <p className="text-lg font-medium">
              {searchQuery ? '未找到匹配的数据源' : '暂无数据源'}
            </p>
            <p className="text-sm mt-1">
              {searchQuery ? '请尝试其他搜索词' : '点击上方按钮添加数据源'}
            </p>
          </div>
        ) : (
          <div className="fluent-table-container">
            <table className="fluent-table">
              <thead>
                <tr>
                  <th className="w-12"></th>
                  <th>名称</th>
                  <th>类型</th>
                  <th>状态</th>
                  <th>最后测试</th>
                  <th className="text-right">操作</th>
                </tr>
              </thead>
              <tbody>
                {paginatedDataSources.map((ds) => {
                  const Icon = typeIcons[ds.type];
                  return (
                    <tr key={ds.id} className="group hover:bg-[var(--fluent-neutral-4)]">
                      <td>
                        <div className="p-2 bg-[var(--fluent-primary-bg)] rounded-lg w-fit">
                          <Icon className="w-4 h-4 text-[var(--fluent-primary)]" />
                        </div>
                      </td>
                      <td>
                        <div className="font-medium text-[var(--fluent-neutral-90)]">{ds.name}</div>
                        {ds.testMessage && ds.testStatus === 'error' && (
                          <div className="text-xs text-[var(--fluent-error)] mt-0.5 truncate max-w-[200px]">
                            {ds.testMessage}
                          </div>
                        )}
                      </td>
                      <td>
                        <span className="fluent-badge fluent-badge-secondary">
                          {typeLabels[ds.type]}
                        </span>
                      </td>
                      <td>
                        <div className="flex items-center gap-2">
                          {testingId === ds.id ? (
                            <Loader2 className="w-4 h-4 animate-spin text-[var(--fluent-primary)]" />
                          ) : (
                            getStatusIcon(ds.testStatus)
                          )}
                          <span className="text-sm text-[var(--fluent-neutral-60)]">
                            {testingId === ds.id ? '测试中...' : getStatusText(ds.testStatus)}
                          </span>
                        </div>
                      </td>
                      <td>
                        <span className="text-sm text-[var(--fluent-neutral-50)]">
                          {ds.lastTested 
                            ? new Date(ds.lastTested).toLocaleString('zh-CN')
                            : '-'
                          }
                        </span>
                      </td>
                      <td>
                        <div className="flex items-center justify-end gap-1">
                          <button
                            onClick={() => handlePreview(ds)}
                            className="p-2 text-[var(--fluent-neutral-50)] hover:text-[var(--fluent-primary)] hover:bg-[var(--fluent-primary-bg)] rounded-lg transition-colors"
                            title="预览数据"
                          >
                            <RefreshCw className="w-4 h-4" />
                          </button>
                          <button
                            onClick={() => handleTest(ds)}
                            disabled={testingId === ds.id}
                            className="p-2 text-[var(--fluent-neutral-50)] hover:text-[var(--fluent-primary)] hover:bg-[var(--fluent-primary-bg)] rounded-lg transition-colors disabled:opacity-50"
                            title="测试连接"
                          >
                            <TestTube className="w-4 h-4" />
                          </button>
                          <button
                            onClick={() => handleEdit(ds)}
                            className="p-2 text-[var(--fluent-neutral-50)] hover:text-[var(--fluent-primary)] hover:bg-[var(--fluent-primary-bg)] rounded-lg transition-colors"
                            title="编辑"
                          >
                            <Edit2 className="w-4 h-4" />
                          </button>
                          <button
                            onClick={() => setShowDeleteConfirm(ds.id)}
                            className="p-2 text-[var(--fluent-neutral-50)] hover:text-[var(--fluent-error)] hover:bg-[var(--fluent-error-bg)] rounded-lg transition-colors"
                            title="删除"
                          >
                            <Trash2 className="w-4 h-4" />
                          </button>
                        </div>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* 分页 */}
      {totalPages > 1 && (
        <div className="flex items-center justify-between px-6 py-4 border-t border-[var(--fluent-neutral-12)]">
          <div className="text-sm text-[var(--fluent-neutral-50)]">
            显示 {(currentPage - 1) * pageSize + 1} - {Math.min(currentPage * pageSize, filteredDataSources.length)} 条，
            共 {filteredDataSources.length} 条
          </div>
          <div className="flex items-center gap-2">
            <button
              onClick={() => setCurrentPage(p => Math.max(1, p - 1))}
              disabled={currentPage === 1}
              className="p-2 hover:bg-[var(--fluent-neutral-4)] rounded-lg disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
            >
              <ChevronLeft className="w-4 h-4" />
            </button>
            <span className="text-sm text-[var(--fluent-neutral-60)] px-2">
              {currentPage} / {totalPages}
            </span>
            <button
              onClick={() => setCurrentPage(p => Math.min(totalPages, p + 1))}
              disabled={currentPage === totalPages}
              className="p-2 hover:bg-[var(--fluent-neutral-4)] rounded-lg disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
            >
              <ChevronRight className="w-4 h-4" />
            </button>
          </div>
        </div>
      )}

      {/* 数据源模态框 */}
      {showModal && (
        <DataSourceModal
          dataSource={editingDataSource}
          onSave={handleSave}
          onClose={() => setShowModal(false)}
          onTest={onTest}
        />
      )}

      {/* 删除确认模态框 */}
      {showDeleteConfirm && (
        <div className="fluent-modal-overlay" onClick={() => setShowDeleteConfirm(null)}>
          <div className="fluent-modal w-full max-w-md" onClick={e => e.stopPropagation()}>
            <div className="fluent-modal-header">
              <div className="flex items-center gap-2 text-[var(--fluent-error)]">
                <AlertTriangle className="w-5 h-5" />
                <h3 className="text-lg font-semibold">确认删除</h3>
              </div>
            </div>
            <div className="fluent-modal-body">
              <p className="text-[var(--fluent-neutral-60)]">
                确定要删除数据源 "{dataSources.find(ds => ds.id === showDeleteConfirm)?.name}" 吗？
                此操作不可撤销，相关的图表组件将无法正常显示数据。
              </p>
            </div>
            <div className="fluent-modal-footer">
              <button 
                onClick={() => setShowDeleteConfirm(null)}
                className="fluent-btn fluent-btn-secondary"
              >
                取消
              </button>
              <button 
                onClick={() => handleDelete(showDeleteConfirm)}
                className="fluent-btn fluent-btn-error"
              >
                删除
              </button>
            </div>
          </div>
        </div>
      )}

      {/* 数据预览模态框 */}
      {showPreview && previewDataSource && (
        <div className="fluent-modal-overlay" onClick={() => setShowPreview(false)}>
          <div 
            className="fluent-modal w-full max-w-6xl h-[80vh] flex flex-col" 
            onClick={e => e.stopPropagation()}
          >
            <div className="fluent-modal-header flex items-center justify-between">
              <div>
                <h3 className="text-lg font-semibold text-[var(--fluent-neutral-90)]">
                  数据预览: {previewDataSource.name}
                </h3>
                <p className="text-sm text-[var(--fluent-neutral-50)] mt-0.5">
                  {previewData ? `${previewData.length} 条记录` : '加载中...'}
                </p>
              </div>
              <button 
                onClick={() => setShowPreview(false)}
                className="p-2 hover:bg-[var(--fluent-neutral-4)] rounded-lg transition-colors"
              >
                ×
              </button>
            </div>
            <div className="flex-1 overflow-hidden p-0">
              <DataPreviewTable 
                data={previewData || []} 
                loading={!previewData}
              />
            </div>
            <div className="fluent-modal-footer">
              <button 
                onClick={() => setShowPreview(false)}
                className="fluent-btn fluent-btn-secondary"
              >
                关闭
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
