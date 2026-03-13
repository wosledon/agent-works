import { useEffect } from 'react';
import { useParams } from 'react-router-dom';
import { useBlogStore } from '../store/blogStore';
import { postsApi, commentsApi } from '../api';

export function PostDetailPage() {
  const { slug } = useParams<{ slug: string }>();
  const { currentPost, comments, isLoading, error, setCurrentPost, setComments, setLoading, setError } = useBlogStore();

  useEffect(() => {
    if (!slug) return;
    const loadPost = async () => {
      setLoading(true);
      try {
        const [postRes, commentsRes] = await Promise.all([
          postsApi.getBySlug(slug),
          commentsApi.getByPostId(slug),
        ]);
        setCurrentPost(postRes.data);
        setComments(commentsRes.data);
        setError(null);
      } catch (err) {
        setError(err instanceof Error ? err.message : '加载文章失败');
      } finally {
        setLoading(false);
      }
    };
    loadPost();
  }, [slug, setCurrentPost, setComments, setLoading, setError]);

  if (isLoading) {
    return (
      <div className="flex justify-center items-center py-20">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-purple-600"></div>
      </div>
    );
  }

  if (error || !currentPost) {
    return (
      <div className="text-center py-20">
        <p className="text-red-500">{error || '文章不存在'}</p>
      </div>
    );
  }

  return (
    <article className="max-w-3xl mx-auto">
      <header className="mb-8">
        <div className="flex items-center space-x-2 text-sm text-gray-500 dark:text-gray-400 mb-4">
          <time dateTime={currentPost.publishedAt}>{new Date(currentPost.publishedAt).toLocaleDateString('zh-CN')}</time>
          <span>·</span>
          <span>{currentPost.viewCount} 阅读</span>
          <span>·</span>
          <span>{currentPost.likeCount} 点赞</span>
        </div>
        
        <h1 className="text-3xl md:text-4xl font-bold text-gray-900 dark:text-white mb-4">{currentPost.title}</h1>
        
        <div className="flex items-center space-x-3">
          {currentPost.author.avatar && <img src={currentPost.author.avatar} alt={currentPost.author.name} className="w-10 h-10 rounded-full" />}
          <div>
            <p className="font-medium text-gray-900 dark:text-white">{currentPost.author.name}</p>
            <p className="text-sm text-gray-500 dark:text-gray-400">{currentPost.author.bio}</p>
          </div>
        </div>
      </header>

      {currentPost.coverImage && (
        <img src={currentPost.coverImage} alt={currentPost.title} className="w-full h-64 md:h-96 object-cover rounded-lg mb-8" />
      )}

      <div className="prose dark:prose-invert max-w-none mb-12">{currentPost.content}</div>

      <div className="flex items-center space-x-2 mb-12">
        {currentPost.tags.map((tag) => (
          <span key={tag.id} className="text-sm px-3 py-1 bg-purple-50 text-purple-600 rounded-full dark:bg-purple-900/30 dark:text-purple-400">
            {tag.name}
          </span>
        ))}
      </div>

      <section className="border-t border-gray-200 dark:border-gray-800 pt-8">
        <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-6">评论 ({comments.length})</h2>
        {comments.length === 0 ? (
          <p className="text-gray-500 dark:text-gray-400">暂无评论，来发表第一条评论吧！</p>
        ) : (
          <div className="space-y-6">
            {comments.map((comment) => (
              <div key={comment.id} className="bg-gray-50 dark:bg-gray-900 p-4 rounded-lg">
                <div className="flex items-center space-x-2 mb-2">
                  <span className="font-medium text-gray-900 dark:text-white">{comment.author.name}</span>
                  <time className="text-sm text-gray-500" dateTime={comment.createdAt}>
                    {new Date(comment.createdAt).toLocaleDateString('zh-CN')}
                  </time>
                </div>
                <p className="text-gray-700 dark:text-gray-300">{comment.content}</p>
              </div>
            ))}
          </div>
        )}
      </section>
    </article>
  );
}
