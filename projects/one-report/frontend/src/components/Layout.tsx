import type { ReactNode } from 'react';
import { useReportStore } from '../store';
import { Eye, EyeOff, ZoomIn, ZoomOut, Grid3x3 } from 'lucide-react';

interface LayoutProps {
  children: ReactNode;
}

export function Layout({ children }: LayoutProps) {
  const { 
    isPreview, 
    scale, 
    snapToGrid,
    config,
    togglePreview, 
    setScale,
    toggleSnapToGrid,
    updateConfig,
  } = useReportStore();

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-900 flex flex-col">
      <header className="h-14 bg-white dark:bg-gray-800 border-b border-gray-200 dark:border-gray-700 flex items-center px-4 shadow-sm">
        <div className="flex items-center gap-2">
          <div className="w-8 h-8 bg-blue-500 rounded-lg flex items-center justify-center">
            <svg className="w-5 h-5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 17v-2m3 2v-4m3 4v-6m2 10H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
            </svg>
          </div>
          <div className="flex flex-col">
            <h1 className="text-sm font-semibold text-gray-900 dark:text-white leading-tight">{config.name}</h1>
            <span className="text-xs text-gray-500 dark:text-gray-400">{isPreview ? '预览模式' : '编辑模式'}</span>
          </div>
        </div>
        
        <div className="flex-1 flex items-center justify-center gap-4">
          {/* 缩放控制 */}
          {!isPreview && (
            <>
              <div className="flex items-center gap-1 bg-gray-100 dark:bg-gray-700 rounded-lg p-1">
                <button
                  onClick={() => setScale(Math.max(0.5, scale - 0.1))}
                  className="p-1.5 hover:bg-white dark:hover:bg-gray-600 rounded"
                  title="缩小"
                >
                  <ZoomOut className="w-4 h-4 text-gray-600 dark:text-gray-300" />
                </button>
                <span className="text-xs text-gray-600 dark:text-gray-300 min-w-[50px] text-center">
                  {Math.round(scale * 100)}%
                </span>
                <button
                  onClick={() => setScale(Math.min(2, scale + 0.1))}
                  className="p-1.5 hover:bg-white dark:hover:bg-gray-600 rounded"
                  title="放大"
                >
                  <ZoomIn className="w-4 h-4 text-gray-600 dark:text-gray-300" />
                </button>
              </div>
              
              <button
                onClick={toggleSnapToGrid}
                className={`flex items-center gap-1 px-3 py-1.5 text-xs rounded-lg transition-colors ${
                  snapToGrid
                    ? 'bg-blue-100 text-blue-600 dark:bg-blue-900/30 dark:text-blue-400'
                    : 'bg-gray-100 text-gray-600 dark:bg-gray-700 dark:text-gray-300 hover:bg-gray-200'
                }`}
                title="吸附网格"
              >
                <Grid3x3 className="w-4 h-4" />
                网格
              </button>
            </>
          )}
        </div>
        
        <div className="flex items-center gap-3">
          <button
            onClick={togglePreview}
            className={`flex items-center gap-1.5 px-3 py-1.5 text-sm rounded-lg transition-colors ${
              isPreview
                ? 'bg-green-500 text-white hover:bg-green-600'
                : 'bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-600'
            }`}
          >
            {isPreview ? (
              <>
                <EyeOff className="w-4 h-4" />
                退出预览
              </>
            ) : (
              <>
                <Eye className="w-4 h-4" />
                预览
              </>
            )}
          </button>
          
          {!isPreview && (
            <button className="px-4 py-1.5 text-sm bg-blue-500 text-white rounded-lg hover:bg-blue-600 transition-colors">
              发布
            </button>
          )}
        </div>
      </header>
      
      {children}
    </div>
  );
}
