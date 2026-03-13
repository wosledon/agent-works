#!/bin/bash
# 内存数据库快速启动脚本（适用于测试/演示）
# 数据不会持久化，重启后消失

echo "Starting one-blog with in-memory SQLite database..."

export ASPNETCORE_ENVIRONMENT=Development
export ConnectionStrings__DefaultConnection=":memory:"

cd "$(dirname "$0")/../backend"

dotnet run --urls "http://localhost:5000"
