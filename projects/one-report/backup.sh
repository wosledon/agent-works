#!/bin/bash
# ==========================================
# OneReport 数据库备份脚本
# 用法: ./backup.sh [备份目录]
# ==========================================

set -e

BACKUP_DIR=${1:-"./backups"}
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DATE=$(date +%Y%m%d_%H%M%S)

cd "$SCRIPT_DIR"

# 加载环境变量
if [ -f ".env" ]; then
    export $(cat ".env" | grep -v '^#' | xargs)
fi

# 创建备份目录
mkdir -p "$BACKUP_DIR"

DB_NAME=${POSTGRES_DB:-one_report}
DB_USER=${POSTGRES_USER:-postgres}
BACKUP_FILE="$BACKUP_DIR/one_report_backup_$DATE.sql"

echo "开始备份数据库 $DB_NAME..."

# 执行备份
docker exec one-report-postgres pg_dump -U "$DB_USER" -d "$DB_NAME" > "$BACKUP_FILE"

# 压缩
 gzip "$BACKUP_FILE"

echo "备份完成: $BACKUP_FILE.gz"

# 清理旧备份（保留7天）
find "$BACKUP_DIR" -name "one_report_backup_*.sql.gz" -mtime +7 -delete

echo "已清理7天前的备份"
