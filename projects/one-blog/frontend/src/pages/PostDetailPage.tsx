/**
 * 文章详情页
 */
import { useEffect } from 'react';
import { useParams, Link, useNavigate } from 'react-router-dom';
import { useBlogStore, useAuthStore } from '../store';
import { MarkdownPreview } from '../components/MarkdownEditor';
import { ArrowLeft, Clock, Eye, Heart, Tag, Share2, MessageCircle, Edit, Trash2 } from 'lucide-react';

export function PostDetailPage() {
  const navigate = useNavigate();
  const { slug } = useParams<{ slug: string }>();
  const { posts, currentPost, setCurrentPost, comments, likePost, deletePost } = useBlogStore();
  const { user, isAuthenticated } = useAuthStore();

  useEffect(() => {
    if (slug) {
      const post = posts.find((p) => p.slug === slug);
      if (post) {
        setCurrentPost(post);
      } else {
        // 文章不存在，返回首页
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

  // 格式化日期
  const formatDate = (dateStr: string) => {
    const date = new Date(dateStr);
    return date.toLocaleDateString('zh-CN', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
    });
  };

  // 计算阅读时间
  const readingTime = Math.ceil(currentPost.content.length / 500);

  // 是否可编辑
  const canEdit = isAuthenticated && (user?.id === currentPost.author.id || user?.role === 'admin');

  // 文章评论
  const postComments = comments.filter((c) => c.postId === currentPost.id);

  return (
    <div className="max-w-4xl mx-auto">
      {/* 返回按钮 */}
      <button
        onClick={() => navigate(-1)}
        className="flex items-center text-gray-500 hover:text-gray-900 mb-6 transition-colors"
      >
        <ArrowLeft className="w-5 h-5 mr-1" />
        返回
      </button>

      <article className="bg-white rounded-xl shadow-sm overflow-hidden">
        {/* 封面图 */}
        {currentPost.coverImage && (
          <div className="h-64 md:h-80">
            <img
              src={currentPost.coverImage}
              alt={currentPost.title}
              className="w-full h-full object-cover"
            />
          </div>
        )}

        <div className="p-6 md:p-10">
          {/* 标签 */}
          <div className="flex flex-wrap gap-2 mb-4">
            {currentPost.tags.map((tag) => (
              <Link
                key={tag.id}
                to={`/tag/${tag.slug}`}
                className="inline-flex items-center px-3 py-1 rounded-full text-sm font-medium bg-blue-50 text-blue-700 hover:bg-blue-100 transition-colors"
              >
                <Tag className="w-3 h-3 mr-1" />
                {tag.name}
              </Link>
            ))}
          </div>

          {/* 标题 */}
          <h1 className="text-3xl md:text-4xl font-bold text-gray-900 mb-6">
            {currentPost.title}
          </h1>

          {/* 作者信息 */}
          <div className="flex items-center justify-between py-6 border-y border-gray-100 mb-8">
            <div className="flex items-center space-x-4">
              <img
                src={currentPost.author.avatar || 'https://api.dicebear.com/7.x/avataaars/svg?seed=guest'}
                alt={currentPost.author.name}
                className="w-12 h-12 rounded-full"
              />
              <div>
                <p className="font-medium text-gray-900">{currentPost.author.name}</p>
                <div className="flex items-center text-sm text-gray-500 space-x-4">
                  <span className="flex items-center">
                    <Clock className="w-4 h-4 mr-1" />
                    {formatDate(currentPost.publishedAt)}
                  </span>
                  <span className="flex items-center">
                    {readingTime} 分钟阅读
                  </span>
                </div>
              </div>
            </div>

            <div className="flex items-center space-x-2">
              <button
                onClick={() => likePost(currentPost.id)}
                className="flex items-center space-x-1 px-4 py-2 text-gray-600 hover:text-red-600 hover:bg-red-50 rounded-lg transition-colors"
              >
                <Heart className="w-5 h-5" />
                <span>{currentPost.likeCount}</span>
              </button>

              <button className="flex items-center space-x-1 px-4 py-2 text-gray-600 hover:text-blue-600 hover:bg-blue-50 rounded-lg transition-colors">
                <Share2 className="w-5 h-5" />
              </button>

              {canEdit && (
                <>
                  <Link
                    to={`/editor/${currentPost.slug}`}
                    className="flex items-center space-x-1 px-4 py-2 text-gray-600 hover:text-blue-600 hover:bg-blue-50 rounded-lg transition-colors"
                  >
                    <Edit className="w-5 h-5" />
                  </Link>
                  <button
                    onClick={() => {
                      if (confirm('确定要删除这篇文章吗？')) {
                        deletePost(currentPost.id);
                        navigate('/');
                      }
                    }}
                    className="flex items-center space-x-1 px-4 py-2 text-gray-600 hover:text-red-600 hover:bg-red-50 rounded-lg transition-colors"
                  >
                    <Trash2 className="w-5 h-5" />
                  </button>
                </>
              )}
            </div>
          </div>

          {/* 文章内容 */}
          <div className="prose prose-slate prose-lg max-w-none">
            <MarkdownPreview content={currentPost.content} />
          </div>

          {/* 底部统计 */}
          <div className="flex items-center justify-between mt-10 pt-6 border-t border-gray-100">
            <div className="flex items-center space-x-6 text-sm text-gray-500">
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

            <p className="text-sm text-gray-400">
              更新于 {formatDate(currentPost.updatedAt)}
            </p>
          </div>
        </div>
      </article>

      {/* 评论区 */}
      <section className="mt-8 bg-white rounded-xl shadow-sm p-6 md:p-8">
        <h2 className="text-xl font-bold text-gray-900 mb-6">
          评论 ({postComments.length})
        </h2>

        {postComments.length === 0 ? (
          <p className="text-gray-500 text-center py-8">暂无评论，来发表第一条评论吧！</p>
        ) : (
          <div className="space-y-6">
            {postComments.map((comment) => (
              <div key={comment.id} className="flex space-x-4">
                <img
                  src={comment.author.avatar || 'https://api.dicebear.com/7.x/avataaars/svg?seed=guest'}
                  alt={comment.author.name}
                  className="w-10 h-10 rounded-full"
                />
                <div className="flex-1">
                  <div className="flex items-center space-x-2 mb-1">
                    <span className="font-medium text-gray-900">{comment.author.name}</span>
                    <span className="text-sm text-gray-400">{formatDate(comment.createdAt)}</span>
                  </div>
                  <p className="text-gray-700">{comment.content}</p>
                </div>
              </div>
            ))}
          </div>
        )}
      </section>
    </div>
  );
}
