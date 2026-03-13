import { useState } from 'react';

interface Property {
  name: string;
  type: 'text' | 'number' | 'select' | 'color' | 'boolean';
  value: unknown;
  options?: string[];
}

export function PropertyPanel() {
  const [activeTab, setActiveTab] = useState<'properties' | 'data'>('properties');

  const properties: Property[] = [
    { name: '标题', type: 'text', value: '未命名报表' },
    { name: '宽度', type: 'number', value: 1200 },
    { name: '主题', type: 'select', value: 'light', options: ['light', 'dark', 'auto'] },
    { name: '主色调', type: 'color', value: '#3b82f6' },
    { name: '显示边框', type: 'boolean', value: true },
  ];

  const renderInput = (prop: Property) => {
    switch (prop.type) {
      case 'text':
        return (
          <input
            type="text"
            defaultValue={prop.value as string}
            className="w-full px-2 py-1 text-sm border border-gray-300 dark:border-gray-600 rounded bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500 focus:border-transparent"
          />
        );
      case 'number':
        return (
          <input
            type="number"
            defaultValue={prop.value as number}
            className="w-full px-2 py-1 text-sm border border-gray-300 dark:border-gray-600 rounded bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500 focus:border-transparent"
          />
        );
      case 'select':
        return (
          <select
            defaultValue={prop.value as string}
            className="w-full px-2 py-1 text-sm border border-gray-300 dark:border-gray-600 rounded bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500 focus:border-transparent"
          >
            {prop.options?.map(opt => (
              <option key={opt} value={opt}>{opt}</option>
            ))}
          </select>
        );
      case 'color':
        return (
          <div className="flex gap-2">
            <input
              type="color"
              defaultValue={prop.value as string}
              className="w-8 h-8 rounded border border-gray-300 dark:border-gray-600 cursor-pointer"
            />
            <input
              type="text"
              defaultValue={prop.value as string}
              className="flex-1 px-2 py-1 text-sm border border-gray-300 dark:border-gray-600 rounded bg-white dark:bg-gray-700 text-gray-900 dark:text-white"
            />
          </div>
        );
      case 'boolean':
        return (
          <input
            type="checkbox"
            defaultChecked={prop.value as boolean}
            className="w-4 h-4 text-blue-500 rounded border-gray-300 focus:ring-blue-500"
          />
        );
      default:
        return null;
    }
  };

  return (
    <aside className="w-72 bg-white dark:bg-gray-800 border-l border-gray-200 dark:border-gray-700 flex flex-col">
      <div className="flex border-b border-gray-200 dark:border-gray-700">
        <button
          onClick={() => setActiveTab('properties')}
          className={`flex-1 px-4 py-3 text-sm font-medium transition-colors ${
            activeTab === 'properties'
              ? 'text-blue-500 border-b-2 border-blue-500'
              : 'text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-white'
          }`}
        >
          属性
        </button>
        <button
          onClick={() => setActiveTab('data')}
          className={`flex-1 px-4 py-3 text-sm font-medium transition-colors ${
            activeTab === 'data'
              ? 'text-blue-500 border-b-2 border-blue-500'
              : 'text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-white'
          }`}
        >
          数据
        </button>
      </div>
      
      <div className="flex-1 overflow-y-auto p-4">
        {activeTab === 'properties' ? (
          <div className="space-y-4">
            <h3 className="text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider">
              通用属性
            </h3>
            {properties.map(prop => (
              <div key={prop.name} className="space-y-1">
                <label className="text-sm text-gray-700 dark:text-gray-300">{prop.name}</label>
                {renderInput(prop)}
              </div>
            ))}
          </div>
        ) : (
          <div className="space-y-4">
            <h3 className="text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider">
              数据源
            </h3>
            <div className="p-4 bg-gray-50 dark:bg-gray-700 rounded-lg border-2 border-dashed border-gray-300 dark:border-gray-600">
              <p className="text-sm text-gray-600 dark:text-gray-300 text-center">
                拖拽数据文件或
                <button className="text-blue-500 hover:text-blue-600 ml-1">选择文件</button>
              </p>
              <p className="text-xs text-gray-400 dark:text-gray-500 text-center mt-2">
                支持 CSV, Excel, JSON
              </p>
            </div>
            <div className="space-y-2">
              <h4 className="text-sm font-medium text-gray-700 dark:text-gray-300">API 数据源</h4>
              <input
                type="text"
                placeholder="输入 API URL"
                className="w-full px-3 py-2 text-sm border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white placeholder-gray-400 focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              />
              <button className="w-full px-3 py-2 text-sm bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-300 rounded-lg hover:bg-gray-200 dark:hover:bg-gray-600 transition-colors">
                测试连接
              </button>
            </div>
          </div>
        )}
      </div>
    </aside>
  );
}