import { useState, useEffect, useMemo } from 'react';
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
  AlertCircle,
  EyeOff,
  Server,
  Key,
  FileJson,
  HardDrive
} from 'lucide-react';
import type { DataSource, DataSourceConfig } from '~/types';
import { DataPreviewTable } from './DataPreviewTable';

interface DataSourceModalProps {
  dataSource: DataSource | null;
  onSave: (ds: Omit<DataSource, 'id'>) => void;
  onClose: () => void;
  onTest: (dataSource: DataSource) => Promise<{ success: boolean; message: string; data?: unknown[] }>;
}

type DataSourceType = 'api' | 'database' | 'file' | 'static';
type DatabaseType = 'mysql' | 'postgresql' | 'mssql' | 'sqlite';

interface FormErrors {
  name?: string;
  url?: string;
  connectionString?: string;
  host?: string;
  port?: string;
  database?: string;
  username?: string;
  sql?: string;
  staticData?: string;
}

const typeOptions: { value: DataSourceType; label: string; icon: React.ComponentType<{ className?: string }>; description: string }[] = [
  { value: 'api', label: 'API 接口', icon: Globe, description: '从 REST API 获取数据' },
  { value: 'database', label: '数据库', icon: Database, description: '连接 MySQL/PostgreSQL 等数据库' },
  { value: 'file', label: '文件', icon: FileText, description: '上传 CSV/JSON/Excel 文件' },
  { value: 'static', label: '静态数据', icon: FileJson, description: '直接输入 JSON 数据' },
];

const dbTypeOptions: { value: DatabaseType; label: string; defaultPort: number }[] = [
  { value: 'mysql', label: 'MySQL', defaultPort: 3306 },
  { value: 'postgresql', label: 'PostgreSQL', defaultPort: 5432 },
  { value: 'mssql', label: 'SQL Server', defaultPort: 1433 },
  { value: 'sqlite', label: 'SQLite', defaultPort: 0 },
];

// 默认端口映射
const defaultPorts: Record<DatabaseType, number> = {
  mysql: 3306,
  postgresql: 5432,
  mssql: 1433,
  sqlite: 0,
};

export function DataSourceModal({ dataSource, onSave, onClose, onTest }: DataSourceModalProps) {
  // 表单状态
  const [name, setName] = useState('');
  const [type, setType] = useState<DataSourceType>('api');
  const [dbType, setDbType] = useState<DatabaseType>('mysql');
  const [config, setConfig] = useState<DataSourceConfig>({});
  
  // 数据库连接字段
  const [host, setHost] = useState('');
  const [port, setPort] = useState('3306');
  const [database, setDatabase] = useState('');
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [sql, setSql] = useState('');
  
  // 测试状态
  const [testStatus, setTestStatus] = useState<'idle' | 'testing' | 'success' | 'error'>('idle');
  const [testMessage, setTestMessage] = useState('');
  const [testData, setTestData] = useState<unknown[] | null>(null);
  
  // UI 状态
  const [activeTab, setActiveTab] = useState<'config' | 'preview'>('config');
  const [errors, setErrors] = useState<FormErrors>({});
  const [touched, setTouched] = useState<Record<string, boolean>>({});

  // 初始化表单
  useEffect(() => {
    if (dataSource) {
      setName(dataSource.name);
      setType(dataSource.type);
      setConfig(dataSource.config);
      setTestStatus(dataSource.testStatus || 'idle');
      setTestMessage(dataSource.testMessage || '');
      
      // 解析数据库连接字符串
      if (dataSource.type === 'database' && dataSource.config.connectionString) {
        parseConnectionString(dataSource.config.connectionString);
      }
      
      if (dataSource.config.sql) {
        setSql(dataSource.config.sql);
      }
    } else {
      resetForm();
    }
  }, [dataSource]);

  const resetForm = () => {
    setName('');
    setType('api');
    setDbType('mysql');
    setConfig({ method: 'GET', headers: {} });
    setHost('');
    setPort('3306');
    setDatabase('');
    setUsername('');
    setPassword('');
    setSql('');
    setTestStatus('idle');
    setTestMessage('');
    setTestData(null);
    setErrors({});
    setTouched({});
  };

  // 解析连接字符串
  const parseConnectionString = (connStr: string) => {
    const pairs = connStr.split(';').filter(Boolean);
    pairs.forEach(pair => {
      const [key, value] = pair.split('=').map(s => s.trim());
      switch (key.toLowerCase()) {
        case 'host':
        case 'server':
          setHost(value);
          break;
        case 'port':
          setPort(value);
          break;
        case 'database':
        case 'dbname':
          setDatabase(value);
          break;
        case 'user':
        case 'username':
        case 'uid':
          setUsername(value);
          break;
        case 'password':
        case 'pwd':
          setPassword(value);
          break;
      }
    });
  };

  // 构建连接字符串
  const buildConnectionString = useMemo(() => {
    if (dbType === 'sqlite') {
      return database;
    }
    const parts = [
      host && `host=${host}`,
      port && `port=${port}`,
      database && `database=${database}`,
      username && `user=${username}`,
      password && `password=${password}`,
    ].filter(Boolean);
    return parts.join(';');
  }, [dbType, host, port, database, username, password]);

  // 更新数据库类型时自动设置默认端口
  const handleDbTypeChange = (newType: DatabaseType) => {
    setDbType(newType);
    setPort(String(defaultPorts[newType]));
  };

  // 表单验证
  const validateForm = (): boolean => {
    const newErrors: FormErrors = {};
    
    if (!name.trim()) {
      newErrors.name = '请输入数据源名称';
    }
    
    if (type === 'api') {
      if (!config.url?.trim()) {
        newErrors.url = '请输入请求 URL';
      } else if (!/^https?:\/\/.+/.test(config.url)) {
        newErrors.url = 'URL 格式不正确';
      }
    }
    
    if (type === 'database') {
      if (dbType !== 'sqlite' && !host.trim()) {
        newErrors.host = '请输入主机地址';
      }
      if (dbType !== 'sqlite') {
        const portNum = parseInt(port);
        if (!port || isNaN(portNum) || portNum < 1 || portNum > 65535) {
          newErrors.port = '端口号必须在 1-65535 之间';
        }
      }
      if (!database.trim()) {
        newErrors.database = '请输入数据库名称';
      }
      if (dbType !== 'sqlite' && !username.trim()) {
        newErrors.username = '请输入用户名';
      }
      if (!sql.trim()) {
        newErrors.sql = '请输入 SQL 查询语句';
      }
    }
    
    if (type === 'static') {
      if (!config.staticData?.trim()) {
        newErrors.staticData = '请输入 JSON 数据';
      } else {
        try {
          const parsed = JSON.parse(config.staticData);
          if (!Array.isArray(parsed)) {
            newErrors.staticData = '数据必须是 JSON 数组格式';
          }
        } catch {
          newErrors.staticData = 'JSON 格式错误';
        }
      }
    }
    
    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  // 测试连接
  const handleTest = async () => {
    if (!validateForm()) return;
    
    setTestStatus('testing');
    setTestMessage('');
    
    const testConfig: DataSourceConfig = { ...config };
    if (type === 'database') {
      testConfig.connectionString = buildConnectionString;
      testConfig.sql = sql;
    }
    
    const testDataSource: DataSource = {
      id: dataSource?.id || 'test',
      name,
      type,
      config: testConfig,
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

  // 保存
  const handleSave = () => {
    if (!validateForm()) {
      // 标记所有字段为已触碰以显示所有错误
      setTouched({ name: true, url: true, host: true, port: true, database: true, username: true, sql: true, staticData: true });
      return;
    }
    
    const finalConfig: DataSourceConfig = { ...config };
    if (type === 'database') {
      finalConfig.connectionString = buildConnectionString;
      finalConfig.sql = sql;
    }
    
    onSave({
      name: name.trim(),
      type,
      config: finalConfig,
      testStatus: testStatus === 'testing' ? 'untested' : testStatus,
      testMessage,
      lastTested: testStatus !== 'idle' ? new Date().toISOString() : undefined,
    });
  };

  // 更新配置
  const updateConfig = (updates: Partial<DataSourceConfig>) => {
    setConfig(prev => ({ ...prev, ...updates }));
    if (testStatus !== 'idle') {
      setTestStatus('idle');
      setTestMessage('');
    }
  };

  // 获取状态显示
  const getStatusDisplay = () => {
    switch (testStatus) {
      case 'testing':
        return {
          icon: <Loader2 className="w-4 h-4 animate-spin" />,
          text: '测试中...',
          className: 'bg-[var(--fluent-neutral-4)] text-[var(--fluent-neutral-60)]'
        };
      case 'success':
        return {
          icon: <CheckCircle className="w-4 h-4" />,
          text: testMessage || '连接成功',
          className: 'bg-[var(--fluent-success-bg)] text-[var(--fluent-success)]'
        };
      case 'error':
        return {
          icon: <XCircle className="w-4 h-4" />,
          text: testMessage || '连接失败',
          className: 'bg-[var(--fluent-error-bg)] text-[var(--fluent-error)]'
        };
      default:
        return null;
    }
  };

  const statusDisplay = getStatusDisplay();

  return (
    <div className="fluent-modal-overlay" onClick={onClose}>
      <div className="fluent-modal w-full max-w-3xl" onClick={e => e.stopPropagation()}>
        <div className="fluent-modal-header flex items-center justify-between">
          <h3 className="text-lg font-semibold text-[var(--fluent-neutral-90)]">
            {dataSource ? '编辑数据源' : '添加数据源'}
          </h3>
          <button 
            onClick={onClose} 
            className="p-1 hover:bg-[var(--fluent-neutral-8)] rounded transition-colors"
          >
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
            onClick={() => setActiveTab('preview')}
            className={`flex items-center gap-2 px-5 py-3 text-sm font-medium transition-colors ${
              activeTab === 'preview'
                ? 'text-[var(--fluent-primary)] border-b-2 border-[var(--fluent-primary)]'
                : 'text-[var(--fluent-neutral-60)] hover:text-[var(--fluent-neutral-90)] hover:bg-[var(--fluent-neutral-4)]'
            }`}
            disabled={!testData}
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
                  onChange={(e) => {
                    setName(e.target.value);
                    setTouched(prev => ({ ...prev, name: true }));
                    if (errors.name) validateForm();
                  }}
                  onBlur={() => {
                    setTouched(prev => ({ ...prev, name: true }));
                    validateForm();
                  }}
                  placeholder="例如：销售数据"
                  className={`fluent-input w-full ${touched.name && errors.name ? 'border-[var(--fluent-error)]' : ''}`}
                  required
                />
                {touched.name && errors.name && (
                  <p className="text-xs text-[var(--fluent-error)] mt-1">{errors.name}</p>
                )}
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
                        onClick={() => {
                          setType(option.value);
                          setTestStatus('idle');
                          setTestData(null);
                        }}
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
                
                {/* API 配置 */}
                {type === 'api' && (
                  <div className="space-y-4">
                    <div>
                      <label className="block text-xs text-[var(--fluent-neutral-50)] mb-1.5">
                        请求 URL <span className="text-[var(--fluent-error)]">*</span>
                      </label>
                      <input
                        type="url"
                        value={config.url || ''}
                        onChange={(e) => {
                          updateConfig({ url: e.target.value });
                          setTouched(prev => ({ ...prev, url: true }));
                        }}
                        onBlur={() => {
                          setTouched(prev => ({ ...prev, url: true }));
                          validateForm();
                        }}
                        placeholder="https://api.example.com/data"
                        className={`fluent-input w-full ${touched.url && errors.url ? 'border-[var(--fluent-error)]' : ''}`}
                      />
                      {touched.url && errors.url && (
                        <p className="text-xs text-[var(--fluent-error)] mt-1">{errors.url}</p>
                      )}
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
                          min={1}
                          max={300}
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
                
                {/* 数据库配置 */}
                {type === 'database' && (
                  <div className="space-y-4">
                    <div>
                      <label className="block text-xs text-[var(--fluent-neutral-50)] mb-1.5">数据库类型</label>
                      <div className="grid grid-cols-4 gap-2">
                        {dbTypeOptions.map((db) => (
                          <button
                            key={db.value}
                            type="button"
                            onClick={() => handleDbTypeChange(db.value)}
                            className={`px-3 py-2 text-sm rounded-lg border transition-all ${
                              dbType === db.value
                                ? 'border-[var(--fluent-primary)] bg-[var(--fluent-primary-bg)] text-[var(--fluent-primary)]'
                                : 'border-[var(--fluent-neutral-12)] hover:border-[var(--fluent-neutral-20)]'
                            }`}
                          >
                            {db.label}
                          </button>
                        ))}
                      </div>
                    </div>
                    
                    {dbType !== 'sqlite' ? (
                      <>
                        <div className="grid grid-cols-2 gap-3">
                          <div>
                            <label className="block text-xs text-[var(--fluent-neutral-50)] mb-1.5">
                              主机 <span className="text-[var(--fluent-error)]">*</span>
                            </label>
                            <div className="relative">
                              <Server className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-[var(--fluent-neutral-40)]" />
                              <input
                                type="text"
                                value={host}
                                onChange={(e) => {
                                  setHost(e.target.value);
                                  setTouched(prev => ({ ...prev, host: true }));
                                }}
                                onBlur={() => {
                                  setTouched(prev => ({ ...prev, host: true }));
                                  validateForm();
                                }}
                                placeholder="localhost"
                                className={`fluent-input w-full pl-10 ${touched.host && errors.host ? 'border-[var(--fluent-error)]' : ''}`}
                              />
                            </div>
                            {touched.host && errors.host && (
                              <p className="text-xs text-[var(--fluent-error)] mt-1">{errors.host}</p>
                            )}
                          </div>
                          <div>
                            <label className="block text-xs text-[var(--fluent-neutral-50)] mb-1.5">
                              端口 <span className="text-[var(--fluent-error)]">*</span>
                            </label>
                            <input
                              type="number"
                              value={port}
                              onChange={(e) => {
                                setPort(e.target.value);
                                setTouched(prev => ({ ...prev, port: true }));
                              }}
                              onBlur={() => {
                                setTouched(prev => ({ ...prev, port: true }));
                                validateForm();
                              }}
                              placeholder={String(defaultPorts[dbType])}
                              className={`fluent-input w-full ${touched.port && errors.port ? 'border-[var(--fluent-error)]' : ''}`}
                            />
                            {touched.port && errors.port && (
                              <p className="text-xs text-[var(--fluent-error)] mt-1">{errors.port}</p>
                            )}
                          </div>
                        </div>
                        
                        <div>
                          <label className="block text-xs text-[var(--fluent-neutral-50)] mb-1.5">
                            数据库名称 <span className="text-[var(--fluent-error)]">*</span>
                          </label>
                          <div className="relative">
                            <Database className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-[var(--fluent-neutral-40)]" />
                            <input
                              type="text"
                              value={database}
                              onChange={(e) => {
                                setDatabase(e.target.value);
                                setTouched(prev => ({ ...prev, database: true }));
                              }}
                              onBlur={() => {
                                setTouched(prev => ({ ...prev, database: true }));
                                validateForm();
                              }}
                              placeholder="mydb"
                              className={`fluent-input w-full pl-10 ${touched.database && errors.database ? 'border-[var(--fluent-error)]' : ''}`}
                            />
                          </div>
                          {touched.database && errors.database && (
                            <p className="text-xs text-[var(--fluent-error)] mt-1">{errors.database}</p>
                          )}
                        </div>
                        
                        <div className="grid grid-cols-2 gap-3">
                          <div>
                            <label className="block text-xs text-[var(--fluent-neutral-50)] mb-1.5">
                              用户名 <span className="text-[var(--fluent-error)]">*</span>
                            </label>
                            <div className="relative">
                              <HardDrive className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-[var(--fluent-neutral-40)]" />
                              <input
                                type="text"
                                value={username}
                                onChange={(e) => {
                                  setUsername(e.target.value);
                                  setTouched(prev => ({ ...prev, username: true }));
                                }}
                                onBlur={() => {
                                  setTouched(prev => ({ ...prev, username: true }));
                                  validateForm();
                                }}
                                placeholder="admin"
                                className={`fluent-input w-full pl-10 ${touched.username && errors.username ? 'border-[var(--fluent-error)]' : ''}`}
                              />
                            </div>
                            {touched.username && errors.username && (
                              <p className="text-xs text-[var(--fluent-error)] mt-1">{errors.username}</p>
                            )}
                          </div>
                          <div>
                            <label className="block text-xs text-[var(--fluent-neutral-50)] mb-1.5">密码</label>
                            <div className="relative">
                              <Key className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-[var(--fluent-neutral-40)]" />
                              <input
                                type={showPassword ? 'text' : 'password'}
                                value={password}
                                onChange={(e) => setPassword(e.target.value)}
                                placeholder={dataSource ? '留空表示不修改' : ''}
                                className="fluent-input w-full pl-10 pr-10"
                              />
                              <button
                                type="button"
                                onClick={() => setShowPassword(!showPassword)}
                                className="absolute right-3 top-1/2 -translate-y-1/2 text-[var(--fluent-neutral-40)] hover:text-[var(--fluent-neutral-60)]"
                              >
                                {showPassword ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
                              </button>
                            </div>
                          </div>
                        </div>
                      </>
                    ) : (
                      <div>
                        <label className="block text-xs text-[var(--fluent-neutral-50)] mb-1.5">
                          数据库文件路径 <span className="text-[var(--fluent-error)]">*</span>
                        </label>
                        <input
                          type="text"
                          value={database}
                          onChange={(e) => {
                            setDatabase(e.target.value);
                            setTouched(prev => ({ ...prev, database: true }));
                          }}
                          onBlur={() => {
                            setTouched(prev => ({ ...prev, database: true }));
                            validateForm();
                          }}
                          placeholder="/path/to/database.sqlite"
                          className={`fluent-input w-full ${touched.database && errors.database ? 'border-[var(--fluent-error)]' : ''}`}
                        />
                        {touched.database && errors.database && (
                          <p className="text-xs text-[var(--fluent-error)] mt-1">{errors.database}</p>
                        )}
                      </div>
                    )}
                    
                    <div>
                      <label className="block text-xs text-[var(--fluent-neutral-50)] mb-1.5">
                        SQL 查询 <span className="text-[var(--fluent-error)]">*</span>
                      </label>
                      <textarea
                        value={sql}
                        onChange={(e) => {
                          setSql(e.target.value);
                          setTouched(prev => ({ ...prev, sql: true }));
                        }}
                        onBlur={() => {
                          setTouched(prev => ({ ...prev, sql: true }));
                          validateForm();
                        }}
                        rows={4}
                        className={`fluent-textarea w-full font-mono text-sm ${touched.sql && errors.sql ? 'border-[var(--fluent-error)]' : ''}`}
                        placeholder="SELECT * FROM table_name WHERE status = 'active'"
                      />
                      {touched.sql && errors.sql && (
                        <p className="text-xs text-[var(--fluent-error)] mt-1">{errors.sql}</p>
                      )}
                    </div>
                  </div>
                )}
                
                {/* 文件配置 */}
                {type === 'file' && (
                  <div className="space-y-4">
                    <div className="border-2 border-dashed border-[var(--fluent-neutral-20)] rounded-lg p-8 text-center hover:border-[var(--fluent-primary)] hover:bg-[var(--fluent-primary-bg)] transition-all cursor-pointer">
                      <input
                        type="file"
                        accept=".csv,.json,.xlsx,.xls"
                        className="hidden"
                        id="file-upload"
                        onChange={(e) => {
                          const file = e.target.files?.[0];
                          if (file) {
                            updateConfig({ fileName: file.name, fileType: file.type });
                          }
                        }}
                      />
                      <label htmlFor="file-upload" className="cursor-pointer">
                        <FileText className="w-10 h-10 text-[var(--fluent-neutral-30)] mx-auto mb-3" />
                        <p className="text-sm text-[var(--fluent-neutral-60)] mb-2">点击或拖拽文件到此处</p>
                        <p className="text-xs text-[var(--fluent-neutral-40)]">支持 CSV, JSON, Excel 格式</p>
                      </label>
                    </div>
                    
                    {config.fileName && (
                      <div className="flex items-center gap-3 p-3 bg-[var(--fluent-primary-bg)] rounded-lg border border-[var(--fluent-primary)]">
                        <FileText className="w-5 h-5 text-[var(--fluent-primary)]" />
                        <div className="flex-1">
                          <p className="text-sm text-[var(--fluent-neutral-90)]">{config.fileName}</p>
                          <p className="text-xs text-[var(--fluent-neutral-50)]">已选择</p>
                        </div>
                        <button
                          onClick={() => updateConfig({ fileName: undefined, fileType: undefined })}
                          className="p-1 hover:bg-[var(--fluent-error-bg)] rounded text-[var(--fluent-error)]"
                        >
                          <X className="w-4 h-4" />
                        </button>
                      </div>
                    )}
                  </div>
                )}
                
                {/* 静态数据配置 */}
                {type === 'static' && (
                  <div>
                    <label className="block text-xs text-[var(--fluent-neutral-50)] mb-1.5">
                      JSON 数据 <span className="text-[var(--fluent-error)]">*</span>
                      <span className="text-[var(--fluent-neutral-40)] ml-1">(数组格式)</span>
                    </label>
                    <textarea
                      value={config.staticData || ''}
                      onChange={(e) => {
                        updateConfig({ staticData: e.target.value });
                        setTouched(prev => ({ ...prev, staticData: true }));
                      }}
                      onBlur={() => {
                        setTouched(prev => ({ ...prev, staticData: true }));
                        validateForm();
                      }}
                      rows={10}
                      className={`fluent-textarea w-full font-mono text-sm ${touched.staticData && errors.staticData ? 'border-[var(--fluent-error)]' : ''}`}
                      placeholder={`[\n  { "name": "产品A", "value": 100 },\n  { "name": "产品B", "value": 200 }\n]`}
                    />
                    {touched.staticData && errors.staticData && (
                      <p className="text-xs text-[var(--fluent-error)] mt-1">{errors.staticData}</p>
                    )}
                  </div>
                )}
              </div>
              
              {/* 测试状态 */}
              {statusDisplay && (
                <div className={`flex items-center gap-2 p-3 rounded-lg ${statusDisplay.className}`}>
                  {statusDisplay.icon}
                  <span className="text-sm">{statusDisplay.text}</span>
                </div>
              )}
            </div>
          ) : (
            /* 数据预览 */
            <div className="space-y-4 h-full">
              {testData ? (
                <DataPreviewTable data={testData} />
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
            disabled={testStatus === 'testing'}
            className="fluent-btn fluent-btn-secondary"
          >
            {testStatus === 'testing' ? (
              <>
                <Loader2 className="w-4 h-4 animate-spin mr-1" />
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
