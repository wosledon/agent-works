/**
 * 博客系统类型定义
 */

// 文章类型
export interface Post {
  id: string;
  title: string;
  slug: string;
  excerpt: string;
  content: string;
  coverImage?: string;
  author: Author;
  tags: Tag[];
  publishedAt: string;
  updatedAt: string;
  viewCount: number;
  likeCount: number;
}

// 作者类型
export interface Author {
  id: string;
  name: string;
  avatar?: string;
  bio?: string;
}

// 标签类型
export interface Tag {
  id: string;
  name: string;
  slug: string;
}

// 评论类型
export interface Comment {
  id: string;
  content: string;
  author: Author;
  postId: string;
  parentId?: string;
  createdAt: string;
}

// API 响应类型
export interface ApiResponse<T> {
  data: T;
  success: boolean;
  message?: string;
}

export interface PaginatedResponse<T> {
  data: T[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
}
