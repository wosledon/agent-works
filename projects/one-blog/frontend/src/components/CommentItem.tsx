/**
 * 评论项组件 - 支持嵌套回复
 */
import { useState } from 'react';
import { MessageCircle, ThumbsUp, MoreHorizontal, Edit2, Trash2 } from 'lucide-react';
import { CommentForm } from './CommentForm';
import type { Comment, Author } from '../types';

interface CommentItemProps {
  comment: Comment;
  replies?: Comment[];
  currentUser?: Author | null;
  onReply: (parentId: string, content: string) => void;
  onEdit?: (commentId: string, content: string) => void;
  onDelete?: (commentId: string) => void;
  onLike?: (commentId: string) => void;
  depth?: number;
}

export function CommentItem({
  comment,
  replies = [],
  currentUser,
  onReply,
  onEdit,
  onDelete,
  onLike,
  depth = 0,
}: CommentItemProps) {
  const [isReplying, setIsReplying] = useState(false);
  const [isEditing, setIsEditing] = useState(false);
  const [showActions, setShowActions] = useState(false);

  const isAuthor = currentUser?.id === comment.author.id;
  const hasReplies = replies.length > 0;

  const formatDate = (dateStr: string) => {
    const date = new Date(dateStr);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);

    if (diffMins < 1) return '刚刚';
    if (diffMins < 60) return `${diffMins}分钟前`;
    if (diffHours < 24) return `${diffHours}小时前`;
    if (diffDays < 7) return `${diffDays}天前`;
    return date.toLocaleDateString('zh-CN', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    });
  };

  const handleReply = (content: string) => {
    onReply(comment.id, content);
    setIsReplying(false);
  };

  const handleEdit = (content: string) => {
    onEdit?.(comment.id, content);
    setIsEditing(false);
  };

  return (
    <div className={`${depth > 0 ? 'ml-12 mt-4' : ''}`}>
      <div className="flex space-x-3">
        {/* 用户头像 */}
        <div className="flex-shrink-0">
          <img
            src={comment.author.avatar || 'https://api.dicebear.com/7.x/avataaars/svg?seed=guest'}
            alt={comment.author.name}
            className="w-10 h-10 rounded-full bg-gray-100 object-cover"
          />
        </div>

        {/* 评论内容 */}
        <div className="flex-1 min-w-0">
          {/* 头部信息 */}
          <div className="flex items-center space-x-2 mb-1">
            <span className="font-semibold text-gray-900 dark:text-gray-100">
              {comment.author.name}
            </span>
            <span className="text-sm text-gray-400">
              {formatDate(comment.createdAt)}
            </span>
            {comment.updatedAt !== comment.createdAt && (
              <span className="text-xs text-gray-400">(已编辑)</span>
            )}
          </div>

          {/* 评论正文 */}
          {isEditing ? (
            <CommentForm
              onSubmit={handleEdit}
              onCancel={() => setIsEditing(false)}
              placeholder="编辑评论..."
              className="mt-2"
            />
          ) : (
            <div className="text-gray-700 dark:text-gray-300 leading-relaxed whitespace-pre-wrap">
              {comment.content}
            </div>
          )}

          {/* 操作按钮 */}
          {!isEditing && (
            <div className="flex items-center space-x-4 mt-2">
              <button
                onClick={() => onLike?.(comment.id)}
                className="flex items-center space-x-1 text-sm text-gray-500 hover:text-blue-600 transition-colors"
              >
                <ThumbsUp className="w-4 h-4" />
                <span>{(comment as any).likeCount || 0}</span>
              </button>

              <button
                onClick={() => setIsReplying(!isReplying)}
                className={`
                  flex items-center space-x-1 text-sm transition-colors
                  ${isReplying ? 'text-blue-600' : 'text-gray-500 hover:text-blue-600'}
                `}
              >
                <MessageCircle className="w-4 h-4" />
                <span>回复</span>
              </button>

              {/* 更多操作 */}
              {isAuthor && (
                <div className="relative">
                  <button
                    onClick={() => setShowActions(!showActions)}
                    className="p-1 text-gray-400 hover:text-gray-600 rounded-full hover:bg-gray-100 transition-colors"
                  >
                    <MoreHorizontal className="w-4 h-4" />
                  </button>

                  {showActions && (
                    <>
                      <div
                        className="fixed inset-0 z-10"
                        onClick={() => setShowActions(false)}
                      />
                      <div className="absolute left-0 mt-1 w-32 bg-white dark:bg-gray-800 rounded-lg shadow-lg border border-gray-100 dark:border-gray-700 py-1 z-20">
                        <button
                          onClick={() => {
                            setIsEditing(true);
                            setShowActions(false);
                          }}
                          className="w-full flex items-center px-4 py-2 text-sm text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors"
                        >
                          <Edit2 className="w-4 h-4 mr-2" />
                          编辑
                        </button>
                        <button
                          onClick={() => {
                            if (confirm('确定要删除这条评论吗？')) {
                              onDelete?.(comment.id);
                            }
                            setShowActions(false);
                          }}
                          className="w-full flex items-center px-4 py-2 text-sm text-red-600 hover:bg-red-50 dark:hover:bg-red-900/20 transition-colors"
                        >
                          <Trash2 className="w-4 h-4 mr-2" />
                          删除
                        </button>
                      </div>
                    </>
                  )}
                </div>
              )}
            </div>
          )}

          {/* 回复表单 */}
          {isReplying && (
            <div className="mt-4">
              <CommentForm
                onSubmit={handleReply}
                onCancel={() => setIsReplying(false)}
                placeholder={`回复 ${comment.author.name}...`}
                replyingTo={comment.author.name}
              />
            </div>
          )}
        </div>
      </div>

      {/* 嵌套回复 */}
      {hasReplies && (
        <div className="mt-4">
          {replies.map((reply) => (
            <CommentItem
              key={reply.id}
              comment={reply}
              currentUser={currentUser}
              onReply={onReply}
              onEdit={onEdit}
              onDelete={onDelete}
              onLike={onLike}
              depth={depth + 1}
            />
          ))}
        </div>
      )}
    </div>
  );
}
