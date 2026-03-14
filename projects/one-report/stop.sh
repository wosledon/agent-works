#!/bin/bash
# ==========================================
# OneReport 停止脚本
# 用法: ./stop.sh [-v|--volumes]
# ==========================================

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

cd "$SCRIPT_DIR"

REMOVE_VOLUMES=false

# 解析参数
for arg in "$@"; do
    case $arg in
        -v|--volumes)
            REMOVE_VOLUMES=true
            shift
            ;;
    esac
done

echo "停止 OneReport 服务..."

if [ "$REMOVE_VOLUMES" = true ]; then
    echo "警告: 将同时删除数据卷!"
    docker-compose down -v
else
    docker-compose down
fi

echo "服务已停止"
