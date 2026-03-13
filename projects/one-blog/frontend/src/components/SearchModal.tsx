/**
 * 搜索对话框组件
 */
import { useState, useEffect, useRef, useCallback } from 'react';
import { Search, X, Clock, ArrowRight, FileText, Loader2 } from 'lucide-react';
import { useBlogStore } from '../store';
import type { Post } from '../types';

interface SearchModalProps {
  isOpen: boolean;
  onClose: () => void;
}

interface SearchResult {
  post: Post;
  score: number;
  matches: {
    title?: boolean;
    excerpt?: boolean;
    content?: boolean;
    tags?: boolean;
  };
}

export function SearchModal({ isOpen, onClose }: SearchModalProps) {
  const [query, setQuery] = useState('');
  const [results, setResults] = useState<SearchResult[]>([]);
  const [selectedIndex, setSelectedIndex] = useState(0);
  const [isSearching, setIsSearching] = useState(false);
  const [recentSearches, setRecentSearches] = useState<string[]>([]);
  const inputRef = useRef<HTMLInputElement>(null);
  const { posts } = useBlogStore();

  // 加载最近搜索
  useEffect(() => {
    const saved = localStorage.getItem('oneblog-recent-searches');
    if (saved) {
      try {
        setRecentSearches(JSON.parse(saved));
      } catch {}
    }
  }, []);

  // 保存最近搜索
  const saveRecentSearch = useCallback((term: string) => {
    if (!term.trim()) return;
    const newSearches = [term, ...recentSearches.filter((s) => s !== term)].slice(0, 5);
    setRecentSearches(newSearches);
    localStorage.setItem('oneblog-recent-searches', JSON.stringify(newSearches));
  }, [recentSearches]);

  // 搜索逻辑
  useEffect(() => {
    if (!query.trim()) {
      setResults([]);
      setIsSearching(false);
      return;
    }

    setIsSearching(true);
    const searchTerm = query.toLowerCase();

    // 简单的模糊搜索
    const searchResults: SearchResult[] = posts
      .map((post) => {
        const titleMatch = post.title.toLowerCase().includes(searchTerm);
        const excerptMatch = post.excerpt.toLowerCase().includes(searchTerm);
        const contentMatch = post.content.toLowerCase().includes(searchTerm);
        const tagsMatch = post.tags.some((t) => t.name.toLowerCase().includes(searchTerm));

        if (!titleMatch && !excerptMatch && !contentMatch && !tagsMatch) {
          return null;
        }

        // 计算匹配分数
        let score = 0;
        if (titleMatch) score += 10;
        if (excerptMatch) score += 5;
        if (contentMatch) score += 3;
        if (tagsMatch) score += 2;

        return {
          post,
          score,
          matches: {
            title: titleMatch,
            excerpt: excerptMatch,
            content: contentMatch,
            tags: tagsMatch,
          },
        };
      })
      .filter(Boolean as any)
      .sort((a, b) => b!.score - a!.score) as SearchResult[];

    // 模拟搜索延迟
    const timer = setTimeout(() => {
      setResults(searchResults);
      setIsSearching(false);
      setSelectedIndex(0);
    }, 150);

    return () => clearTimeout(timer);
  }, [query, posts]);

  // 聚焦输入框
  useEffect(() => {
    if (isOpen) {
      setTimeout(() => inputRef.current?.focus(), 100);
    }
  }, [isOpen]);

  // 键盘导航
  const handleKeyDown = (e: React.KeyboardEvent) => {
    switch (e.key) {
      case 'Escape':
        onClose();
        break;
      case 'ArrowDown':
        e.preventDefault();
        setSelectedIndex((prev) =>
          Math.min(prev + 1, results.length - 1)
        );
        break;
      case 'ArrowUp':
        e.preventDefault();
        setSelectedIndex((prev) => Math.max(prev - 1, 0));
        break;
      case 'Enter':
        if (results[selectedIndex]) {
          handleSelect(results[selectedIndex].post);
        }
        break;
    }
  };

  const handleSelect = (post: Post) => {
    saveRecentSearch(query);
    window.location.href = `/post/${post.slug}`;
    onClose();
  };

  const handleRecentSearchClick = (term: string) => {
    setQuery(term);
  };

  const clearRecentSearches = () => {
    setRecentSearches([]);
    localStorage.removeItem('oneblog-recent-searches');
  };

  const highlightMatch = (text: string, term: string) => {
    if (!term.trim()) return text;
    const regex = new RegExp(`(${term.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')})`, 'gi');
    const parts = text.split(regex);
    return parts.map((part, i) =>
      regex.test(part) ? (
        <mark key={i} className="bg-yellow-200 dark:bg-yellow-800 text-inherit">{part}</mark>
      ) : (
        part
      )
    );
  };

  if (!isOpen) return null;

  return (
    <>
      {/* 背景遮罩 */}
      <div
        className="fixed inset-0 bg-black/50 backdrop-blur-sm z-50"
        onClick={onClose}
      />

      {/* 搜索框 */}
      <div className="fixed inset-x-4 top-[10vh] md:inset-x-auto md:left-1/2 md:-translate-x-1/2 md:w-full md:max-w-2xl z-50">
        <div className="bg-white dark:bg-gray-900 rounded-2xl shadow-2xl overflow-hidden">
          {/* 输入框 */}
          <div className="flex items-center px-4 py-3 border-b border-gray-100 dark:border-gray-800">
            <Search className="w-5 h-5 text-gray-400 mr-3 flex-shrink-0" />
            <input
              ref={inputRef}
              type="text"
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              onKeyDown={handleKeyDown}
              placeholder="搜索文章、标签..."
              className="
                flex-1 bg-transparent outline-none text-lg
                text-gray-900 dark:text-gray-100 placeholder-gray-400
              "
            />
            {isSearching ? (
              <Loader2 className="w-5 h-5 text-gray-400 animate-spin flex-shrink-0" />
            ) : query ? (
              <button
                onClick={() => setQuery('')}
                className="p-1 hover:bg-gray-100 dark:hover:bg-gray-800 rounded-full transition-colors"
              >
                <X className="w-5 h-5 text-gray-400" />
              </button>
            ) : (
              <kbd className="hidden md:inline-block px-2 py-1 text-xs bg-gray-100 dark:bg-gray-800 rounded">
                ESC
              </kbd>
            )}
          </div>

          {/* 结果区域 */}
          <div className="max-h-[60vh] overflow-y-auto">
            {/* 最近搜索 */}
            {!query && recentSearches.length > 0 && (
              <div className="p-4">
                <div className="flex items-center justify-between mb-3">
                  <span className="text-sm font-medium text-gray-500">最近搜索</span>
                  <button
                    onClick={clearRecentSearches}
                    className="text-xs text-gray-400 hover:text-gray-600"
                  >
                    清除
                  </button>
                </div>
                <div className="flex flex-wrap gap-2">
                  {recentSearches.map((term) => (
                    <button
                      key={term}
                      onClick={() => handleRecentSearchClick(term)}
                      className="
                        flex items-center px-3 py-1.5 text-sm
                        bg-gray-100 dark:bg-gray-800 text-gray-700 dark:text-gray-300
                        rounded-full hover:bg-gray-200 dark:hover:bg-gray-700
                        transition-colors
                      "
                    >
                      <Clock className="w-3 h-3 mr-1.5 text-gray-400" />
                      {term}
                    </button>
                  ))}
                </div>
              </div>
            )}

            {/* 搜索结果 */}
            {query && (
              <>
                {results.length === 0 && !isSearching ? (
                  <div className="p-8 text-center">
                    <div className="w-12 h-12 bg-gray-100 dark:bg-gray-800 rounded-full flex items-center justify-center mx-auto mb-3">
                      <Search className="w-6 h-6 text-gray-400" />
                    </div>
                    <p className="text-gray-500">未找到相关文章</p>
                    <p className="text-sm text-gray-400 mt-1">
                      试试其他关键词
                    </p>
                  </div>
                ) : (
                  <ul className="py-2">
                    {results.map(({ post, matches }, index) => (
                      <li
                        key={post.id}
                        onClick={() => handleSelect(post)}
                        onMouseEnter={() => setSelectedIndex(index)}
                        className={`
                          px-4 py-3 cursor-pointer flex items-start space-x-3
                          transition-colors
                          ${index === selectedIndex
                            ? 'bg-blue-50 dark:bg-blue-900/20'
                            : 'hover:bg-gray-50 dark:hover:bg-gray-800'
                          }
                        `}
                      >
                        <FileText className="w-5 h-5 text-gray-400 mt-0.5 flex-shrink-0" />
                        <div className="flex-1 min-w-0">
                          <h4 className="font-medium text-gray-900 dark:text-gray-100 truncate">
                            {matches.title
                              ? highlightMatch(post.title, query)
                              : post.title}
                          </h4>
                          <p className="text-sm text-gray-500 truncate mt-0.5">
                            {matches.excerpt
                              ? highlightMatch(post.excerpt, query)
                              : post.excerpt}
                          </p>
                          {matches.tags && (
                            <div className="flex items-center space-x-1 mt-1">
                              <span className="text-xs text-gray-400">标签匹配:</span>
                              {post.tags
                                .filter((t) =>
                                  t.name.toLowerCase().includes(query.toLowerCase())
                                )
                                .map((tag) => (
                                  <span
                                    key={tag.id}
                                    className="text-xs px-1.5 py-0.5 bg-blue-100 dark:bg-blue-900/50 text-blue-700 dark:text-blue-300 rounded"
                                  >
                                    {highlightMatch(tag.name, query)}
                                  </span>
                                ))}
                            </div>
                          )}
                        </div>
                        <ArrowRight
                          className={`
                            w-4 h-4 mt-1 flex-shrink-0 transition-opacity
                            ${index === selectedIndex ? 'opacity-100' : 'opacity-0'}
                          `}
                        />
                      </li>
                    ))}
                  </ul>
                )}
              </>
            )}

            {/* 快捷提示 */}
            <div className="px-4 py-3 bg-gray-50 dark:bg-gray-800/50 border-t border-gray-100 dark:border-gray-800">
              <div className="flex items-center justify-center md:justify-start space-x-4 text-xs text-gray-400">
                <span className="flex items-center">
                  <kbd className="px-1.5 py-0.5 bg-white dark:bg-gray-800 rounded border mr-1">↑↓</kbd>
                  选择
                </span>
                <span className="flex items-center">
                  <kbd className="px-1.5 py-0.5 bg-white dark:bg-gray-800 rounded border mr-1">↵</kbd>
                  确认
                </span>
                <span className="flex items-center">
                  <kbd className="px-1.5 py-0.5 bg-white dark:bg-gray-800 rounded border mr-1">ESC</kbd>
                  关闭
                </span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </>
  );
}
