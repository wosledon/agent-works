/**
 * 布局组件 - 支持暗色模式和搜索
 */
import { Outlet, Link, useNavigate } from 'react-router-dom';
import { useAuthStore, useThemeStore } from '../store';
import { ThemeToggle } from './ThemeToggle';
import { SearchModal } from './SearchModal';
import { PenLine, LogOut, User, Search, Menu, X } from 'lucide-react';
import { useState, useEffect } from 'react';

export function Layout() {
  const navigate = useNavigate();
  const { user, isAuthenticated, logout } = useAuthStore();
  const { isDark } = useThemeStore();
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);
  const [searchOpen, setSearchOpen] = useState(false);

  // 键盘快捷键打开搜索
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if ((e.metaKey || e.ctrlKey) && e.key === 'k') {
        e.preventDefault();
        setSearchOpen(true);
      }
    };
    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, []);

  // 应用暗色模式
  useEffect(() => {
    if (isDark) {
      document.documentElement.classList.add('dark');
    } else {
      document.documentElement.classList.remove('dark');
    }
  }, [isDark]);

  const handleLogout = () => {
    logout();
    navigate('/');
  };

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-950 transition-colors">
      {/* 导航栏 */}
      <header className="
        bg-white/80 dark:bg-gray-900/80 backdrop-blur-md 
        shadow-sm dark:shadow-gray-800/50 
        sticky top-0 z-40 transition-colors
      ">
        <div className="max-w-6xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between items-center h-16">
            {/* Logo */}
            <Link to="/" className="flex items-center space-x-2">
              <div className="
                w-8 h-8 bg-gradient-to-br from-blue-500 to-purple-600 
                rounded-lg flex items-center justify-center shadow-lg
              ">
                <span className="text-white font-bold text-sm">OB</span>
              </div>
              <span className="
                text-xl font-bold bg-gradient-to-r from-blue-600 to-purple-600 
                bg-clip-text text-transparent
              ">
                OneBlog
              </span>
            </Link>

            {/* 桌面导航 */}
            <nav className="hidden md:flex items-center space-x-6">
              <Link
                to="/"
                className="
                  text-gray-600 dark:text-gray-300 
                  hover:text-gray-900 dark:hover:text-white 
                  font-medium transition-colors
                "
              >
                首页
              </Link>
              <Link
                to="/"
                className="
                  text-gray-600 dark:text-gray-300 
                  hover:text-gray-900 dark:hover:text-white 
                  font-medium transition-colors
                "
              >
                文章
              </Link>
              <Link
                to="/"
                className="
                  text-gray-600 dark:text-gray-300 
                  hover:text-gray-900 dark:hover:text-white 
                  font-medium transition-colors
                "
              >
                标签
              </Link>
            </nav>

            {/* 右侧操作区 */}
            <div className="hidden md:flex items-center space-x-2">
              {/* 搜索按钮 */}
              <button
                onClick={() => setSearchOpen(true)}
                className="
                  flex items-center space-x-2 px-3 py-1.5
                  text-gray-500 dark:text-gray-400 
                  hover:text-gray-700 dark:hover:text-gray-200
                  hover:bg-gray-100 dark:hover:bg-gray-800
                  rounded-lg transition-colors
                "
              >
                <Search className="w-4 h-4" />
                <span className="text-sm">搜索</span>
                <kbd className="
                  hidden lg:inline-block px-1.5 py-0.5 
                  text-xs bg-gray-100 dark:bg-gray-800 
                  rounded border border-gray-200 dark:border-gray-700
                ">
                  ⌘K
                </kbd>
              </button>

              {/* 主题切换 */}
              <ThemeToggle />

              {isAuthenticated ? (
                <>
                  <Link
                    to="/editor"
                    className="
                      flex items-center space-x-1 px-4 py-2 
                      bg-blue-600 hover:bg-blue-700 
                      text-white rounded-lg 
                      transition-colors shadow-sm hover:shadow-md
                    "
                  >
                    <PenLine className="w-4 h-4" />
                    <span className="hidden lg:inline">写文章</span>
                  </Link>
                  <div className="relative group">
                    <button className="
                      flex items-center space-x-2 p-1 rounded-full 
                      hover:bg-gray-100 dark:hover:bg-gray-800 
                      transition-colors
                    "
                    >
                      <img
                        src={user?.avatar || 'https://api.dicebear.com/7.x/avataaars/svg?seed=guest'}
                        alt={user?.name}
                        className="w-8 h-8 rounded-full"
                      />
                    </button>
                    {/* 下拉菜单 */}
                    <div className="
                      absolute right-0 mt-2 w-48 
                      bg-white dark:bg-gray-800 
                      rounded-lg shadow-lg border 
                      border-gray-100 dark:border-gray-700
                      opacity-0 invisible group-hover:opacity-100 group-hover:visible 
                      transition-all
                    ">
                      <div className="px-4 py-3 border-b border-gray-100 dark:border-gray-700">
                        <p className="text-sm font-medium text-gray-900 dark:text-gray-100">
                          {user?.name}
                        </p>
                        <p className="text-xs text-gray-500 dark:text-gray-400">{user?.email}</p>
                      </div>
                      <button
                        onClick={handleLogout}
                        className="
                          w-full flex items-center space-x-2 px-4 py-2 text-sm 
                          text-red-600 hover:bg-red-50 dark:hover:bg-red-900/20 
                          transition-colors
                        "
                      >
                        <LogOut className="w-4 h-4" />
                        <span>退出登录</span>
                      </button>
                    </div>
                  </div>
                </>
              ) : (
                <Link
                  to="/login"
                  className="
                    flex items-center space-x-1 px-4 py-2 
                    text-gray-700 dark:text-gray-300 
                    hover:text-gray-900 dark:hover:text-white 
                    hover:bg-gray-100 dark:hover:bg-gray-800
                    font-medium rounded-lg transition-colors
                  "
                >
                  <User className="w-4 h-4" />
                  <span>登录</span>
                </Link>
              )}
            </div>

            {/* 移动端菜单按钮 */}
            <div className="md:hidden flex items-center space-x-2">
              <button
                onClick={() => setSearchOpen(true)}
                className="
                  p-2 text-gray-500 dark:text-gray-400 
                  hover:text-gray-700 dark:hover:text-gray-200
                "
              >
                <Search className="w-5 h-5" />
              </button>
              <button
                className="p-2 text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-200"
                onClick={() => setMobileMenuOpen(!mobileMenuOpen)}
              >
                {mobileMenuOpen ? (
                  <X className="w-6 h-6" />
                ) : (
                  <Menu className="w-6 h-6" />
                )}
              </button>
            </div>
          </div>

          {/* 移动端菜单 */}
          {mobileMenuOpen && (
            <div className="
              md:hidden py-4 border-t 
              border-gray-100 dark:border-gray-800
            ">
              <nav className="flex flex-col space-y-1">
                <Link
                  to="/"
                  className="
                    text-gray-600 dark:text-gray-300 
                    hover:text-gray-900 dark:hover:text-white 
                    hover:bg-gray-50 dark:hover:bg-gray-800
                    font-medium px-3 py-2 rounded-lg transition-colors
                  "
                  onClick={() => setMobileMenuOpen(false)}
                >
                  首页
                </Link>
                <Link
                  to="/"
                  className="
                    text-gray-600 dark:text-gray-300 
                    hover:text-gray-900 dark:hover:text-white 
                    hover:bg-gray-50 dark:hover:bg-gray-800
                    font-medium px-3 py-2 rounded-lg transition-colors
                  "
                  onClick={() => setMobileMenuOpen(false)}
                >
                  文章
                </Link>
                <Link
                  to="/"
                  className="
                    text-gray-600 dark:text-gray-300 
                    hover:text-gray-900 dark:hover:text-white 
                    hover:bg-gray-50 dark:hover:bg-gray-800
                    font-medium px-3 py-2 rounded-lg transition-colors
                  "
                  onClick={() => setMobileMenuOpen(false)}
                >
                  标签
                </Link>
                <div className="border-t border-gray-100 dark:border-gray-800 my-2"></div>
                <div className="flex items-center justify-between px-3 py-2">
                  <span className="text-gray-600 dark:text-gray-300">主题</span>
                  <ThemeToggle />
                </div>
                {!isAuthenticated && (
                  <Link
                    to="/login"
                    className="
                      text-blue-600 dark:text-blue-400 
                      font-medium px-3 py-2 rounded-lg 
                      hover:bg-blue-50 dark:hover:bg-blue-900/20
                      transition-colors
                    "
                    onClick={() => setMobileMenuOpen(false)}
                  >
                    登录
                  </Link>
                )}
                {isAuthenticated && (
                  <>
                    <Link
                      to="/editor"
                      className="
                        text-blue-600 dark:text-blue-400 
                        font-medium px-3 py-2 rounded-lg 
                        hover:bg-blue-50 dark:hover:bg-blue-900/20
                        transition-colors
                      "
                      onClick={() => setMobileMenuOpen(false)}
                    >
                      写文章
                    </Link>
                    <button
                      onClick={() => {
                        handleLogout();
                        setMobileMenuOpen(false);
                      }}
                      className="
                        text-red-600 dark:text-red-400 
                        font-medium px-3 py-2 rounded-lg text-left
                        hover:bg-red-50 dark:hover:bg-red-900/20
                        transition-colors
                      "
                    >
                      退出登录
                    </button>
                  </>
                )}
              </nav>
            </div>
          )}
        </div>
      </header>

      {/* 主内容区 */}
      <main className="max-w-6xl mx-auto px-4 sm:px-6 lg:px-8 py-6 md:py-8">
        <Outlet />
      </main>

      {/* 页脚 */}
      <footer className="
        bg-white dark:bg-gray-900 
        border-t border-gray-200 dark:border-gray-800 
        mt-auto transition-colors
      ">
        <div className="max-w-6xl mx-auto px-4 sm:px-6 lg:px-8 py-6 md:py-8">
          <div className="
            flex flex-col md:flex-row justify-between items-center 
            space-y-4 md:space-y-0
          ">
            <div className="flex items-center space-x-2">
              <div className="
                w-6 h-6 bg-gradient-to-br from-blue-500 to-purple-600 
                rounded flex items-center justify-center
              ">
                <span className="text-white font-bold text-xs">OB</span>
              </div>
              <span className="text-gray-600 dark:text-gray-400">
                OneBlog &copy; 2024
              </span>
            </div>
            <div className="flex space-x-4 md:space-x-6">
              <a href="#" className="
                text-gray-500 dark:text-gray-400 
                hover:text-gray-700 dark:hover:text-gray-200
              ">
                关于
              </a>
              <a href="#" className="
                text-gray-500 dark:text-gray-400 
                hover:text-gray-700 dark:hover:text-gray-200
              ">
                隐私政策
              </a>
              <a href="#" className="
                text-gray-500 dark:text-gray-400 
                hover:text-gray-700 dark:hover:text-gray-200
              ">
                联系我们
              </a>
            </div>
          </div>
        </div>
      </footer>

      {/* 搜索弹窗 */}
      <SearchModal isOpen={searchOpen} onClose={() => setSearchOpen(false)} />
    </div>
  );
}
