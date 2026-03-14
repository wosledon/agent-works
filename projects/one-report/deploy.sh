#!/bin/bash
# ==========================================
# OneReport 一键部署脚本
# 用法: ./deploy.sh [dev|prod]
# ==========================================

set -e

# 颜色定义
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# 默认环境
ENV=${1:-dev}
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_NAME="one-report"

echo -e "${GREEN}==========================================${NC}"
echo -e "${GREEN}  OneReport 部署脚本${NC}"
echo -e "${GREEN}  环境: $ENV${NC}"
echo -e "${GREEN}==========================================${NC}"

# 检查 Docker
check_docker() {
    echo -e "${YELLOW}[1/6] 检查 Docker 环境...${NC}"
    if ! command -v docker &> /dev/null; then
        echo -e "${RED}错误: Docker 未安装${NC}"
        exit 1
    fi
    if ! command -v docker-compose &> /dev/null && ! docker compose version &> /dev/null; then
        echo -e "${RED}错误: Docker Compose 未安装${NC}"
        exit 1
    fi
    echo -e "${GREEN}  ✓ Docker 环境正常${NC}"
}

# 加载环境变量
load_env() {
    echo -e "${YELLOW}[2/6] 加载环境配置...${NC}"
    cd "$SCRIPT_DIR"
    
    if [ -f ".env.$ENV" ]; then
        export $(cat ".env.$ENV" | grep -v '^#' | xargs)
        echo -e "${GREEN}  ✓ 加载 .env.$ENV${NC}"
    elif [ -f ".env" ]; then
        export $(cat ".env" | grep -v '^#' | xargs)
        echo -e "${GREEN}  ✓ 加载 .env${NC}"
    else
        echo -e "${YELLOW}  ⚠ 未找到环境文件，使用默认配置${NC}"
    fi
}

# 构建镜像
build_images() {
    echo -e "${YELLOW}[3/6] 构建 Docker 镜像...${NC}"
    cd "$SCRIPT_DIR"
    
    if [ "$ENV" = "prod" ]; then
        docker-compose build --no-cache
    else
        docker-compose build
    fi
    
    echo -e "${GREEN}  ✓ 镜像构建完成${NC}"
}

# 启动服务
start_services() {
    echo -e "${YELLOW}[4/6] 启动服务...${NC}"
    cd "$SCRIPT_DIR"
    
    docker-compose down --remove-orphans 2>/dev/null || true
    
    if [ "$ENV" = "prod" ]; then
        docker-compose up -d
    else
        docker-compose up -d
    fi
    
    echo -e "${GREEN}  ✓ 服务启动完成${NC}"
}

# 等待服务就绪
wait_for_services() {
    echo -e "${YELLOW}[5/6] 等待服务就绪...${NC}"
    
    # 等待数据库
    echo -e "    等待 PostgreSQL..."
    until docker exec one-report-postgres pg_isready -U ${POSTGRES_USER:-postgres} >/dev/null 2>&1; do
        sleep 1
    done
    echo -e "${GREEN}    ✓ PostgreSQL 就绪${NC}"
    
    # 等待 API
    echo -e "    等待 API 服务..."
    for i in {1..30}; do
        if curl -sf http://localhost:${API_PORT:-5000}/api/system/health >/dev/null 2>&1; then
            echo -e "${GREEN}    ✓ API 服务就绪${NC}"
            break
        fi
        sleep 2
    done
    
    # 等待前端
    echo -e "    等待前端服务..."
    for i in {1..30}; do
        if curl -sf http://localhost:${FRONTEND_PORT:-80}/health >/dev/null 2>&1; then
            echo -e "${GREEN}    ✓ 前端服务就绪${NC}"
            break
        fi
        sleep 2
    done
}

# 显示状态
show_status() {
    echo -e "${YELLOW}[6/6] 部署状态${NC}"
    echo -e "${GREEN}==========================================${NC}"
    docker-compose ps
    echo -e "${GREEN}==========================================${NC}"
    echo -e "${GREEN}服务访问地址:${NC}"
    echo -e "  前端: http://localhost:${FRONTEND_PORT:-80}"
    echo -e "  API:  http://localhost:${API_PORT:-5000}"
    echo -e "  API文档: http://localhost:${API_PORT:-5000}/swagger"
    echo -e "${GREEN}==========================================${NC}"
}

# 主流程
main() {
    check_docker
    load_env
    build_images
    start_services
    wait_for_services
    show_status
    
    echo -e "${GREEN}部署完成!${NC}"
}

main
