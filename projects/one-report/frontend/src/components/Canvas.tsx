import { useRef, useState, useCallback } from 'react';
import { useReportStore } from '~/store';
import { ComponentRenderer } from '~/components/renderers';
import type { ComponentType, ReportComponent } from '~/types';
import { Trash2, GripVertical } from 'lucide-react';

export function Canvas() {
  const canvasRef = useRef<HTMLDivElement>(null);
  const [draggedOver, setDraggedOver] = useState(false);
  const [resizing, setResizing] = useState<string | null>(null);
  const [dragging, setDragging] = useState<string | null>(null);
  const [dragOffset, setDragOffset] = useState({ x: 0, y: 0 });
  
  const {
    config,
    selectedComponentId,
    isPreview,
    scale,
    setSelectedComponent,
    addComponent,
    moveComponent,
    resizeComponent,
    removeComponent,
  } = useReportStore();

  const { components, width, height, gridSize, showGrid } = config;

  // 网格背景样式
  const gridStyle = showGrid && !isPreview ? {
    backgroundImage: `
      linear-gradient(to right, #e5e7eb 1px, transparent 1px),
      linear-gradient(to bottom, #e5e7eb 1px, transparent 1px)
    `,
    backgroundSize: `${gridSize}px ${gridSize}px`,
  } : {};

  const handleDragOver = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    setDraggedOver(true);
  }, []);

  const handleDragLeave = useCallback(() => {
    setDraggedOver(false);
  }, []);

  const handleDrop = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    setDraggedOver(false);
    
    const componentType = e.dataTransfer.getData('componentType') as ComponentType;
    if (!componentType || !canvasRef.current) return;
    
    const rect = canvasRef.current.getBoundingClientRect();
    const x = (e.clientX - rect.left) / scale;
    const y = (e.clientY - rect.top) / scale;
    
    addComponent(componentType, x, y);
  }, [addComponent, scale]);

  const handleMouseDown = useCallback((e: React.MouseEvent, id: string) => {
    if (isPreview) return;
    e.stopPropagation();
    setSelectedComponent(id);
    setDragging(id);
    
    const component = components.find((c: ReportComponent) => c.id === id);
    if (component) {
      setDragOffset({
        x: e.clientX - component.style.x * scale,
        y: e.clientY - component.style.y * scale,
      });
    }
  }, [isPreview, components, scale, setSelectedComponent]);

  const handleResizeStart = useCallback((e: React.MouseEvent, id: string) => {
    e.stopPropagation();
    e.preventDefault();
    setResizing(id);
  }, []);

  const handleMouseMove = useCallback((e: React.MouseEvent) => {
    if (dragging) {
      const x = (e.clientX - dragOffset.x) / scale;
      const y = (e.clientY - dragOffset.y) / scale;
      moveComponent(dragging, Math.max(0, x), Math.max(0, y));
    }
    
    if (resizing && canvasRef.current) {
      const component = components.find((c: ReportComponent) => c.id === resizing);
      if (component) {
        const rect = canvasRef.current.getBoundingClientRect();
        const newWidth = (e.clientX - rect.left) / scale - component.style.x;
        const newHeight = (e.clientY - rect.top) / scale - component.style.y;
        resizeComponent(resizing, Math.max(50, newWidth), Math.max(30, newHeight));
      }
    }
  }, [dragging, resizing, components, dragOffset, scale, moveComponent, resizeComponent]);

  const handleMouseUp = useCallback(() => {
    setDragging(null);
    setResizing(null);
  }, []);

  const handleCanvasClick = useCallback(() => {
    setSelectedComponent(null);
  }, [setSelectedComponent]);

  return (
    <main 
      className={`flex-1 relative overflow-auto transition-colors ${
        draggedOver ? 'bg-blue-50/50 dark:bg-blue-900/10' : 'bg-gray-100 dark:bg-gray-900'
      } ${isPreview ? 'preview-mode' : ''}`}
      onMouseMove={handleMouseMove}
      onMouseUp={handleMouseUp}
      onMouseLeave={handleMouseUp}
    >
      <div className="min-h-full p-8 flex items-start justify-center">
        <div
          ref={canvasRef}
          className={`relative bg-white dark:bg-gray-800 shadow-lg transition-all ${
            isPreview ? '' : 'ring-2 ring-blue-500/20'
          }`}
          style={{
            width: width,
            height: height,
            transform: `scale(${scale})`,
            transformOrigin: 'top left',
            ...gridStyle,
          }}
          onDragOver={handleDragOver}
          onDragLeave={handleDragLeave}
          onDrop={handleDrop}
          onClick={handleCanvasClick}
        >
          {components.length === 0 && !isPreview && (
            <div className="absolute inset-0 flex items-center justify-center pointer-events-none">
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
          
          {components.map((component: ReportComponent) => (
            <div
              key={component.id}
              className={`absolute group ${
                selectedComponentId === component.id && !isPreview
                  ? 'ring-2 ring-blue-500 z-10' 
                  : 'hover:ring-1 hover:ring-blue-300'
              }`}
              style={{
                left: component.style.x,
                top: component.style.y,
                width: component.style.width,
                height: component.style.height,
              }}
              onMouseDown={(e) => handleMouseDown(e, component.id)}
            >
              {/* 组件内容 */}
              <div className="w-full h-full overflow-hidden"
                style={{
                  borderRadius: component.style.borderRadius || 0,
                }}
              >
                <ComponentRenderer component={component} isPreview={isPreview} />
              </div>
              
              {/* 选中时的控制手柄 */}
              {selectedComponentId === component.id && !isPreview && (
                <>
                  {/* 拖拽手柄 */}
                  <div className="absolute -top-6 left-0 flex items-center gap-1 bg-blue-500 text-white text-xs px-2 py-1 rounded-t"
                  >
                    <GripVertical className="w-3 h-3" />
                    <span>{component.name}</span>
                  </div>
                  
                  {/* 删除按钮 */}
                  <button
                    className="absolute -top-6 right-0 bg-red-500 text-white p-1 rounded-t hover:bg-red-600"
                    onClick={(e) => {
                      e.stopPropagation();
                      removeComponent(component.id);
                    }}
                  >
                    <Trash2 className="w-3 h-3" />
                  </button>
                  
                  {/* 缩放手柄 */}
                  <div
                    className="absolute -bottom-1 -right-1 w-3 h-3 bg-blue-500 rounded-full cursor-se-resize"
                    onMouseDown={(e) => handleResizeStart(e, component.id)}
                  />
                </>
              )}
            </div>
          ))}
        </div>
      </div>
    </main>
  );
}
