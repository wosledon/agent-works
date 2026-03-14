import type { ReportComponent } from '~/types';
import { TableComponent } from './TableComponent';
import { BarChartComponent } from './BarChartComponent';
import { LineChartComponent } from './LineChartComponent';
import { PieChartComponent } from './PieChartComponent';
import { TextComponent } from './TextComponent';
import { ImageComponent } from './ImageComponent';
import { FilterComponent } from './FilterComponent';
import { DateRangeComponent } from './DateRangeComponent';

interface ComponentRendererProps {
  component: ReportComponent;
  isPreview?: boolean;
  onUpdateProps?: (props: Record<string, unknown>) => void;
}

export function ComponentRenderer({ component, isPreview = false, onUpdateProps }: ComponentRendererProps) {
  const { type, props, style, dataSourceId, dataMapping } = component;

  switch (type) {
    case 'table':
      return <TableComponent props={props} style={style} />;
    case 'chart-bar':
      return (
        <BarChartComponent 
          props={props} 
          style={style} 
          dataSourceId={dataSourceId}
          dataMapping={dataMapping}
          onUpdateProps={onUpdateProps}
        />
      );
    case 'chart-line':
      return (
        <LineChartComponent 
          props={props} 
          style={style}
          dataSourceId={dataSourceId}
          dataMapping={dataMapping}
          onUpdateProps={onUpdateProps}
        />
      );
    case 'chart-pie':
      return (
        <PieChartComponent 
          props={props} 
          style={style}
          dataSourceId={dataSourceId}
          dataMapping={dataMapping}
          onUpdateProps={onUpdateProps}
        />
      );
    case 'text':
      return <TextComponent props={props} style={style} isPreview={isPreview} />;
    case 'image':
      return <ImageComponent props={props} style={style} />;
    case 'filter':
      return <FilterComponent props={props} style={style} />;
    case 'date-range':
      return <DateRangeComponent props={props} style={style} />;
    default:
      return (
        <div className="h-full flex items-center justify-center text-gray-400 text-sm">
          未知组件类型: {type}
        </div>
      );
  }
}
