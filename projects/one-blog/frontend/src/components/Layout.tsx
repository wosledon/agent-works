/**
 * 布局组件
 */
import { Outlet, Link, useNavigate } from 'react-router-dom';
import { useAuthStore } from '../store';
import { PenLine, LogOut, User, Search, Menu, X } from 'lucide-react';
import { useState } from 'react';

export function Layout() {
  const { user, isAuthenticated, logout } = useAuthStore();
  const navigate = useNavigate();
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);

  const handleLogout = () => {
    logout();
    navigate('/');
  };

  return (
    <div className="min-h-screen bg-gray-50">
      {/* 导航栏 */}
      <header className="bg-white shadow-sm sticky top-0 z-50">
        <div className="max-w-6xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between items-center h-16">
            {/* Logo */}
            <Link to="/" className="flex items-center space-x-2">
              <div className="w-8 h-8 bg-gradient-to-br from-blue-500 to-purple-600 rounded-lg flex items-center justify-center">
                <span className="text-white font-bold text-sm">OB</span>
              </div>
              <span className="text-xl font-bold bg-gradient-to-r from-blue-600 to-purple-600 bg-clip-text text-transparent">
                OneBlog
              </span>
            </Link>

            {/* 桌面导航 */}
            <nav className="hidden md:flex items-center space-x-6">
              <Link
                to="/"
                className="text-gray-600 hover:text-gray-900 font-medium transition-colors"
              >
                首页
              </Link>
              <Link
                to="/"
                className="text-gray-600 hover:text-gray-900 font-medium transition-colors"
              >
                文章
              </Link>
              <Link
                to="/"
                className="text-gray-600 hover:text-gray-900 font-medium transition-colors"
              >
                标签
              </Link>
            </nav>

            {/* 右侧操作区 */}
            <div className="hidden md:flex items-center space-x-4">
              <button className="p-2 text-gray-500 hover:text-gray-700 transition-colors">
                <Search className="w-5 h-5" />
              </button>

              {isAuthenticated ? (
                <>
                  <Link
                    to="/editor"
                    className="flex items-center space-x-1 px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors"
                  >
                    <PenLine className="w-4 h-4" />
                    <span>写文章</span>
                  </Link>
                  <div className="relative group">
                    <button className="flex items-center space-x-2 p-1 rounded-full hover:bg-gray-100 transition-colors">
                      <img
                        src={user?.avatar || 'https://api.dicebear.com/7.x/avataaars/svg?seed=guest'}
                        alt={user?.name}
                        className="w-8 h-8 rounded-full"
                      />
                    </button>
                    {/* 下拉菜单 */}
                    <div className="absolute right-0 mt-2 w-48 bg-white rounded-lg shadow-lg border border-gray-100 opacity-0 invisible group-hover:opacity-100 group-hover:visible transition-all">
                      <div className="px-4 py-3 border-b border-gray-100">
                        <p className="text-sm font-medium text-gray-900">{user?.name}</p>
                        <p className="text-xs text-gray-500">{user?.email}</p>
                      </div>
                      <button
                        onClick={handleLogout}
                        className="w-full flex items-center space-x-2 px-4 py-2 text-sm text-red-600 hover:bg-red-50 transition-colors"
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
                  className="flex items-center space-x-1 px-4 py-2 text-gray-700 hover:text-gray-900 font-medium transition-colors"
                >
                  <User className="w-4 h-4" />
                  <span>登录</span>
                </Link>
              )}
            </div>

            {/* 移动端菜单按钮 */}
            <button
              className="md:hidden p-2 text-gray-500 hover:text-gray-700"
              onClick={() => setMobileMenuOpen(!mobileMenuOpen)}
            >
              {mobileMenuOpen ? (
                <X className="w-6 h-6" />
              ) : (
                <Menu className="w-6 h-6" />
              )}
            </button>
          </div>

          {/* 移动端菜单 */}
          {mobileMenuOpen && (
            <div className="md:hidden py-4 border-t border-gray-100">
              <nav className="flex flex-col space-y-3">
                <Link
                  to="/"
                  className="text-gray-600 hover:text-gray-900 font-medium px-2 py-1"
                  onClick={() => setMobileMenuOpen(false)}
                >
                  首页
                </Link>
                <Link
                  to="/"
                  className="text-gray-600 hover:text-gray-900 font-medium px-2 py-1"
                  onClick={() => setMobileMenuOpen(false)}
                >
                  文章
                </Link>
                <Link
                  to="/"
                  className="text-gray-600 hover:text-gray-900 font-medium px-2 py-1"
                  onClick={() => setMobileMenuOpen(false)}
                >
                  标签
                </Link>
                {!isAuthenticated && (
                  <Link
                    to="/login"
                    className="text-blue-600 font-medium px-2 py-1"
                    onClick={() => setMobileMenuOpen(false)}
                  >
                    登录
                  </Link>
                )}
                {isAuthenticated && (
                  <>
                    <Link
                      to="/editor"
                      className="text-blue-600 font-medium px-2 py-1"
                      onClick={() => setMobileMenuOpen(false)}
                    >
                      写文章
                    </Link>
                    <button
                      onClick={() => {
                        handleLogout();
                        setMobileMenuOpen(false);
                      }}
                      className="text-red-600 font-medium px-2 py-1 text-left"
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
      <main className="max-w-6xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <Outlet />
      </main>

      {/* 页脚 */}
      <footer className="bg-white border-t border-gray-200 mt-auto">
        <div className="max-w-6xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
          <div className="flex flex-col md:flex-row justify-between items-center space-y-4 md:space-y-0">
            <div className="flex items-center space-x-2">
              <div className="w-6 h-6 bg-gradient-to-br from-blue-500 to-purple-600 rounded flex items-center justify-center">
                <span className="text-white font-bold text-xs">OB</span>
              </div>
              <span className="text-gray-600">OneBlog &copy; 2024</span>
            </div>
            <div className="flex space-x-6">
              <a href="#" className="text-gray-500 hover:text-gray-700">关于</a>
              <a href="#" className="text-gray-500 hover:text-gray-700">隐私政策</a>
              <a href="#" className="text-gray-500 hover:text-gray-700">联系我们</a>
            </div>
          </div>
        </div>
      </footer>
    </div>
  );
}
