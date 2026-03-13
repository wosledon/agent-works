/**
 * 博客系统类型定义
 */

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

export interface Author {
  id: string;
  name: string;
  avatar?: string;
  bio?: string;
}

export interface Tag {
  id: string;
  name: string;
  slug: string;
}

export interface Comment {
  id: string;
  content: string;
  author: Author;
  postId: string;
  parentId?: string;
  createdAt: string;
}

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
