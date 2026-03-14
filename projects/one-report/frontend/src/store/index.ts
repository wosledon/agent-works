import { create } from 'zustand';
import type { 
  ReportConfig, 
  ReportComponent, 
  DataSource, 
  ComponentType,
  ComponentStyle,
  ComponentProps,
  DataSourceConfig,
} from '../types';

interface ReportStore {
  // 报表配置
  config: ReportConfig;
  // 编辑器状态
  selectedComponentId: string | null;
  isPreview: boolean;
  scale: number;
  snapToGrid: boolean;
  showDataSourceModal: boolean;
  showDataPreviewModal: boolean;
  editingDataSource: DataSource | null;
  previewDataSource: DataSource | null;
  previewData: unknown[] | null;
  isPreviewLoading: boolean;
  previewError: string | null;
  
  // Actions
  setSelectedComponent: (id: string | null) => void;
  addComponent: (type: ComponentType, x: number, y: number) => void;
  updateComponent: (id: string, updates: Partial<ReportComponent>) => void;
  updateComponentStyle: (id: string, style: Partial<ComponentStyle>) => void;
  updateComponentProps: (id: string, props: Partial<ComponentProps>) => void;
  removeComponent: (id: string) => void;
  moveComponent: (id: string, x: number, y: number) => void;
  resizeComponent: (id: string, width: number, height: number) => void;
  
  // 数据源操作
  addDataSource: (dataSource: Omit<DataSource, 'id'>) => void;
  updateDataSource: (id: string, updates: Partial<DataSource>) => void;
  removeDataSource: (id: string) => void;
  testDataSource: (dataSource: DataSource) => Promise<{ success: boolean; message: string; data?: unknown[] }>;
  setShowDataSourceModal: (show: boolean) => void;
  setEditingDataSource: (dataSource: DataSource | null) => void;
  
  // 数据预览操作
  setShowDataPreviewModal: (show: boolean) => void;
  setPreviewDataSource: (dataSource: DataSource | null) => void;
  loadPreviewData: (dataSourceId: string) => Promise<void>;
  refreshComponentData: (componentId: string) => Promise<void>;
  
  // 编辑器操作
  togglePreview: () => void;
  setScale: (scale: number) => void;
  toggleSnapToGrid: () => void;
  updateConfig: (updates: Partial<ReportConfig>) => void;
  
  // 保存/加载
  saveConfig: () => string;
  loadConfig: (json: string) => void;
  exportConfig: () => void;
  importConfig: (file: File) => Promise<void>;
}

const defaultConfig: ReportConfig = {
  id: 'report-1',
  name: '未命名报表',
  description: '',
  theme: 'light',
  primaryColor: '#3b82f6',
  width: 1200,
  height: 800,
  gridSize: 20,
  showGrid: true,
  components: [],
  dataSources: [],
  createdAt: new Date().toISOString(),
  updatedAt: new Date().toISOString(),
};

// 组件默认配置
const componentDefaults: Record<ComponentType, { props: ComponentProps; style: ComponentStyle }> = {
  table: {
    props: {
      title: '数据表格',
      columns: [
        { key: 'col1', title: '列1', dataIndex: 'col1' },
        { key: 'col2', title: '列2', dataIndex: 'col2' },
      ],
      data: [],
    },
    style: { x: 0, y: 0, width: 400, height: 300, shadow: 'small', borderRadius: 8 },
  },
  'chart-bar': {
    props: {
      title: '柱状图',
      xAxis: 'category',
      yAxis: 'value',
      categoryField: 'category',
      valueField: 'value',
      data: [],
    },
    style: { x: 0, y: 0, width: 400, height: 300, shadow: 'small', borderRadius: 8 },
  },
  'chart-line': {
    props: {
      title: '折线图',
      xAxis: 'date',
      yAxis: 'value',
      categoryField: 'date',
      valueField: 'value',
      data: [],
    },
    style: { x: 0, y: 0, width: 400, height: 300, shadow: 'small', borderRadius: 8 },
  },
  'chart-pie': {
    props: {
      title: '饼图',
      nameField: 'name',
      valueField: 'value',
      data: [],
    },
    style: { x: 0, y: 0, width: 350, height: 300, shadow: 'small', borderRadius: 8 },
  },
  text: {
    props: {
      text: '文本内容',
    },
    style: { x: 0, y: 0, width: 200, height: 60, borderRadius: 4 },
  },
  image: {
    props: {
      src: '',
      alt: '图片',
    },
    style: { x: 0, y: 0, width: 300, height: 200, shadow: 'small', borderRadius: 8 },
  },
  filter: {
    props: {
      placeholder: '请输入筛选条件',
    },
    style: { x: 0, y: 0, width: 200, height: 40, borderRadius: 6 },
  },
  'date-range': {
    props: {
      placeholder: '选择日期范围',
    },
    style: { x: 0, y: 0, width: 240, height: 40, borderRadius: 6 },
  },
};

// 模拟 API 调用
const mockFetchData = async (config: DataSourceConfig): Promise<unknown[]> => {
  // 模拟网络延迟
  await new Promise(resolve => setTimeout(resolve, 500));
  
  // 如果是静态数据
  if (config.staticData) {
    try {
      return JSON.parse(config.staticData);
    } catch {
      throw new Error('静态数据格式错误');
    }
  }
  
  // 模拟 API 返回数据
  return [
    { id: 1, name: '产品A', value: 400, category: '类别1', date: '2024-01' },
    { id: 2, name: '产品B', value: 300, category: '类别2', date: '2024-02' },
    { id: 3, name: '产品C', value: 200, category: '类别1', date: '2024-03' },
    { id: 4, name: '产品D', value: 278, category: '类别3', date: '2024-04' },
    { id: 5, name: '产品E', value: 189, category: '类别2', date: '2024-05' },
  ];
};

export const useReportStore = create<ReportStore>((set, get) => ({
  config: defaultConfig,
  selectedComponentId: null,
  isPreview: false,
  scale: 1,
  snapToGrid: true,
  showDataSourceModal: false,
  showDataPreviewModal: false,
  editingDataSource: null,
  previewDataSource: null,
  previewData: null,
  isPreviewLoading: false,
  previewError: null,

  setSelectedComponent: (id) => set({ selectedComponentId: id }),

  addComponent: (type, x, y) => {
    const defaults = componentDefaults[type];
    const newComponent: ReportComponent = {
      id: `comp-${Date.now()}`,
      type,
      name: type,
      props: { ...defaults.props },
      style: {
        ...defaults.style,
        x,
        y,
      },
    };

    set((state) => ({
      config: {
        ...state.config,
        components: [...state.config.components, newComponent],
        updatedAt: new Date().toISOString(),
      },
      selectedComponentId: newComponent.id,
    }));
  },

  updateComponent: (id, updates) => {
    set((state) => ({
      config: {
        ...state.config,
        components: state.config.components.map((c) =>
          c.id === id ? { ...c, ...updates } : c
        ),
        updatedAt: new Date().toISOString(),
      },
    }));
  },

  updateComponentStyle: (id, style) => {
    set((state) => ({
      config: {
        ...state.config,
        components: state.config.components.map((c) =>
          c.id === id ? { ...c, style: { ...c.style, ...style } } : c
        ),
        updatedAt: new Date().toISOString(),
      },
    }));
  },

  updateComponentProps: (id, props) => {
    set((state) => ({
      config: {
        ...state.config,
        components: state.config.components.map((c) =>
          c.id === id ? { ...c, props: { ...c.props, ...props } } : c
        ),
        updatedAt: new Date().toISOString(),
      },
    }));
  },

  removeComponent: (id) => {
    set((state) => ({
      config: {
        ...state.config,
        components: state.config.components.filter((c) => c.id !== id),
        updatedAt: new Date().toISOString(),
      },
      selectedComponentId: null,
    }));
  },

  moveComponent: (id, x, y) => {
    const { snapToGrid, config } = get();
    let finalX = x;
    let finalY = y;

    if (snapToGrid) {
      finalX = Math.round(x / config.gridSize) * config.gridSize;
      finalY = Math.round(y / config.gridSize) * config.gridSize;
    }

    set((state) => ({
      config: {
        ...state.config,
        components: state.config.components.map((c) =>
          c.id === id ? { ...c, style: { ...c.style, x: finalX, y: finalY } } : c
        ),
        updatedAt: new Date().toISOString(),
      },
    }));
  },

  resizeComponent: (id, width, height) => {
    const { snapToGrid, config } = get();
    let finalWidth = width;
    let finalHeight = height;

    if (snapToGrid) {
      finalWidth = Math.round(width / config.gridSize) * config.gridSize;
      finalHeight = Math.round(height / config.gridSize) * config.gridSize;
    }

    set((state) => ({
      config: {
        ...state.config,
        components: state.config.components.map((c) =>
          c.id === id
            ? { ...c, style: { ...c.style, width: finalWidth, height: finalHeight } }
            : c
        ),
        updatedAt: new Date().toISOString(),
      },
    }));
  },

  addDataSource: (dataSource) => {
    const newDataSource: DataSource = {
      ...dataSource,
      id: `ds-${Date.now()}`,
      testStatus: 'untested',
    };
    set((state) => ({
      config: {
        ...state.config,
        dataSources: [...state.config.dataSources, newDataSource],
        updatedAt: new Date().toISOString(),
      },
    }));
  },

  updateDataSource: (id, updates) => {
    set((state) => ({
      config: {
        ...state.config,
        dataSources: state.config.dataSources.map((ds) =>
          ds.id === id ? { ...ds, ...updates } : ds
        ),
        updatedAt: new Date().toISOString(),
      },
    }));
  },

  removeDataSource: (id) => {
    set((state) => ({
      config: {
        ...state.config,
        dataSources: state.config.dataSources.filter((ds) => ds.id !== id),
        updatedAt: new Date().toISOString(),
      },
    }));
  },

  testDataSource: async (dataSource) => {
    try {
      const data = await mockFetchData(dataSource.config);
      return { success: true, message: '连接成功', data };
    } catch (error) {
      return { 
        success: false, 
        message: error instanceof Error ? error.message : '连接失败' 
      };
    }
  },

  setShowDataSourceModal: (show) => set({ showDataSourceModal: show }),
  setEditingDataSource: (dataSource) => set({ editingDataSource: dataSource }),
  
  setShowDataPreviewModal: (show) => set({ showDataPreviewModal: show }),
  setPreviewDataSource: (dataSource) => set({ previewDataSource: dataSource }),
  
  loadPreviewData: async (dataSourceId) => {
    const { config } = get();
    const dataSource = config.dataSources.find(ds => ds.id === dataSourceId);
    
    if (!dataSource) {
      set({ previewError: '数据源不存在' });
      return;
    }
    
    set({ isPreviewLoading: true, previewError: null, previewDataSource: dataSource });
    
    try {
      const result = await mockFetchData(dataSource.config);
      set({ previewData: result, isPreviewLoading: false });
    } catch (error) {
      set({ 
        previewError: error instanceof Error ? error.message : '加载数据失败', 
        isPreviewLoading: false 
      });
    }
  },
  
  refreshComponentData: async (componentId) => {
    const { config, updateComponentProps } = get();
    const component = config.components.find(c => c.id === componentId);
    
    if (!component?.dataSourceId) return;
    
    const dataSource = config.dataSources.find(ds => ds.id === component.dataSourceId);
    if (!dataSource) return;
    
    try {
      const data = await mockFetchData(dataSource.config);
      updateComponentProps(componentId, { data });
    } catch (error) {
      console.error('Failed to refresh component data:', error);
    }
  },

  togglePreview: () => set((state) => ({ isPreview: !state.isPreview })),
  setScale: (scale) => set({ scale }),
  toggleSnapToGrid: () => set((state) => ({ snapToGrid: !state.snapToGrid })),

  updateConfig: (updates) => {
    set((state) => ({
      config: { ...state.config, ...updates, updatedAt: new Date().toISOString() },
    }));
  },
  
  // 保存配置为 JSON 字符串
  saveConfig: () => {
    const { config } = get();
    return JSON.stringify(config, null, 2);
  },
  
  // 从 JSON 字符串加载配置
  loadConfig: (json) => {
    try {
      const parsed = JSON.parse(json) as ReportConfig;
      set({
        config: {
          ...parsed,
          updatedAt: new Date().toISOString(),
        },
        selectedComponentId: null,
      });
    } catch (error) {
      console.error('Failed to load config:', error);
      alert('加载报表配置失败，请检查文件格式');
    }
  },
  
  // 导出配置为文件
  exportConfig: () => {
    const { config } = get();
    const json = JSON.stringify(config, null, 2);
    const blob = new Blob([json], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `${config.name || 'report'}.json`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  },
  
  // 从文件导入配置
  importConfig: async (file) => {
    return new Promise((resolve, reject) => {
      const reader = new FileReader();
      reader.onload = (e) => {
        try {
          const content = e.target?.result as string;
          const parsed = JSON.parse(content) as ReportConfig;
          set({
            config: {
              ...parsed,
              updatedAt: new Date().toISOString(),
            },
            selectedComponentId: null,
          });
          resolve();
        } catch (error) {
          console.error('Failed to import config:', error);
          alert('导入报表配置失败，请检查文件格式');
          reject(error);
        }
      };
      reader.onerror = reject;
      reader.readAsText(file);
    });
  },
}));
