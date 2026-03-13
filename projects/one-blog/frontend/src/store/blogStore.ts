/**
 * Zustand 状态管理
 */
import { create } from 'zustand';
import type { Post, Comment } from '../types';

interface BlogState {
  // 状态
  posts: Post[];
  currentPost: Post | null;
  comments: Comment[];
  isLoading: boolean;
  error: string | null;
  
  // 动作
  setPosts: (posts: Post[]) => void;
  setCurrentPost: (post: Post | null) => void;
  setComments: (comments: Comment[]) => void;
  setLoading: (loading: boolean) => void;
  setError: (error: string | null) => void;
  
  // 派生状态
  getPostById: (id: string) => Post | undefined;
}

export const useBlogStore = create<BlogState>((set, get) => ({
  posts: [],
  currentPost: null,
  comments: [],
  isLoading: false,
  error: null,
  
  setPosts: (posts) => set({ posts }),
  setCurrentPost: (post) => set({ currentPost: post }),
  setComments: (comments) => set({ comments }),
  setLoading: (loading) => set({ isLoading: loading }),
  setError: (error) => set({ error }),
  
  getPostById: (id) => get().posts.find((post) => post.id === id),
}));
