/**
 * 首页（文章列表页）- 响应式优化
 */
import { useEffect } from 'react';
import { Link, useParams, useNavigate } from 'react-router-dom';
import { useBlogStore } from '../store';
import { Search, Eye, Heart, Tag, BookOpen } from 'lucide-react';

export function HomePage() {
  const navigate = useNavigate();
  const { tagSlug } = useParams();
  const {
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
      'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-300',
      'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-300',
      'bg-purple-100 text-purple-700 dark:bg-purple-900/30 dark:text-purple-300',
      'bg-orange-100 text-orange-700 dark:bg-orange-900/30 dark:text-orange-300',
      'bg-pink-100 text-pink-700 dark:bg-pink-900/30 dark:text-pink-300',
      'bg-cyan-100 text-cyan-700 dark:bg-cyan-900/30 dark:text-cyan-300',
    ];
    return colors[index % colors.length];
  };

  return (
    <div className="space-y-6 md:space-y-8">
      {/* 搜索和筛选区 */}
      <section className="
        bg-white dark:bg-gray-900 rounded-xl shadow-sm 
        p-4 sm:p-6 transition-colors
      ">
        <div className="
          flex flex-col sm:flex-row sm:items-center 
          justify-between gap-4
        ">
          <div className="relative flex-1 max-w-md">
            <Search className="
              absolute left-3 top-1/2 -translate-y-1/2 
              w-5 h-5 text-gray-400
            " />
            <input
              type="text"
              placeholder="搜索文章..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="
                w-full pl-10 pr-4 py-2.5 
                bg-gray-50 dark:bg-gray-800
                border border-gray-200 dark:border-gray-700 
                rounded-lg 
                text-gray-900 dark:text-gray-100
                placeholder-gray-400 dark:placeholder-gray-500
                focus:ring-2 focus:ring-blue-500 focus:border-transparent 
                outline-none transition-all
              "
            />
          </div>

          <div className="flex items-center space-x-2">
            <span className="text-sm text-gray-500 dark:text-gray-400">
              共 {filteredPosts.length} 篇文章
            </span>
          </div>
        </div>

        {/* 标签筛选 */}
        <div className="mt-4 flex flex-wrap gap-2">
          <button
            onClick={() => {
              setSelectedTag(null);
              navigate('/');
            }}
            className={`
              px-3 py-1.5 rounded-full text-sm font-medium transition-colors
              ${!selectedTag
                ? 'bg-gray-900 dark:bg-gray-100 text-white dark:text-gray-900'
                : 'bg-gray-100 dark:bg-gray-800 text-gray-700 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-700'
              }
            `}
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
              className={`
                px-3 py-1.5 rounded-full text-sm font-medium transition-colors
                ${selectedTag === tag.id
                  ? 'bg-gray-900 dark:bg-gray-100 text-white dark:text-gray-900'
                  : getTagColor(index)
                }
              `}
            >
              {tag.name}
            </button>
          ))}
        </div>
      </section>

      {/* 文章列表 */}
      <section className="space-y-4 md:space-y-6">
        {filteredPosts.length === 0 ? (
          <div className="text-center py-12 md:py-16">
            <div className="
              w-16 h-16 md:w-20 md:h-20 
              bg-gray-100 dark:bg-gray-800 
              rounded-full flex items-center justify-center 
              mx-auto mb-4
            ">
              <Search className="w-8 h-8 md:w-10 md:h-10 text-gray-400" />
            </div>
            <h3 className="
              text-lg font-medium 
              text-gray-900 dark:text-gray-100 mb-2
            ">
              没有找到相关文章
            </h3>
            <p className="text-gray-500 dark:text-gray-400">
              试试其他关键词或标签
            </p>
          </div>
        ) : (
          <div className="grid gap-4 md:gap-6">
            {filteredPosts.map((post) => (
              <article
                key={post.id}
                className="
                  bg-white dark:bg-gray-900 rounded-xl shadow-sm 
                  overflow-hidden hover:shadow-md dark:hover:shadow-gray-800/50
                  transition-all
                "
              >
                <Link 
                  to={`/post/${post.slug}`} 
                  className="flex flex-col md:flex-row"
                >
                  {/* 封面图 */}
                  {post.coverImage && (
                    <div className="md:w-64 lg:w-72 h-48 md:h-auto flex-shrink-0">
                      <img
                        src={post.coverImage}
                        alt={post.title}
                        className="w-full h-full object-cover"
                      />
                    </div>
                  )}

                  <div className="flex-1 p-4 sm:p-6">
                    {/* 标签 */}
                    <div className="flex flex-wrap gap-2 mb-3">
                      {post.tags.slice(0, 3).map((tag) => (
                        <span
                          key={tag.id}
                          className="
                            inline-flex items-center px-2 py-0.5 
                            rounded text-xs font-medium 
                            bg-blue-50 dark:bg-blue-900/30 
                            text-blue-700 dark:text-blue-300
                          "
                        >
                          <Tag className="w-3 h-3 mr-1" />
                          {tag.name}
                        </span>
                      ))}
                      {post.tags.length > 3 && (
                        <span className="text-xs text-gray-400">
                          +{post.tags.length - 3}
                        </span>
                      )}
                    </div>

                    {/* 标题 */}
                    <h2 className="
                      text-lg sm:text-xl font-bold 
                      text-gray-900 dark:text-gray-100 mb-2 
                      hover:text-blue-600 dark:hover:text-blue-400 
                      transition-colors line-clamp-2
                    ">
                      {post.title}
                    </h2>

                    {/* 摘要 */}
                    <p className="
                      text-gray-600 dark:text-gray-400 
                      line-clamp-2 mb-4 text-sm sm:text-base
                    ">
                      {post.excerpt}
                    </p>

                    {/* 作者和元信息 */}
                    <div className="
                      flex items-center justify-between 
                      flex-wrap gap-2
                    ">
                      <div className="flex items-center space-x-2 sm:space-x-3">
                        <img
                          src={post.author.avatar || 'https://api.dicebear.com/7.x/avataaars/svg?seed=guest'}
                          alt={post.author.name}
                          className="w-7 h-7 sm:w-8 sm:h-8 rounded-full"
                        />
                        <div>
                          <p className="text-sm font-medium text-gray-900 dark:text-gray-100">
                            {post.author.name}
                          </p>
                          <p className="text-xs text-gray-500 dark:text-gray-400">
                            {formatDate(post.publishedAt)}
                          </p>
                        </div>
                      </div>

                      <div className="flex items-center space-x-3 sm:space-x-4 text-sm text-gray-500 dark:text-gray-400">
                        <span className="flex items-center">
                          <Eye className="w-4 h-4 mr-1" />
                          <span className="hidden sm:inline">{post.viewCount}</span>
                        </span>
                        <span className="flex items-center">
                          <Heart className="w-4 h-4 mr-1" />
                          <span className="hidden sm:inline">{post.likeCount}</span>
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

      {/* 空状态提示 */}
      {filteredPosts.length > 0 && filteredPosts.length < 3 && (
        <div className="text-center py-8">
          <div className="
            inline-flex items-center space-x-2 
            text-gray-400 dark:text-gray-500
          ">
            <BookOpen className="w-5 h-5" />
            <span>已经到底了~</span>
          </div>
        </div>
      )}
    </div>
  );
}
