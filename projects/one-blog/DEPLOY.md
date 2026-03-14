# one-blog 部署指南

## 快速开始

### 1. 环境准备

确保已安装：
- Docker Engine 24.0+
- Docker Compose 2.20+
- Git

### 2. 配置环境变量

```bash
# 复制环境变量模板
cp .env.example .env

# 编辑 .env 文件，配置实际值
nano .env
```

关键配置项：
- `DB_PASSWORD` - 数据库密码（必须修改）
- `JWT_SECRET_KEY` - JWT 签名密钥（脚本会自动生成）

### 3. 一键部署

```bash
# 开发环境
./scripts/deploy.sh dev

# 生产环境
./scripts/deploy.sh prod

# 带 Redis 缓存的生产环境
./scripts/deploy.sh prod-redis
```

## 常用命令

| 命令 | 说明 |
|------|------|
| `./scripts/deploy.sh dev` | 启动开发环境 |
| `./scripts/deploy.sh prod` | 启动生产环境 |
| `./scripts/deploy.sh stop` | 停止所有服务 |
| `./scripts/deploy.sh logs [服务]` | 查看日志 |
| `./scripts/deploy.sh health` | 健康检查 |
| `./scripts/deploy.sh clean` | 清理环境 |

### 数据库迁移

```bash
# 更新数据库
./scripts/migrate.sh update

# 创建新迁移
./scripts/migrate.sh create MigrationName

# 列出迁移
./scripts/migrate.sh list

# 回滚迁移
./scripts/migrate.sh rollback 0
```

## 服务架构

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│   Nginx     │────▶│   Frontend  │     │   Redis     │
│   (80/443)  │     │   (React)   │     │   (6379)    │
└──────┬──────┘     └─────────────┘     └─────────────┘
       │
       ▼
┌─────────────┐     ┌─────────────┐
│   Backend   │────▶│  PostgreSQL │
│   (8080)    │     │   (5432)    │
└─────────────┘     └─────────────┘
```

## 健康检查端点

- `GET /health` - 后端健康检查
- `GET /health` - 前端健康检查 (通过 Nginx)
- `GET /nginx-health` - Nginx 健康检查

## 生产环境注意事项

1. **SSL 证书**: 将证书放在 `nginx/ssl/` 目录，并在 `nginx/conf.d/default.conf` 中启用 HTTPS
2. **数据备份**: 定期备份 `postgres_data` Docker 卷
3. **日志**: Nginx 日志挂载在 `nginx_logs` Docker 卷
4. **安全**: 修改默认的数据库密码和 JWT 密钥

## 故障排查

```bash
# 查看所有容器状态
docker compose ps

# 查看后端日志
docker compose logs backend

# 进入后端容器
docker compose exec backend sh

# 重启服务
docker compose restart backend
```
