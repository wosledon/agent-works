/**
 * 评论表单组件
 */
import { useState } from 'react';
import { Send, CornerDownRight, X } from 'lucide-react';

interface CommentFormProps {
  onSubmit: (content: string) => void;
  onCancel?: () => void;
  placeholder?: string;
  replyingTo?: string;
  className?: string;
}

export function CommentForm({
  onSubmit,
  onCancel,
  placeholder = '写下你的评论...',
  replyingTo,
  className = '',
}: CommentFormProps) {
  const [content, setContent] = useState('');
  const [isFocused, setIsFocused] = useState(false);

  const handleSubmit = () => {
    const trimmedContent = content.trim();
    if (!trimmedContent) return;

    onSubmit(trimmedContent);
    setContent('');
    setIsFocused(false);
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && (e.metaKey || e.ctrlKey)) {
      handleSubmit();
    }
  };

  return (
    <div className={`${className}`}>
      {replyingTo && (
        <div className="flex items-center text-sm text-gray-500 mb-2">
          <CornerDownRight className="w-4 h-4 mr-1" />
          <span>回复 <span className="font-medium text-gray-700">{replyingTo}</span></span>
          {onCancel && (
            <button
              onClick={onCancel}
              className="ml-2 p-1 hover:bg-gray-100 rounded-full transition-colors"
            >
              <X className="w-3 h-3" />
            </button>
          )}
        </div>
      )}
      <div
        className={`
          relative rounded-lg transition-all duration-200
          ${isFocused ? 'ring-2 ring-blue-500' : 'border border-gray-200 dark:border-gray-700'}
        `}
      >
        <textarea
          value={content}
          onChange={(e) => setContent(e.target.value)}
          onFocus={() => setIsFocused(true)}
          onBlur={() => !content && setIsFocused(false)}
          onKeyDown={handleKeyDown}
          placeholder={placeholder}
          rows={isFocused || content ? 3 : 2}
          className="
            w-full px-4 py-3 bg-transparent resize-none outline-none
            text-gray-900 dark:text-gray-100 placeholder-gray-400
            rounded-lg
          "
        />
        <div className="flex items-center justify-between px-4 py-2 border-t border-gray-100 dark:border-gray-700">
          <span className="text-xs text-gray-400">
            {isFocused && 'Cmd + Enter 发送'}
          </span>
          <div className="flex items-center space-x-2">
            {onCancel && (
              <button
                onClick={onCancel}
                className="
                  px-3 py-1.5 text-sm text-gray-600 hover:text-gray-800
                  transition-colors rounded-md hover:bg-gray-100
                "
              >
                取消
              </button>
            )}
            <button
              onClick={handleSubmit}
              disabled={!content.trim()}
              className={`
                flex items-center space-x-1 px-4 py-1.5 rounded-lg
                text-sm font-medium transition-all
                ${content.trim()
                  ? 'bg-blue-600 text-white hover:bg-blue-700 shadow-sm'
                  : 'bg-gray-100 text-gray-400 cursor-not-allowed'
                }
              `}
            >
              <Send className="w-4 h-4" />
              <span>发送</span>
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
