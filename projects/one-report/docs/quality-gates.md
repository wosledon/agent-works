# One-Report 质量门禁（Quality Gates）

> 项目: one-report（低代码报表工具）
> 生效日期: 2026-03-14

---

## 1. 质量门禁概述

质量门禁是代码合并和发布的强制性检查点。任何代码必须通过所有门禁才能进入下一阶段。

```
开发 → [门禁1: 代码质量] → [门禁2: 功能测试] → [门禁3: 性能基准] → 合并
                                       ↓
发布候选 → [门禁4: 全量回归] → [门禁5: 安全扫描] → [门禁6: 兼容性] → 发布
```

---

## 2. 门禁清单

### 2.1 门禁1: 代码质量门禁（Code Quality Gate）

**触发时机:** PR 创建、代码推送

| 检查项 | 工具 | 阈值 | 阻塞发布 |
|--------|------|------|----------|
| 单元测试覆盖率 | Jest/JUnit | ≥ 80% | ✅ |
| 代码重复率 | SonarQube | < 3% | ✅ |
| 代码异味 | SonarQube | 0 严重/阻断级 | ✅ |
| 安全漏洞 | SonarQube + Snyk | 0 高危/严重 | ✅ |
| Lint 检查 | ESLint/Checkstyle | 0 Error | ✅ |
| Type 检查 | TypeScript | 0 Error | ✅ |

**SonarQube 质量配置:**
```properties
# sonar-project.properties
sonar.coverage.exclusions=**/*.test.ts,**/mocks/**
sonar.cpd.exclusions=**/generated/**
sonar.qualitygate.wait=true
```

---

### 2.2 门禁2: 功能测试门禁（Functional Test Gate）

**触发时机:** 代码合并到 develop 分支

| 检查项 | 范围 | 通过率 | 阻塞发布 |
|--------|------|--------|----------|
| 冒烟测试 | P0 用例 | 100% | ✅ |
| 单元测试 | 全量 | 100% | ✅ |
| 集成测试 | 核心模块 | 100% | ✅ |
| API 契约测试 | 全部接口 | 100% | ✅ |

**P0 冒烟测试清单:**
- [ ] 用户登录/登出
- [ ] 报表创建与保存
- [ ] 数据源连接配置
- [ ] 基础数据查询
- [ ] 导出功能（Excel/CSV）

---

### 2.3 门禁3: 性能基准门禁（Performance Baseline Gate）

**触发时机:** 每日构建、发布候选版本

| 检查项 | 指标 | 阈值 | 阻塞发布 |
|--------|------|------|----------|
| 大文件导出 | 10万行数据 | < 15秒 | ✅ |
| 大文件导出 | 100万行数据 | < 120秒 | ⚠️ |
| 内存峰值 | 导出过程 | < 4GB | ✅ |
| 并发响应 | 20并发 | P95 < 30秒 | ✅ |
| 错误率 | 全量测试 | < 0.1% | ✅ |

**性能测试流水线:**
```yaml
# .github/workflows/perf-gate.yml
name: Performance Gate

on:
  schedule:
    - cron: '0 2 * * *'  # 每日凌晨2点
  workflow_dispatch:

jobs:
  performance:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Start Test Environment
        run: docker-compose -f docker-compose.test.yml up -d
      
      - name: Run k6 Performance Tests
        run: |
          k6 run --summary-export=perf.json tests/perf/export-benchmark.js
      
      - name: Check Performance Gate
        run: |
          node scripts/check-perf-gate.js perf.json
      
      - name: Upload Results
        uses: actions/upload-artifact@v4
        with:
          name: perf-results
          path: perf.json
```

**性能门禁检查脚本:**
```javascript
// scripts/check-perf-gate.js
const fs = require('fs');

const results = JSON.parse(fs.readFileSync(process.argv[2], 'utf8'));
const gates = [
  { name: 'export_100k', metric: 'http_req_duration{p95}', threshold: 15000 },
  { name: 'export_1m', metric: 'http_req_duration{p95}', threshold: 120000 },
  { name: 'memory_peak', metric: 'memory_peak', threshold: 4 * 1024 * 1024 * 1024 },
  { name: 'error_rate', metric: 'error_rate', threshold: 0.001 },
];

let failed = 0;
gates.forEach(gate => {
  const value = getMetric(results, gate.metric);
  if (value > gate.threshold) {
    console.error(`❌ Gate failed: ${gate.name} (${value} > ${gate.threshold})`);
    failed++;
  } else {
    console.log(`✅ Gate passed: ${gate.name}`);
  }
});

process.exit(failed > 0 ? 1 : 0);
```

---

### 2.4 门禁4: 内存安全门禁（Memory Safety Gate）

**触发时机:** 每次构建、大版本发布前

| 检查项 | 方法 | 阈值 | 阻塞发布 |
|--------|------|------|----------|
| 内存泄漏检测 | Heap dump 对比 | 无增长 > 10% | ✅ |
| GC 暂停时间 | GC logs | < 200ms | ⚠️ |
| OOM 风险 | 压力测试 | 0 OOM | ✅ |
| 内存回落 | 导出后 GC | 回落 > 80% | ⚠️ |

**内存测试流水线:**
```yaml
  memory-test:
    runs-on: ubuntu-latest
    steps:
      - name: Run Memory Profiling
        run: |
          # 启动应用并启用 JFR
          java -XX:+FlightRecorder \
               -XX:StartFlightRecording=filename=memory.jfr \
               -jar app.jar &
          
          # 执行内存测试
          node tests/memory/export-stress.js
          
          # 分析 JFR 数据
          jfr print --events OldObjectSample memory.jfr > memory-report.txt
      
      - name: Check Memory Gate
        run: |
          node scripts/check-memory-gate.js memory-report.txt
```

---

### 2.5 门禁5: 多数据源兼容性格禁（Compatibility Gate）

**触发时机:** 发布候选版本、数据源驱动更新

| 数据源 | 版本 | 连接测试 | 查询测试 | 导出测试 | 阻塞发布 |
|--------|------|----------|----------|----------|----------|
| MySQL | 8.0 | ✅ | ✅ | ✅ | P0 |
| PostgreSQL | 14+ | ✅ | ✅ | ✅ | P0 |
| SQL Server | 2019 | ✅ | ✅ | ✅ | P1 |
| Oracle | 19c | ✅ | ✅ | ✅ | P1 |
| MongoDB | 6.0 | ✅ | ✅ | ✅ | P1 |
| Elasticsearch | 8.x | ✅ | ✅ | ✅ | P2 |
| REST API | - | ✅ | ✅ | ✅ | P1 |

**兼容性测试矩阵:**
```yaml
# tests/compatibility/matrix.yml
matrix:
  databases:
    - name: mysql
      version: "8.0"
      image: mysql:8.0
      env:
        MYSQL_ROOT_PASSWORD: test
      
    - name: postgres
      version: "14"
      image: postgres:14
      env:
        POSTGRES_PASSWORD: test
    
    - name: mongodb
      version: "6.0"
      image: mongo:6.0
  
  tests:
    - connection
    - query_simple
    - query_complex
    - export_excel
    - export_csv
```

---

### 2.6 门禁6: 发布门禁（Release Gate）

**触发时机:** 生产发布前

| 检查项 | 负责人 | 标准 | 阻塞发布 |
|--------|--------|------|----------|
| 全量回归通过 | QA | 100% P0/P1 通过 | ✅ |
| 性能基准达标 | QA | 全部性能指标通过 | ✅ |
| 安全扫描 | Security | 0 高危漏洞 | ✅ |
| 兼容性验证 | QA | P0/P1 数据源通过 | ✅ |
| 文档完整 | Tech Writer | API 文档更新 | ✅ |
| 回滚方案 | DevOps | 方案就绪 | ✅ |
| 监控配置 | SRE | 告警规则配置 | ✅ |

**发布检查清单:**
```markdown
## 发布前检查
- [ ] 版本号已更新
- [ ] CHANGELOG 已更新
- [ ] 数据库迁移脚本已验证
- [ ] 配置变更已同步
- [ ] 回滚脚本已准备
- [ ] 生产监控已配置
- [ ] 值班人员已通知
```

---

## 3. 门禁阈值汇总

### 3.1 性能基准阈值

| 场景 | 响应时间 | 内存上限 | 并发数 | 错误率 |
|------|----------|----------|--------|--------|
| 报表列表查询 | < 500ms | < 200MB | 100 | < 0.1% |
| 报表预览 | < 3s | < 500MB | 50 | < 0.1% |
| 导出 1万行 | < 3s | < 512MB | 20 | < 0.1% |
| 导出 10万行 | < 15s | < 1GB | 10 | < 0.1% |
| 导出 100万行 | < 120s | < 4GB | 5 | < 0.5% |
| 导出 500万行 | < 600s | < 8GB | 2 | < 1% |

### 3.2 覆盖率阈值

| 类型 | 阈值 | 计算方式 |
|------|------|----------|
| 行覆盖率 | ≥ 80% | 已执行行 / 总行数 |
| 分支覆盖率 | ≥ 75% | 已执行分支 / 总分支 |
| 函数覆盖率 | ≥ 85% | 已执行函数 / 总函数 |
| 核心模块 | ≥ 90% | 报表引擎、导出模块 |

---

## 4. 门禁失败处理

### 4.1 失败响应流程

```
门禁失败
   │
   ▼
自动通知 → Slack #alerts-ci
   │
   ▼
创建 Jira Ticket → 分配给相关责任人
   │
   ▼
修复 → 重新触发门禁
   │
   ▼
通过 → 继续流程
```

### 4.2 豁免机制

以下情况可申请门禁豁免（需 Tech Lead + QA Lead 双签）:

1. **紧急热修复**: 安全漏洞修复
2. **已知问题**: 已有跟进 Ticket，不影响核心功能
3. **环境故障**: CI 环境问题导致失败

**豁免申请模板:**
```markdown
## 门禁豁免申请
- 门禁: [门禁名称]
- PR: [链接]
- 原因: [详细说明]
- 风险: [影响评估]
- 回滚方案: [如有]
- 申请人: [姓名]
- Tech Lead 审批: [签名]
- QA Lead 审批: [签名]
```

---

## 5. 监控与报告

### 5.1 门禁看板

```
┌─────────────────────────────────────────────────────┐
│  Quality Gates Dashboard                            │
├─────────────────────────────────────────────────────┤
│  本周门禁通过率: 94.5%                              │
│  平均修复时间: 2.3 小时                             │
├─────────────────────────────────────────────────────┤
│  Code Quality    ████████████████████░░░░  92%      │
│  Functional      ██████████████████████░░  95%      │
│  Performance     ██████████████████░░░░░░  88%      │
│  Memory          ████████████████████░░░░  91%      │
│  Compatibility   █████████████████████░░░  93%      │
└─────────────────────────────────────────────────────┘
```

### 5.2 周报内容

- 各门禁通过率趋势
- 高频失败项 TOP3
- 修复耗时分析
- 改进建议

---

## 6. 附录

### 6.1 相关文档

- [测试计划](./test-plan.md)
- [CI/CD 配置](./ci-cd-config.md)
- [监控告警规则](./monitoring-rules.md)

### 6.2 工具链

| 用途 | 工具 |
|------|------|
| 代码质量 | SonarQube |
| 性能测试 | k6, JMeter |
| 内存分析 | JFR, Chrome DevTools |
| 覆盖率 | JaCoCo, Istanbul |
| 安全扫描 | Snyk, SonarQube |
| 监控 | Prometheus + Grafana |

### 6.3 联系方式

| 角色 | 联系人 |
|------|--------|
| QA Lead | qa-lead@company.com |
| Tech Lead | tech-lead@company.com |
| DevOps | devops@company.com |

---

**文档版本:** v1.0  
**最后更新:** 2026-03-14  
**维护者:** QA Team & DevOps Team
