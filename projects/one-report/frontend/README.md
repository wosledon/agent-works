# OneReport - 低代码报表工具

基于 React 19 + TypeScript + Vite + Tailwind CSS 构建的低代码报表设计器。

## 技术栈

- **React 19** - 前端框架
- **TypeScript** - 类型安全
- **Vite** - 构建工具
- **Tailwind CSS** - 样式框架

## 项目结构

```
frontend/
├── src/
│   ├── components/      # UI 组件
│   │   ├── Layout.tsx      # 布局组件
│   │   ├── Sidebar.tsx     # 左侧组件库
│   │   ├── Canvas.tsx      # 画布区域
│   │   ├── PropertyPanel.tsx  # 右侧属性面板
│   │   └── index.ts        # 组件导出
│   ├── hooks/          # 自定义 Hooks
│   │   └── index.ts
│   ├── types/          # TypeScript 类型定义
│   │   └── index.ts
│   ├── utils/          # 工具函数
│   │   └── index.ts
│   ├── App.tsx         # 应用主组件
│   ├── main.tsx        # 入口文件
│   └── index.css       # 全局样式
├── public/             # 静态资源
├── index.html          # HTML 入口
├── package.json        # 依赖配置
├── tsconfig.json       # TypeScript 配置
├── vite.config.ts      # Vite 配置
├── tailwind.config.js  # Tailwind 配置
└── postcss.config.js   # PostCSS 配置
```

## 功能特性

- 🎨 **组件库面板** - 拖拽式组件选择
- 📊 **可视化画布** - 自由拖拽布局
- ⚙️ **属性配置** - 实时编辑组件属性
- 🌙 **深色模式** - 支持亮色/深色主题
- 📱 **响应式设计** - 适配不同屏幕尺寸

## 开发命令

```bash
# 安装依赖
npm install

# 开发模式
npm run dev

# 构建生产版本
npm run build

# 预览生产版本
npm run preview
```

## 开发服务器

```bash
npm run dev
```

默认启动在 http://localhost:5173

## 构建部署

```bash
npm run build
```

构建产物输出到 `dist/` 目录。