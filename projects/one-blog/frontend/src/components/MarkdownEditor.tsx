/**
 * Markdown 编辑器组件 - 暗色模式 + 移动端适配
 */
import { useState, useEffect } from 'react';
import MDEditor from '@uiw/react-md-editor';
import rehypeSanitize from 'rehype-sanitize';
import { 
  Eye, EyeOff, Image, Link as LinkIcon, Heading, Bold, Italic, Code, Quote, List, ListOrdered,
  Smartphone, Monitor
} from 'lucide-react';

interface MarkdownEditorProps {
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
  height?: number;
  preview?: 'live' | 'edit' | 'preview';
}

export function MarkdownEditor({
  value,
  onChange,
  placeholder = '开始写作...',
  height = 500,
  preview = 'live',
}: MarkdownEditorProps) {
  const [previewMode, setPreviewMode] = useState<'edit' | 'live' | 'preview'>(preview);
  const [isDark, setIsDark] = useState(false);
  const [isMobile, setIsMobile] = useState(false);

  // 检测暗色模式和移动端
  useEffect(() => {
    const checkDarkMode = () => {
      setIsDark(document.documentElement.classList.contains('dark'));
    };
    
    const checkMobile = () => {
      setIsMobile(window.innerWidth < 768);
    };

    checkDarkMode();
    checkMobile();

    // 监听暗色模式变化
    const observer = new MutationObserver(checkDarkMode);
    observer.observe(document.documentElement, { attributes: true, attributeFilter: ['class'] });

    // 监听窗口大小变化
    window.addEventListener('resize', checkMobile);

    return () => {
      observer.disconnect();
      window.removeEventListener('resize', checkMobile);
    };
  }, []);

  // 移动端默认使用编辑模式
  useEffect(() => {
    if (isMobile && previewMode === 'live') {
      setPreviewMode('edit');
    }
  }, [isMobile]);

  // 插入文本到编辑器
  const insertText = (before: string, after: string = '') => {
    const textarea = document.querySelector('.w-md-editor-text-input') as HTMLTextAreaElement;
    if (!textarea) return;

    const start = textarea.selectionStart;
    const end = textarea.selectionEnd;
    const selectedText = value.substring(start, end);
    const newText = value.substring(0, start) + before + selectedText + after + value.substring(end);
    
    onChange(newText);
    
    // 恢复焦点和选区
    setTimeout(() => {
      textarea.focus();
      textarea.setSelectionRange(start + before.length, end + before.length);
    }, 0);
  };

  const toolbarButtons = [
    { icon: Bold, label: '粗体', action: () => insertText('**', '**') },
    { icon: Italic, label: '斜体', action: () => insertText('*', '*') },
    { icon: Heading, label: '标题', action: () => insertText('## ') },
    { icon: LinkIcon, label: '链接', action: () => insertText('[', '](url)') },
    { icon: Image, label: '图片', action: () => insertText('![alt](', ')') },
    { icon: Code, label: '代码', action: () => insertText('```\n', '\n```') },
    { icon: Quote, label: '引用', action: () => insertText('> ') },
    { icon: List, label: '无序列表', action: () => insertText('- ') },
    { icon: ListOrdered, label: '有序列表', action: () => insertText('1. ') },
  ];

  return (
    <div className={`border rounded-lg overflow-hidden transition-colors ${isDark ? 'border-gray-700 bg-gray-900' : 'border-gray-200 bg-white'}`}>
      {/* 工具栏 */}
      <div className={`flex items-center justify-between px-3 md:px-4 py-2 border-b transition-colors ${isDark ? 'bg-gray-800 border-gray-700' : 'bg-gray-50 border-gray-200'}`}>
        <div className="flex items-center space-x-0.5 md:space-x-1 overflow-x-auto scrollbar-hide">
          {toolbarButtons.map((btn) => (
            <button
              key={btn.label}
              type="button"
              onClick={btn.action}
              title={btn.label}
              className={`p-1.5 md:p-2 rounded transition-colors flex-shrink-0 ${
                isDark 
                  ? 'text-gray-400 hover:text-gray-100 hover:bg-gray-700' 
                  : 'text-gray-600 hover:text-gray-900 hover:bg-gray-200'
              }`}
            >
              <btn.icon className="w-4 h-4" />
            </button>
          ))}
        </div>

        <div className="flex items-center space-x-1 md:space-x-2">
          {/* 移动端/桌面端模式指示 */}
          <div className={`hidden md:flex items-center px-2 py-1 rounded text-xs ${isDark ? 'text-gray-500' : 'text-gray-400'}`}>
            {isMobile ? <Smartphone className="w-3 h-3" /> : <Monitor className="w-3 h-3" />}
          </div>
          <div className={`flex rounded-lg p-0.5 ${isDark ? 'bg-gray-700' : 'bg-gray-200'}`}>
            <button
              type="button"
              onClick={() => setPreviewMode('edit')}
              className={`px-2 md:px-3 py-1 text-xs md:text-sm rounded transition-colors ${
                previewMode === 'edit'
                  ? isDark 
                    ? 'bg-gray-600 text-gray-100 shadow-sm' 
                    : 'bg-white text-gray-900 shadow-sm'
                  : isDark 
                    ? 'text-gray-400 hover:text-gray-200' 
                    : 'text-gray-600 hover:text-gray-900'
              }`}
            >
              <EyeOff className="w-3 h-3 md:w-4 md:h-4 inline md:mr-1" /
              <span className="hidden md:inline">编辑</span>
            </button>
            <button
              type="button"
              onClick={() => setPreviewMode('live')}
              className={`px-2 md:px-3 py-1 text-xs md:text-sm rounded transition-colors hidden md:block ${
                previewMode === 'live'
                  ? isDark 
                    ? 'bg-gray-600 text-gray-100 shadow-sm' 
                    : 'bg-white text-gray-900 shadow-sm'
                  : isDark 
                    ? 'text-gray-400 hover:text-gray-200' 
                    : 'text-gray-600 hover:text-gray-900'
              }`}
            >
              分屏
            </button>
            <button
              type="button"
              onClick={() => setPreviewMode('preview')}
              className={`px-2 md:px-3 py-1 text-xs md:text-sm rounded transition-colors ${
                previewMode === 'preview'
                  ? isDark 
                    ? 'bg-gray-600 text-gray-100 shadow-sm' 
                    : 'bg-white text-gray-900 shadow-sm'
                  : isDark 
                    ? 'text-gray-400 hover:text-gray-200' 
                    : 'text-gray-600 hover:text-gray-900'
              }`}
            >
              <Eye className="w-3 h-3 md:w-4 md:h-4 inline md:mr-1" /
              <span className="hidden md:inline">预览</span>
            </button>
          </div>
        </div>
      </div>

      {/* 编辑器 */}
      <div className="w-md-editor-container" data-color-mode={isDark ? 'dark' : 'light'}>
        <MDEditor
          value={value}
          onChange={(val) => onChange(val || '')}
          height={height}
          preview={previewMode}
          hideToolbar={true}
          textareaProps={{
            placeholder,
          }}
          previewOptions={{
            rehypePlugins: [[rehypeSanitize]],
          }}
        />
      </div>

      {/* 字数统计 */}
      <div className={`flex justify-between items-center px-4 py-2 border-t text-xs md:text-sm transition-colors ${isDark ? 'bg-gray-800 border-gray-700 text-gray-400' : 'bg-gray-50 border-gray-200 text-gray-500'}`}>
        <span>{value.length} 字符</span>
        <span>{value.split(/\s+/).filter(Boolean).length} 词</span>
      </div>
    </div>
  );
}

// Markdown 内容渲染组件 - 暗色模式支持
interface MarkdownPreviewProps {
  content: string;
  className?: string;
}

export function MarkdownPreview({ content, className = '' }: MarkdownPreviewProps) {
  const [isDark, setIsDark] = useState(false);

  useEffect(() => {
    const checkDarkMode = () => {
      setIsDark(document.documentElement.classList.contains('dark'));
    };
    
    checkDarkMode();

    const observer = new MutationObserver(checkDarkMode);
    observer.observe(document.documentElement, { attributes: true, attributeFilter: ['class'] });

    return () => observer.disconnect();
  }, []);

  return (
    <div 
      className={`prose prose-slate dark:prose-invert max-w-none ${className}`} 
      data-color-mode={isDark ? 'dark' : 'light'}
    >
      <MDEditor.Markdown
        source={content}
        rehypePlugins={[[rehypeSanitize]]}
      />
    </div>
  );
}
