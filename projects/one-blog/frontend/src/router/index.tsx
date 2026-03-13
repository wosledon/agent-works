/**
 * 路由配置
 */
import { createBrowserRouter, Navigate } from 'react-router-dom';
import { Layout } from '../components/Layout';
import { HomePage } from '../pages/HomePage';
import { PostDetailPage } from '../pages/PostDetailPage';
import { LoginPage } from '../pages/LoginPage';
import { EditorPage } from '../pages/EditorPage';
import { ProtectedRoute } from '../components/ProtectedRoute';

export const router = createBrowserRouter([
  {
    path: '/',
    element: <Layout />,
    children: [
      {
        index: true,
        element: <HomePage />,
      },
      {
        path: 'post/:slug',
        element: <PostDetailPage />,
      },
      {
        path: 'tag/:tagSlug',
        element: <HomePage />,
      },
      {
        path: 'login',
        element: <LoginPage />,
      },
      {
        path: 'editor',
        element: (
          <ProtectedRoute>
            <EditorPage />
          </ProtectedRoute>
        ),
      },
      {
        path: 'editor/:slug',
        element: (
          <ProtectedRoute>
            <EditorPage />
          </ProtectedRoute>
        ),
      },
      {
        path: '*',
        element: <Navigate to="/" replace />,
      },
    ],
  },
]);
