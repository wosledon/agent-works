# .NET 开发团队

技术栈：React（前端）+ .NET（后端）

## 团队角色

| 角色 | 职责 | 技术关键词 |
|------|------|-----------|
| [项目经理](./roles/pm.md) | 进度管控、资源协调、风险管理 | 排期、里程碑、周报 |
| [产品经理](./roles/po.md) | 需求定义、用户故事、优先级排序 | PRD、原型、验收标准 |
| [架构师](./roles/architect.md) | 系统设计、技术选型、性能规划 | 架构图、API 设计、数据库 |
| [UI/UX 设计师](./roles/ui-designer.md) | 设计系统、视觉规范、交互原型 | Figma、Tailwind、组件库 |
| [前端开发](./roles/frontend.md) | React 组件开发、状态管理、UI 实现 | React, TypeScript, Tailwind |
| [后端开发](./roles/backend.md) | .NET API 开发、业务逻辑、数据层 | .NET 8/9, EF Core, Minimal API |
| [测试工程师](./roles/qa.md) | 测试用例、自动化测试、质量门禁 | xUnit, Playwright, CI/CD |

## 协作流程

```
PO 产出需求 → Architect 设计架构 → UI Designer 设计界面 → PM 排期拆分
                                    ↓
Frontend ↔ Backend 并行开发 → QA 测试 → 验收上线
```

**设计接力**: UI 设计师产出设计系统和组件规范后，前端才能开始组件开发。

## 启动新项目

1. 创建项目目录：`projects/{project-name}/`
2. 召集全员开会（spawn 各角色子代理）
3. 各角色输出初始文档到 `docs/`
4. PM 汇总里程碑到 `roadmap.md`

## 每日站会格式

- 昨日完成
- 今日计划
- 阻塞问题
