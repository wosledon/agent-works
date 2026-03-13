/**
 * 文章详情页 - 集成评论系统
 */
import { useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useBlogStore, useAuthStore } from '../store';
import { CommentList } from '../components/CommentList';
import { MarkdownPreview } from '../components/MarkdownEditor';
import { ArrowLeft, Clock, Eye, Heart, Tag, Share2, MessageCircle, Edit, Trash2 } from 'lucide-react';

export function PostDetailPage() {
  const navigate = useNavigate();
  const { slug } = useParams<{ slug: string }>();
  const { 
    posts, 
    currentPost, 
    setCurrentPost, 
    getPostComments,
    addComment,
    updateComment,
    deleteComment,
    likeComment,
    likePost, 
    deletePost 
  } = useBlogStore();
  const { user, isAuthenticated } = useAuthStore();

  useEffect(() => {
    if (slug) {
      const post = posts.find((p) => p.slug === slug);
      if (post) {
        setCurrentPost(post);
      } else {
        navigate('/');
      }
    }
    return () => {
      setCurrentPost(null);
    };
  }, [slug, posts, setCurrentPost, navigate]);

  if (!currentPost) {
    return (
      <div className="flex items-center justify-center min-h-[60vh]">
        <div className="animate-spin rounded-full h-12 w-12 border-4 border-blue-200 border-t-blue-600"></div>
      </div>
    );
  }

  const formatDate = (dateStr: string) => {
    const date = new Date(dateStr);
    return date.toLocaleDateString('zh-CN', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
    });
  };

  const readingTime = Math.ceil(currentPost.content.length / 500);
  const canEdit = isAuthenticated && (user?.id === currentPost.author.id || user?.role === 'admin');
  const postComments = getPostComments(currentPost.id);

  // 评论操作
  const handleAddComment = (content: string, parentId?: string) => {
    if (!user) return;
    addComment(currentPost.id, content, {
      id: user.id,
      name: user.name,
      avatar: user.avatar,
    }, parentId);
  };

  const handleEditComment = (commentId: string, content: string) => {
    updateComment(commentId, content);
  };

  const handleDeleteComment = (commentId: string) => {
    deleteComment(commentId);
  };

  const handleLikeComment = (commentId: string) => {
    likeComment(commentId);
  };

  return (
    <div className="max-w-4xl mx-auto">
      {/* 返回按钮 */}
      <button
        onClick={() => navigate(-1)}
        className="
          flex items-center text-gray-500 dark:text-gray-400 
          hover:text-gray-900 dark:hover:text-gray-100 mb-4 md:mb-6 
          transition-colors
        "
      >
        <ArrowLeft className="w-5 h-5 mr-1" />
        返回
      </button>

      <article className="
        bg-white dark:bg-gray-900 rounded-xl shadow-sm 
        overflow-hidden transition-colors
      ">
        {/* 封面图 */}
        {currentPost.coverImage && (
          <div className="h-48 sm:h-64 md:h-80">
            <img
              src={currentPost.coverImage}
              alt={currentPost.title}
              className="w-full h-full object-cover"
            />
          </div>
        )}

        <div className="p-4 sm:p-6 md:p-10">
          {/* 标签 */}
          <div className="flex flex-wrap gap-2 mb-4">
            {currentPost.tags.map((tag) => (
              <button
                key={tag.id}
                onClick={() => navigate(`/tag/${tag.slug}`)}
                className="
                  inline-flex items-center px-3 py-1 rounded-full 
                  text-sm font-medium 
                  bg-blue-50 dark:bg-blue-900/30 
                  text-blue-700 dark:text-blue-300 
                  hover:bg-blue-100 dark:hover:bg-blue-900/50 
                  transition-colors
                "
              >
                <Tag className="w-3 h-3 mr-1" />
                {tag.name}
              </button>
            ))}
          </div>

          {/* 标题 */}
          <h1 className="
            text-2xl sm:text-3xl md:text-4xl font-bold 
            text-gray-900 dark:text-gray-100 mb-4 md:mb-6
          ">
            {currentPost.title}
          </h1>

          {/* 作者信息 */}
          <div className="
            flex flex-col sm:flex-row sm:items-center 
            justify-between py-4 sm:py-6 
            border-y border-gray-100 dark:border-gray-800 mb-6 md:mb-8
          ">
            <div className="flex items-center space-x-3 sm:space-x-4">
              <img
                src={currentPost.author.avatar || 'https://api.dicebear.com/7.x/avataaars/svg?seed=guest'}
                alt={currentPost.author.name}
                className="w-10 h-10 sm:w-12 sm:h-12 rounded-full"
              />
              <div>
                <p className="font-medium text-gray-900 dark:text-gray-100">
                  {currentPost.author.name}
                </p>
                <div className="flex flex-wrap items-center text-sm text-gray-500 dark:text-gray-400 gap-2 sm:gap-4">
                  <span className="flex items-center">
                    <Clock className="w-4 h-4 mr-1" />
                    {formatDate(currentPost.publishedAt)}
                  </span>
                  <span className="hidden sm:inline">•</span>
                  <span>{readingTime} 分钟阅读</span>
                </div>
              </div>
            </div>

            <div className="flex items-center space-x-1 mt-4 sm:mt-0">
              <button
                onClick={() => likePost(currentPost.id)}
                className="
                  flex items-center space-x-1 px-3 sm:px-4 py-2 
                  text-gray-600 dark:text-gray-400 
                  hover:text-red-600 dark:hover:text-red-400 
                  hover:bg-red-50 dark:hover:bg-red-900/20 
                  rounded-lg transition-colors
                "
              >
                <Heart className="w-5 h-5" />
                <span className="hidden sm:inline">{currentPost.likeCount}</span>
              </button>

              <button 
                className="
                  flex items-center space-x-1 px-3 sm:px-4 py-2 
                  text-gray-600 dark:text-gray-400 
                  hover:text-blue-600 dark:hover:text-blue-400 
                  hover:bg-blue-50 dark:hover:bg-blue-900/20 
                  rounded-lg transition-colors
                "
              >
                <Share2 className="w-5 h-5" />
              </button>

              {canEdit && (
                <>
                  <button
                    onClick={() => navigate(`/editor/${currentPost.slug}`)}
                    className="
                      flex items-center space-x-1 px-3 sm:px-4 py-2 
                      text-gray-600 dark:text-gray-400 
                      hover:text-blue-600 dark:hover:text-blue-400 
                      hover:bg-blue-50 dark:hover:bg-blue-900/20 
                      rounded-lg transition-colors
                    "
                  >
                    <Edit className="w-5 h-5" />
                  </button>
                  <button
                    onClick={() => {
                      if (confirm('确定要删除这篇文章吗？')) {
                        deletePost(currentPost.id);
                        navigate('/');
                      }
                    }}
                    className="
                      flex items-center space-x-1 px-3 sm:px-4 py-2 
                      text-gray-600 dark:text-gray-400 
                      hover:text-red-600 dark:hover:text-red-400 
                      hover:bg-red-50 dark:hover:bg-red-900/20 
                      rounded-lg transition-colors
                    "
                  >
                    <Trash2 className="w-5 h-5" />
                  </button>
                </>
              )}
            </div>
          </div>

          {/* 文章内容 */}
          <div className="prose prose-slate prose-lg dark:prose-invert max-w-none">
            <MarkdownPreview content={currentPost.content} />
          </div>

          {/* 底部统计 */}
          <div className="
            flex flex-col sm:flex-row sm:items-center justify-between 
            mt-8 md:mt-10 pt-6 
            border-t border-gray-100 dark:border-gray-800
          ">
            <div className="flex items-center space-x-4 sm:space-x-6 text-sm text-gray-500 dark:text-gray-400">
              <span className="flex items-center">
                <Eye className="w-4 h-4 mr-1" />
                {currentPost.viewCount} 阅读
              </span>
              <span className="flex items-center">
                <Heart className="w-4 h-4 mr-1" />
                {currentPost.likeCount} 喜欢
              </span>
              <span className="flex items-center">
                <MessageCircle className="w-4 h-4 mr-1" />
                {postComments.length} 评论
              </span>
            </div>

            <p className="text-sm text-gray-400 dark:text-gray-500 mt-2 sm:mt-0">
              更新于 {formatDate(currentPost.updatedAt)}
            </p>
          </div>
        </div>
      </article>

      {/* 评论区 */}
      <div className="mt-6 md:mt-8">
        <CommentList
          comments={postComments}
          currentUser={user ? { id: user.id, name: user.name, avatar: user.avatar } : null}
          onAddComment={handleAddComment}
          onEditComment={handleEditComment}
          onDeleteComment={handleDeleteComment}
          onLikeComment={handleLikeComment}
        />
      </div>
    </div>
  );
}
