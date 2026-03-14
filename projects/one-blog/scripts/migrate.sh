#!/bin/bash
# 数据库迁移脚本 - one-blog
# 用法: ./scripts/migrate.sh [命令]

set -e

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

print_info() { echo -e "${BLUE}[INFO]${NC} $1"; }
print_success() { echo -e "${GREEN}[SUCCESS]${NC} $1"; }
print_warning() { echo -e "${YELLOW}[WARNING]${NC} $1"; }
print_error() { echo -e "${RED}[ERROR]${NC} $1"; }

PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$PROJECT_ROOT"

# 等待数据库就绪
wait_for_db() {
    print_info "等待 PostgreSQL 就绪..."
    local retries=30
    local wait_time=2
    
    while [ $retries -gt 0 ]; do
        if docker compose exec -T postgres pg_isready -U bloguser >/dev/null 2>&1; then
            print_success "PostgreSQL 已就绪"
            return 0
        fi
        
        retries=$((retries - 1))
        print_info "等待中... 剩余 $retries 次尝试"
        sleep $wait_time
    done
    
    print_error "PostgreSQL 未能在预期时间内就绪"
    return 1
}

# 创建迁移
create_migration() {
    local name=$1
    if [ -z "$name" ]; then
        print_error "请提供迁移名称"
        echo "用法: ./migrate.sh create [迁移名称]"
        exit 1
    fi
    
    print_info "创建迁移: $name"
    cd backend
    dotnet ef migrations add "$name"
    cd ..
    print_success "迁移创建完成"
}

# 更新数据库
update_database() {
    print_info "更新数据库..."
    
    # 检查容器是否运行
    if ! docker compose ps | grep -q "backend"; then
        print_warning "后端容器未运行，尝试启动..."
        docker compose up -d postgres
        wait_for_db
        docker compose up -d backend
        sleep 5
    fi
    
    docker compose exec backend dotnet ef database update
    print_success "数据库更新完成"
}

# 回滚迁移
rollback_migration() {
    local target=$1
    if [ -z "$target" ]; then
        target="0"
    fi
    
    print_info "回滚到迁移: $target"
    docker compose exec backend dotnet ef database update "$target"
    print_success "回滚完成"
}

# 列出所有迁移
list_migrations() {
    print_info "已应用的数据库迁移:"
    docker compose exec backend dotnet ef migrations list
}

# 显示帮助
show_help() {
    echo "one-blog 数据库迁移脚本"
    echo ""
    echo "用法: ./scripts/migrate.sh [命令] [参数]"
    echo ""
    echo "命令:"
    echo "  create [名称]   创建新的迁移"
    echo "  update          更新数据库到最新迁移"
    echo "  rollback [目标] 回滚到指定迁移 (默认回滚全部)"
    echo "  list            列出所有迁移"
    echo "  help            显示帮助"
    echo ""
    echo "示例:"
    echo "  ./scripts/migrate.sh create InitialCreate"
    echo "  ./scripts/migrate.sh update"
    echo "  ./scripts/migrate.sh rollback 0"
}

# 主逻辑
main() {
    local command=$1
    shift
    
    case "$command" in
        create)
            create_migration "$@"
            ;;
        update|up)
            update_database
            ;;
        rollback|down)
            rollback_migration "$@"
            ;;
        list|status)
            list_migrations
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
