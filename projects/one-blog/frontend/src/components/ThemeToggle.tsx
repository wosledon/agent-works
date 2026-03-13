/**
 * 主题切换按钮组件
 */
import { Sun, Moon, Monitor } from 'lucide-react';
import { useThemeStore } from '../store/themeStore';
import { useState, useRef, useEffect } from 'react';

type Theme = 'light' | 'dark' | 'system';

const themes: { value: Theme; label: string; icon: typeof Sun }[] = [
  { value: 'light', label: '浅色', icon: Sun },
  { value: 'dark', label: '暗色', icon: Moon },
  { value: 'system', label: '跟随系统', icon: Monitor },
];

export function ThemeToggle() {
  const { theme, setTheme, isDark, toggleTheme } = useThemeStore();
  const [isOpen, setIsOpen] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);

  // 点击外部关闭
  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    }
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const currentIcon = themes.find((t) => t.value === theme)?.icon || Sun;

  return (
    <div ref={containerRef} className="relative">
      {/* 切换按钮 */}
      <button
        onClick={() => setIsOpen(!isOpen)}
        className="
          p-2 rounded-lg text-gray-500 hover:text-gray-700 dark:text-gray-400 
          dark:hover:text-gray-200 hover:bg-gray-100 dark:hover:bg-gray-800
          transition-colors
        "
        title="切换主题"
      >
        {isDark ? (
          <Moon className="w-5 h-5" />
        ) : (
          <Sun className="w-5 h-5" />
        )}
      </button>

      {/* 下拉菜单 */}
      {isOpen && (
        <>
          <div
            className="fixed inset-0 z-10"
            onClick={() => setIsOpen(false)}
          />
          <div className="
            absolute right-0 mt-2 w-40 bg-white dark:bg-gray-800 
            rounded-lg shadow-lg border border-gray-100 dark:border-gray-700 
            py-1 z-20
          ">
            {themes.map(({ value, label, icon: Icon }) => (
              <button
                key={value}
                onClick={() => {
                  setTheme(value);
                  setIsOpen(false);
                }}
                className={`
                  w-full flex items-center px-4 py-2 text-sm
                  transition-colors
                  ${theme === value
                    ? 'text-blue-600 bg-blue-50 dark:bg-blue-900/20'
                    : 'text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700'
                  }
                `}
              >
                <Icon className="w-4 h-4 mr-3" />
                {label}
              </button>
            ))}
          </div>
        </>
      )}
    </div>
  );
}

// 简洁版主题切换（仅切换 light/dark）
export function ThemeToggleSimple() {
  const { isDark, toggleTheme } = useThemeStore();

  return (
    <button
      onClick={toggleTheme}
      className="
        p-2 rounded-lg text-gray-500 hover:text-gray-700 dark:text-gray-400 
        dark:hover:text-gray-200 hover:bg-gray-100 dark:hover:bg-gray-800
        transition-colors
      "
      title={isDark ? '切换到浅色模式' : '切换到暗色模式'}
    >
      {isDark ? (
        <Sun className="w-5 h-5" />
      ) : (
        <Moon className="w-5 h-5" />
      )}
    </button>
  );
}
