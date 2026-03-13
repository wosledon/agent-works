import { create } from 'zustand';
import type { Post, Comment } from '../types';

interface BlogState {
  posts: Post[];
  currentPost: Post | null;
  comments: Comment[];
  isLoading: boolean;
  error: string | null;
  setPosts: (posts: Post[]) => void;
  setCurrentPost: (post: Post | null) => void;
  setComments: (comments: Comment[]) => void;
  setLoading: (loading: boolean) => void;
  setError: (error: string | null) => void;
}

export const useBlogStore = create<BlogState>((set) => ({
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
}));
