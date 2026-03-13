import { useState, useEffect } from 'react';

export function usePosts() {
  const [posts, setPosts] = useState([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    // TODO: 实现获取文章列表逻辑
  }, []);

  return { posts, isLoading, error };
}

export function usePost(slug: string) {
  const [post, setPost] = useState(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    // TODO: 实现获取单个文章逻辑
  }, [slug]);

  return { post, isLoading, error };
}
