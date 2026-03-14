import { useState } from 'react';
import { useReportStore } from '~/store';
import { 
  Type, 
  Layout, 
  Database,
  Trash2,
  Edit3,
  Plus,
  FileText,
  Globe,
  Database as DatabaseIcon
} from 'lucide-react';
import type { DataSource, ReportComponent } from '~/types';

export function PropertyPanel() {
  const [activeTab, setActiveTab] = useState<'properties' | 'data' | 'canvas'>('properties');
  
  const {
    config,
    selectedComponentId,
    showDataSourceModal,
    editingDataSource,
    updateComponentProps,
    updateComponentStyle,
    removeComponent,
    addDataSource,
    updateDataSource,
    removeDataSource,
    setShowDataSourceModal,
    setEditingDataSource,
    updateConfig,
  } = useReportStore();

  const { components, dataSources } = config;
  const snapToGridValue = useReportStore((state: { snapToGrid: boolean }) => state.snapToGrid);
  // 使用 void 表达式来避免未使用变量的警告
  void snapToGridValue;
  
  const selectedComponent = components.find((c: ReportComponent) => c.id === selectedComponentId);

  const handleAddDataSource = () => {
    setEditingDataSource(null);
    setShowDataSourceModal(true);
  };

  const handleEditDataSource = (ds: DataSource) => {
    setEditingDataSource(ds);
    setShowDataSourceModal(true);
  };

  return (
    <>
      <aside className="w-72 bg-white dark:bg-gray-800 border-l border-gray-200 dark:border-gray-700 flex flex-col">
        <div className="flex border-b border-gray-200 dark:border-gray-700">
          {[
            { id: 'properties', label: '属性', icon: Type },
            { id: 'data', label: '数据', icon: Database },
            { id: 'canvas', label: '画布', icon: Layout },
          ].map(({ id, label, icon: Icon }) => (
            <button
              key={id}
              onClick={() => setActiveTab(id as typeof activeTab)}
              className={`flex-1 px-3 py-3 text-sm font-medium flex items-center justify-center gap-1 transition-colors ${
                activeTab === id
                  ? 'text-blue-500 border-b-2 border-blue-500'
                  : 'text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-white'
              }`}
            >
              <Icon className="w-4 h-4" />
              {label}
            </button>
          ))}
        </div>
        
        <div className="flex-1 overflow-y-auto p-4">
          {activeTab === 'properties' && selectedComponent && (
            <ComponentProperties 
              component={selectedComponent}
              onUpdateProps={updateComponentProps}
              onUpdateStyle={updateComponentStyle}
              onRemove={removeComponent}
            />
          )}
          
          {activeTab === 'properties' && !selectedComponent && (
            <div className="text-center text-gray-500 dark:text-gray-400 py-8">
              <Layout className="w-12 h-12 mx-auto mb-3 opacity-50" />
              <p>选择一个组件以编辑属性</p>
            </div>
          )}
          
          {activeTab === 'data' && (
            <DataSourcePanel 
              dataSources={dataSources}
              onAdd={handleAddDataSource}
              onEdit={handleEditDataSource}
              onRemove={removeDataSource}
            />
          )}
          
          {activeTab === 'canvas' && (
            <CanvasSettings 
              config={config}
              onUpdate={updateConfig}
            />
          )}
        </div>
      </aside>

      {showDataSourceModal && (
        <DataSourceModal
          dataSource={editingDataSource}
          onSave={(ds) => {
            if (editingDataSource) {
              updateDataSource(editingDataSource.id, ds);
            } else {
              addDataSource(ds);
            }
            setShowDataSourceModal(false);
          }}
          onClose={() => setShowDataSourceModal(false)}
        />
      )}
    </>
  );
}

interface ComponentPropertiesProps {
  component: ReportComponent;
  onUpdateProps: (id: string, props: Record<string, unknown>) => void;
  onUpdateStyle: (id: string, style: Record<string, unknown>) => void;
  onRemove: (id: string) => void;
}

function ComponentProperties({ component, onUpdateProps, onUpdateStyle, onRemove }: ComponentPropertiesProps) {
  const { props, style } = component;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h3 className="text-sm font-medium text-gray-900 dark:text-white">{component.name}</h3>
        <button
          onClick={() => onRemove(component.id)}
          className="text-red-500 hover:text-red-600 p-1"
        >
          <Trash2 className="w-4 h-4" />
        </button>
      </div>

      {/* 内容属性 */}
      <div className="space-y-3">
        <h4 className="text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase">内容</h4>
        
        {'title' in props && (
          <div>
            <label className="text-xs text-gray-600 dark:text-gray-300">标题</label>
            <input
              type="text"
              value={(props.title as string) || ''}
              onChange={(e) => onUpdateProps(component.id, { title: e.target.value })}
              className="w-full mt-1 px-2 py-1.5 text-sm border border-gray-300 dark:border-gray-600 rounded bg-white dark:bg-gray-700"
            />
          </div>
        )}
        
        {'text' in props && (
          <div>
            <label className="text-xs text-gray-600 dark:text-gray-300">文本内容</label>
            <textarea
              value={(props.text as string) || ''}
              onChange={(e) => onUpdateProps(component.id, { text: e.target.value })}
              rows={3}
              className="w-full mt-1 px-2 py-1.5 text-sm border border-gray-300 dark:border-gray-600 rounded bg-white dark:bg-gray-700 resize-none"
            />
          </div>
        )}
        
        {'placeholder' in props && (
          <div>
            <label className="text-xs text-gray-600 dark:text-gray-300">占位符</label>
            <input
              type="text"
              value={(props.placeholder as string) || ''}
              onChange={(e) => onUpdateProps(component.id, { placeholder: e.target.value })}
              className="w-full mt-1 px-2 py-1.5 text-sm border border-gray-300 dark:border-gray-600 rounded bg-white dark:bg-gray-700"
            />
          </div>
        )}
      </div>

      {/* 样式属性 */}
      <div className="space-y-3">
        <h4 className="text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase">样式</h4>
        
        <div className="grid grid-cols-2 gap-2">
          <div>
            <label className="text-xs text-gray-600 dark:text-gray-300">X 坐标</label>
            <input
              type="number"
              value={style.x}
              onChange={(e) => onUpdateStyle(component.id, { x: Number(e.target.value) })}
              className="w-full mt-1 px-2 py-1.5 text-sm border border-gray-300 dark:border-gray-600 rounded bg-white dark:bg-gray-700"
            />
          </div>
          <div>
            <label className="text-xs text-gray-600 dark:text-gray-300">Y 坐标</label>
            <input
              type="number"
              value={style.y}
              onChange={(e) => onUpdateStyle(component.id, { y: Number(e.target.value) })}
              className="w-full mt-1 px-2 py-1.5 text-sm border border-gray-300 dark:border-gray-600 rounded bg-white dark:bg-gray-700"
            />
          </div>
          <div>
            <label className="text-xs text-gray-600 dark:text-gray-300">宽度</label>
            <input
              type="number"
              value={style.width}
              onChange={(e) => onUpdateStyle(component.id, { width: Number(e.target.value) })}
              className="w-full mt-1 px-2 py-1.5 text-sm border border-gray-300 dark:border-gray-600 rounded bg-white dark:bg-gray-700"
            />
          </div>
          <div>
            <label className="text-xs text-gray-600 dark:text-gray-300">高度</label>
            <input
              type="number"
              value={style.height}
              onChange={(e) => onUpdateStyle(component.id, { height: Number(e.target.value) })}
              className="w-full mt-1 px-2 py-1.5 text-sm border border-gray-300 dark:border-gray-600 rounded bg-white dark:bg-gray-700"
            />
          </div>
        </div>
        
        <div>
          <label className="text-xs text-gray-600 dark:text-gray-300">背景颜色</label>
          <div className="flex gap-2 mt-1">
            <input
              type="color"
              value={style.backgroundColor || '#ffffff'}
              onChange={(e) => onUpdateStyle(component.id, { backgroundColor: e.target.value })}
              className="w-8 h-8 rounded border border-gray-300 dark:border-gray-600"
            />
            <input
              type="text"
              value={style.backgroundColor || ''}
              onChange={(e) => onUpdateStyle(component.id, { backgroundColor: e.target.value })}
              placeholder="transparent"
              className="flex-1 px-2 py-1.5 text-sm border border-gray-300 dark:border-gray-600 rounded bg-white dark:bg-gray-700"
            />
          </div>
        </div>
        
        <div>
          <label className="text-xs text-gray-600 dark:text-gray-300">文字颜色</label>
          <div className="flex gap-2 mt-1">
            <input
              type="color"
              value={style.color || '#374151'}
              onChange={(e) => onUpdateStyle(component.id, { color: e.target.value })}
              className="w-8 h-8 rounded border border-gray-300 dark:border-gray-600"
            />
            <input
              type="text"
              value={style.color || ''}
              onChange={(e) => onUpdateStyle(component.id, { color: e.target.value })}
              placeholder="inherit"
              className="flex-1 px-2 py-1.5 text-sm border border-gray-300 dark:border-gray-600 rounded bg-white dark:bg-gray-700"
            />
          </div>
        </div>
        
        <div>
          <label className="text-xs text-gray-600 dark:text-gray-300">字体大小 (px)</label>
          <input
            type="number"
            value={style.fontSize || 14}
            onChange={(e) => onUpdateStyle(component.id, { fontSize: Number(e.target.value) })}
            className="w-full mt-1 px-2 py-1.5 text-sm border border-gray-300 dark:border-gray-600 rounded bg-white dark:bg-gray-700"
          />
        </div>
        
        <div>
          <label className="text-xs text-gray-600 dark:text-gray-300">圆角 (px)</label>
          <input
            type="number"
            value={style.borderRadius || 0}
            onChange={(e) => onUpdateStyle(component.id, { borderRadius: Number(e.target.value) })}
            className="w-full mt-1 px-2 py-1.5 text-sm border border-gray-300 dark:border-gray-600 rounded bg-white dark:bg-gray-700"
          />
        </div>
      </div>
    </div>
  );
}

// 数据源面板
interface DataSourcePanelProps {
  dataSources: DataSource[];
  onAdd: () => void;
  onEdit: (ds: DataSource) => void;
  onRemove: (id: string) => void;
}

function DataSourcePanel({ dataSources, onAdd, onEdit, onRemove }: DataSourcePanelProps) {
  const typeIcons: Record<DataSource['type'], React.ComponentType<{ className?: string }>> = {
    api: Globe,
    static: FileText,
    file: FileText,
    database: DatabaseIcon,
  };

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h3 className="text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase">数据源</h3>
        <button
          onClick={onAdd}
          className="flex items-center gap-1 px-2 py-1 text-xs bg-blue-500 text-white rounded hover:bg-blue-600"
        >
          <Plus className="w-3 h-3" />
          添加
        </button>
      </div>
      
      {dataSources.length === 0 ? (
        <div className="text-center py-8 text-gray-500 dark:text-gray-400">
          <Database className="w-12 h-12 mx-auto mb-3 opacity-50" />
          <p className="text-sm">暂无数据源</p>
          <p className="text-xs mt-1">点击上方按钮添加</p>
        </div>
      ) : (
        <div className="space-y-2">
          {dataSources.map((ds) => {
            const Icon = typeIcons[ds.type];
            return (
              <div
                key={ds.id}
                className="p-3 bg-gray-50 dark:bg-gray-700 rounded-lg border border-gray-200 dark:border-gray-600 group"
              >
                <div className="flex items-center gap-2">
                  <Icon className="w-4 h-4 text-gray-500" />
                  <span className="flex-1 text-sm font-medium text-gray-900 dark:text-white truncate">
                    {ds.name}
                  </span>
                  <div className="flex gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
                    <button
                      onClick={() => onEdit(ds)}
                      className="p-1 text-gray-500 hover:text-blue-500"
                    >
                      <Edit3 className="w-3 h-3" />
                    </button>
                    <button
                      onClick={() => onRemove(ds.id)}
                      className="p-1 text-gray-500 hover:text-red-500"
                    >
                      <Trash2 className="w-3 h-3" />
                    </button>
                  </div>
                </div>
                <div className="text-xs text-gray-500 mt-1 capitalize">{ds.type}</div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}

// 画布设置
interface CanvasSettingsProps {
  config: ReturnType<typeof useReportStore.getState>['config'];
  onUpdate: (updates: Partial<ReturnType<typeof useReportStore.getState>['config']>) => void;
}

function CanvasSettings({ config, onUpdate }: CanvasSettingsProps) {
  return (
    <div className="space-y-4">
      <h3 className="text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase">画布设置</h3>
      
      <div>
        <label className="text-xs text-gray-600 dark:text-gray-300">报表名称</label>
        <input
          type="text"
          value={config.name}
          onChange={(e) => onUpdate({ name: e.target.value })}
          className="w-full mt-1 px-2 py-1.5 text-sm border border-gray-300 dark:border-gray-600 rounded bg-white dark:bg-gray-700"
        />
      </div>
      
      <div className="grid grid-cols-2 gap-2">
        <div>
          <label className="text-xs text-gray-600 dark:text-gray-300">宽度 (px)</label>
          <input
            type="number"
            value={config.width}
            onChange={(e) => onUpdate({ width: Number(e.target.value) })}
            className="w-full mt-1 px-2 py-1.5 text-sm border border-gray-300 dark:border-gray-600 rounded bg-white dark:bg-gray-700"
          />
        </div>
        <div>
          <label className="text-xs text-gray-600 dark:text-gray-300">高度 (px)</label>
          <input
            type="number"
            value={config.height}
            onChange={(e) => onUpdate({ height: Number(e.target.value) })}
            className="w-full mt-1 px-2 py-1.5 text-sm border border-gray-300 dark:border-gray-600 rounded bg-white dark:bg-gray-700"
          />
        </div>
      </div>
      
      <div>
        <label className="text-xs text-gray-600 dark:text-gray-300">网格大小 (px)</label>
        <input
          type="number"
          value={config.gridSize}
          onChange={(e) => onUpdate({ gridSize: Number(e.target.value) })}
          className="w-full mt-1 px-2 py-1.5 text-sm border border-gray-300 dark:border-gray-600 rounded bg-white dark:bg-gray-700"
        />
      </div>
      
      <div className="flex items-center gap-2">
        <input
          type="checkbox"
          id="showGrid"
          checked={config.showGrid}
          onChange={(e) => onUpdate({ showGrid: e.target.checked })}
          className="rounded border-gray-300"
        />
        <label htmlFor="showGrid" className="text-sm text-gray-600 dark:text-gray-300">
          显示网格
        </label>
      </div>
      
      <div className="flex items-center gap-2">
        <input
          type="checkbox"
          id="snapToGrid"
          checked={useReportStore.getState().snapToGrid}
          onChange={() => useReportStore.getState().toggleSnapToGrid()}
          className="rounded border-gray-300"
        />
        <label htmlFor="snapToGrid" className="text-sm text-gray-600 dark:text-gray-300">
          吸附网格
        </label>
      </div>
    </div>
  );
}

// 数据源模态框
interface DataSourceModalProps {
  dataSource: DataSource | null;
  onSave: (ds: Omit<DataSource, 'id'>) => void;
  onClose: () => void;
}

function DataSourceModal({ dataSource, onSave, onClose }: DataSourceModalProps) {
  const [formData, setFormData] = useState<Omit<DataSource, 'id'>>(
    dataSource || {
      name: '',
      type: 'api',
      config: {},
    }
  );

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (formData.name) {
      onSave(formData);
    }
  };

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow-xl w-full max-w-md mx-4">
        <div className="p-4 border-b border-gray-200 dark:border-gray-700">
          <h3 className="text-lg font-medium text-gray-900 dark:text-white">
            {dataSource ? '编辑数据源' : '添加数据源'}
          </h3>
        </div>
        
        <form onSubmit={handleSubmit} className="p-4 space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
              名称
            </label>
            <input
              type="text"
              value={formData.name}
              onChange={(e) => setFormData({ ...formData, name: e.target.value })}
              className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700"
              required
            />
          </div>
          
          <div>
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
              类型
            </label>
            <select
              value={formData.type}
              onChange={(e) => setFormData({ ...formData, type: e.target.value as DataSource['type'] })}
              className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700"
            >
              <option value="api">API 接口</option>
              <option value="database">数据库</option>
              <option value="file">文件</option>
              <option value="static">静态数据</option>
            </select>
          </div>
          
          {formData.type === 'api' && (
            <>
              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                  URL
                </label>
                <input
                  type="url"
                  value={formData.config.url || ''}
                  onChange={(e) => setFormData({ 
                    ...formData, 
                    config: { ...formData.config, url: e.target.value }
                  })}
                  className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700"
                  placeholder="https://api.example.com/data"
                />
              </div>
              
              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                  请求方法
                </label>
                <select
                  value={formData.config.method || 'GET'}
                  onChange={(e) => setFormData({ 
                    ...formData, 
                    config: { ...formData.config, method: e.target.value as 'GET' | 'POST' | 'PUT' | 'DELETE' }
                  })}
                  className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700"
                >
                  <option value="GET">GET</option>
                  <option value="POST">POST</option>
                  <option value="PUT">PUT</option>
                  <option value="DELETE">DELETE</option>
                </select>
              </div>
            </>
          )}
          
          {formData.type === 'database' && (
            <>
              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                  连接字符串
                </label>
                <input
                  type="text"
                  value={formData.config.connectionString || ''}
                  onChange={(e) => setFormData({ 
                    ...formData, 
                    config: { ...formData.config, connectionString: e.target.value }
                  })}
                  className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700"
                  placeholder="host=localhost;port=5432;database=mydb"
                />
              </div>
              
              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                  SQL 查询
                </label>
                <textarea
                  value={formData.config.sql || ''}
                  onChange={(e) => setFormData({ 
                    ...formData, 
                    config: { ...formData.config, sql: e.target.value }
                  })}
                  rows={3}
                  className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 resize-none font-mono text-sm"
                  placeholder="SELECT * FROM table_name"
                />
              </div>
            </>
          )}
          
          <div className="flex justify-end gap-2 pt-4">
            <button
              type="button"
              onClick={onClose}
              className="px-4 py-2 text-sm text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg"
            >
              取消
            </button>
            <button
              type="submit"
              className="px-4 py-2 text-sm bg-blue-500 text-white rounded-lg hover:bg-blue-600"
            >
              保存
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
