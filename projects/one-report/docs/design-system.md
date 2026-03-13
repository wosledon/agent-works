# One-Report 设计系统

> 基于 Microsoft Fluent Design System 的低代码报表工具设计规范

## 目录
1. [设计原则](#设计原则)
2. [色彩系统](#色彩系统)
3. [字体排版](#字体排版)
4. [间距系统](#间距系统)
5. [阴影与深度](#阴影与深度)
6. [圆角系统](#圆角系统)
7. [组件库](#组件库)
8. [图标系统](#图标系统)
9. [动效规范](#动效规范)

---

## 设计原则

### Fluent Design 核心
One-Report 遵循 Microsoft Fluent Design 的设计语言：

- **光（Light）**：使用深度和光照创建层次结构
- **深度（Depth）**：Z轴空间层次，表达元素关系
- **动效（Motion）**：有意义的过渡，引导用户注意力
- **材质（Material）**： acrylic 材质效果，创造视觉层次
- **尺度（Scale）**：从细微到宏大的空间适配

### 工具类产品的特殊考量
- **效率优先**：减少认知负荷，快速完成任务
- **视觉密度**：平衡信息密度与可读性
- **专业感**：传达可靠、精确的品牌形象

---

## 色彩系统

### 主色调

| Token | 色值 | 用途 |
|-------|------|------|
| `--color-brand-100` | `#0F6CBD` | 主按钮、链接、激活状态 |
| `--color-brand-90` | `#115EA3` | 悬停状态 |
| `--color-brand-80` | `#0C3B5E` | 按下状态 |
| `--color-brand-60` | `#0F548C` | 深色背景上的强调 |

### 中性色

| Token | 色值 | 用途 |
|-------|------|------|
| `--color-neutral-100` | `#242424` | 主要文本 |
| `--color-neutral-90` | `#424242` | 次要文本 |
| `--color-neutral-80` | `#616161` | 占位文本、禁用状态 |
| `--color-neutral-70` | `#757575` | 图标默认色 |
| `--color-neutral-60` | `#808080` | 分割线 |
| `--color-neutral-40` | `#B3B3B3` | 边框、滑块 |
| `--color-neutral-30` | `#C2C2C2` | 悬停边框 |
| `--color-neutral-20` | `#E0E0E0` | 分隔线、输入框边框 |
| `--color-neutral-10` | `#F5F5F5` | 背景强调 |
| `--color-neutral-5` | `#F0F0F0` | 表头背景 |
| `--color-neutral-0` | `#FFFFFF` | 主背景 |

### 语义色

| 类型 | Token | 色值 | 用途 |
|------|-------|------|------|
| 成功 | `--color-success` | `#107C10` | 成功消息、保存确认 |
| 警告 | `--color-warning` | `#FFB900` | 警告提示 |
| 错误 | `--color-error` | `#D83B01` | 错误消息、删除确认 |
| 信息 | `--color-info` | `#0F6CBD` | 信息提示 |

### 工具特定色彩

| Token | 色值 | 用途 |
|-------|------|------|
| `--color-canvas-bg` | `#FAFAFA` | 设计器画布背景 |
| `--color-canvas-grid` | `#E8E8E8` | 画布网格线 |
| `--color-snap-guide` | `#0F6CBD` | 对齐辅助线 |
| `--color-selection` | `#0F6CBD` | 选中框 |
| `--color-component-ghost` | `rgba(15, 108, 189, 0.1)` | 拖拽预览 |

---

## 字体排版

### 字体栈

```css
--font-family-base: "Segoe UI Variable", "Segoe UI", system-ui, -apple-system, sans-serif;
--font-family-mono: "Cascadia Mono", "Consolas", monospace;
```

### 字号层级

| Token | 大小 | 行高 | 字重 | 用途 |
|-------|------|------|------|------|
| `--font-hero` | 28px | 36px | 600 | 页面标题 |
| `--font-title` | 20px | 28px | 600 | 区块标题 |
| `--font-subtitle` | 16px | 24px | 600 | 卡片标题 |
| `--font-body` | 14px | 20px | 400 | 正文内容 |
| `--font-caption` | 12px | 16px | 400 | 辅助说明、标签 |
| `--font-code` | 13px | 20px | 400 | 代码、数据预览 |

### 字重

| Token | 值 | 用途 |
|-------|-----|------|
| `--font-weight-regular` | 400 | 正文 |
| `--font-weight-medium` | 500 | 强调文本 |
| `--font-weight-semibold` | 600 | 标题、按钮 |

---

## 间距系统

### 基础间距单位：4px

| Token | 值 | 用途 |
|-------|-----|------|
| `--space-0` | 0px | 无间距 |
| `--space-2` | 2px | 微间距 |
| `--space-4` | 4px | 紧凑间距 |
| `--space-8` | 8px | 小间距 |
| `--space-12` | 12px | 默认间距 |
| `--space-16` | 16px | 中等间距 |
| `--space-20` | 20px | 大间距 |
| `--space-24` | 24px | 区块间距 |
| `--space-32` | 32px | 区域间距 |
| `--space-48` | 48px | 大区域分隔 |

### 布局间距

- **面板间距**：16px
- **卡片内边距**：16px
- **表单字段间距**：16px
- **工具栏高度**：48px
- **侧边栏宽度**：280px
- **属性面板宽度**：320px

---

## 阴影与深度

### Z-Index 层级

| 层级 | Z-Index | 用途 |
|------|---------|------|
| Base | 0 | 基础内容 |
| Elevated | 100 | 卡片、面板 |
| Navigation | 200 | 导航栏、侧边栏 |
| Overlay | 300 | 遮罩层 |
| Popover | 400 | 下拉菜单、提示 |
| Modal | 500 | 模态框 |
| Toast | 600 | 通知消息 |

### 阴影定义

| Token | 值 | 用途 |
|-------|-----|------|
| `--shadow-2` | `0 1px 2px rgba(0,0,0,0.1)` | 轻微提升 |
| `--shadow-4` | `0 2px 4px rgba(0,0,0,0.1)` | 卡片、输入框聚焦 |
| `--shadow-8` | `0 4px 8px rgba(0,0,0,0.1)` | 下拉菜单 |
| `--shadow-16` | `0 8px 16px rgba(0,0,0,0.12)` | 模态框、浮层面板 |
| `--shadow-64` | `0 32px 64px rgba(0,0,0,0.16)` | 对话框 |

---

## 圆角系统

| Token | 值 | 用途 |
|-------|-----|------|
| `--radius-none` | 0px | 工具栏、分割线 |
| `--radius-small` | 2px | 标签、小按钮 |
| `--radius-medium` | 4px | 按钮、输入框、卡片 |
| `--radius-large` | 8px | 大卡片、模态框 |
| `--radius-xlarge` | 12px | 对话框 |
| `--radius-full` | 999px | 圆形按钮、头像 |

---

## 组件库

### 按钮 Button

#### 主要按钮 Primary Button
- 背景：`--color-brand-100`
- 文字：白色
- 圆角：`--radius-medium` (4px)
- 高度：32px
- 内边距：8px 16px
- 悬停：`--color-brand-90`
- 按下：`--color-brand-80`

#### 次要按钮 Secondary Button
- 背景：透明
- 边框：1px solid `--color-neutral-20`
- 文字：`--color-neutral-100`
- 悬停背景：`--color-neutral-10`

#### 工具栏按钮 Toolbar Button
- 尺寸：32px × 32px
- 图标尺寸：20px
- 悬停背景：`--color-neutral-10`
- 激活背景：`--color-brand-100` (反转图标色)

#### 图标按钮 Icon Button
- 尺寸：32px × 32px
- 圆角：`--radius-medium`
- 透明背景
- 悬停：半透明品牌色背景

### 输入框 Input

#### 文本输入 Text Input
- 高度：32px
- 边框：1px solid `--color-neutral-20`
- 圆角：`--radius-medium`
- 内边距：8px 12px
- 聚焦边框：`--color-brand-100`
- 聚焦阴影：`0 0 0 2px rgba(15, 108, 189, 0.2)`

#### 下拉选择 Select
- 同文本输入样式
- 右侧箭头图标
- 下拉面板：`--shadow-8`

#### 搜索框 Search Input
- 左侧搜索图标
- 占位符颜色：`--color-neutral-80`
- 右侧清除按钮（有内容时显示）

### 面板 Panel

#### 侧边栏 Sidebar
- 宽度：280px
- 背景：`--color-neutral-0`
- 右边框：1px solid `--color-neutral-20`
- 内边距：16px

#### 属性面板 Property Panel
- 宽度：320px
- 背景：`--color-neutral-0`
- 左边框：1px solid `--color-neutral-20`

#### 工具面板 Tool Panel
- 可折叠分组
- 分组标题：14px 半粗体
- 展开/折叠箭头
- 分组间距：16px

### 数据展示 Data Display

#### 表格 Table
- 表头背景：`--color-neutral-5`
- 表头文字：12px 半粗体
- 行高：40px
- 行悬停：`--color-neutral-10`
- 选中行：`rgba(15, 108, 189, 0.08)`
- 分割线：1px solid `--color-neutral-20`

#### 列表 List
- 项高度：36px
- 项内边距：8px 16px
- 悬停：`--color-neutral-10`
- 选中：品牌色背景 + 品牌色左边框 3px

#### 树形控件 Tree
- 缩进：20px 每级
- 展开图标：12px
- 节点高度：32px

### 反馈 Feedback

#### 提示消息 Toast
- 位置：右下角，距边 24px
- 背景：`--color-neutral-100`
- 文字：白色
- 圆角：`--radius-large`
- 阴影：`--shadow-16`
- 自动消失：3000ms

#### 工具提示 Tooltip
- 背景：`--color-neutral-100`
- 文字：白色 12px
- 圆角：`--radius-small`
- 内边距：6px 10px
- 最大宽度：240px

#### 对话框 Dialog
- 宽度：480px（标准）、640px（大）
- 圆角：`--radius-xlarge`
- 阴影：`--shadow-64`
- 遮罩：`rgba(0, 0, 0, 0.4)`
- 头部：标题 + 关闭按钮
- 底部：操作按钮区（右对齐）

### 报表设计器专属组件

#### 画布 Canvas
- 背景：`--color-canvas-bg`
- 网格：20px 点阵或线阵
- 网格颜色：`--color-canvas-grid`
- 页面边距：20px

#### 组件 Component
- 选中边框：2px dashed `--color-selection`
- 调整手柄：8px 正方形，白色填充
- 拖拽预览：`--color-component-ghost`

#### 组件面板 Component Palette
- 分类分组
- 组件项：图标 + 名称
- 拖拽手柄

---

## 图标系统

### 图标库
使用 **Fluent UI System Icons** (Microsoft 官方图标)

### 图标尺寸

| 尺寸 | 用途 |
|------|------|
| 16px | 内联图标、列表 |
| 20px | 按钮、工具栏 |
| 24px | 导航、大按钮 |
| 32px | 空状态、功能图标 |

### 图标颜色

| 状态 | 颜色 |
|------|------|
| 默认 | `--color-neutral-70` |
| 悬停 | `--color-neutral-100` |
| 激活 | `--color-brand-100` |
| 禁用 | `--color-neutral-40` |

### 核心图标映射

| 功能 | 图标名称 |
|------|----------|
| 添加 | Add |
| 删除 | Delete |
| 编辑 | Edit |
| 保存 | Save |
| 导出 | Export |
| 预览 | Eye |
| 设置 | Settings |
| 数据源 | Database |
| 文本 | Text |
| 表格 | Table |
| 图表 | Chart |
| 图片 | Image |
| 拖拽 | Drag |
| 撤销 | ArrowUndo |
| 重做 | ArrowRedo |
| 放大 | ZoomIn |
| 缩小 | ZoomOut |

---

## 动效规范

### 缓动函数

| 名称 | 值 | 用途 |
|------|-----|------|
| Standard | `cubic-bezier(0.4, 0.0, 0.2, 1)` | 普通过渡 |
| Decelerate | `cubic-bezier(0.0, 0.0, 0.2, 1)` | 元素进入 |
| Accelerate | `cubic-bezier(0.4, 0.0, 1, 1)` | 元素退出 |
| Bounce | `cubic-bezier(0.34, 1.56, 0.64, 1)` | 弹性效果 |

### 持续时间

| 时长 | 用途 |
|------|------|
| 100ms | 微交互（按钮按下） |
| 200ms | 小状态变化（悬停、聚焦） |
| 300ms | 标准过渡（展开、折叠） |
| 500ms | 大元素动画（模态框、侧边栏） |

### 具体动效

#### 悬停效果
- 背景色过渡：200ms Standard
- 缩放效果：150ms Standard

#### 展开/折叠
- 高度变化：300ms Standard
- 透明度：同步 300ms

#### 拖拽反馈
- 开始拖拽：100ms scale(1.02)
- 放置动画：200ms Bounce
- 对齐吸附：150ms Decelerate

#### 页面切换
- 滑入滑出：300ms Standard
- 淡入淡出：200ms Standard

#### 加载状态
- 旋转动画：1s linear infinite
- 脉冲动画：1.5s ease-in-out infinite

---

## 无障碍设计

### 色彩对比度
- 正文文本对比度 ≥ 4.5:1
- 大文本对比度 ≥ 3:1
- UI组件对比度 ≥ 3:1

### 焦点状态
- 所有可交互元素有可见焦点环
- 焦点环：2px solid `--color-brand-100`
- 偏移：2px

### 键盘导航
- Tab 键顺序符合视觉顺序
- 支持所有功能的键盘操作
- 提供快捷键提示

---

## 暗色模式（预留）

| Token | 色值 |
|-------|------|
| `--color-bg-primary-dark` | `#1F1F1F` |
| `--color-bg-secondary-dark` | `#2D2D2D` |
| `--color-bg-tertiary-dark` | `#3D3D3D` |
| `--color-text-primary-dark` | `#FFFFFF` |
| `--color-text-secondary-dark` | `#C8C8C8` |

---

*文档版本：1.0*
*最后更新：2026-03-14*
