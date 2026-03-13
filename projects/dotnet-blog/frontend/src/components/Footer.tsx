interface FooterProps {
  copyright?: string;
}

export function Footer({ copyright = '© 2026 .NET Blog' }: FooterProps) {
  return (
    <footer className="bg-gray-50 border-t border-gray-200 dark:bg-gray-900 dark:border-gray-800 mt-auto">
      <div className="max-w-6xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <div className="flex flex-col md:flex-row justify-between items-center">
          <p className="text-gray-500 dark:text-gray-400 text-sm">{copyright}</p>
          <div className="flex space-x-6 mt-4 md:mt-0">
            <a href="https://github.com" target="_blank" rel="noopener noreferrer" className="text-gray-400 hover:text-gray-600 dark:hover:text-gray-300">GitHub</a>
            <a href="https://twitter.com" target="_blank" rel="noopener noreferrer" className="text-gray-400 hover:text-gray-600 dark:hover:text-gray-300">Twitter</a>
          </div>
        </div>
      </div>
    </footer>
  );
}
