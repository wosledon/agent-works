// 组件类型定义
export type ComponentType = 
  | 'table' 
  | 'chart-bar' 
  | 'chart-line' 
  | 'chart-pie' 
  | 'text' 
  | 'image' 
  | 'filter' 
  | 'date-range';

// 组件样式 - Fluent Design 风格
export interface ComponentStyle {
  x: number;
  y: number;
  width: number;
  height: number;
  backgroundColor?: string;
  borderColor?: string;
  borderWidth?: number;
  borderRadius?: number;
  fontSize?: number;
  color?: string;
  padding?: number;
  shadow?: 'none' | 'small' | 'medium' | 'large';
  opacity?: number;
}

// 数据源配置
export interface DataSource {
  id: string;
  name: string;
  type: 'api' | 'static' | 'file' | 'database';
  config: DataSourceConfig;
  testStatus?: 'untested' | 'testing' | 'success' | 'error';
  testMessage?: string;
  lastTested?: string;
  data?: unknown[];
}

export interface DataSourceConfig {
  url?: string;
  method?: 'GET' | 'POST' | 'PUT' | 'DELETE';
  headers?: Record<string, string>;
  data?: unknown;
  fileName?: string;
  fileType?: string;
  connectionString?: string;
  tableName?: string;
  sql?: string;
  staticData?: string;
}

// 数据字段映射
export interface DataMapping {
  field: string;
  label: string;
  type: 'string' | 'number' | 'date' | 'boolean';
}

// 报表组件
export interface ReportComponent {
  id: string;
  type: ComponentType;
  name: string;
  props: ComponentProps;
  style: ComponentStyle;
  dataSourceId?: string;
  dataMapping?: DataMapping[];
}

// 组件属性
export interface ComponentProps {
  title?: string;
  data?: unknown[];
  columns?: TableColumn[];
  xAxis?: string;
  yAxis?: string;
  categoryField?: string;
  valueField?: string;
  nameField?: string;
  text?: string;
  src?: string;
  placeholder?: string;
  dataSourceId?: string;
  [key: string]: unknown;
}

// 表格列定义
export interface TableColumn {
  key: string;
  title: string;
  dataIndex: string;
  width?: number;
  align?: 'left' | 'center' | 'right';
}

// 主题配置 - Fluent Design
export interface ThemeConfig {
  name: string;
  colors: {
    primary: string;
    secondary: string;
    success: string;
    warning: string;
    error: string;
    info: string;
  };
  borderRadius: {
    small: number;
    medium: number;
    large: number;
  };
  shadows: {
    small: string;
    medium: string;
    large: string;
  };
}

// 报表配置
export interface ReportConfig {
  id: string;
  name: string;
  description?: string;
  theme: 'light' | 'dark' | 'auto';
  primaryColor: string;
  width: number;
  height: number;
  gridSize: number;
  showGrid: boolean;
  components: ReportComponent[];
  dataSources: DataSource[];
  createdAt: string;
  updatedAt: string;
}

// 编辑器状态
export interface EditorState {
  scale: number;
  isPreview: boolean;
  selectedComponentId: string | null;
  snapToGrid: boolean;
}

// 组件库项
export interface ComponentLibraryItem {
  id: ComponentType;
  name: string;
  icon: string;
  category: 'basic' | 'chart' | 'interactive';
  description: string;
  defaultProps: Partial<ComponentProps>;
  defaultStyle: Partial<ComponentStyle>;
}
