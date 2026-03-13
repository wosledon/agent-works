/**
 * 评论列表组件
 */
import { useState } from 'react';
import { MessageSquare } from 'lucide-react';
import { CommentItem } from './CommentItem';
import { CommentForm } from './CommentForm';
import type { Comment } from '../types';

interface CommentListProps {
  comments: Comment[];
  currentUser?: { id: string; name: string; avatar?: string } | null;
  onAddComment?: (content: string, parentId?: string) => void;
  onEditComment?: (commentId: string, content: string) => void;
  onDeleteComment?: (commentId: string) => void;
  onLikeComment?: (commentId: string) => void;
  className?: string;
}

export function CommentList({
  comments,
  currentUser,
  onAddComment,
  onEditComment,
  onDeleteComment,
  onLikeComment,
  className = '',
}: CommentListProps) {
  const [sortBy, setSortBy] = useState<'newest' | 'oldest'>('newest');

  // 构建评论树结构
  const buildCommentTree = (flatComments: Comment[]): { root: Comment; replies: Comment[] }[] => {
    const commentMap = new Map<string, Comment & { children: Comment[] }>();
    
    // 初始化所有评论的 children 数组
    flatComments.forEach(comment => {
      commentMap.set(comment.id, { ...comment, children: [] });
    });

    const roots: { root: Comment; replies: Comment[] }[] = [];

    // 构建树结构
    flatComments.forEach(comment => {
      const commentWithChildren = commentMap.get(comment.id)!;
      if (comment.parentId && commentMap.has(comment.parentId)) {
        const parent = commentMap.get(comment.parentId)!;
        parent.children.push(commentWithChildren);
      } else {
        roots.push({ root: commentWithChildren, replies: commentWithChildren.children });
      }
    });

    // 排序
    const sortComments = (a: Comment, b: Comment) => {
      if (sortBy === 'newest') {
        return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime();
      }
      return new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime();
    };

    roots.sort((a, b) => sortComments(a.root, b.root));
    roots.forEach(({ replies }) => replies.sort(sortComments));

    return roots;
  };

  const commentTree = buildCommentTree(comments);
  const totalComments = comments.length;

  const handleAddComment = (content: string) => {
    onAddComment?.(content);
  };

  const handleReply = (parentId: string, content: string) => {
    onAddComment?.(content, parentId);
  };

  return (
    <section className={`bg-white dark:bg-gray-900 rounded-xl shadow-sm ${className}`}>
      {/* 头部 */}
      <div className="p-6 border-b border-gray-100 dark:border-gray-800">
        <div className="flex items-center justify-between">
          <div className="flex items-center space-x-2">
            <MessageSquare className="w-5 h-5 text-gray-400" />
            <h2 className="text-lg font-bold text-gray-900 dark:text-gray-100">
              评论
              <span className="ml-2 text-sm font-normal text-gray-500">
                ({totalComments})
              </span>
            </h2>
          </div>

          {totalComments > 0 && (
            <div className="flex items-center space-x-1 text-sm">
              <button
                onClick={() => setSortBy('newest')}
                className={`
                  px-3 py-1 rounded-full transition-colors
                  ${sortBy === 'newest'
                    ? 'bg-gray-900 dark:bg-gray-700 text-white'
                    : 'text-gray-500 hover:bg-gray-100 dark:hover:bg-gray-800'
                  }
                `}
              >
                最新
              </button>
              <button
                onClick={() => setSortBy('oldest')}
                className={`
                  px-3 py-1 rounded-full transition-colors
                  ${sortBy === 'oldest'
                    ? 'bg-gray-900 dark:bg-gray-700 text-white'
                    : 'text-gray-500 hover:bg-gray-100 dark:hover:bg-gray-800'
                  }
                `}
              >
                最早
              </button>
            </div>
          )}
        </div>
      </div>

      {/* 评论表单 */}
      {currentUser ? (
        <div className="p-6 border-b border-gray-100 dark:border-gray-800">
          <div className="flex space-x-3">
            <img
              src={currentUser.avatar || 'https://api.dicebear.com/7.x/avataaars/svg?seed=guest'}
              alt={currentUser.name}
              className="w-10 h-10 rounded-full bg-gray-100"
            />
            <div className="flex-1">
              <CommentForm
                onSubmit={handleAddComment}
                placeholder={`${currentUser.name}，写下你的评论...`}
              />
            </div>
          </div>
        </div>
      ) : (
        <div className="p-6 border-b border-gray-100 dark:border-gray-800">
          <div className="text-center py-6 bg-gray-50 dark:bg-gray-800/50 rounded-lg">
            <p className="text-gray-500">
              请<a href="/login" className="text-blue-600 hover:underline">登录</a>后发表评论
            </p>
          </div>
        </div>
      )}

      {/* 评论列表 */}
      <div className="p-6">
        {totalComments === 0 ? (
          <div className="text-center py-12">
            <div className="w-16 h-16 bg-gray-100 dark:bg-gray-800 rounded-full flex items-center justify-center mx-auto mb-4">
              <MessageSquare className="w-8 h-8 text-gray-400" />
            </div>
            <h3 className="text-lg font-medium text-gray-900 dark:text-gray-100 mb-2">
              暂无评论
            </h3>
            <p className="text-gray-500">
              {currentUser ? '来发表第一条评论吧！' : '登录后发表评论'}
            </p>
          </div>
        ) : (
          <div className="space-y-6">
            {commentTree.map(({ root, replies }) => (
              <CommentItem
                key={root.id}
                comment={root}
                replies={replies}
                currentUser={currentUser || undefined}
                onReply={handleReply}
                onEdit={onEditComment}
                onDelete={onDeleteComment}
                onLike={onLikeComment}
              />
            ))}
          </div>
        )}
      </div>
    </section>
  );
}
