import { Link } from 'react-router-dom';
import type { Post } from '../types';

interface PostCardProps {
  post: Post;
}

export function PostCard({ post }: PostCardProps) {
  return (
    <article className="bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800 overflow-hidden hover:shadow-lg transition-shadow">
      {post.coverImage && (
        <img src={post.coverImage} alt={post.title} className="w-full h-48 object-cover" />
      )}
      <div className="p-6">
        <div className="flex items-center space-x-2 text-sm text-gray-500 dark:text-gray-400 mb-3">
          <time dateTime={post.publishedAt}>{new Date(post.publishedAt).toLocaleDateString('zh-CN')}</time>
          <span>·</span>
          <span>{post.viewCount} 阅读</span>
        </div>
        
        <Link to={`/posts/${post.slug}`}>
          <h2 className="text-xl font-bold text-gray-900 dark:text-white mb-2 hover:text-purple-600 dark:hover:text-purple-400 transition-colors">
            {post.title}
          </h2>
        </Link>
        
        <p className="text-gray-600 dark:text-gray-300 mb-4 line-clamp-2">{post.excerpt}</p>
        
        <div className="flex items-center justify-between">
          <div className="flex items-center space-x-2">
            {post.tags.map((tag) => (
              <Link
                key={tag.id}
                to={`/tags/${tag.slug}`}
                className="text-xs px-2 py-1 bg-purple-50 text-purple-600 rounded-full hover:bg-purple-100 dark:bg-purple-900/30 dark:text-purple-400"
              >
                {tag.name}
              </Link>
            ))}
          </div>
          <div className="flex items-center space-x-2">
            {post.author.avatar && <img src={post.author.avatar} alt={post.author.name} className="w-6 h-6 rounded-full" />}
            <span className="text-sm text-gray-600 dark:text-gray-400">{post.author.name}</span>
          </div>
        </div>
      </div>
    </article>
  );
}
