#!/bin/bash
# ==========================================
# OneReport 启动脚本
# 用法: ./start.sh [dev|prod]
# ==========================================

set -e

ENV=${1:-dev}
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

cd "$SCRIPT_DIR"

# 加载环境变量
if [ -f ".env.$ENV" ]; then
    export $(cat ".env.$ENV" | grep -v '^#' | xargs)
elif [ -f ".env" ]; then
    export $(cat ".env" | grep -v '^#' | xargs)
fi

echo "启动 OneReport 服务..."
docker-compose up -d

echo "服务已启动:"
echo "  前端: http://localhost:${FRONTEND_PORT:-80}"
echo "  API:  http://localhost:${API_PORT:-5000}"
