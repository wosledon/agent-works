/**
 * Zustand 状态管理 - 博客状态（增强版）
 */
import { create } from 'zustand';
import type { Post, Comment, Author, Tag } from '../types';

// 增强的文章状态
interface BlogState {
  // 状态
  posts: Post[];
  currentPost: Post | null;
  comments: Comment[];
  tags: Tag[];
  isLoading: boolean;
  error: string | null;
  
  // UI 状态
  searchQuery: string;
  selectedTag: string | null;
  currentPage: number;
  pageSize: number;
  totalPosts: number;
  
  // 动作
  setPosts: (posts: Post[]) => void;
  setCurrentPost: (post: Post | null) => void;
  setComments: (comments: Comment[]) => void;
  setTags: (tags: Tag[]) => void;
  setLoading: (loading: boolean) => void;
  setError: (error: string | null) => void;
  
  // UI 动作
  setSearchQuery: (query: string) => void;
  setSelectedTag: (tagId: string | null) => void;
  setCurrentPage: (page: number) => void;
  
  // 文章操作
  createPost: (post: Omit<Post, 'id' | 'viewCount' | 'likeCount'> & { publishedAt?: string; updatedAt?: string }) => Post;
  updatePost: (id: string, updates: Partial<Post>) => void;
  deletePost: (id: string) => void;
  likePost: (id: string) => void;
  
  // 派生状态
  getPostById: (id: string) => Post | undefined;
  getPostBySlug: (slug: string) => Post | undefined;
  getFilteredPosts: () => Post[];
}

// 模拟标签数据
const MOCK_TAGS: Tag[] = [
  { id: '1', name: 'React', slug: 'react' },
  { id: '2', name: 'TypeScript', slug: 'typescript' },
  { id: '3', name: 'Node.js', slug: 'nodejs' },
  { id: '4', name: '前端开发', slug: 'frontend' },
  { id: '5', name: '后端开发', slug: 'backend' },
  { id: '6', name: '数据库', slug: 'database' },
  { id: '7', name: 'DevOps', slug: 'devops' },
  { id: '8', name: '设计', slug: 'design' },
];

// 模拟作者数据
const MOCK_AUTHORS: Author[] = [
  {
    id: '1',
    name: '张三',
    avatar: 'https://api.dicebear.com/7.x/avataaars/svg?seed=zhangsan',
    bio: '全栈开发者',
  },
  {
    id: '2',
    name: '李四',
    avatar: 'https://api.dicebear.com/7.x/avataaars/svg?seed=lisi',
    bio: '前端专家',
  },
  {
    id: '3',
    name: '王五',
    avatar: 'https://api.dicebear.com/7.x/avataaars/svg?seed=wangwu',
    bio: '技术博主',
  },
];

// 模拟文章数据
const MOCK_POSTS: Post[] = [
  {
    id: '1',
    title: 'React 19 新特性详解',
    slug: 'react-19-new-features',
    excerpt: 'React 19 带来了许多令人兴奋的新特性，包括服务器组件、Actions、改进的表单处理等。本文将详细介绍这些新特性及其使用方法。',
    content: `# React 19 新特性详解

React 19 是 React 团队推出的最新主要版本，带来了许多令人兴奋的新特性。

## 1. 服务器组件 (Server Components)

服务器组件允许开发者在服务器端渲染组件，减少客户端 JavaScript 体积。

### 优点
- 减少客户端 JavaScript 体积
- 直接访问后端资源
- 更好的性能表现

## 2. Actions

Actions 提供了一种处理表单和数据变更的新方式。

\`\`\`jsx
function UpdateName() {
  async function handleSubmit(formData) {
    'use server';
    await updateName(formData.get('name'));
  }
  
  return (
    <form action={handleSubmit}>
      <input name="name" />
      <button type="submit">更新</button>
    </form>
  );
}
\`\`\`

## 3. 改进的表单处理

React 19 原生支持表单处理，无需额外的状态管理。

## 总结

React 19 的这些新特性将大大简化 Web 应用的开发流程。`,
    coverImage: 'https://images.unsplash.com/photo-1633356122544-f134324a6cee?w=800&auto=format&fit=crop',
    author: MOCK_AUTHORS[0],
    tags: [MOCK_TAGS[0], MOCK_TAGS[1]],
    publishedAt: '2024-03-10T08:00:00Z',
    updatedAt: '2024-03-10T08:00:00Z',
    viewCount: 1234,
    likeCount: 89,
  },
  {
    id: '2',
    title: 'TypeScript 最佳实践指南',
    slug: 'typescript-best-practices',
    excerpt: '本文总结了在使用 TypeScript 开发项目时的最佳实践，包括类型定义、接口设计、泛型使用等方面的建议。',
    content: `# TypeScript 最佳实践指南

TypeScript 已成为现代前端开发的标配。本文分享一些最佳实践。

## 1. 严格模式配置

始终启用严格模式：

\`\`\`json
{
  "compilerOptions": {
    "strict": true
  }
}
\`\`\`

## 2. 类型定义优先

优先使用类型别名还是接口？

- 对象形状：使用接口
- 联合类型：使用类型别名

## 3. 泛型的使用

合理使用泛型提高代码复用性。`,
    coverImage: 'https://images.unsplash.com/photo-1516116216624-53e697fedbea?w=800&auto=format&fit=crop',
    author: MOCK_AUTHORS[1],
    tags: [MOCK_TAGS[1]],
    publishedAt: '2024-03-08T10:30:00Z',
    updatedAt: '2024-03-09T14:20:00Z',
    viewCount: 892,
    likeCount: 67,
  },
  {
    id: '3',
    title: 'Node.js 性能优化技巧',
    slug: 'nodejs-performance-tips',
    excerpt: 'Node.js 应用的性能优化是一个重要话题。本文将介绍一些实用的性能优化技巧，帮助你的应用运行得更快。',
    content: `# Node.js 性能优化技巧

性能优化是后端开发的重要课题。

## 1. 使用集群模式

利用多核 CPU：

\`\`\`javascript
const cluster = require('cluster');
const os = require('os');

if (cluster.isMaster) {
  const numCPUs = os.cpus().length;
  for (let i = 0; i < numCPUs; i++) {
    cluster.fork();
  }
}
\`\`\`

## 2. 数据库连接池

合理配置连接池大小。`,
    coverImage: 'https://images.unsplash.com/photo-1627398242454-45a1465c2479?w=800&auto=format&fit=crop',
    author: MOCK_AUTHORS[2],
    tags: [MOCK_TAGS[2], MOCK_TAGS[4]],
    publishedAt: '2024-03-05T16:00:00Z',
    updatedAt: '2024-03-05T16:00:00Z',
    viewCount: 756,
    likeCount: 45,
  },
  {
    id: '4',
    title: '现代 CSS 布局技巧',
    slug: 'modern-css-layout-tips',
    excerpt: 'CSS Grid 和 Flexbox 已经成为现代网页布局的核心技术。本文将深入探讨如何使用这些技术创建响应式布局。',
    content: `# 现代 CSS 布局技巧

CSS 布局技术已经发生了巨大变化。

## Grid 布局

\`\`\`css
.container {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
  gap: 20px;
}
\`\`\`

## Flexbox 布局

弹性盒子布局：

\`\`\`css
.flex-container {
  display: flex;
  justify-content: center;
  align-items: center;
}
\`\`\``,    coverImage: 'https://images.unsplash.com/photo-1507721999472-8ed4421c4af2?w=800&auto=format&fit=crop',
    author: MOCK_AUTHORS[1],
    tags: [MOCK_TAGS[3]],
    publishedAt: '2024-03-01T09:00:00Z',
    updatedAt: '2024-03-02T11:30:00Z',
    viewCount: 543,
    likeCount: 32,
  },
  {
    id: '5',
    title: 'PostgreSQL 数据库设计指南',
    slug: 'postgresql-database-design',
    excerpt: '良好的数据库设计是应用成功的关键。本文将介绍 PostgreSQL 数据库设计的最佳实践。',
    content: `# PostgreSQL 数据库设计指南

数据库设计的重要性不言而喻。

## 规范化与反规范化

- 第三范式是基础
- 适当反规范化提升查询性能

## 索引策略

合理使用索引：

\`\`\`sql
CREATE INDEX idx_users_email ON users(email);
CREATE INDEX idx_posts_created_at ON posts(created_at DESC);
\`\`\``,    coverImage: 'https://images.unsplash.com/photo-1544383835-bda2bc66a55d?w=800&auto=format&fit=crop',
    author: MOCK_AUTHORS[0],
    tags: [MOCK_TAGS[5]],
    publishedAt: '2024-02-28T14:00:00Z',
    updatedAt: '2024-02-28T14:00:00Z',
    viewCount: 678,
    likeCount: 56,
  },
  {
    id: '6',
    title: 'Docker 容器化部署实践',
    slug: 'docker-deployment-practice',
    excerpt: 'Docker 已经成为现代应用部署的标准工具。本文将介绍如何使用 Docker 容器化部署你的应用。',
    content: `# Docker 容器化部署实践

Docker 简化了应用部署流程。

## Dockerfile 编写

\`\`\`dockerfile
FROM node:18-alpine
WORKDIR /app
COPY package*.json ./
RUN npm ci --only=production
COPY . .
EXPOSE 3000
CMD ["node", "server.js"]
\`\`\`

## Docker Compose

多容器编排：

\`\`\`yaml
version: '3.8'
services:
  app:
    build: .
    ports:
      - "3000:3000"
\`\`\``,    coverImage: 'https://images.unsplash.com/photo-1605745341112-85968b19335b?w=800&auto=format&fit=crop',
    author: MOCK_AUTHORS[2],
    tags: [MOCK_TAGS[6]],
    publishedAt: '2024-02-25T11:00:00Z',
    updatedAt: '2024-02-26T09:30:00Z',
    viewCount: 445,
    likeCount: 28,
  },
];

// 模拟评论数据
const MOCK_COMMENTS: Comment[] = [
  {
    id: '1',
    content: '写得很详细，学到了很多！',
    author: MOCK_AUTHORS[1],
    postId: '1',
    createdAt: '2024-03-10T10:00:00Z',
  },
  {
    id: '2',
    content: 'React 19 的 Actions 确实很实用',
    author: MOCK_AUTHORS[2],
    postId: '1',
    createdAt: '2024-03-10T12:30:00Z',
  },
];

export const useBlogStore = create<BlogState>((set, get) => ({
  posts: MOCK_POSTS,
  currentPost: null,
  comments: MOCK_COMMENTS,
  tags: MOCK_TAGS,
  isLoading: false,
  error: null,
  searchQuery: '',
  selectedTag: null,
  currentPage: 1,
  pageSize: 10,
  totalPosts: MOCK_POSTS.length,
  
  setPosts: (posts) => set({ posts, totalPosts: posts.length }),
  setCurrentPost: (post) => set({ currentPost: post }),
  setComments: (comments) => set({ comments }),
  setTags: (tags) => set({ tags }),
  setLoading: (isLoading) => set({ isLoading }),
  setError: (error) => set({ error }),
  
  setSearchQuery: (searchQuery) => set({ searchQuery, currentPage: 1 }),
  setSelectedTag: (selectedTag) => set({ selectedTag, currentPage: 1 }),
  setCurrentPage: (currentPage) => set({ currentPage }),
  
  createPost: (postData) => {
    const newPost: Post = {
      id: Date.now().toString(),
      ...postData,
      publishedAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
      viewCount: 0,
      likeCount: 0,
    };
    set((state) => ({
      posts: [newPost, ...state.posts],
      totalPosts: state.totalPosts + 1,
    }));
    return newPost;
  },
  
  updatePost: (id, updates) => {
    set((state) => ({
      posts: state.posts.map((post) =>
        post.id === id
          ? { ...post, ...updates, updatedAt: new Date().toISOString() }
          : post
      ),
      currentPost:
        state.currentPost?.id === id
          ? { ...state.currentPost, ...updates, updatedAt: new Date().toISOString() }
          : state.currentPost,
    }));
  },
  
  deletePost: (id) => {
    set((state) => ({
      posts: state.posts.filter((post) => post.id !== id),
      totalPosts: state.totalPosts - 1,
      currentPost: state.currentPost?.id === id ? null : state.currentPost,
    }));
  },
  
  likePost: (id) => {
    set((state) => ({
      posts: state.posts.map((post) =>
        post.id === id ? { ...post, likeCount: post.likeCount + 1 } : post
      ),
      currentPost:
        state.currentPost?.id === id
          ? { ...state.currentPost, likeCount: state.currentPost.likeCount + 1 }
          : state.currentPost,
    }));
  },
  
  getPostById: (id) => get().posts.find((post) => post.id === id),
  getPostBySlug: (slug) => get().posts.find((post) => post.slug === slug),
  getFilteredPosts: () => {
    const { posts, searchQuery, selectedTag } = get();
    return posts.filter((post) => {
      const matchesSearch =
        !searchQuery ||
        post.title.toLowerCase().includes(searchQuery.toLowerCase()) ||
        post.excerpt.toLowerCase().includes(searchQuery.toLowerCase());
      const matchesTag =
        !selectedTag || post.tags.some((tag) => tag.id === selectedTag);
      return matchesSearch && matchesTag;
    });
  },
}));
