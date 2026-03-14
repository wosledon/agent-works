#!/bin/bash
# 一键启动脚本 - one-blog 部署
# 用法: ./deploy.sh [dev|prod|stop|logs|clean]

set -e

# 颜色定义
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# 项目根目录
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
cd "$PROJECT_ROOT"

# 打印带颜色的消息
print_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# 检查 Docker 和 Docker Compose
check_docker() {
    if ! command -v docker >/dev/null 2>&1; then
        print_error "Docker 未安装，请先安装 Docker"
        exit 1
    fi
    
    if ! command -v docker compose >/dev/null 2>&1; then
        print_error "Docker Compose 未安装，请先安装 Docker Compose"
        exit 1
    fi
    
    # 检查 Docker 守护进程
    if ! docker info >/dev/null 2>&1; then
        print_error "Docker 守护进程未运行"
        exit 1
    fi
    
    print_success "Docker 环境检查通过"
}

# 检查环境文件
check_env() {
    if [ ! -f ".env" ]; then
        print_warning ".env 文件不存在，使用 .env.example 创建"
        cp .env.example .env
        print_warning "请编辑 .env 文件配置实际的环境变量后再运行"
        exit 1
    fi
    print_success "环境配置检查通过"
}

# 生成安全的 JWT 密钥
generate_secrets() {
    if [ -f ".env" ]; then
        # 检查是否使用了默认密钥
        if grep -q "your_jwt_secret_key_here" .env || grep -q "your_jwt_refresh_secret_key_here" .env; then
            print_warning "检测到默认 JWT 密钥，正在生成安全密钥..."
            
            JWT_SECRET=$(openssl rand -base64 32)
            JWT_REFRESH_SECRET=$(openssl rand -base64 32)
            
            # 使用临时文件避免 sed 兼容性问题
            sed "s/your_jwt_secret_key_here_min_32_chars/${JWT_SECRET}/g" .env > .env.tmp && \
            sed "s/your_jwt_refresh_secret_key_here/${JWT_REFRESH_SECRET}/g" .env.tmp > .env && \
            rm .env.tmp
            
            print_success "已自动生成并更新 JWT 密钥"
        fi
    fi
}

# 开发环境启动
dev_up() {
    print_info "启动开发环境..."
    check_env
    generate_secrets
    
    docker compose up -d postgres
    print_info "等待 PostgreSQL 启动..."
    sleep 5
    
    docker compose up -d backend
    print_info "等待后端启动..."
    sleep 10
    
    # 开发环境前端使用本地 npm run dev
    print_info "前端请手动运行: cd frontend && npm run dev"
    
    print_success "开发环境已启动！"
    echo "  - API: http://localhost:8080"
    echo "  - 前端: http://localhost:5173 (请手动启动)"
}

# 生产环境启动
prod_up() {
    print_info "启动生产环境..."
    check_env
    generate_secrets
    
    print_info "构建并启动所有服务..."
    docker compose --profile production up -d --build
    
    print_info "等待服务启动..."
    sleep 15
    
    # 健康检查
    print_info "执行健康检查..."
    check_health
    
    print_success "生产环境已启动！"
    echo "  - 网站: http://localhost (或配置的域名)"
    echo "  - API: http://localhost/api"
}

# 带 Redis 的生产环境
prod_with_redis() {
    print_info "启动生产环境 (含 Redis 缓存)..."
    check_env
    
    # 启用 Redis
    sed -i 's/REDIS_ENABLED=false/REDIS_ENABLED=true/' .env
    
    docker compose --profile production up -d redis
    sleep 3
    
    prod_up
}

# 停止服务
stop_services() {
    print_info "停止所有服务..."
    docker compose --profile production down
    docker compose down
    print_success "所有服务已停止"
}

# 查看日志
show_logs() {
    local service=$1
    if [ -n "$service" ]; then
        docker compose logs -f "$service"
    else
        docker compose logs -f
    fi
}

# 清理环境
clean() {
    print_warning "这将删除所有容器和数据卷！"
    read -p "确定要继续吗？输入 'yes' 确认: " confirm
    if [ "$confirm" = "yes" ]; then
        docker compose --profile production down -v
        docker compose down -v
        docker system prune -f
        print_success "清理完成"
    else
        print_info "已取消清理"
    fi
}

# 健康检查
check_health() {
    print_info "检查服务健康状态..."
    
    # 检查后端
    if curl -sf http://localhost:8080/health >/dev/null 2>&1; then
        print_success "后端服务健康"
    else
        print_error "后端服务未就绪"
    fi
    
    # 检查前端
    if curl -sf http://localhost:80/health >/dev/null 2>&1; then
        print_success "前端服务健康"
    else
        print_warning "前端服务可能未就绪"
    fi
}

# 数据库迁移
run_migration() {
    print_info "执行数据库迁移..."
    docker compose exec backend dotnet ef database update
}

# 显示帮助
show_help() {
    echo "one-blog 部署脚本"
    echo ""
    echo "用法: ./deploy.sh [命令]"
    echo ""
    echo "命令:"
    echo "  dev          启动开发环境 (PostgreSQL + 后端)"
    echo "  prod         启动生产环境 (全栈 + Nginx)"
    echo "  prod-redis   启动生产环境 (含 Redis 缓存)"
    echo "  stop         停止所有服务"
    echo "  logs [服务]  查看日志，可指定服务名"
    echo "  health       健康检查"
    echo "  migrate      执行数据库迁移"
    echo "  clean        清理所有容器和数据"
    echo "  help         显示帮助"
}

# 主逻辑
main() {
    local command=$1
    shift
    
    case "$command" in
        dev)
            check_docker
            dev_up
            ;;
        prod)
            check_docker
            prod_up
            ;;
        prod-redis)
            check_docker
            prod_with_redis
            ;;
        stop)
            stop_services
            ;;
        logs)
            show_logs "$@"
            ;;
        health)
            check_health
            ;;
        migrate)
            run_migration
            ;;
        clean)
            clean
            ;;
        help|--help|-h|"")
            show_help
            ;;
        *)
            print_error "未知命令: $command"
            show_help
            exit 1
            ;;
    esac
}

main "$@"
