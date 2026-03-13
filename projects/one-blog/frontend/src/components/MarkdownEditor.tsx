/**
 * Markdown 编辑器组件
 */
import { useState } from 'react';
import MDEditor from '@uiw/react-md-editor';
import rehypeSanitize from 'rehype-sanitize';
import { Eye, EyeOff, Image, Link as LinkIcon, Heading, Bold, Italic, Code, Quote, List, ListOrdered } from 'lucide-react';

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
    <div className="border border-gray-200 rounded-lg overflow-hidden bg-white">
      {/* 工具栏 */}
      <div className="flex items-center justify-between px-4 py-2 bg-gray-50 border-b border-gray-200">
        <div className="flex items-center space-x-1">
          {toolbarButtons.map((btn) => (
            <button
              key={btn.label}
              type="button"
              onClick={btn.action}
              title={btn.label}
              className="p-2 text-gray-600 hover:text-gray-900 hover:bg-gray-200 rounded transition-colors"
            >
              <btn.icon className="w-4 h-4" />
            </button>
          ))}
        </div>

        <div className="flex items-center space-x-2">
          <div className="flex bg-gray-200 rounded-lg p-1">
            <button
              type="button"
              onClick={() => setPreviewMode('edit')}
              className={`px-3 py-1 text-sm rounded transition-colors ${
                previewMode === 'edit'
                  ? 'bg-white text-gray-900 shadow-sm'
                  : 'text-gray-600 hover:text-gray-900'
              }`}
            >
              <EyeOff className="w-4 h-4 inline mr-1" />
              编辑
            </button>
            <button
              type="button"
              onClick={() => setPreviewMode('live')}
              className={`px-3 py-1 text-sm rounded transition-colors ${
                previewMode === 'live'
                  ? 'bg-white text-gray-900 shadow-sm'
                  : 'text-gray-600 hover:text-gray-900'
              }`}
            >
              分屏
            </button>
            <button
              type="button"
              onClick={() => setPreviewMode('preview')}
              className={`px-3 py-1 text-sm rounded transition-colors ${
                previewMode === 'preview'
                  ? 'bg-white text-gray-900 shadow-sm'
                  : 'text-gray-600 hover:text-gray-900'
              }`}
            >
              <Eye className="w-4 h-4 inline mr-1" />
              预览
            </button>
          </div>
        </div>
      </div>

      {/* 编辑器 */}
      <div className="w-md-editor-container" data-color-mode="light">
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
      <div className="flex justify-between items-center px-4 py-2 bg-gray-50 border-t border-gray-200 text-sm text-gray-500">
        <span>{value.length} 字符</span>
        <span>{value.split(/\s+/).filter(Boolean).length} 词</span>
      </div>
    </div>
  );
}

// Markdown 内容渲染组件
interface MarkdownPreviewProps {
  content: string;
  className?: string;
}

export function MarkdownPreview({ content, className = '' }: MarkdownPreviewProps) {
  return (
    <div className={`prose prose-slate max-w-none ${className}`} data-color-mode="light">
      <MDEditor.Markdown
        source={content}
        rehypePlugins={[[rehypeSanitize]]}
      />
    </div>
  );
}
