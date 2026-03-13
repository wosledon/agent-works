# 质量门禁配置

## 代码提交门禁 (Pre-commit / CI)

### 单元测试门禁
```yaml
# .github/workflows/quality-gates.yml
name: Quality Gates

on: [push, pull_request]

jobs:
  unit-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      
      - name: Run Unit Tests
        run: |
          dotnet test tests/unit \
            --collect:"XPlat Code Coverage" \
            --logger trx
      
      - name: Check Coverage
        uses: irongut/CodeCoverageSummary@v1.3.0
        with:
          filename: '**/coverage.cobertura.xml'
          badge: true
          fail_below_min: true
          min_coverage: 70
          format: markdown
```

### 代码扫描门禁
```yaml
  code-scan:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Run SonarQube
        uses: SonarSource/sonarqube-scan-action@master
        env:
          SONAR_TOKEN: ${{ secrets.SONAR_TOKEN }}
        
      - name: Check Vulnerabilities
        run: |
          dotnet list package --vulnerable --include-transitive 2>/dev/null | \
            grep -q "has the following vulnerable packages" && exit 1 || exit 0
```

---

## 集成测试门禁

### API 契约测试
```yaml
  integration-tests:
    runs-on: ubuntu-latest
    services:
      postgres:
        image: postgres:16
        env:
          POSTGRES_PASSWORD: test
          POSTGRES_DB: blog_test
        options: >-
          --health-cmd pg_isready
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5
        ports:
          - 5432:5432
    
    steps:
      - uses: actions/checkout@v4
      
      - name: Run Integration Tests
        run: |
          dotnet test tests/integration \
            --logger trx \
            --verbosity normal
        env:
          ConnectionStrings__Default: "Host=localhost;Port=5432;Database=blog_test;Username=postgres;Password=test"
```

---

## E2E 测试门禁

### Playwright 关键路径测试
```yaml
  e2e-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup Node
        uses: actions/setup-node@v4
        with:
          node-version: '20'
      
      - name: Install Playwright
        run: |
          npm install -g @playwright/test
          npx playwright install --with-deps
      
      - name: Start Application
        run: |
          docker-compose -f docker-compose.test.yml up -d
          sleep 30  # 等待服务启动
      
      - name: Run E2E Tests
        run: |
          npx playwright test tests/e2e/critical/
      
      - name: Upload Report
        uses: actions/upload-artifact@v4
        with:
          name: playwright-report
          path: playwright-report/
        if: always()
```

---

## 发布前门禁 (Release Gates)

### 门禁清单

| 检查项 | 标准 | 检查方式 |
|--------|------|----------|
| 单元测试通过率 | 100% | CI 自动 |
| 代码覆盖率 | ≥ 70% | Coverlet 报告 |
| 集成测试通过率 | 100% | CI 自动 |
| E2E 关键路径 | 100% | Playwright |
| P0/P1 缺陷 | 0 | JIRA 查询 |
| 安全漏洞 | 0 高危 | SonarQube |
| 性能基准 | P95 < 500ms | k6 负载测试 |

### 发布脚本
```bash
#!/bin/bash
# scripts/release-gate.sh

echo "=== 发布前质量门禁检查 ==="

# 1. 单元测试
if ! dotnet test tests/unit --no-build; then
    echo "❌ 单元测试未通过"
    exit 1
fi
echo "✅ 单元测试通过"

# 2. 检查覆盖率
COVERAGE=$(dotnet test tests/unit --collect:"XPlat Code Coverage" 2>/dev/null | grep -oP 'Total.*\K[0-9.]+' || echo "0")
if (( $(echo "$COVERAGE < 70" | bc -l) )); then
    echo "❌ 代码覆盖率 $COVERAGE% 低于 70%"
    exit 1
fi
echo "✅ 代码覆盖率 $COVERAGE%"

# 3. 集成测试
if ! dotnet test tests/integration; then
    echo "❌ 集成测试未通过"
    exit 1
fi
echo "✅ 集成测试通过"

# 4. E2E 关键路径
if ! npx playwright test tests/e2e/critical; then
    echo "❌ E2E 关键路径测试未通过"
    exit 1
fi
echo "✅ E2E 关键路径测试通过"

# 5. 缺陷检查
P0_COUNT=$(curl -s "https://jira.example.com/api/..." | jq '.total')
if [ "$P0_COUNT" -gt 0 ]; then
    echo "❌ 存在 $P0_COUNT 个 P0/P1 缺陷"
    exit 1
fi
echo "✅ 无 P0/P1 缺陷"

echo ""
echo "🎉 所有质量门禁通过，可以发布！"
```

---

## 环境门禁

### 各环境准入标准

```
┌────────────────────────────────────────────────────────┐
│  开发环境 (Dev)                                         │
│  ├─ 代码审查通过                                        │
│  ├─ 本地单元测试通过                                    │
│  └─ 无编译错误                                          │
├────────────────────────────────────────────────────────┤
│  测试环境 (Test)                                        │
│  ├─ CI 全部通过                                         │
│  ├─ 单元覆盖率 ≥ 70%                                    │
│  └─ 代码扫描无高危漏洞                                  │
├────────────────────────────────────────────────────────┤
│  预发布环境 (Staging)                                   │
│  ├─ 集成测试 100% 通过                                  │
│  ├─ E2E 关键路径通过                                    │
│  └─ 性能测试达标                                        │
├────────────────────────────────────────────────────────┤
│  生产环境 (Production)                                  │
│  ├─ 全量回归测试通过                                    │
│  ├─ 无 P0/P1 缺陷                                       │
│  ├─ 发布清单审批                                        │
│  └─ 回滚方案就绪                                        │
└────────────────────────────────────────────────────────┘
```
