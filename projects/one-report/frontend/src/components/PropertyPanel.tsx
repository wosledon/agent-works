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
  Database as DatabaseIcon,
  CheckCircle,
  XCircle,
  HelpCircle,
  Eye,
  RefreshCw,
  Link2,
  X
} from 'lucide-react';
import { DataSourceModal } from './DataSourceModal';
import { ChartBindingPanel } from './ChartBindingPanel';
import type { DataSource, ReportComponent } from '~/types';

export function PropertyPanel() {
  const [activeTab, setActiveTab] = useState<'properties' | 'data' | 'canvas'>('properties');
  const [showBindingPanel, setShowBindingPanel] = useState(false);
  
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
    testDataSource,
    updateConfig,
    refreshComponentData,
    updateComponent,
  } = useReportStore();

  const { components, dataSources } = config;
  
  const selectedComponent = components.find((c: ReportComponent) => c.id === selectedComponentId);

  const handleAddDataSource = () => {
    setEditingDataSource(null);
    setShowDataSourceModal(true);
  };

  const handleEditDataSource = (ds: DataSource) => {
    setEditingDataSource(ds);
    setShowDataSourceModal(true);
  };

  // 处理数据源绑定
  const handleUpdateDataSource = (componentId: string, dataSourceId: string) => {
    updateComponentProps(componentId, { dataSourceId: dataSourceId || undefined });
    if (dataSourceId) {
      refreshComponentData(componentId);
    }
  };

  // 处理数据映射更新
  const handleUpdateDataMapping = (componentId: string, mapping: Array<{ field: string; label: string; type: string }>) => {
    updateComponent(componentId, { dataMapping: mapping });
  };

  return (
    <>
      <aside className="w-72 bg-[var(--fluent-neutral-0)] border-l border-[var(--fluent-neutral-12)] flex flex-col">
        <div className="flex border-b border-[var(--fluent-neutral-12)]">
          {[
            { id: 'properties', label: '属性', icon: Type },
            { id: 'data', label: '数据', icon: Database },
            { id: 'canvas', label: '画布', icon: Layout },
          ].map(({ id, label, icon: Icon }) => (
            <button
              key={id}
              onClick={() => setActiveTab(id as typeof activeTab)}
              className={`flex-1 px-3 py-3 text-sm font-medium flex items-center justify-center gap-1.5 transition-colors ${
                activeTab === id
                  ? 'text-[var(--fluent-primary)] border-b-2 border-[var(--fluent-primary)]'
                  : 'text-[var(--fluent-neutral-50)] hover:text-[var(--fluent-neutral-90)] hover:bg-[var(--fluent-neutral-4)]'
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
              dataSources={dataSources}
              onUpdateProps={updateComponentProps}
              onUpdateStyle={updateComponentStyle}
              onRemove={removeComponent}
              onRefreshData={refreshComponentData}
              onOpenBindingPanel={() => setShowBindingPanel(true)}
            />
          )}
          
          {activeTab === 'properties' && !selectedComponent && (
            <div className="text-center text-[var(--fluent-neutral-40)] py-12">
              <Layout className="w-12 h-12 mx-auto mb-3 opacity-50" />
              <p className="text-sm">选择一个组件以编辑属性</p>
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

      {/* 数据源模态框 */}
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
          onTest={testDataSource}
        />
      )}

      {/* 数据绑定面板 */}
      {showBindingPanel && selectedComponent && (
        <div className="fixed inset-0 z-50 flex justify-end">
          <div 
            className="flex-1 bg-black/20" 
            onClick={() => setShowBindingPanel(false)}
          />
          <ChartBindingPanel
            component={selectedComponent}
            dataSources={dataSources}
            onUpdateDataSource={(dsId) => handleUpdateDataSource(selectedComponent.id, dsId)}
            onUpdateDataMapping={(mapping) => handleUpdateDataMapping(selectedComponent.id, mapping)}
            onUpdateProps={(props) => updateComponentProps(selectedComponent.id, props)}
            onTestDataSource={testDataSource}
            onClose={() => setShowBindingPanel(false)}
          />
        </div>
      )}
    </>
  );
}

interface ComponentPropertiesProps {
  component: ReportComponent;
  dataSources: DataSource[];
  onUpdateProps: (id: string, props: Record<string, unknown>) => void;
  onUpdateStyle: (id: string, style: Record<string, unknown>) => void;
  onRemove: (id: string) => void;
  onRefreshData: (id: string) => Promise<void>;
  onOpenBindingPanel: () => void;
}

function ComponentProperties({ 
  component, 
  dataSources, 
  onUpdateProps, 
  onUpdateStyle, 
  onRemove, 
  onRefreshData,
  onOpenBindingPanel 
}: ComponentPropertiesProps) {
  const { props, style, type } = component;
  const [isRefreshing, setIsRefreshing] = useState(false);

  const handleRefreshData = async () => {
    if (!component.dataSourceId) return;
    setIsRefreshing(true);
    await onRefreshData(component.id);
    setIsRefreshing(false);
  };

  // 判断是否支持数据绑定
  const supportsDataBinding = ['table', 'chart-bar', 'chart-line', 'chart-pie'].includes(type);

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h3 className="text-sm font-medium text-[var(--fluent-neutral-90)]">{component.name}</h3>
        <button
          onClick={() => onRemove(component.id)}
          className="p-1.5 text-[var(--fluent-error)] hover:bg-[var(--fluent-error-bg)] rounded transition-colors"
          title="删除组件"
        >
          <Trash2 className="w-4 h-4" />
        </button>
      </div>

      {/* 数据源绑定区域 */}
      {supportsDataBinding && (
        <div className="space-y-3 p-3 bg-[var(--fluent-neutral-4)] rounded-lg">
          <h4 className="text-xs font-semibold text-[var(--fluent-neutral-50)] uppercase flex items-center gap-1">
            <Database className="w-3 h-3" />
            数据源绑定
          </h4>
          
          <select
            value={component.dataSourceId || ''}
            onChange={(e) => onUpdateProps(component.id, { dataSourceId: e.target.value || undefined })}
            className="fluent-select w-full"
          >
            <option value="">-- 选择数据源 --</option>
            {dataSources.map((ds) => (
              <option key={ds.id} value={ds.id}>
                {ds.name}
              </option>
            ))}
          </select>
          
          {component.dataSourceId && (
            <>
              <div className="flex gap-2">
                <button
                  onClick={handleRefreshData}
                  disabled={isRefreshing}
                  className="flex-1 fluent-btn fluent-btn-secondary text-xs"
                >
                  <RefreshCw className={`w-3 h-3 mr-1 ${isRefreshing ? 'animate-spin' : ''}`} />
                  刷新数据
                </button>
                <button
                  onClick={onOpenBindingPanel}
                  className="flex-1 fluent-btn fluent-btn-primary text-xs"
                >
                  <Link2 className="w-3 h-3 mr-1" />
                  详细配置
                </button>
              </div>
              
              {/* 快速字段映射 */}
              {(type === 'chart-bar' || type === 'chart-line') && (
                <div className="space-y-2 pt-2 border-t border-[var(--fluent-neutral-12)]">
                  <label className="text-xs text-[var(--fluent-neutral-60)]">快速字段映射</label>
                  <div className="grid grid-cols-2 gap-2">
                    <div>
                      <input
                        type="text"
                        value={(props.categoryField as string) || 'category'}
                        onChange={(e) => onUpdateProps(component.id, { 
                          categoryField: e.target.value,
                          xAxis: e.target.value 
                        })}
                        placeholder="X轴字段"
                        className="fluent-input w-full text-xs"
                      />
                    </div>
                    <div>
                      <input
                        type="text"
                        value={(props.valueField as string) || 'value'}
                        onChange={(e) => onUpdateProps(component.id, { 
                          valueField: e.target.value,
                          yAxis: e.target.value 
                        })}
                        placeholder="Y轴字段"
                        className="fluent-input w-full text-xs"
                      />
                    </div>
                  </div>
                </div>
              )}
            </>
          )}
        </div>
      )}

      {/* 内容属性 */}
      <div className="space-y-3">
        <h4 className="text-xs font-semibold text-[var(--fluent-neutral-50)] uppercase">内容</h4>
        
        {'title' in props && (
          <div>
            <label className="text-xs text-[var(--fluent-neutral-60)]">标题</label>
            <input
              type="text"
              value={(props.title as string) || ''}
              onChange={(e) => onUpdateProps(component.id, { title: e.target.value })}
              className="fluent-input w-full mt-1"
            />
          </div>
        )}
        
        {'text' in props && (
          <div>
            <label className="text-xs text-[var(--fluent-neutral-60)]">文本内容</label>
            <textarea
              value={(props.text as string) || ''}
              onChange={(e) => onUpdateProps(component.id, { text: e.target.value })}
              rows={3}
              className="fluent-textarea w-full mt-1"
            />
          </div>
        )}
        
        {'placeholder' in props && (
          <div>
            <label className="text-xs text-[var(--fluent-neutral-60)]">占位符</label>
            <input
              type="text"
              value={(props.placeholder as string) || ''}
              onChange={(e) => onUpdateProps(component.id, { placeholder: e.target.value })}
              className="fluent-input w-full mt-1"
            />
          </div>
        )}

        {/* 图表数据字段映射（当没有数据源时显示） */}
        {!component.dataSourceId && (type === 'chart-bar' || type === 'chart-line') && (
          <>
            <div>
              <label className="text-xs text-[var(--fluent-neutral-60)]">分类字段 (X轴)</label>
              <input
                type="text"
                value={(props.categoryField as string) || 'category'}
                onChange={(e) => onUpdateProps(component.id, { categoryField: e.target.value, xAxis: e.target.value })}
                placeholder="例如：name, category"
                className="fluent-input w-full mt-1"
              />
            </div>
            <div>
              <label className="text-xs text-[var(--fluent-neutral-60)]">数值字段 (Y轴)</label>
              <input
                type="text"
                value={(props.valueField as string) || 'value'}
                onChange={(e) => onUpdateProps(component.id, { valueField: e.target.value, yAxis: e.target.value })}
                placeholder="例如：value, count"
                className="fluent-input w-full mt-1"
              />
            </div>
          </>
        )}

        {type === 'chart-pie' && !component.dataSourceId && (
          <>
            <div>
              <label className="text-xs text-[var(--fluent-neutral-60)]">名称字段</label>
              <input
                type="text"
                value={(props.nameField as string) || 'name'}
                onChange={(e) => onUpdateProps(component.id, { nameField: e.target.value })}
                placeholder="例如：name"
                className="fluent-input w-full mt-1"
              />
            </div>
            <div>
              <label className="text-xs text-[var(--fluent-neutral-60)]">数值字段</label>
              <input
                type="text"
                value={(props.valueField as string) || 'value'}
                onChange={(e) => onUpdateProps(component.id, { valueField: e.target.value })}
                placeholder="例如：value"
                className="fluent-input w-full mt-1"
              />
            </div>
          </>
        )}
      </div>

      {/* 样式属性 */}
      <div className="space-y-3">
        <h4 className="text-xs font-semibold text-[var(--fluent-neutral-50)] uppercase">样式</h4>
        
        <div className="grid grid-cols-2 gap-2">
          <div>
            <label className="text-xs text-[var(--fluent-neutral-60)]">X 坐标</label>
            <input
              type="number"
              value={style.x}
              onChange={(e) => onUpdateStyle(component.id, { x: Number(e.target.value) })}
              className="fluent-input w-full mt-1"
            />
          </div>
          <div>
            <label className="text-xs text-[var(--fluent-neutral-60)]">Y 坐标</label>
            <input
              type="number"
              value={style.y}
              onChange={(e) => onUpdateStyle(component.id, { y: Number(e.target.value) })}
              className="fluent-input w-full mt-1"
            />
          </div>
          <div>
            <label className="text-xs text-[var(--fluent-neutral-60)]">宽度</label>
            <input
              type="number"
              value={style.width}
              onChange={(e) => onUpdateStyle(component.id, { width: Number(e.target.value) })}
              className="fluent-input w-full mt-1"
            />
          </div>
          <div>
            <label className="text-xs text-[var(--fluent-neutral-60)]">高度</label>
            <input
              type="number"
              value={style.height}
              onChange={(e) => onUpdateStyle(component.id, { height: Number(e.target.value) })}
              className="fluent-input w-full mt-1"
            />
          </div>
        </div>
        
        <div>
          <label className="text-xs text-[var(--fluent-neutral-60)]">背景颜色</label>
          <div className="flex gap-2 mt-1">
            <input
              type="color"
              value={style.backgroundColor || '#ffffff'}
              onChange={(e) => onUpdateStyle(component.id, { backgroundColor: e.target.value })}
              className="w-8 h-8 rounded border border-[var(--fluent-neutral-20)] cursor-pointer"
            />
            <input
              type="text"
              value={style.backgroundColor || ''}
              onChange={(e) => onUpdateStyle(component.id, { backgroundColor: e.target.value })}
              placeholder="transparent"
              className="fluent-input flex-1"
            />
          </div>
        </div>
        
        <div>
          <label className="text-xs text-[var(--fluent-neutral-60)]">文字颜色</label>
          <div className="flex gap-2 mt-1">
            <input
              type="color"
              value={style.color || '#323130'}
              onChange={(e) => onUpdateStyle(component.id, { color: e.target.value })}
              className="w-8 h-8 rounded border border-[var(--fluent-neutral-20)] cursor-pointer"
            />
            <input
              type="text"
              value={style.color || ''}
              onChange={(e) => onUpdateStyle(component.id, { color: e.target.value })}
              placeholder="inherit"
              className="fluent-input flex-1"
            />
          </div>
        </div>
        
        <div>
          <label className="text-xs text-[var(--fluent-neutral-60)]">字体大小 (px)</label>
          <input
            type="range"
            min={10}
            max={32}
            value={style.fontSize || 14}
            onChange={(e) => onUpdateStyle(component.id, { fontSize: Number(e.target.value) })}
            className="w-full mt-1"
          />
          <div className="text-xs text-[var(--fluent-neutral-40)] text-center mt-1">
            {style.fontSize || 14}px
          </div>
        </div>
        
        <div>
          <label className="text-xs text-[var(--fluent-neutral-60)]">圆角 (px)</label>
          <input
            type="range"
            min={0}
            max={24}
            value={style.borderRadius || 0}
            onChange={(e) => onUpdateStyle(component.id, { borderRadius: Number(e.target.value) })}
            className="w-full mt-1"
          />
          <div className="text-xs text-[var(--fluent-neutral-40)] text-center mt-1">
            {style.borderRadius || 0}px
          </div>
        </div>

        <div>
          <label className="text-xs text-[var(--fluent-neutral-60)]">阴影</label>
          <select
            value={style.shadow || 'none'}
            onChange={(e) => onUpdateStyle(component.id, { shadow: e.target.value as typeof style.shadow })}
            className="fluent-select w-full mt-1"
          >
            <option value="none">无</option>
            <option value="small">小</option>
            <option value="medium">中</option>
            <option value="large">大</option>
          </select>
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

  const getStatusIcon = (status?: DataSource['testStatus']) => {
    switch (status) {
      case 'success':
        return <CheckCircle className="w-3.5 h-3.5 text-[var(--fluent-success)]" />;
      case 'error':
        return <XCircle className="w-3.5 h-3.5 text-[var(--fluent-error)]" />;
      default:
        return <HelpCircle className="w-3.5 h-3.5 text-[var(--fluent-neutral-40)]" />;
    }
  };

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h3 className="text-xs font-semibold text-[var(--fluent-neutral-50)] uppercase">数据源管理</h3>
        <button
          onClick={onAdd}
          className="fluent-btn fluent-btn-primary text-xs"
        >
          <Plus className="w-3.5 h-3.5" />
          添加
        </button>
      </div>
      
      {dataSources.length === 0 ? (
        <div className="text-center py-10 text-[var(--fluent-neutral-40)]">
          <Database className="w-12 h-12 mx-auto mb-3 opacity-30" />
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
                className="group p-3 bg-[var(--fluent-neutral-4)] hover:bg-[var(--fluent-neutral-8)] rounded-lg border border-transparent hover:border-[var(--fluent-neutral-12)] transition-all"
              >
                <div className="flex items-center gap-2">
                  <div className="p-1.5 bg-[var(--fluent-neutral-0)] rounded">
                    <Icon className="w-4 h-4 text-[var(--fluent-primary)]" />
                  </div>
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-1.5">
                      <span className="text-sm font-medium text-[var(--fluent-neutral-90)] truncate">
                        {ds.name}
                      </span>
                      {getStatusIcon(ds.testStatus)}
                    </div>
                    <div className="text-xs text-[var(--fluent-neutral-40)] capitalize">
                      {ds.type} {ds.lastTested && `· ${new Date(ds.lastTested).toLocaleDateString()}`}
                    </div>
                  </div>
                  <div className="flex gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
                    <button
                      onClick={() => onEdit(ds)}
                      className="p-1.5 text-[var(--fluent-neutral-50)] hover:text-[var(--fluent-primary)] hover:bg-[var(--fluent-primary-bg)] rounded transition-colors"
                      title="编辑"
                    >
                      <Edit3 className="w-3.5 h-3.5" />
                    </button>
                    <button
                      onClick={() => onRemove(ds.id)}
                      className="p-1.5 text-[var(--fluent-neutral-50)] hover:text-[var(--fluent-error)] hover:bg-[var(--fluent-error-bg)] rounded transition-colors"
                      title="删除"
                    >
                      <Trash2 className="w-3.5 h-3.5" />
                    </button>
                  </div>
                </div>
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
  const snapToGrid = useReportStore((state) => state.snapToGrid);
  const toggleSnapToGrid = useReportStore((state) => state.toggleSnapToGrid);

  return (
    <div className="space-y-5">
      <h3 className="text-xs font-semibold text-[var(--fluent-neutral-50)] uppercase">画布设置</h3>
      
      <div>
        <label className="text-xs text-[var(--fluent-neutral-60)]">报表名称</label>
        <input
          type="text"
          value={config.name}
          onChange={(e) => onUpdate({ name: e.target.value })}
          className="fluent-input w-full mt-1"
        />
      </div>

      <div>
        <label className="text-xs text-[var(--fluent-neutral-60)]">描述</label>
        <textarea
          value={config.description || ''}
          onChange={(e) => onUpdate({ description: e.target.value })}
          rows={3}
          className="fluent-textarea w-full mt-1"
          placeholder="报表描述信息..."
        />
      </div>
      
      <div className="grid grid-cols-2 gap-3">
        <div>
          <label className="text-xs text-[var(--fluent-neutral-60)]">宽度 (px)</label>
          <input
            type="number"
            value={config.width}
            onChange={(e) => onUpdate({ width: Number(e.target.value) })}
            className="fluent-input w-full mt-1"
          />
        </div>
        <div>
          <label className="text-xs text-[var(--fluent-neutral-60)]">高度 (px)</label>
          <input
            type="number"
            value={config.height}
            onChange={(e) => onUpdate({ height: Number(e.target.value) })}
            className="fluent-input w-full mt-1"
          />
        </div>
      </div>
      
      <div>
        <label className="text-xs text-[var(--fluent-neutral-60)]">网格大小 (px)</label>
        <input
          type="number"
          value={config.gridSize}
          onChange={(e) => onUpdate({ gridSize: Number(e.target.value) })}
          className="fluent-input w-full mt-1"
        />
      </div>
      
      <div className="space-y-2 pt-2">
        <label className="flex items-center gap-2 cursor-pointer">
          <input
            type="checkbox"
            checked={config.showGrid}
            onChange={(e) => onUpdate({ showGrid: e.target.checked })}
            className="fluent-checkbox"
          />
          <span className="text-sm text-[var(--fluent-neutral-60)]">显示网格</span>
        </label>
        
        <label className="flex items-center gap-2 cursor-pointer">
          <input
            type="checkbox"
            checked={snapToGrid}
            onChange={toggleSnapToGrid}
            className="fluent-checkbox"
          />
          <span className="text-sm text-[var(--fluent-neutral-60)]">吸附网格</span>
        </label>
      </div>

      <div className="pt-4 border-t border-[var(--fluent-neutral-12)]">
        <h4 className="text-xs font-semibold text-[var(--fluent-neutral-50)] uppercase mb-3">主题</h4>
        <select
          value={config.theme}
          onChange={(e) => onUpdate({ theme: e.target.value as typeof config.theme })}
          className="fluent-select w-full"
        >
          <option value="light">浅色</option>
          <option value="dark">深色</option>
          <option value="auto">跟随系统</option>
        </select>
      </div>
    </div>
  );
}
