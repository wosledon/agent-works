import { useState } from 'react';

interface ComponentItem {
  id: string;
  name: string;
  icon: string;
  category: string;
}

const components: ComponentItem[] = [
  { id: 'table', name: '数据表格', icon: '⊞', category: '基础' },
  { id: 'chart-bar', name: '柱状图', icon: '📊', category: '图表' },
  { id: 'chart-line', name: '折线图', icon: '📈', category: '图表' },
  { id: 'chart-pie', name: '饼图', icon: '🥧', category: '图表' },
  { id: 'text', name: '文本', icon: 'T', category: '基础' },
  { id: 'image', name: '图片', icon: '🖼', category: '基础' },
  { id: 'filter', name: '筛选器', icon: '🔍', category: '交互' },
  { id: 'date-range', name: '日期范围', icon: '📅', category: '交互' },
];

const categories = ['全部', '基础', '图表', '交互'];

export function Sidebar() {
  const [activeCategory, setActiveCategory] = useState('全部');

  const filteredComponents = activeCategory === '全部' 
    ? components 
    : components.filter(c => c.category === activeCategory);

  return (
    <aside className="w-64 bg-white dark:bg-gray-800 border-r border-gray-200 dark:border-gray-700 flex flex-col">
      <div className="p-4 border-b border-gray-200 dark:border-gray-700">
        <h2 className="text-sm font-medium text-gray-900 dark:text-white mb-3">组件库</h2>
        <div className="flex flex-wrap gap-1">
          {categories.map(cat => (
            <button
              key={cat}
              onClick={() => setActiveCategory(cat)}
              className={`px-2 py-1 text-xs rounded-full transition-colors ${
                activeCategory === cat
                  ? 'bg-blue-500 text-white'
                  : 'bg-gray-100 dark:bg-gray-700 text-gray-600 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-600'
              }`}
            >
              {cat}
            </button>
          ))}
        </div>
      </div>
      <div className="flex-1 overflow-y-auto p-4">
        <div className="grid grid-cols-2 gap-2">
          {filteredComponents.map(comp => (
            <div
              key={comp.id}
              draggable
              className="p-3 bg-gray-50 dark:bg-gray-700 rounded-lg cursor-move hover:bg-blue-50 dark:hover:bg-blue-900/20 hover:border-blue-200 dark:hover:border-blue-800 border border-transparent transition-all group"
            >
              <div className="text-2xl mb-2 group-hover:scale-110 transition-transform">{comp.icon}</div>
              <div className="text-xs text-gray-600 dark:text-gray-300">{comp.name}</div>
            </div>
          ))}
        </div>
      </div>
    </aside>
  );
}