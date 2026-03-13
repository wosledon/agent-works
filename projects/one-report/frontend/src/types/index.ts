// 组件类型定义
export interface ReportComponent {
  id: string;
  type: ComponentType;
  name: string;
  props: Record<string, unknown>;
  style: ComponentStyle;
  data?: DataSource;
}

export type ComponentType = 
  | 'table' 
  | 'chart-bar' 
  | 'chart-line' 
  | 'chart-pie' 
  | 'text' 
  | 'image' 
  | 'filter' 
  | 'date-range';

export interface ComponentStyle {
  x: number;
  y: number;
  width: number;
  height: number;
  backgroundColor?: string;
  borderColor?: string;
  borderWidth?: number;
  borderRadius?: number;
}

export interface DataSource {
  type: 'api' | 'static' | 'file';
  config: Record<string, unknown>;
}

export interface ReportConfig {
  id: string;
  name: string;
  description?: string;
  theme: 'light' | 'dark' | 'auto';
  primaryColor: string;
  components: ReportComponent[];
  createdAt: string;
  updatedAt: string;
}