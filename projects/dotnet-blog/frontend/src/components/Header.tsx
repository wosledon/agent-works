import React from 'react';
import { Link } from 'react-router-dom';

interface HeaderProps {
  siteTitle?: string;
}

export const Header: React.FC<HeaderProps> = ({ siteTitle = '.NET Blog' }) => {
  return (
    <header className="sticky top-0 z-50 bg-white/80 backdrop-blur-md border-b border-gray-200 dark:bg-gray-900/80 dark:border-gray-800">
      <div className="max-w-6xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex justify-between items-center h-16">
          <Link to="/" className="text-xl font-bold text-gray-900 dark:text-white hover:text-purple-600 transition-colors">
            {siteTitle}
          </Link>
          <nav className="hidden md:flex space-x-8">
            <Link to="/" className="text-gray-600 hover:text-purple-600 dark:text-gray-300 dark:hover:text-purple-400 transition-colors">
              首页
            </Link>
            <Link to="/tags" className="text-gray-600 hover:text-purple-600 dark:text-gray-300 dark:hover:text-purple-400 transition-colors">
              标签
            </Link>
            <Link to="/about" className="text-gray-600 hover:text-purple-600 dark:text-gray-300 dark:hover:text-purple-400 transition-colors">
              关于
            </Link>
          </nav>
        </div>
      </div>
    </header>
  );
};
