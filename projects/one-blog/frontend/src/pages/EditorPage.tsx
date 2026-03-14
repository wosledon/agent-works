/**
 * 文章编辑器页面 - 移动端适配 + 暗色模式
 */
import { useState, useEffect } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useBlogStore, useAuthStore } from '../store';
import { MarkdownEditor } from '../components/MarkdownEditor';
import { Save, ArrowLeft, Loader2, Image, Tag as TagIcon, ChevronDown, ChevronUp } from 'lucide-react';

export function EditorPage() {
  const navigate = useNavigate();
  const { slug } = useParams<{ slug: string }>();
  const { user } = useAuthStore();
  const { tags, createPost, updatePost, getPostBySlug } = useBlogStore();

  const isEditing = !!slug;
  const existingPost = slug ? getPostBySlug(slug) : null;

  const [title, setTitle] = useState('');
  const [content, setContent] = useState('');
  const [excerpt, setExcerpt] = useState('');
  const [coverImage, setCoverImage] = useState('');
  const [selectedTags, setSelectedTags] = useState<string[]>([]);
  const [isSaving, setIsSaving] = useState(false);
  const [showSettings, setShowSettings] = useState(false);

  // 加载现有文章数据
  useEffect(() => {
    if (existingPost) {
      setTitle(existingPost.title);
      setContent(existingPost.content);
      setExcerpt(existingPost.excerpt);
      setCoverImage(existingPost.coverImage || '');
      setSelectedTags(existingPost.tags.map((t) => t.id));
    }
  }, [existingPost]);

  // 生成 slug
  const generateSlug = (text: string) => {
    return text
      .toLowerCase()
      .replace(/[^\w\s-]/g, '')
      .replace(/\s+/g, '-')
      .substring(0, 50);
  };

  // 自动从内容生成摘要
  const generateExcerpt = (text: string) => {
    const plainText = text.replace(/[#*`,[\]()]/g, '');
    return plainText.substring(0, 150) + (plainText.length > 150 ? '...' : '');
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!title.trim() || !content.trim()) {
      alert('标题和内容不能为空');
      return;
    }

    setIsSaving(true);

    // 模拟 API 延迟
    await new Promise((resolve) => setTimeout(resolve, 800));

    const postData = {
      title: title.trim(),
      slug: generateSlug(title),
      excerpt: excerpt.trim() || generateExcerpt(content),
      content: content.trim(),
      coverImage: coverImage.trim() || undefined,
      author: {
        id: user?.id || '1',
        name: user?.name || '匿名用户',
        avatar: user?.avatar,
      },
      tags: tags.filter((t) => selectedTags.includes(t.id)),
      publishedAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    };

    try {
      if (isEditing && existingPost) {
        updatePost(existingPost.id, postData);
        navigate(`/post/${postData.slug}`);
      } else {
        const newPost = createPost(postData);
        navigate(`/post/${newPost.slug}`);
      }
    } catch {
      alert('保存失败，请重试');
    } finally {
      setIsSaving(false);
    }
  };

  const toggleTag = (tagId: string) => {
    setSelectedTags((prev) =>
      prev.includes(tagId)
        ? prev.filter((id) => id !== tagId)
        : [...prev, tagId]
    );
  };

  return (
    <div className="max-w-5xl mx-auto">
      {/* 顶部工具栏 - 移动端适配 */}
      <div className="flex items-center justify-between mb-4 md:mb-6">
        <button
          onClick={() => navigate(-1)}
          className="flex items-center text-gray-500 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-100 transition-colors"
        >
          <ArrowLeft className="w-5 h-5 mr-1" />
          <span className="hidden sm:inline">返回</span>
        </button>

        {/* 移动端：设置按钮 */}
        <div className="flex items-center space-x-2">
          <button
            onClick={() => setShowSettings(!showSettings)}
            className="md:hidden flex items-center space-x-1 px-3 py-2 text-sm text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-100 hover:bg-gray-100 dark:hover:bg-gray-800 rounded-lg transition-colors"
          >
            {showSettings ? <ChevronUp className="w-4 h-4" /> : <ChevronDown className="w-4 h-4" />}
            <span>设置</span>
          </button>
          <button
            onClick={handleSubmit}
            disabled={isSaving}
            className="flex items-center px-4 md:px-6 py-2 bg-blue-600 text-white font-medium rounded-lg hover:bg-blue-700 focus:ring-4 focus:ring-blue-200 disabled:opacity-50 disabled:cursor-not-allowed transition-all text-sm md:text-base"
          >
            {isSaving ? (
              <>
                <Loader2 className="w-4 h-4 md:w-5 md:h-5 mr-1 md:mr-2 animate-spin" />
                <span className="hidden sm:inline">保存中...</span>
                <span className="sm:hidden">...</span>
              </>
            ) : (
              <>
                <Save className="w-4 h-4 md:w-5 md:h-5 mr-1 md:mr-2" />
                {isEditing ? '更新' : '发布'}
              </>
            )}
          </button>
        </div>
      </div>

      <form onSubmit={handleSubmit} className="space-y-4 md:space-y-6">
        {/* 标题输入 - 始终在顶部 */}
        <div className="bg-white dark:bg-gray-900 rounded-xl shadow-sm p-4 md:p-6 transition-colors">
          <div>
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
              文章标题
            </label>
            <input
              type="text"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              placeholder="输入文章标题..."
              className="w-full px-4 py-3 text-lg md:text-xl font-bold bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent outline-none transition-all text-gray-900 dark:text-gray-100 placeholder-gray-400 dark:placeholder-gray-500"
            />
          </div>
        </div>

        {/* 设置面板 - 桌面端始终显示，移动端可折叠 */}
        <div className={`bg-white dark:bg-gray-900 rounded-xl shadow-sm p-4 md:p-6 transition-colors ${!showSettings ? 'hidden md:block' : ''}`}>
          <div className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                文章摘要
              </label>
              <textarea
                value={excerpt}
                onChange={(e) => setExcerpt(e.target.value)}
                placeholder="输入文章摘要（可选，留空将自动生成）..."
                rows={2}
                className="w-full px-4 py-3 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent outline-none transition-all resize-none text-gray-900 dark:text-gray-100 placeholder-gray-400 dark:placeholder-gray-500 text-sm"
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                封面图片 URL
              </label>
              <div className="relative">
                <Image className="absolute left-3 top-1/2 -translate-y-1/2 w-5 h-5 text-gray-400" />
                <input
                  type="url"
                  value={coverImage}
                  onChange={(e) => setCoverImage(e.target.value)}
                  placeholder="https://example.com/image.jpg"
                  className="w-full pl-10 pr-4 py-3 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent outline-none transition-all text-gray-900 dark:text-gray-100 placeholder-gray-400 dark:placeholder-gray-500 text-sm"
                />
              </div>
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                标签
              </label>
              <div className="flex flex-wrap gap-2">
                {tags.map((tag) => (
                  <button
                    key={tag.id}
                    type="button"
                    onClick={() => toggleTag(tag.id)}
                    className={`inline-flex items-center px-2.5 md:px-3 py-1.5 rounded-full text-xs md:text-sm font-medium transition-colors ${
                      selectedTags.includes(tag.id)
                        ? 'bg-blue-600 text-white'
                        : 'bg-gray-100 dark:bg-gray-800 text-gray-700 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-700'
                    }`}
                  >
                    <TagIcon className="w-3 h-3 mr-1" />
                    {tag.name}
                  </button>
                ))}
              </div>
            </div>
          </div>
        </div>

        {/* Markdown 编辑器 */}
        <div>
          <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
            文章内容
          </label>
          <MarkdownEditor
            value={content}
            onChange={setContent}
            placeholder="开始写作... 支持 Markdown 语法"
            height={typeof window !== 'undefined' && window.innerWidth < 768 ? 400 : 600}
          />
        </div>
      </form>
    </div>
  );
}
