# ==========================================
# OneReport 部署文档
# ==========================================

## 快速开始

### 1. 环境准备

- Docker 20.10+
- Docker Compose 2.0+
- 4GB 可用内存
- 10GB 可用磁盘空间

### 2. 配置环境变量

```bash
# 复制环境配置模板
cp .env.example .env

# 编辑配置文件
vim .env
```

### 3. 一键部署

```bash
# 开发环境
./deploy.sh dev

# 生产环境
./deploy.sh prod
```

### 4. 访问服务

- 前端界面: http://localhost
- API 服务: http://localhost:5000
- API 文档: http://localhost:5000/swagger

## 常用命令

```bash
# 启动服务
./start.sh

# 停止服务
./stop.sh

# 停止并删除数据
./stop.sh -v

# 查看日志
docker-compose logs -f

# 备份数据库
./backup.sh

# 重新构建并部署
./deploy.sh prod
```

## 目录结构

```
one-report/
├── backend/
│   ├── Dockerfile          # 后端 Docker 配置
│   └── src/
├── frontend/
│   ├── Dockerfile          # 前端 Docker 配置
│   ├── nginx.conf          # Nginx 配置
│   └── ...
├── scripts/
│   └── init-db.sql         # 数据库初始化
├── docker-compose.yml      # 服务编排
├── .env.example            # 环境配置模板
├── .env                    # 环境配置（本地）
├── deploy.sh               # 部署脚本
├── start.sh                # 启动脚本
├── stop.sh                 # 停止脚本
└── backup.sh               # 备份脚本
```

## 生产环境建议

### 1. 安全配置

- 修改默认数据库密码
- 使用 HTTPS（配合反向代理）
- 配置防火墙规则
- 定期更新镜像

### 2. 性能优化

- 启用 Redis 缓存: `docker-compose --profile with-redis up -d`
- 调整数据库连接池大小
- 配置 CDN 加速静态资源

### 3. 监控告警

- 配置健康检查告警
- 监控磁盘空间
- 设置日志轮转

### 4. 备份策略

```bash
# 添加定时任务 (crontab -e)
# 每天凌晨2点备份
0 2 * * * /path/to/one-report/backup.sh /data/backups
```

## 故障排查

### 服务无法启动

```bash
# 检查日志
docker-compose logs api
docker-compose logs frontend
docker-compose logs postgres

# 检查端口占用
netstat -tlnp | grep -E '80|5000|5432'
```

### 数据库连接失败

```bash
# 检查数据库状态
docker exec one-report-postgres pg_isready -U postgres

# 重置数据库（会丢失数据）
docker-compose down -v
docker-compose up -d postgres
```

### 前端无法访问 API

```bash
# 检查网络
docker network ls
docker network inspect one-report-network

# 测试 API 连通性
curl http://localhost:5000/api/system/health
```
