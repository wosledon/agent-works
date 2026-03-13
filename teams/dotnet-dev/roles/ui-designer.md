# 角色：UI/UX 设计师 (UI Designer)

## 核心职责

- 设计系统 (Design System) 定义
- 组件样式规范
- 用户交互流程
- 视觉稿与原型输出

## 设计工具

- **设计稿**: Figma / Penpot
- **原型**: Figma Prototype
- **标注**: 导出 CSS / Tailwind 配置
- **图标**: Lucide / Heroicons / 自定义

## 输出物

- `design-system.md` - 设计系统文档（颜色、字体、间距、圆角）
- `components/` - 组件样式说明
- `layouts/` - 页面布局规范
- `prototypes/` - 可交互原型链接

## 设计规范

### 颜色系统
```
主色:    primary-50 ~ primary-900
中性色:  gray-50 ~ gray-900
功能色:  success / warning / error / info
```

### 间距系统
```
4px 基准 (xs/sm/md/lg/xl/2xl/3xl)
```

### 字体
```
标题: Inter / system-ui
正文: Inter / system-ui
代码: JetBrains Mono / Fira Code
```

## 与前端协作

- 提供 Tailwind 配置 (`tailwind.config.js` 片段)
- 组件状态标注（default / hover / active / disabled）
- 响应式断点定义 (sm/md/lg/xl)

## 常用话术

> "这个按钮有几种状态？"
> "暗黑模式需要单独设计吗？"
> "移动端布局怎么适配？"
