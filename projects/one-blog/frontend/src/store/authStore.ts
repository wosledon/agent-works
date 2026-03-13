/**
 * 用户状态管理
 */
import { create } from 'zustand';

export interface User {
  id: string;
  name: string;
  email: string;
  avatar?: string;
  bio?: string;
  role: 'admin' | 'user';
}

interface AuthState {
  // 状态
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  error: string | null;
  
  // 动作
  setUser: (user: User | null) => void;
  setLoading: (loading: boolean) => void;
  setError: (error: string | null) => void;
  login: (email: string, password: string) => Promise<boolean>;
  logout: () => void;
  clearError: () => void;
}

// 模拟用户数据
const MOCK_USERS: User[] = [
  {
    id: '1',
    name: '管理员',
    email: 'admin@oneblog.com',
    avatar: 'https://api.dicebear.com/7.x/avataaars/svg?seed=admin',
    bio: '博客管理员',
    role: 'admin',
  },
  {
    id: '2',
    name: '测试用户',
    email: 'user@oneblog.com',
    avatar: 'https://api.dicebear.com/7.x/avataaars/svg?seed=user',
    bio: '普通用户',
    role: 'user',
  },
];

export const useAuthStore = create<AuthState>((set) => ({
  user: null,
  isAuthenticated: false,
  isLoading: false,
  error: null,
  
  setUser: (user) => set({ user, isAuthenticated: !!user }),
  setLoading: (isLoading) => set({ isLoading }),
  setError: (error) => set({ error }),
  clearError: () => set({ error: null }),
  
  login: async (email: string, password: string) => {
    set({ isLoading: true, error: null });
    
    // 模拟 API 延迟
    await new Promise((resolve) => setTimeout(resolve, 800));
    
    // 简单验证（实际项目中应该调用真实 API）
    if (password.length < 6) {
      set({ isLoading: false, error: '密码至少需要 6 位字符' });
      return false;
    }
    
    const user = MOCK_USERS.find((u) => u.email === email);
    
    if (user) {
      set({ user, isAuthenticated: true, isLoading: false });
      // 保存到 localStorage 保持登录状态
      localStorage.setItem('user', JSON.stringify(user));
      return true;
    } else {
      set({ isLoading: false, error: '邮箱或密码错误' });
      return false;
    }
  },
  
  logout: () => {
    localStorage.removeItem('user');
    set({ user: null, isAuthenticated: false, error: null });
  },
}));

// 初始化时检查 localStorage
const storedUser = localStorage.getItem('user');
if (storedUser) {
  try {
    const user = JSON.parse(storedUser);
    useAuthStore.setState({ user, isAuthenticated: true });
  } catch {
    localStorage.removeItem('user');
  }
}
