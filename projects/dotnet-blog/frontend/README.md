# .NET Blog 前端

基于 React + TypeScript + Tailwind CSS + Vite 构建的博客系统前端

## 技术栈

- **框架**: React 18+ (Functional Components + Hooks)
- **语言**: TypeScript (严格模式)
- **样式**: Tailwind CSS
- **状态管理**: Zustand
- **路由**: React Router v6
- **构建工具**: Vite

## 项目结构

```
src/
├── components/     # React 组件
├── hooks/          # 自定义 Hooks
├── store/          # 状态管理 (Zustand)
├── api/            # API 客户端
├── types/          # TypeScript 类型定义
├── pages/          # 页面组件
├── utils/          # 工具函数
├── App.tsx         # 应用主组件
├── main.tsx        # 应用入口
└── index.css       # 全局样式
```

## 开发命令

```bash
# 安装依赖
npm install

# 启动开发服务器
npm run dev

# 构建生产版本
npm run build

# 预览生产构建
npm run preview
```

## 环境变量

复制 `.env.example` 为 `.env` 并配置：

```
VITE_API_URL=http://localhost:5000/api
```
