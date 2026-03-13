-- ============================================================
-- One-Report 数据库设计
-- 技术栈: PostgreSQL 16
-- 设计原则: 支持大数据量、流式导出、低资源占用
-- ============================================================

-- 启用必要扩展
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pg_trgm";  -- 用于模糊搜索
CREATE EXTENSION IF NOT EXISTS "btree_gin"; -- 用于JSONB索引

-- ============================================================
-- 1. 用户与权限模块
-- ============================================================

CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    username VARCHAR(50) NOT NULL UNIQUE,
    email VARCHAR(100) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    display_name VARCHAR(100),
    avatar_url VARCHAR(500),
    status VARCHAR(20) DEFAULT 'active' CHECK (status IN ('active', 'inactive', 'locked')),
    last_login_at TIMESTAMP,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE roles (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(50) NOT NULL UNIQUE,
    description VARCHAR(255),
    permissions JSONB DEFAULT '[]',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE user_roles (
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    role_id UUID NOT NULL REFERENCES roles(id) ON DELETE CASCADE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (user_id, role_id)
);

-- ============================================================
-- 2. 数据源管理模块
-- ============================================================

CREATE TABLE data_sources (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(100) NOT NULL,
    type VARCHAR(20) NOT NULL CHECK (type IN ('postgresql', 'mysql', 'sqlserver', 'oracle', 'api', 'excel', 'csv')),
    
    -- 连接配置（加密存储敏感信息）
    config JSONB NOT NULL DEFAULT '{}',
    
    -- 连接池配置
    pool_max_size INTEGER DEFAULT 10,
    pool_idle_timeout INTEGER DEFAULT 300, -- 秒
    
    -- 查询限制
    query_timeout INTEGER DEFAULT 300, -- 秒
    max_rows_per_query BIGINT DEFAULT 1000000,
    
    -- 状态
    status VARCHAR(20) DEFAULT 'active' CHECK (status IN ('active', 'inactive', 'error')),
    last_tested_at TIMESTAMP,
    last_error_message TEXT,
    
    -- 元数据
    created_by UUID REFERENCES users(id),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    -- 软删除
    deleted_at TIMESTAMP,
    deleted_by UUID REFERENCES users(id)
);

-- 数据源索引
CREATE INDEX idx_data_sources_status ON data_sources(status) WHERE deleted_at IS NULL;
CREATE INDEX idx_data_sources_type ON data_sources(type) WHERE deleted_at IS NULL;
CREATE INDEX idx_data_sources_created_by ON data_sources(created_by) WHERE deleted_at IS NULL;

-- ============================================================
-- 3. 报表定义模块
-- ============================================================

CREATE TABLE reports (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(100) NOT NULL,
    description TEXT,
    
    -- 关联数据源
    data_source_id UUID NOT NULL REFERENCES data_sources(id),
    
    -- 报表配置（JSON存储，便于灵活扩展）
    config JSONB NOT NULL DEFAULT '{}',
    
    -- 报表状态
    status VARCHAR(20) DEFAULT 'draft' CHECK (status IN ('draft', 'published', 'archived')),
    
    -- 查询配置
    query_sql TEXT, -- SQL查询语句
    query_api_config JSONB, -- API数据源配置
    
    -- 版本控制
    version INTEGER DEFAULT 1,
    
    -- 元数据
    created_by UUID REFERENCES users(id),
    updated_by UUID REFERENCES users(id),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    -- 软删除
    deleted_at TIMESTAMP,
    deleted_by UUID REFERENCES users(id)
);

-- 报表索引（优化查询性能）
CREATE INDEX idx_reports_status ON reports(status) WHERE deleted_at IS NULL;
CREATE INDEX idx_reports_data_source ON reports(data_source_id) WHERE deleted_at IS NULL;
CREATE INDEX idx_reports_created_by ON reports(created_by) WHERE deleted_at IS NULL;
CREATE INDEX idx_reports_updated_at ON reports(updated_at DESC) WHERE deleted_at IS NULL;

-- GIN索引用于JSONB字段搜索
CREATE INDEX idx_reports_config_gin ON reports USING GIN (config jsonb_path_ops);

-- 模糊搜索索引
CREATE INDEX idx_reports_name_trgm ON reports USING gin (name gin_trgm_ops) WHERE deleted_at IS NULL;

-- ============================================================
-- 4. 报表参数模块
-- ============================================================

CREATE TABLE report_parameters (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    report_id UUID NOT NULL REFERENCES reports(id) ON DELETE CASCADE,
    
    name VARCHAR(50) NOT NULL,
    label VARCHAR(100) NOT NULL,
    type VARCHAR(20) NOT NULL CHECK (type IN ('string', 'number', 'date', 'datetime', 'boolean', 'select', 'multiSelect')),
    
    -- 参数配置
    config JSONB DEFAULT '{}', -- 包含defaultValue, required, validation等
    
    -- 下拉选项（如适用）
    options JSONB DEFAULT '[]',
    
    -- 排序
    sort_order INTEGER DEFAULT 0,
    
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    UNIQUE (report_id, name)
);

CREATE INDEX idx_report_parameters_report ON report_parameters(report_id);

-- ============================================================
-- 5. 报表字段配置模块
-- ============================================================

CREATE TABLE report_columns (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    report_id UUID NOT NULL REFERENCES reports(id) ON DELETE CASCADE,
    
    field VARCHAR(100) NOT NULL, -- 数据字段名
    title VARCHAR(100) NOT NULL, -- 显示标题
    
    -- 显示配置
    width INTEGER, -- 列宽（像素）
    align VARCHAR(10) DEFAULT 'left' CHECK (align IN ('left', 'center', 'right')),
    visible BOOLEAN DEFAULT true,
    sortable BOOLEAN DEFAULT true,
    
    -- 数据格式
    format VARCHAR(50), -- 如：yyyy-MM-dd, #,##0.00
    data_type VARCHAR(20) DEFAULT 'string' CHECK (data_type IN ('string', 'number', 'date', 'datetime', 'boolean')),
    
    -- 数据脱敏
    mask_type VARCHAR(20) DEFAULT 'none' CHECK (mask_type IN ('none', 'phone', 'idCard', 'bankCard', 'email')),
    
    -- 聚合配置
    aggregation VARCHAR(20) CHECK (aggregation IN (NULL, 'sum', 'avg', 'count', 'min', 'max')),
    aggregation_position VARCHAR(20) CHECK (aggregation_position IN (NULL, 'group', 'total')),
    
    -- 排序
    sort_order INTEGER DEFAULT 0,
    
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_report_columns_report ON report_columns(report_id);

-- ============================================================
-- 6. 导出任务模块（分区表设计）
-- ============================================================

CREATE TABLE export_tasks (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    report_id UUID NOT NULL REFERENCES reports(id),
    
    -- 导出配置
    format VARCHAR(10) NOT NULL CHECK (format IN ('pdf', 'excel', 'csv')),
    parameters JSONB DEFAULT '{}',
    options JSONB DEFAULT '{}',
    
    -- 执行状态
    status VARCHAR(20) DEFAULT 'pending' CHECK (status IN ('pending', 'running', 'completed', 'failed', 'cancelled')),
    progress INTEGER DEFAULT 0 CHECK (progress >= 0 AND progress <= 100),
    
    -- 执行信息
    current_row BIGINT DEFAULT 0,
    total_rows BIGINT,
    processed_rows BIGINT DEFAULT 0,
    
    -- 文件信息
    file_path VARCHAR(500),
    file_size BIGINT,
    file_url VARCHAR(500),
    
    -- 错误信息
    error_message TEXT,
    error_details TEXT,
    
    -- 性能指标
    started_at TIMESTAMP,
    completed_at TIMESTAMP,
    execution_time_ms INTEGER,
    memory_peak_mb INTEGER,
    
    -- 元数据
    created_by UUID REFERENCES users(id),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    expires_at TIMESTAMP -- 文件过期时间
) PARTITION BY RANGE (created_at);

-- 创建月度分区（示例创建近期分区，实际应根据需要创建更多）
CREATE TABLE export_tasks_2024_01 PARTITION OF export_tasks
    FOR VALUES FROM ('2024-01-01') TO ('2024-02-01');
CREATE TABLE export_tasks_2024_02 PARTITION OF export_tasks
    FOR VALUES FROM ('2024-02-01') TO ('2024-03-01');
CREATE TABLE export_tasks_2024_03 PARTITION OF export_tasks
    FOR VALUES FROM ('2024-03-01') TO ('2024-04-01');
CREATE TABLE export_tasks_2024_04 PARTITION OF export_tasks
    FOR VALUES FROM ('2024-04-01') TO ('2024-05-01');
CREATE TABLE export_tasks_2024_05 PARTITION OF export_tasks
    FOR VALUES FROM ('2024-05-01') TO ('2024-06-01');
CREATE TABLE export_tasks_2024_06 PARTITION OF export_tasks
    FOR VALUES FROM ('2024-06-01') TO ('2024-07-01');

-- 未来分区可自动创建（通过脚本或定时任务）

-- 导出任务索引
CREATE INDEX idx_export_tasks_report ON export_tasks(report_id);
CREATE INDEX idx_export_tasks_status ON export_tasks(status) WHERE status IN ('pending', 'running');
CREATE INDEX idx_export_tasks_created_by ON export_tasks(created_by);
CREATE INDEX idx_export_tasks_created_at ON export_tasks(created_at DESC);

-- 用于清理过期文件的索引
CREATE INDEX idx_export_tasks_expires_at ON export_tasks(expires_at) WHERE file_path IS NOT NULL;

-- ============================================================
-- 7. 定时任务调度模块
-- ============================================================

CREATE TABLE schedules (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(100) NOT NULL,
    report_id UUID NOT NULL REFERENCES reports(id),
    
    -- 调度配置
    cron_expression VARCHAR(100) NOT NULL,
    timezone VARCHAR(50) DEFAULT 'Asia/Shanghai',
    enabled BOOLEAN DEFAULT true,
    
    -- 导出配置
    export_format VARCHAR(10) DEFAULT 'excel' CHECK (export_format IN ('pdf', 'excel', 'csv')),
    export_options JSONB DEFAULT '{}',
    parameters JSONB DEFAULT '{}',
    
    -- 通知配置
    recipients JSONB DEFAULT '[]', -- 邮件列表
    webhook_url VARCHAR(500),
    
    -- 执行统计
    last_run_at TIMESTAMP,
    next_run_at TIMESTAMP,
    run_count INTEGER DEFAULT 0,
    success_count INTEGER DEFAULT 0,
    fail_count INTEGER DEFAULT 0,
    
    -- 元数据
    created_by UUID REFERENCES users(id),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    deleted_at TIMESTAMP,
    deleted_by UUID REFERENCES users(id)
);

CREATE INDEX idx_schedules_report ON schedules(report_id) WHERE deleted_at IS NULL;
CREATE INDEX idx_schedules_enabled ON schedules(enabled) WHERE deleted_at IS NULL;
CREATE INDEX idx_schedules_next_run ON schedules(next_run_at) WHERE enabled = true AND deleted_at IS NULL;

-- ============================================================
-- 8. 定时任务执行历史模块（分区表）
-- ============================================================

CREATE TABLE schedule_history (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    schedule_id UUID NOT NULL REFERENCES schedules(id),
    
    status VARCHAR(20) NOT NULL CHECK (status IN ('success', 'failed', 'timeout', 'cancelled')),
    
    -- 执行信息
    started_at TIMESTAMP NOT NULL,
    completed_at TIMESTAMP,
    duration_ms INTEGER,
    
    -- 导出结果
    export_task_id UUID REFERENCES export_tasks(id),
    file_url VARCHAR(500),
    
    -- 错误信息
    error_message TEXT,
    
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
) PARTITION BY RANGE (created_at);

-- 历史分区（保留最近6个月）
CREATE TABLE schedule_history_2024_01 PARTITION OF schedule_history
    FOR VALUES FROM ('2024-01-01') TO ('2024-02-01');
CREATE TABLE schedule_history_2024_02 PARTITION OF schedule_history
    FOR VALUES FROM ('2024-02-01') TO ('2024-03-01');
CREATE TABLE schedule_history_2024_03 PARTITION OF schedule_history
    FOR VALUES FROM ('2024-03-01') TO ('2024-04-01');
CREATE TABLE schedule_history_2024_04 PARTITION OF schedule_history
    FOR VALUES FROM ('2024-04-01') TO ('2024-05-01');
CREATE TABLE schedule_history_2024_05 PARTITION OF schedule_history
    FOR VALUES FROM ('2024-05-01') TO ('2024-06-01');
CREATE TABLE schedule_history_2024_06 PARTITION OF schedule_history
    FOR VALUES FROM ('2024-06-01') TO ('2024-07-01');

CREATE INDEX idx_schedule_history_schedule ON schedule_history(schedule_id);
CREATE INDEX idx_schedule_history_status ON schedule_history(status);
CREATE INDEX idx_schedule_history_created_at ON schedule_history(created_at DESC);

-- ============================================================
-- 9. 系统配置与审计日志
-- ============================================================

CREATE TABLE system_configs (
    key VARCHAR(100) PRIMARY KEY,
    value TEXT NOT NULL,
    description TEXT,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_by UUID REFERENCES users(id)
);

CREATE TABLE audit_logs (
    id BIGSERIAL PRIMARY KEY,
    user_id UUID REFERENCES users(id),
    action VARCHAR(50) NOT NULL,
    entity_type VARCHAR(50) NOT NULL,
    entity_id UUID,
    old_values JSONB,
    new_values JSONB,
    ip_address INET,
    user_agent TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
) PARTITION BY RANGE (created_at);

-- 审计日志分区
CREATE TABLE audit_logs_2024_01 PARTITION OF audit_logs
    FOR VALUES FROM ('2024-01-01') TO ('2024-02-01');
CREATE TABLE audit_logs_2024_02 PARTITION OF audit_logs
    FOR VALUES FROM ('2024-02-01') TO ('2024-03-01');
CREATE TABLE audit_logs_2024_03 PARTITION OF audit_logs
    FOR VALUES FROM ('2024-03-01') TO ('2024-04-01');

CREATE INDEX idx_audit_logs_user ON audit_logs(user_id);
CREATE INDEX idx_audit_logs_entity ON audit_logs(entity_type, entity_id);
CREATE INDEX idx_audit_logs_created_at ON audit_logs(created_at DESC);

-- ============================================================
-- 10. 报表模板与共享
-- ============================================================

CREATE TABLE report_templates (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(100) NOT NULL,
    description TEXT,
    category VARCHAR(50),
    
    -- 模板配置
    config JSONB NOT NULL,
    preview_image_url VARCHAR(500),
    
    -- 统计
    usage_count INTEGER DEFAULT 0,
    
    -- 元数据
    is_system BOOLEAN DEFAULT false,
    created_by UUID REFERENCES users(id),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_report_templates_category ON report_templates(category);
CREATE INDEX idx_report_templates_system ON report_templates(is_system) WHERE is_system = true;

-- 报表共享
CREATE TABLE report_shares (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    report_id UUID NOT NULL REFERENCES reports(id),
    
    -- 共享方式
    share_type VARCHAR(20) NOT NULL CHECK (share_type IN ('public', 'password', 'internal')),
    share_token VARCHAR(100) UNIQUE,
    password_hash VARCHAR(255),
    
    -- 限制
    expires_at TIMESTAMP,
    max_views INTEGER,
    current_views INTEGER DEFAULT 0,
    allow_export BOOLEAN DEFAULT true,
    
    -- 元数据
    created_by UUID REFERENCES users(id),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    last_accessed_at TIMESTAMP
);

CREATE INDEX idx_report_shares_report ON report_shares(report_id);
CREATE INDEX idx_report_shares_token ON report_shares(share_token);

-- ============================================================
-- 11. 性能优化视图与函数
-- ============================================================

-- 报表统计视图
CREATE VIEW v_report_statistics AS
SELECT 
    r.id,
    r.name,
    r.status,
    COUNT(DISTINCT et.id) as total_exports,
    COUNT(DISTINCT CASE WHEN et.status = 'completed' THEN et.id END) as success_exports,
    MAX(et.created_at) as last_export_at,
    COALESCE(SUM(et.file_size), 0) as total_export_size
FROM reports r
LEFT JOIN export_tasks et ON r.id = et.report_id AND et.created_at > CURRENT_DATE - INTERVAL '30 days'
WHERE r.deleted_at IS NULL
GROUP BY r.id, r.name, r.status;

-- 导出任务清理函数（删除过期文件记录）
CREATE OR REPLACE FUNCTION cleanup_expired_exports()
RETURNS INTEGER AS $$
DECLARE
    deleted_count INTEGER;
BEGIN
    UPDATE export_tasks 
    SET file_path = NULL, file_url = NULL, file_size = 0
    WHERE expires_at < CURRENT_TIMESTAMP 
      AND file_path IS NOT NULL;
    
    GET DIAGNOSTICS deleted_count = ROW_COUNT;
    RETURN deleted_count;
END;
$$ LANGUAGE plpgsql;

-- 更新更新时间戳的触发器函数
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- 为需要自动更新 updated_at 的表创建触发器
CREATE TRIGGER update_users_updated_at BEFORE UPDATE ON users
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_data_sources_updated_at BEFORE UPDATE ON data_sources
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_reports_updated_at BEFORE UPDATE ON reports
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_schedules_updated_at BEFORE UPDATE ON schedules
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- ============================================================
-- 12. 初始化数据
-- ============================================================

-- 初始化角色
INSERT INTO roles (name, description, permissions) VALUES
('admin', '系统管理员', '["*"]'),
('editor', '报表编辑者', '["reports:*", "datasources:*", "exports:*"]'),
('viewer', '报表查看者', '["reports:view", "exports:create"]');

-- 初始化系统配置
INSERT INTO system_configs (key, value, description) VALUES
('export.max_file_size_mb', '500', '单个导出文件最大大小（MB）'),
('export.max_rows_per_query', '1000000', '单次查询最大行数'),
('export.chunk_size', '1000', '流式导出分块大小'),
('export.file_retention_days', '7', '导出文件保留天数'),
('query.default_timeout', '300', '查询默认超时时间（秒）'),
('scheduler.max_concurrent_jobs', '5', '定时任务最大并发数');

-- ============================================================
-- 13. 性能相关注释
-- ============================================================

/*
性能优化要点：

1. 分区表使用
   - export_tasks: 按月分区，便于快速清理历史数据
   - schedule_history: 按月分区，历史记录可定期归档或删除
   - audit_logs: 按月分区，审计日志量大，分区提升查询性能

2. 索引策略
   - 软删除字段配合条件索引，避免查询已删除数据
   - JSONB字段使用GIN索引支持高效查询
   - 分区键自动成为索引的一部分

3. 连接池配置
   - PostgreSQL 默认连接数 100，建议配置 pool_max_size = 10
   - 应用层连接池配置：Minimum=5, Maximum=50

4. 流式查询优化
   - 使用 server-side cursor (DECLARE CURSOR)
   - 设置合适的 fetch size (1000-5000)
   - 避免在流式查询中使用 ORDER BY（大数据量时）

5. 数据清理策略
   - 导出任务：保留7天后软删除，30天后物理删除
   - 历史记录：保留6个月，之后迁移到归档表
   - 审计日志：保留3个月，之后归档或删除

6. 备份建议
   - reports, data_sources: 全量备份（数据量小，重要）
   - export_tasks, schedule_history, audit_logs: 仅备份最近分区
*/
