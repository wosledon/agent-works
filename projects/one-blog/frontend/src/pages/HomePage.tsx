/**
 * 首页（文章列表页）
 */
import { useEffect } from 'react';
import { Link, useParams, useNavigate } from 'react-router-dom';
import { useBlogStore } from '../store';
import { Search, Clock, Eye, Heart, Tag, ChevronRight } from 'lucide-react';

export function HomePage() {
  const navigate = useNavigate();
  const { tagSlug } = useParams();
  const {
    posts,
    tags,
    searchQuery,
    selectedTag,
    setSearchQuery,
    setSelectedTag,
    getFilteredPosts,
  } = useBlogStore();

  // 处理标签筛选
  useEffect(() => {
    if (tagSlug) {
      const tag = tags.find((t) => t.slug === tagSlug);
      setSelectedTag(tag?.id || null);
    } else {
      setSelectedTag(null);
    }
  }, [tagSlug, tags, setSelectedTag]);

  const filteredPosts = getFilteredPosts();

  // 格式化日期
  const formatDate = (dateStr: string) => {
    const date = new Date(dateStr);
    return date.toLocaleDateString('zh-CN', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
    });
  };

  // 获取标签颜色
  const getTagColor = (index: number) => {
    const colors = [
      'bg-blue-100 text-blue-700',
      'bg-green-100 text-green-700',
      'bg-purple-100 text-purple-700',
      'bg-orange-100 text-orange-700',
      'bg-pink-100 text-pink-700',
      'bg-cyan-100 text-cyan-700',
    ];
    return colors[index % colors.length];
  };

  return (
    <div className="space-y-8">
      {/* 搜索和筛选区 */}
      <section className="bg-white rounded-xl shadow-sm p-6">
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
          <div className="relative flex-1 max-w-md">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-5 h-5 text-gray-400" />
            <input
              type="text"
              placeholder="搜索文章..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="w-full pl-10 pr-4 py-2 border border-gray-200 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent outline-none transition-all"
            />
          </div>

          <div className="flex items-center space-x-2">
            <span className="text-sm text-gray-500">共 {filteredPosts.length} 篇文章</span>
          </div>
        </div>

        {/* 标签筛选 */}
        <div className="mt-4 flex flex-wrap gap-2">
          <button
            onClick={() => {
              setSelectedTag(null);
              navigate('/');
            }}
            className={`px-3 py-1 rounded-full text-sm font-medium transition-colors ${
              !selectedTag
                ? 'bg-gray-900 text-white'
                : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
            }`}
          >
            全部
          </button>
          {tags.map((tag, index) => (
            <button
              key={tag.id}
              onClick={() => {
                setSelectedTag(tag.id);
                navigate(`/tag/${tag.slug}`);
              }}
              className={`px-3 py-1 rounded-full text-sm font-medium transition-colors ${
                selectedTag === tag.id
                  ? 'bg-gray-900 text-white'
                  : getTagColor(index)
              }`}
            >
              {tag.name}
            </button>
          ))}
        </div>
      </section>

      {/* 文章列表 */}
      <section className="space-y-6">
        {filteredPosts.length === 0 ? (
          <div className="text-center py-16">
            <div className="w-20 h-20 bg-gray-100 rounded-full flex items-center justify-center mx-auto mb-4">
              <Search className="w-10 h-10 text-gray-400" />
            </div>
            <h3 className="text-lg font-medium text-gray-900 mb-2">没有找到相关文章</h3>
            <p className="text-gray-500">试试其他关键词或标签</p>
          </div>
        ) : (
          <div className="grid gap-6">
            {filteredPosts.map((post) => (
              <article
                key={post.id}
                className="bg-white rounded-xl shadow-sm overflow-hidden hover:shadow-md transition-shadow"
              >
                <Link to={`/post/${post.slug}`} className="flex flex-col md:flex-row"
                >
                  {/* 封面图 */}
                  {post.coverImage && (
                    <div className="md:w-64 h-48 md:h-auto flex-shrink-0">
                      <img
                        src={post.coverImage}
                        alt={post.title}
                        className="w-full h-full object-cover"
                      />
                    </div>
                  )}

                  <div className="flex-1 p-6">
                    {/* 标签 */}
                    <div className="flex flex-wrap gap-2 mb-3">
                      {post.tags.map((tag) => (
                        <span
                          key={tag.id}
                          className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-blue-50 text-blue-700"
                        >
                          <Tag className="w-3 h-3 mr-1" />
                          {tag.name}
                        </span>
                      ))}
                    </div>

                    {/* 标题 */}
                    <h2 className="text-xl font-bold text-gray-900 mb-2 hover:text-blue-600 transition-colors">
                      {post.title}
                    </h2>

                    {/* 摘要 */}
                    <p className="text-gray-600 line-clamp-2 mb-4">{post.excerpt}</p>

                    {/* 作者和元信息 */}
                    <div className="flex items-center justify-between">
                      <div className="flex items-center space-x-3">
                        <img
                          src={post.author.avatar || 'https://api.dicebear.com/7.x/avataaars/svg?seed=guest'}
                          alt={post.author.name}
                          className="w-8 h-8 rounded-full"
                        />
                        <div>
                          <p className="text-sm font-medium text-gray-900">{post.author.name}</p>
                          <p className="text-xs text-gray-500">{formatDate(post.publishedAt)}</p>
                        </div>
                      </div>

                      <div className="flex items-center space-x-4 text-sm text-gray-500">
                        <span className="flex items-center">
                          <Eye className="w-4 h-4 mr-1" />
                          {post.viewCount}
                        </span>
                        <span className="flex items-center">
                          <Heart className="w-4 h-4 mr-1" />
                          {post.likeCount}
                        </span>
                      </div>
                    </div>
                  </div>
                </Link>
              </article>
            ))}
          </div>
        )}
      </section>
    </div>
  );
}
