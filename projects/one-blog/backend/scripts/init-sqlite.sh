#!/bin/bash
# SQLite 数据库初始化脚本
# 用于本地开发环境快速初始化

echo "Initializing SQLite database for one-blog..."

# 创建数据库目录
mkdir -p data

# 设置环境变量
export ASPNETCORE_ENVIRONMENT=Development
export ConnectionStrings__DefaultConnection="DataSource=data/blog.db"

cd "$(dirname "$0")/../backend"

# 还原包
dotnet restore

# 运行迁移
echo "Applying database migrations..."
dotnet ef database update

echo ""
echo "SQLite database initialized successfully!"
echo "Database location: data/blog.db"
