# dotnet-blog 设计系统

> 基于 Microsoft Fluent Design System 2
> 创建日期: 2025-03-14

---

## 1. 设计概述

dotnet-blog 是一个现代化的博客系统，采用 Fluent Design 设计语言，强调：
- **清晰层次** - 通过深度和阴影表达层级关系
- **毛玻璃质感** - 使用 Acrylic/Mica 效果营造现代感
- **生产力导向** - 高效的信息展示与操作流程
- **响应式优先** - 桌面端为主，适配平板和移动端

---

## 2. 颜色系统

### 2.1 主题色板

| 颜色名称 | Light Mode | Dark Mode | 用途 |
|---------|------------|-----------|------|
| **主色 (Primary)** | #0078D4 | #4FC3F7 | 链接、按钮、品牌标识 |
| **强调色 (Accent)** | #106EBE | #29B6F6 | 悬停状态、激活状态 |
| **辅助色 (Secondary)** | #005A9E | #81D4FA | 次要操作、图标 |
| **背景主色** | #FFFFFF | #1F1F1F | 页面主背景 |
| **背景次级** | #F3F2F1 | #2D2D2D | 卡片、面板背景 |
| **背景三级** | #FAF9F8 | #252525 | 输入框、悬浮背景 |

### 2.2 中性色阶

```
gray-50:  #FAFAFA  (最浅背景)
gray-100: #F5F5F5
gray-200: #E5E5E5  (边框、分隔线)
gray-300: #D4D4D4
gray-400: #A3A3A3  (占位符)
gray-500: #737373  (次要文字)
gray-600: #525252
gray-700: #404040  (正文文字)
gray-800: #262626
gray-900: #171717  (标题文字)
```

### 2.3 功能色

| 状态 | 颜色 | 背景变体 |
|------|------|----------|
| **成功 Success** | #107C10 | #DFF6DD |
| **警告 Warning** | #FFB900 | #FFF4CE |
| **错误 Error** | #D13438 | #FDE7E9 |
| **信息 Info** | #0078D4 | #E5F1FB |

---

## 3. 排版系统

### 3.1 字体家族

```css
--font-heading: 'Segoe UI Variable', 'Segoe UI', system-ui, sans-serif;
--font-body: 'Segoe UI Variable', 'Segoe UI', system-ui, sans-serif;
--font-code: 'Cascadia Code', 'JetBrains Mono', 'Consolas', monospace;
```

### 3.2 字号规范

| 层级 | 字号 | 字重 | 行高 | 用途 |
|------|------|------|------|------|
| **Hero** | 40px | 600 | 1.2 | 首页大标题 |
| **H1** | 32px | 600 | 1.25 | 文章标题、页面主标题 |
| **H2** | 24px | 600 | 1.3 | 章节标题 |
| **H3** | 20px | 600 | 1.35 | 小节标题 |
| **H4** | 16px | 600 | 1.4 | 卡片标题 |
| **Body Large** | 16px | 400 | 1.6 | 正文阅读 |
| **Body** | 14px | 400 | 1.5 | 默认正文 |
| **Caption** | 12px | 400 | 1.4 | 辅助说明 |
| **Code** | 13px | 400 | 1.6 | 代码块 |

---

## 4. 间距系统

### 4.1 基础间距（4px 基准）

```
4px   - xs  (超紧凑)
8px   - sm  (紧凑)
12px  - md  (默认)
16px  - lg  (舒适)
20px  - xl  (宽松)
24px  - 2xl (大间距)
32px  - 3xl (章节间距)
48px  - 4xl (区块间距)
64px  - 5xl (页面间距)
```

### 4.2 圆角规范

| 名称 | 值 | 用途 |
|------|-----|------|
| **none** | 0px | 直角元素 |
| **sm** | 4px | 小按钮、标签 |
| **md** | 8px | 默认按钮、输入框 |
| **lg** | 12px | 卡片、弹窗 |
| **xl** | 16px | 大图、特色卡片 |
| **full** | 9999px | 圆形按钮、头像 |

---

## 5. 阴影与深度

### 5.1 阴影层级

```css
/* 卡片 - 轻微浮起 */
--shadow-sm: 0 1px 2px rgba(0, 0, 0, 0.05);

/* 默认卡片、按钮悬停 */
--shadow-md: 0 4px 6px -1px rgba(0, 0, 0, 0.1), 
             0 2px 4px -2px rgba(0, 0, 0, 0.1);

/* 下拉菜单、悬浮面板 */
--shadow-lg: 0 10px 15px -3px rgba(0, 0, 0, 0.1), 
             0 4px 6px -4px rgba(0, 0, 0, 0.1);

/* 弹窗、模态框 */
--shadow-xl: 0 20px 25px -5px rgba(0, 0, 0, 0.1), 
             0 8px 10px -6px rgba(0, 0, 0, 0.1);
```

### 5.2 Fluent 深度效果

```css
/* Acrylic 毛玻璃效果 */
.acrylic {
  background: rgba(255, 255, 255, 0.85);
  backdrop-filter: blur(20px) saturate(125%);
  -webkit-backdrop-filter: blur(20px) saturate(125%);
}

/* Mica 材料效果 */
.mica {
  background: linear-gradient(
    135deg,
    rgba(255, 255, 255, 0.1) 0%,
    rgba(255, 255, 255, 0.05) 100%
  );
  backdrop-filter: blur(8px);
}
```

---

## 6. 组件规范

### 6.1 按钮 (Button)

#### 主按钮 (Primary)
- 背景: `#0078D4`
- 文字: `#FFFFFF`
- 内边距: `10px 16px`
- 圆角: `8px`
- 字号: `14px`，字重 `600`

**状态变化:**
| 状态 | 背景 | 阴影 |
|------|------|------|
| Default | #0078D4 | none |
| Hover | #106EBE | shadow-md |
| Active | #005A9E | inset shadow |
| Disabled | #F3F2F1, 文字 gray-400 | none |

#### 次级按钮 (Secondary)
- 背景: `#F3F2F1`
- 文字: `#242424`
- 边框: 无

#### 幽灵按钮 (Ghost)
- 背景: transparent
- 文字: `#0078D4`
- 悬停: 背景 `rgba(0, 120, 212, 0.08)`

### 6.2 输入框 (Input)

- 高度: `36px`
- 内边距: `8px 12px`
- 边框: `1px solid #E5E5E5`
- 圆角: `8px`
- 聚焦边框: `#0078D4`，`ring: 0 0 0 3px rgba(0,120,212,0.2)`

### 6.3 卡片 (Card)

- 背景: `#FFFFFF`
- 圆角: `12px`
- 内边距: `24px`
- 阴影: `--shadow-md`
- 悬停: `--shadow-lg` + `translateY(-2px)`

### 6.4 标签 (Tag)

- 背景: `#F3F2F1`
- 文字: `#616161`
- 内边距: `4px 12px`
- 圆角: `4px`
- 字号: `12px`

### 6.5 导航 (Navigation)

#### 顶部导航栏
- 高度: `64px`
- 背景: Acrylic 效果
- 底部边框: `1px solid rgba(0,0,0,0.08)`

#### 侧边导航
- 宽度: `240px` (展开) / `64px` (收起)
- 背景: `#FFFFFF` / `#1F1F1F`
- 项高: `44px`
- 选中: 左侧 `3px` 蓝色指示条 + 背景高亮

---

## 7. 图标系统

- **图标库**: Lucide Icons / Heroicons
- **默认尺寸**: 20px
- **线条粗细**: 1.5px
- **颜色**: 继承当前文字颜色

| 场景 | 尺寸 |
|------|------|
| 导航图标 | 20px |
| 按钮内图标 | 16px |
| 大图标 | 24px |
| 超大图标 | 32px |

---

## 8. 响应式断点

```css
/* 移动端优先 */
sm: 640px   /* 小平板 */
md: 768px   /* 平板竖屏 */
lg: 1024px  /* 平板横屏 / 小笔记本 */
xl: 1280px  /* 桌面 */
2xl: 1536px /* 大屏桌面 */
```

### 容器宽度

| 断点 | 最大宽度 | 内边距 |
|------|----------|--------|
| 默认 | 100% | 16px |
| sm | 640px | 24px |
| lg | 1024px | 32px |
| xl | 1200px | 48px |

---

## 9. 动画与过渡

### 9.1 缓动函数

```css
--ease-default: cubic-bezier(0.4, 0, 0.2, 1);
--ease-in: cubic-bezier(0.4, 0, 1, 1);
--ease-out: cubic-bezier(0, 0, 0.2, 1);
--ease-spring: cubic-bezier(0.34, 1.56, 0.64, 1);
```

### 9.2 持续时间

| 类型 | 时长 | 用途 |
|------|------|------|
| Instant | 0ms | 颜色变化 |
| Fast | 100ms | 按钮点击、图标切换 |
| Normal | 200ms | 悬停状态、展开收起 |
| Slow | 300ms | 页面过渡、模态框 |
| Page | 400ms | 路由切换 |

### 9.3 常用动画

```css
/* 淡入 */
@keyframes fadeIn {
  from { opacity: 0; }
  to { opacity: 1; }
}

/* 从下滑入 */
@keyframes slideUp {
  from { opacity: 0; transform: translateY(10px); }
  to { opacity: 1; transform: translateY(0); }
}

/* 缩放弹出 */
@keyframes scaleIn {
  from { opacity: 0; transform: scale(0.95); }
  to { opacity: 1; transform: scale(1); }
}
```

---

## 10. Tailwind 配置参考

```javascript
// tailwind.config.js 片段
module.exports = {
  theme: {
    extend: {
      colors: {
        primary: {
          DEFAULT: '#0078D4',
          hover: '#106EBE',
          active: '#005A9E',
        },
        surface: {
          DEFAULT: '#FFFFFF',
          secondary: '#F3F2F1',
          tertiary: '#FAF9F8',
        },
      },
      fontFamily: {
        sans: ['Segoe UI Variable', 'Segoe UI', 'system-ui', 'sans-serif'],
        mono: ['Cascadia Code', 'JetBrains Mono', 'Consolas', 'monospace'],
      },
      borderRadius: {
        'fluent': '8px',
        'fluent-lg': '12px',
      },
      boxShadow: {
        'fluent': '0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -2px rgba(0, 0, 0, 0.1)',
        'fluent-lg': '0 10px 15px -3px rgba(0, 0, 0, 0.1), 0 4px 6px -4px rgba(0, 0, 0, 0.1)',
      },
    },
  },
}
```

---

## 11. 无障碍 (A11y)

- **对比度**: 正文文字与背景对比度 ≥ 4.5:1
- **焦点样式**: 所有可交互元素必须有可见的焦点指示器
- **焦点环**: `ring: 0 0 0 3px rgba(0,120,212,0.4)`
- **减少动效**: 支持 `prefers-reduced-motion` 媒体查询

---

## 12. 资源链接

- [Fluent UI 官方文档](https://developer.microsoft.com/en-us/fluentui)
- [Fluent Design 系统](https://www.microsoft.com/design/fluent/)
- [Segoe UI Variable 字体](https://learn.microsoft.com/en-us/windows/apps/design/downloads/#fonts)
- [Lucide Icons](https://lucide.dev/)

---

*最后更新: 2025-03-14*
