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

## 启动新项目流程

```
1. Kimi 询问用户项目需求
2. **用户确认项目名称** ← 必须步骤，未确认不得启动
3. 创建项目目录：`projects/{project-name}/`
4. **明确告知各角色：工作目录是 `projects/{project-name}/`** ← 防止写错位置
5. **告知 Git 提交规范**：小步快跑，1-2 小时必须提交一次
6. **告知云端组件规范**：所有外部依赖必须提供本地/内存降级方案
7. 召集全员开会（spawn 各角色子代理，显式指定输出路径）
8. 各角色输出初始文档到 `docs/`
9. PM 整合撰写项目 `README.md`
10. PM 汇总里程碑到 `roadmap.md`
```

⚠️ **重要规矩**:
- 项目名称必须由用户确认
- 每次 spawn 必须显式指定 `输出到：projects/{name}/`
- **Git 提交**：小步快跑，禁止攒代码，详见 [git-guidelines.md](./git-guidelines.md)
- **云端组件**：必须支持本地/内存降级，详见 [cloud-component-guidelines.md](./cloud-component-guidelines.md)
- **前端语言**：**强制 TypeScript**，禁止 JavaScript

## 截图流程（开发完成后）

PM 在 README 中标记截图位置，Kimi 执行截图：

```markdown
<!-- PM 标注 --
[SCREENSHOT:homepage]

<!-- Kimi 执行后替换为 --
![首页预览](./docs/screenshots/homepage.png)
```

**时机**: 项目开发完成，本地/部署可访问后
**工具**: 无头浏览器 (Playwright)
**存储**: `docs/screenshots/`

## 项目 README 模板

每个项目的 `README.md` 由 **PM 统筹撰写**，整合各角色产出：

```markdown
# {项目名称}

## 项目概述
产品经理产出 - 一句话描述 + 核心价值

## 技术架构
架构师产出 - 架构图 + 技术栈

## 设计规范
UI 设计师产出 - 设计系统链接 + 风格说明

## 项目成员
| 角色 | 负责人 |
|------|--------|
| PM | @... |
| PO | @... |
| ... | ... |

## 快速开始
前端/后端产出 - 本地运行步骤

## 项目进度
PM 维护 - 当前阶段 + 里程碑

## 相关文档
- [产品需求](./docs/prd.md)
- [API 文档](./docs/api-contract.yaml)
- [测试计划](./docs/test-plan.md)
```

## 每日站会格式

- 昨日完成
- 今日计划
- 阻塞问题
