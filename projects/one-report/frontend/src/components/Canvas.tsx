import { useState } from 'react';

interface CanvasItem {
  id: string;
  type: string;
  x: number;
  y: number;
  width: number;
  height: number;
}

export function Canvas() {
  const [items, setItems] = useState<CanvasItem[]>([]);
  const [draggedOver, setDraggedOver] = useState(false);

  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault();
    setDraggedOver(true);
  };

  const handleDragLeave = () => {
    setDraggedOver(false);
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    setDraggedOver(false);
    
    const rect = e.currentTarget.getBoundingClientRect();
    const x = e.clientX - rect.left;
    const y = e.clientY - rect.top;
    
    // 模拟添加组件
    const newItem: CanvasItem = {
      id: Date.now().toString(),
      type: 'component',
      x: x - 100,
      y: y - 50,
      width: 200,
      height: 100,
    };
    
    setItems(prev => [...prev, newItem]);
  };

  return (
    <main 
      className={`flex-1 relative overflow-auto transition-colors ${
        draggedOver ? 'bg-blue-50/50 dark:bg-blue-900/10' : 'bg-gray-50 dark:bg-gray-900'
      }`}
      onDragOver={handleDragOver}
      onDragLeave={handleDragLeave}
      onDrop={handleDrop}
    >
      <div className="min-h-full p-8">
        <div className="max-w-6xl mx-auto bg-white dark:bg-gray-800 rounded-lg shadow-sm border border-gray-200 dark:border-gray-700 min-h-[800px] p-8 relative">
          {items.length === 0 && (
            <div className="absolute inset-0 flex items-center justify-center">
              <div className="text-center">
                <div className="w-16 h-16 bg-gray-100 dark:bg-gray-700 rounded-full flex items-center justify-center mx-auto mb-4">
                  <svg className="w-8 h-8 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
                  </svg>
                </div>
                <p className="text-gray-500 dark:text-gray-400">从左侧拖拽组件到这里</p>
                <p className="text-sm text-gray-400 dark:text-gray-500 mt-1">开始构建你的报表</p>
              </div>
            </div>
          )}
          
          {items.map(item => (
            <div
              key={item.id}
              className="absolute bg-white dark:bg-gray-700 border-2 border-dashed border-blue-300 dark:border-blue-600 rounded-lg p-4 cursor-move hover:border-blue-500 transition-colors"
              style={{
                left: item.x,
                top: item.y,
                width: item.width,
                height: item.height,
              }}
            >
              <div className="text-sm text-gray-600 dark:text-gray-300 text-center">
                组件 {item.id.slice(-4)}
              </div>
            </div>
          ))}
        </div>
      </div>
    </main>
  );
}