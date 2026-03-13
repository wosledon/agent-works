/**
 * 主题状态管理 - 暗色模式
 */
import { create } from 'zustand';
import { persist } from 'zustand/middleware';

type Theme = 'light' | 'dark' | 'system';

interface ThemeState {
  theme: Theme;
  isDark: boolean;
  setTheme: (theme: Theme) => void;
  toggleTheme: () => void;
}

function getSystemTheme(): boolean {
  if (typeof window === 'undefined') return false;
  return window.matchMedia('(prefers-color-scheme: dark)').matches;
}

function applyTheme(isDark: boolean) {
  if (typeof document === 'undefined') return;
  
  const root = document.documentElement;
  if (isDark) {
    root.classList.add('dark');
  } else {
    root.classList.remove('dark');
  }
}

export const useThemeStore = create<ThemeState>()(
  persist(
    (set, get) => ({
      theme: 'system',
      isDark: getSystemTheme(),

      setTheme: (theme) => {
        const isDark = theme === 'system' ? getSystemTheme() : theme === 'dark';
        set({ theme, isDark });
        applyTheme(isDark);
      },

      toggleTheme: () => {
        const newIsDark = !get().isDark;
        set({ theme: newIsDark ? 'dark' : 'light', isDark: newIsDark });
        applyTheme(newIsDark);
      },
    }),
    {
      name: 'oneblog-theme',
      onRehydrateStorage: () => (state) => {
        if (state) {
          // 重新应用主题
          const isDark = state.theme === 'system' 
            ? getSystemTheme() 
            : state.theme === 'dark';
          applyTheme(isDark);
        }
      },
    }
  )
);

// 初始化系统主题监听
if (typeof window !== 'undefined') {
  const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
  mediaQuery.addEventListener('change', () => {
    const { theme } = useThemeStore.getState();
    if (theme === 'system') {
      useThemeStore.getState().setTheme('system');
    }
  });
}
