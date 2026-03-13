import { useEffect } from 'react';
import { PostCard } from '../components/PostCard';
import { useBlogStore } from '../store/blogStore';
import { postsApi } from '../api';

export function HomePage() {
  const { posts, isLoading, error, setPosts, setLoading, setError } = useBlogStore();

  useEffect(() => {
    const loadPosts = async () => {
      setLoading(true);
      try {
        const response = await postsApi.getAll();
        setPosts(response.data.data);
        setError(null);
      } catch (err) {
        setError(err instanceof Error ? err.message : '加载文章失败');
      } finally {
        setLoading(false);
      }
    };
    loadPosts();
  }, [setPosts, setLoading, setError]);

  if (isLoading) {
    return (
      <div className="flex justify-center items-center py-20">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-purple-600"></div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="text-center py-20">
        <p className="text-red-500">{error}</p>
        <button
          onClick={() => window.location.reload()}
          className="mt-4 px-4 py-2 bg-purple-600 text-white rounded-lg hover:bg-purple-700"
        >重试</button>
      </div>
    );
  }

  return (
    <div>
      <section className="mb-12">
        <h1 className="text-4xl font-bold text-gray-900 dark:text-white mb-4">欢迎来到 .NET Blog</h1>
        <p className="text-lg text-gray-600 dark:text-gray-300">探索 .NET 生态系统的最新技术文章、教程和最佳实践</p>
      </section>

      <section>
        <h2 className="text-2xl font-semibold text-gray-900 dark:text-white mb-6">最新文章</h2>
        {posts.length === 0 ? (
          <p className="text-gray-500 dark:text-gray-400">暂无文章</p>
        ) : (
          <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
            {posts.map((post) => (
              <PostCard key={post.id} post={post} />
            ))}
          </div>
        )}
      </section>
    </div>
  );
}
