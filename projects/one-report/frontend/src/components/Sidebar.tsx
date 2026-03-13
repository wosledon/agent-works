import { useState } from 'react';
import { 
  Table2, 
  Type, 
  Image, 
  BarChart3, 
  LineChart, 
  PieChart, 
  Filter, 
  CalendarRange 
} from 'lucide-react';
import { useReportStore } from '../../store';
import { componentLibrary, categories } from '../../config/componentLibrary';
import type { ComponentType } from '../../types';

const iconMap: Record<string, React.ComponentType<{ className?: string }>> = {
  Table2,
  Type,
  Image,
  BarChart3,
  LineChart,
  PieChart,
  Filter,
  CalendarRange,
};

export function Sidebar() {
  const [activeCategory, setActiveCategory] = useState('all');
  const addComponent = useReportStore((state) => state.addComponent);

  const filteredComponents = activeCategory === 'all'
    ? componentLibrary
    : componentLibrary.filter(c => c.category === activeCategory);

  const handleDragStart = (e: React.DragEvent, type: ComponentType) => {
    e.dataTransfer.setData('componentType', type);
    e.dataTransfer.effectAllowed = 'copy';
  };

  return (
    <aside className="w-64 bg-white dark:bg-gray-800 border-r border-gray-200 dark:border-gray-700 flex flex-col">
      <div className="p-4 border-b border-gray-200 dark:border-gray-700">
        <h2 className="text-sm font-medium text-gray-900 dark:text-white mb-3">组件库</h2>
        <div className="flex flex-wrap gap-1">
          {categories.map(cat => (
            <button
              key={cat.id}
              onClick={() => setActiveCategory(cat.id)}
              className={`px-2 py-1 text-xs rounded-full transition-colors ${
                activeCategory === cat.id
                  ? 'bg-blue-500 text-white'
                  : 'bg-gray-100 dark:bg-gray-700 text-gray-600 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-600'
              }`}
            >
              {cat.name}
            </button>
          ))}
        </div>
      </div>
      
      <div className="flex-1 overflow-y-auto p-4">
        <div className="grid grid-cols-2 gap-2">
          {filteredComponents.map(comp => {
            const Icon = iconMap[comp.icon];
            return (
              <div
                key={comp.id}
                draggable
                onDragStart={(e) => handleDragStart(e, comp.id)}
                className="p-3 bg-gray-50 dark:bg-gray-700 rounded-lg cursor-move hover:bg-blue-50 dark:hover:bg-blue-900/20 hover:border-blue-200 dark:hover:border-blue-800 border border-transparent transition-all group"
                title={comp.description}
              >
                <div className="mb-2 flex justify-center">
                  {Icon && <Icon className="w-6 h-6 text-gray-600 dark:text-gray-300 group-hover:text-blue-500 transition-colors" />}
                </div>
                <div className="text-xs text-gray-600 dark:text-gray-300 text-center">{comp.name}</div>
              </div>
            );
          })}
        </div>
      </div>
    </aside>
  );
}
