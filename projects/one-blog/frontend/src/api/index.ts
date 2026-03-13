/**
 * API 客户端
 */
import type { ApiResponse, PaginatedResponse, Post, Comment } from '../types';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api';

async function fetchApi<T>(endpoint: string, options?: RequestInit): Promise<T> {
  const response = await fetch(`${API_BASE_URL}${endpoint}`, {
    headers: {
      'Content-Type': 'application/json',
    },
    ...options,
  });

  if (!response.ok) {
    throw new Error(`API Error: ${response.status} ${response.statusText}`);
  }

  return response.json();
}

// 文章相关 API
export const postsApi = {
  getAll: (page = 1, pageSize = 10) =>
    fetchApi<ApiResponse<PaginatedResponse<Post>>>(`/posts?page=${page}&pageSize=${pageSize}`),
  
  getBySlug: (slug: string) =>
    fetchApi<ApiResponse<Post>>(`/posts/${slug}`),
  
  getByTag: (tagSlug: string, page = 1) =>
    fetchApi<ApiResponse<PaginatedResponse<Post>>>(`/posts?tag=${tagSlug}&page=${page}`),
};

// 评论相关 API
export const commentsApi = {
  getByPostId: (postId: string) =>
    fetchApi<ApiResponse<Comment[]>>(`/posts/${postId}/comments`),
  
  create: (postId: string, content: string, parentId?: string) =>
    fetchApi<ApiResponse<Comment>>(`/posts/${postId}/comments`, {
      method: 'POST',
      body: JSON.stringify({ content, parentId }),
    }),
};
