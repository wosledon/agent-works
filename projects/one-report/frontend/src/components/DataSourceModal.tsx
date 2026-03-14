import { useState, useEffect } from 'react';
import { 
  X, 
  Save, 
  TestTube, 
  Database, 
  Globe, 
  FileText, 
  CheckCircle, 
  XCircle, 
  Loader2,
  Eye,
  Play,
  AlertCircle
} from 'lucide-react';
import type { DataSource, DataSourceConfig } from '~/types';

interface DataSourceModalProps {
  dataSource: DataSource | null;
  onSave: (ds: Omit<DataSource, 'id'>) => void;
  onClose: () => void;
  onTest: (dataSource: DataSource) => Promise<{ success: boolean; message: string; data?: unknown[] }>;
}

type DataSourceType = 'api' | 'database' | 'file' | 'static';

const typeOptions: { value: DataSourceType; label: string; icon: React.ComponentType<{ className?: string }>; description: string }[] = [
  { value: 'api', label: 'API 接口', icon: Globe, description: '从 REST API 获取数据' },
  { value: 'database', label: '数据库', icon: Database, description: '连接 MySQL/PostgreSQL 等数据库' },
  { value: 'file', label: '文件', icon: FileText, description: '上传 CSV/JSON/Excel 文件' },
  { value: 'static', label: '静态数据', icon: FileText, description: '直接输入 JSON 数据' },
];

export function DataSourceModal({ dataSource, onSave, onClose, onTest }: DataSourceModalProps) {
  const [name, setName] = useState('');
  const [type, setType] = useState<DataSourceType>('api');
  const [config, setConfig] = useState<DataSourceConfig>({});
  const [testStatus, setTestStatus] = useState<'idle' | 'testing' | 'success' | 'error'>('idle');
  const [testMessage, setTestMessage] = useState('');
  const [testData, setTestData] = useState<unknown[] | null>(null);
  const [showPreview, setShowPreview] = useState(false);
  const [activeTab, setActiveTab] = useState<'config' | 'preview'>('config');

  useEffect(() => {
    if (dataSource) {
      setName(dataSource.name);
      setType(dataSource.type);
      setConfig(dataSource.config);
      setTestStatus(dataSource.testStatus || 'untested');
      setTestMessage(dataSource.testMessage || '');
    } else {
      setName('');
      setType('api');
      setConfig({ method: 'GET', headers: {} });
      setTestStatus('idle');
      setTestMessage('');
      setTestData(null);
    }
  }, [dataSource]);

  const handleTest = async () => {
    setTestStatus('testing');
    setTestMessage('');
    
    const testDataSource: DataSource = {
      id: dataSource?.id || 'test',
      name,
      type,
      config,
    };
    
    try {
      const result = await onTest(testDataSource);
      setTestStatus(result.success ? 'success' : 'error');
      setTestMessage(result.message);
      if (result.data) {
        setTestData(result.data);
      }
    } catch (error) {
      setTestStatus('error');
      setTestMessage(error instanceof Error ? error.message : '测试失败');
    }
  };

  const handleSave = () => {
    if (!name.trim()) return;
    
    onSave({
      name: name.trim(),
      type,
      config,
      testStatus: testStatus === 'testing' ? 'untested' : testStatus,
      testMessage,
      lastTested: testStatus !== 'idle' ? new Date().toISOString() : undefined,
    });
  };

  const updateConfig = (updates: Partial<DataSourceConfig>) => {
    setConfig(prev => ({ ...prev, ...updates }));
    // 重置测试状态当配置改变时
    if (testStatus !== 'idle') {
      setTestStatus('idle');
      setTestMessage('');
    }
  };

  const getStatusIcon = () => {
    switch (testStatus) {
      case 'testing':
        return <Loader2 className="w-4 h-4 fluent-spin" />;
      case 'success':
        return <CheckCircle className="w-4 h-4 text-[#107c10]" />;
      case 'error':
        return <XCircle className="w-4 h-4 text-[#d13438]" />;
      default:
        return null;
    }
  };

  const getStatusText = () => {
    switch (testStatus) {
      case 'testing':
        return '测试中...';
      case 'success':
        return testMessage || '连接成功';
      case 'error':
        return testMessage || '连接失败';
      default:
        return '未测试';
    }
  };

  return (
    <div className="fluent-modal-overlay" onClick={onClose}>
      <div className="fluent-modal w-full max-w-2xl" onClick={e => e.stopPropagation()}>
        <div className="fluent-modal-header flex items-center justify-between">
          <h3 className="text-lg font-semibold text-[var(--fluent-neutral-90)]">
            {dataSource ? '编辑数据源' : '添加数据源'}
          </h3>
          <button onClick={onClose} className="p-1 hover:bg-[var(--fluent-neutral-8)] rounded transition-colors">
            <X className="w-5 h-5 text-[var(--fluent-neutral-50)]" />
          </button>
        </div>
        
        {/* 标签页 */}
        <div className="flex border-b border-[var(--fluent-neutral-12)]">
          <button
            onClick={() => setActiveTab('config')}
            className={`flex items-center gap-2 px-5 py-3 text-sm font-medium transition-colors ${
              activeTab === 'config'
                ? 'text-[var(--fluent-primary)] border-b-2 border-[var(--fluent-primary)]'
                : 'text-[var(--fluent-neutral-60)] hover:text-[var(--fluent-neutral-90)] hover:bg-[var(--fluent-neutral-4)]'
            }`}
          >
            <Database className="w-4 h-4" />
            配置
          </button>
          <button
            onClick={() => {
              setActiveTab('preview');
              setShowPreview(true);
            }}
            className={`flex items-center gap-2 px-5 py-3 text-sm font-medium transition-colors ${
              activeTab === 'preview'
                ? 'text-[var(--fluent-primary)] border-b-2 border-[var(--fluent-primary)]'
                : 'text-[var(--fluent-neutral-60)] hover:text-[var(--fluent-neutral-90)] hover:bg-[var(--fluent-neutral-4)]'
            }`}
            disabled={!testData && testStatus !== 'success'}
          >
            <Eye className="w-4 h-4" />
            数据预览
            {testData && (
              <span className="fluent-badge fluent-badge-info">{testData.length} 条</span>
            )}
          </button>
        </div>
        
        <div className="fluent-modal-body max-h-[60vh] overflow-y-auto">
          {activeTab === 'config' ? (
            <div className="space-y-5">
              {/* 数据源名称 */}
              <div>
                <label className="block text-sm font-medium text-[var(--fluent-neutral-90)] mb-2">
                  数据源名称 <span className="text-[var(--fluent-error)]">*</span>
                </label>
                <input
                  type="text"
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  placeholder="例如：销售数据"
                  className="fluent-input w-full"
                  required
                />
              </div>
              
              {/* 数据源类型 */}
              <div>
                <label className="block text-sm font-medium text-[var(--fluent-neutral-90)] mb-2">
                  数据源类型
                </label>
                <div className="grid grid-cols-2 gap-3">
                  {typeOptions.map((option) => {
                    const Icon = option.icon;
                    return (
                      <button
                        key={option.value}
                        type="button"
                        onClick={() => setType(option.value)}
                        className={`flex items-start gap-3 p-4 border rounded-lg text-left transition-all ${
                          type === option.value
                            ? 'border-[var(--fluent-primary)] bg-[var(--fluent-primary-bg)]'
                            : 'border-[var(--fluent-neutral-12)] hover:border-[var(--fluent-neutral-20)] hover:bg-[var(--fluent-neutral-4)]'
                        }`}
                      >
                        <Icon className={`w-5 h-5 mt-0.5 ${type === option.value ? 'text-[var(--fluent-primary)]' : 'text-[var(--fluent-neutral-50)]'}`} />
                        <div>
                          <div className={`text-sm font-medium ${type === option.value ? 'text-[var(--fluent-primary)]' : 'text-[var(--fluent-neutral-90)]'}`}>
                            {option.label}
                          </div>
                          <div className="text-xs text-[var(--fluent-neutral-40)] mt-0.5">
                            {option.description}
                          </div>
                        </div>
                      </button>
                    );
                  })}
                </div>
              </div>
              
              {/* 类型特定配置 */}
              <div className="border-t border-[var(--fluent-neutral-12)] pt-5">
                <label className="block text-sm font-medium text-[var(--fluent-neutral-90)] mb-3">
                  连接配置
                </label>
                
                {type === 'api' && (
                  <div className="space-y-4">
                    <div>
                      <label className="block text-xs text-[var(--fluent-neutral-50)] mb-1.5">请求 URL</label>
                      <input
                        type="url"
                        value={config.url || ''}
                        onChange={(e) => updateConfig({ url: e.target.value })}
                        placeholder="https://api.example.com/data"
                        className="fluent-input w-full"
                      />
                    </div>
                    <div className="grid grid-cols-2 gap-3">
                      <div>
                        <label className="block text-xs text-[var(--fluent-neutral-50)] mb-1.5">请求方法</label>
                        <select
                          value={config.method || 'GET'}
                          onChange={(e) => updateConfig({ method: e.target.value as DataSourceConfig['method'] })}
                          className="fluent-select w-full"
                        >
                          <option value="GET">GET</option>
                          <option value="POST">POST</option>
                          <option value="PUT">PUT</option>
                          <option value="DELETE">DELETE</option>
                        </select>
                      </div>
                      <div>
                        <label className="block text-xs text-[var(--fluent-neutral-50)] mb-1.5">超时时间 (秒)</label>
                        <input
                          type="number"
                          defaultValue={30}
                          className="fluent-input w-full"
                        />
                      </div>
                    </div>
                    <div>
                      <label className="block text-xs text-[var(--fluent-neutral-50)] mb-1.5">请求头 (JSON)</label>
                      <textarea
                        value={config.headers ? JSON.stringify(config.headers, null, 2) : '{}'}
                        onChange={(e) => {
                          try {
                            const headers = JSON.parse(e.target.value);
                            updateConfig({ headers });
                          } catch {
                            // 忽略解析错误
                          }
                        }}
                        rows={3}
                        className="fluent-textarea w-full font-mono text-xs"
                        placeholder='{"Authorization": "Bearer token"}'
                      />
                    </div>
                  </div>
                )}
                
                {type === 'database' && (
                  <div className="space-y-4">
                    <div>
                      <label className="block text-xs text-[var(--fluent-neutral-50)] mb-1.5">数据库类型</label>
                      <select className="fluent-select w-full">
                        <option value="mysql">MySQL</option>
                        <option value="postgresql">PostgreSQL</option>
                        <option value="mssql">SQL Server</option>
                        <option value="sqlite">SQLite</option>
                      </select>
                    </div>
                    <div>
                      <label className="block text-xs text-[var(--fluent-neutral-50)] mb-1.5">连接字符串</label>
                      <input
                        type="text"
                        value={config.connectionString || ''}
                        onChange={(e) => updateConfig({ connectionString: e.target.value })}
                        placeholder="host=localhost;port=5432;database=mydb;user=admin;password=***"
                        className="fluent-input w-full"
                      />
                    </div>
                    <div>
                      <label className="block text-xs text-[var(--fluent-neutral-50)] mb-1.5">SQL 查询</label>
                      <textarea
                        value={config.sql || ''}
                        onChange={(e) => updateConfig({ sql: e.target.value })}
                        rows={4}
                        className="fluent-textarea w-full font-mono text-sm"
                        placeholder="SELECT * FROM table_name WHERE status = 'active'"
                      />
                    </div>
                  </div>
                )}
                
                {type === 'file' && (
                  <div className="space-y-4">
                    <div className="border-2 border-dashed border-[var(--fluent-neutral-20)] rounded-lg p-8 text-center">
                      <FileText className="w-10 h-10 text-[var(--fluent-neutral-30)] mx-auto mb-3" />
                      <p className="text-sm text-[var(--fluent-neutral-60)] mb-2">拖拽文件到这里，或点击上传</p>
                      <p className="text-xs text-[var(--fluent-neutral-40)]">支持 CSV, JSON, Excel 格式</p>
                      <input
                        type="file"
                        accept=".csv,.json,.xlsx,.xls"
                        className="hidden"
                        onChange={(e) => {
                          const file = e.target.files?.[0];
                          if (file) {
                            updateConfig({ fileName: file.name, fileType: file.type });
                          }
                        }}
                      />
                    </div>
                    {config.fileName && (
                      <div className="flex items-center gap-2 p-3 bg-[var(--fluent-neutral-4)] rounded-lg">
                        <FileText className="w-4 h-4 text-[var(--fluent-primary)]" />
                        <span className="text-sm text-[var(--fluent-neutral-90)]">{config.fileName}</span>
                      </div>
                    )}
                  </div>
                )}
                
                {type === 'static' && (
                  <div>
                    <label className="block text-xs text-[var(--fluent-neutral-50)] mb-1.5">
                      JSON 数据
                      <span className="text-[var(--fluent-neutral-40)] ml-1">(数组格式)</span>
                    </label>
                    <textarea
                      value={config.staticData || ''}
                      onChange={(e) => updateConfig({ staticData: e.target.value })}
                      rows={10}
                      className="fluent-textarea w-full font-mono text-sm"
                      placeholder={`[\n  { "name": "产品A", "value": 100 },\n  { "name": "产品B", "value": 200 }\n]`}
                    />
                  </div>
                )}
              </div>
              
              {/* 测试状态 */}
              {testStatus !== 'idle' && (
                <div className={`flex items-center gap-2 p-3 rounded-lg ${
                  testStatus === 'success' ? 'bg-[var(--fluent-success-bg)]' :
                  testStatus === 'error' ? 'bg-[var(--fluent-error-bg)]' :
                  'bg-[var(--fluent-neutral-4)]'
                }`}>
                  {getStatusIcon()}
                  <span className={`text-sm ${
                    testStatus === 'success' ? 'text-[var(--fluent-success)]' :
                    testStatus === 'error' ? 'text-[var(--fluent-error)]' :
                    'text-[var(--fluent-neutral-60)]'
                  }`}>
                    {getStatusText()}
                  </span>
                </div>
              )}
            </div>
          ) : (
            /* 数据预览 */
            <div className="space-y-4">
              {testData ? (
                <>
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-2">
                      <span className="fluent-badge fluent-badge-info">{testData.length} 条记录</span>
                      <span className="fluent-badge fluent-badge-success">{Object.keys(testData[0] || {}).length} 个字段</span>
                    </div>
                    <button
                      onClick={handleTest}
                      disabled={testStatus === 'testing'}
                      className="fluent-btn fluent-btn-secondary text-xs"
                    >
                      <Loader2 className={`w-3 h-3 mr-1 ${testStatus === 'testing' ? 'fluent-spin' : 'hidden'}`} />
                      刷新数据
                    </button>
                  </div>
                  <div className="border border-[var(--fluent-neutral-12)] rounded-lg overflow-hidden">
                    <div className="overflow-x-auto max-h-[300px]">
                      <table className="fluent-table">
                        <thead className="sticky top-0">
                          <tr>
                            {Object.keys(testData[0] || {}).map((key) => (
                              <th key={key} className="whitespace-nowrap">{key}</th>
                            ))}
                          </tr>
                        </thead>
                        <tbody>
                          {testData.slice(0, 100).map((row, idx) => (
                            <tr key={idx}>
                              {Object.values(row as Record<string, unknown>).map((val, vidx) => (
                                <td key={vidx} className="whitespace-nowrap max-w-[200px] truncate">
                                  {String(val)}
                                </td>
                              ))}
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                    {testData.length > 100 && (
                      <div className="p-2 text-center text-xs text-[var(--fluent-neutral-40)] border-t border-[var(--fluent-neutral-12)]">
                        仅显示前 100 条记录
                      </div>
                    )}
                  </div>
                </>
              ) : (
                <div className="text-center py-12">
                  <AlertCircle className="w-12 h-12 text-[var(--fluent-neutral-30)] mx-auto mb-3" />
                  <p className="text-sm text-[var(--fluent-neutral-60)]">暂无数据</p>
                  <p className="text-xs text-[var(--fluent-neutral-40)] mt-1">请先测试连接以获取数据预览</p>
                </div>
              )}
            </div>
          )}
        </div>
        
        <div className="fluent-modal-footer">
          <button
            type="button"
            onClick={onClose}
            className="fluent-btn fluent-btn-secondary"
          >
            取消
          </button>
          <button
            type="button"
            onClick={handleTest}
            disabled={testStatus === 'testing' || !name.trim()}
            className="fluent-btn fluent-btn-secondary"
          >
            {testStatus === 'testing' ? (
              <>
                <Loader2 className="w-4 h-4 fluent-spin mr-1" />
                测试中...
              </>
            ) : (
              <>
                <TestTube className="w-4 h-4 mr-1" />
                测试连接
              </>
            )}
          </button>
          <button
            type="button"
            onClick={handleSave}
            disabled={!name.trim()}
            className="fluent-btn fluent-btn-primary"
          >
            <Save className="w-4 h-4 mr-1" />
            保存
          </button>
        </div>
      </div>
    </div>
  );
}
