import type { ComponentLibraryItem } from '~/types';

export const componentLibrary: ComponentLibraryItem[] = [
  {
    id: 'table',
    name: '数据表格',
    icon: 'Table2',
    category: 'basic',
    description: '展示结构化数据的表格组件',
    defaultProps: {
      title: '数据表格',
      columns: [],
      data: [],
    },
    defaultStyle: {
      width: 400,
      height: 300,
    },
  },
  {
    id: 'text',
    name: '文本',
    icon: 'Type',
    category: 'basic',
    description: '普通文本内容',
    defaultProps: {
      text: '文本内容',
    },
    defaultStyle: {
      width: 200,
      height: 60,
      fontSize: 14,
    },
  },
  {
    id: 'image',
    name: '图片',
    icon: 'Image',
    category: 'basic',
    description: '展示图片内容',
    defaultProps: {
      src: '',
      alt: '图片',
    },
    defaultStyle: {
      width: 300,
      height: 200,
    },
  },
  {
    id: 'chart-bar',
    name: '柱状图',
    icon: 'BarChart3',
    category: 'chart',
    description: '展示数据对比的柱状图',
    defaultProps: {
      title: '柱状图',
      xAxis: 'category',
      yAxis: 'value',
      data: [],
    },
    defaultStyle: {
      width: 400,
      height: 300,
    },
  },
  {
    id: 'chart-line',
    name: '折线图',
    icon: 'LineChart',
    category: 'chart',
    description: '展示趋势变化的折线图',
    defaultProps: {
      title: '折线图',
      xAxis: 'date',
      yAxis: 'value',
      data: [],
    },
    defaultStyle: {
      width: 400,
      height: 300,
    },
  },
  {
    id: 'chart-pie',
    name: '饼图',
    icon: 'PieChart',
    category: 'chart',
    description: '展示占比分布的饼图',
    defaultProps: {
      title: '饼图',
      data: [],
    },
    defaultStyle: {
      width: 350,
      height: 300,
    },
  },
  {
    id: 'filter',
    name: '筛选器',
    icon: 'Filter',
    category: 'interactive',
    description: '数据筛选输入框',
    defaultProps: {
      placeholder: '请输入筛选条件',
    },
    defaultStyle: {
      width: 200,
      height: 40,
    },
  },
  {
    id: 'date-range',
    name: '日期范围',
    icon: 'CalendarRange',
    category: 'interactive',
    description: '日期范围选择器',
    defaultProps: {
      placeholder: '选择日期范围',
    },
    defaultStyle: {
      width: 240,
      height: 40,
    },
  },
];

export const categories = [
  { id: 'all', name: '全部' },
  { id: 'basic', name: '基础' },
  { id: 'chart', name: '图表' },
  { id: 'interactive', name: '交互' },
] as const;
