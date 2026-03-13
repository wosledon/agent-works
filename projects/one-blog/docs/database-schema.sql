-- =============================================
-- one-blog 数据库设计
-- 数据库: PostgreSQL 16
-- 编码: UTF-8
-- =============================================

-- 启用 UUID 扩展
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- 设置时区
SET TIMEZONE = 'Asia/Shanghai';

-- =============================================
-- 1. 用户相关表
-- =============================================

-- 用户表
CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    username VARCHAR(50) NOT NULL,
    email VARCHAR(255) NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    display_name VARCHAR(100),
    avatar VARCHAR(500),
    bio TEXT,
    role VARCHAR(20) NOT NULL DEFAULT 'Reader' CHECK (role IN ('Admin', 'Author', 'Reader')),
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    email_verified BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE,
    last_login_at TIMESTAMP WITH TIME ZONE
);

-- 用户表索引
CREATE UNIQUE INDEX idx_users_username ON users(username);
CREATE UNIQUE INDEX idx_users_email ON users(email);
CREATE INDEX idx_users_role ON users(role);
CREATE INDEX idx_users_created_at ON users(created_at);

-- 用户登录记录表（安全审计）
CREATE TABLE user_login_logs (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    ip_address INET,
    user_agent VARCHAR(500),
    login_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    is_success BOOLEAN NOT NULL,
    failure_reason VARCHAR(100)
);

CREATE INDEX idx_user_login_logs_user_id ON user_login_logs(user_id);
CREATE INDEX idx_user_login_logs_login_at ON user_login_logs(login_at);

-- 刷新令牌表（用于 JWT Token 续期）
CREATE TABLE refresh_tokens (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    token VARCHAR(255) NOT NULL,
    expires_at TIMESTAMP WITH TIME ZONE NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    revoked_at TIMESTAMP WITH TIME ZONE,
    replaced_by_token VARCHAR(255),
    ip_address INET,
    user_agent VARCHAR(500)
);

CREATE UNIQUE INDEX idx_refresh_tokens_token ON refresh_tokens(token);
CREATE INDEX idx_refresh_tokens_user_id ON refresh_tokens(user_id);

-- =============================================
-- 2. 文章相关表
-- =============================================

-- 文章状态枚举
CREATE TYPE post_status AS ENUM ('Draft', 'Published', 'Archived');

-- 文章表
CREATE TABLE posts (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    title VARCHAR(200) NOT NULL,
    slug VARCHAR(200) NOT NULL,
    content TEXT NOT NULL,
    summary VARCHAR(500),
    cover_image VARCHAR(500),
    status post_status NOT NULL DEFAULT 'Draft',
    view_count INTEGER NOT NULL DEFAULT 0,
    author_id UUID NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE,
    published_at TIMESTAMP WITH TIME ZONE,
    meta_title VARCHAR(200),
    meta_description VARCHAR(500),
    is_featured BOOLEAN NOT NULL DEFAULT FALSE
);

-- 文章表索引
CREATE UNIQUE INDEX idx_posts_slug ON posts(slug);
CREATE INDEX idx_posts_author_id ON posts(author_id);
CREATE INDEX idx_posts_status ON posts(status);
CREATE INDEX idx_posts_status_created_at ON posts(status, created_at DESC);
CREATE INDEX idx_posts_published_at ON posts(published_at DESC) WHERE status = 'Published';
CREATE INDEX idx_posts_is_featured ON posts(is_featured) WHERE is_featured = TRUE;
CREATE INDEX idx_posts_search ON posts USING gin(to_tsvector('simple', title || ' ' || COALESCE(content, '')));

-- 文章版本历史表（用于内容回滚）
CREATE TABLE post_versions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    post_id UUID NOT NULL REFERENCES posts(id) ON DELETE CASCADE,
    content TEXT NOT NULL,
    title VARCHAR(200) NOT NULL,
    created_by UUID NOT NULL REFERENCES users(id),
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    change_summary VARCHAR(200)
);

CREATE INDEX idx_post_versions_post_id ON post_versions(post_id);
CREATE INDEX idx_post_versions_created_at ON post_versions(created_at DESC);

-- =============================================
-- 3. 分类相关表
-- =============================================

-- 分类表
CREATE TABLE categories (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(100) NOT NULL,
    slug VARCHAR(100) NOT NULL,
    description VARCHAR(500),
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE
);

-- 分类表索引
CREATE UNIQUE INDEX idx_categories_slug ON categories(slug);
CREATE INDEX idx_categories_name ON categories(name);

-- 文章-分类关联表（多对多）
CREATE TABLE post_categories (
    post_id UUID NOT NULL REFERENCES posts(id) ON DELETE CASCADE,
    category_id UUID NOT NULL REFERENCES categories(id) ON DELETE CASCADE,
    PRIMARY KEY (post_id, category_id)
);

CREATE INDEX idx_post_categories_category_id ON post_categories(category_id);

-- =============================================
-- 4. 标签相关表
-- =============================================

-- 标签表
CREATE TABLE tags (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(50) NOT NULL,
    slug VARCHAR(50) NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- 标签表索引
CREATE UNIQUE INDEX idx_tags_slug ON tags(slug);
CREATE UNIQUE INDEX idx_tags_name ON tags(name);

-- 文章-标签关联表（多对多）
CREATE TABLE post_tags (
    post_id UUID NOT NULL REFERENCES posts(id) ON DELETE CASCADE,
    tag_id UUID NOT NULL REFERENCES tags(id) ON DELETE CASCADE,
    PRIMARY KEY (post_id, tag_id)
);

CREATE INDEX idx_post_tags_tag_id ON post_tags(tag_id);

-- =============================================
-- 5. 评论相关表
-- =============================================

-- 评论状态枚举
CREATE TYPE comment_status AS ENUM ('Pending', 'Approved', 'Rejected', 'Spam');

-- 评论表
CREATE TABLE comments (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    post_id UUID NOT NULL REFERENCES posts(id) ON DELETE CASCADE,
    content TEXT NOT NULL,
    author_name VARCHAR(100),
    author_email VARCHAR(255),
    author_website VARCHAR(255),
    user_id UUID REFERENCES users(id) ON DELETE SET NULL,
    parent_id UUID REFERENCES comments(id) ON DELETE CASCADE,
    status comment_status NOT NULL DEFAULT 'Pending',
    ip_address INET,
    user_agent VARCHAR(500),
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE
);

-- 评论表索引
CREATE INDEX idx_comments_post_id ON comments(post_id);
CREATE INDEX idx_comments_user_id ON comments(user_id);
CREATE INDEX idx_comments_parent_id ON comments(parent_id);
CREATE INDEX idx_comments_status ON comments(status);
CREATE INDEX idx_comments_created_at ON comments(created_at DESC);
CREATE INDEX idx_comments_post_status ON comments(post_id, status);

-- =============================================
-- 6. 媒体文件表
-- =============================================

-- 媒体类型枚举
CREATE TYPE media_type AS ENUM ('Image', 'Document', 'Video', 'Audio', 'Other');

-- 媒体文件表
CREATE TABLE media_files (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    filename VARCHAR(255) NOT NULL,
    original_name VARCHAR(255) NOT NULL,
    file_path VARCHAR(500) NOT NULL,
    file_url VARCHAR(500) NOT NULL,
    file_size BIGINT NOT NULL,
    mime_type VARCHAR(100) NOT NULL,
    media_type media_type NOT NULL,
    width INTEGER,
    height INTEGER,
    alt_text VARCHAR(255),
    uploaded_by UUID REFERENCES users(id) ON DELETE SET NULL,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_media_files_uploaded_by ON media_files(uploaded_by);
CREATE INDEX idx_media_files_media_type ON media_files(media_type);
CREATE INDEX idx_media_files_created_at ON media_files(created_at DESC);

-- =============================================
-- 7. 站点配置表
-- =============================================

-- 站点配置表
CREATE TABLE site_settings (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    key VARCHAR(100) NOT NULL UNIQUE,
    value TEXT,
    description VARCHAR(500),
    is_public BOOLEAN NOT NULL DEFAULT TRUE,
    updated_at TIMESTAMP WITH TIME ZONE,
    updated_by UUID REFERENCES users(id) ON DELETE SET NULL
);

CREATE INDEX idx_site_settings_key ON site_settings(key);
CREATE INDEX idx_site_settings_is_public ON site_settings(is_public);

-- =============================================
-- 8. 视图（方便查询）
-- =============================================

-- 文章列表视图
CREATE VIEW v_post_list AS
SELECT 
    p.id,
    p.title,
    p.slug,
    p.summary,
    p.cover_image,
    p.status,
    p.view_count,
    p.created_at,
    p.published_at,
    p.is_featured,
    u.id AS author_id,
    u.username AS author_username,
    u.display_name AS author_display_name,
    u.avatar AS author_avatar,
    COUNT(DISTINCT c.id) AS comment_count,
    COUNT(DISTINCT pt.tag_id) AS tag_count
FROM posts p
JOIN users u ON p.author_id = u.id
LEFT JOIN comments c ON p.id = c.post_id AND c.status = 'Approved'
LEFT JOIN post_tags pt ON p.id = pt.post_id
GROUP BY p.id, u.id;

-- 分类统计视图
CREATE VIEW v_category_stats AS
SELECT 
    c.id,
    c.name,
    c.slug,
    c.description,
    COUNT(DISTINCT pc.post_id) AS post_count,
    MAX(p.published_at) AS last_post_date
FROM categories c
LEFT JOIN post_categories pc ON c.id = pc.category_id
LEFT JOIN posts p ON pc.post_id = p.id AND p.status = 'Published'
GROUP BY c.id;

-- 标签统计视图
CREATE VIEW v_tag_stats AS
SELECT 
    t.id,
    t.name,
    t.slug,
    COUNT(DISTINCT pt.post_id) AS post_count
FROM tags t
LEFT JOIN post_tags pt ON t.id = pt.tag_id
LEFT JOIN posts p ON pt.post_id = p.id AND p.status = 'Published'
GROUP BY t.id;

-- =============================================
-- 9. 函数和触发器
-- =============================================

-- 自动更新 updated_at 的函数
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

-- 为用户表添加触发器
CREATE TRIGGER update_users_updated_at
    BEFORE UPDATE ON users
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- 为文章表添加触发器
CREATE TRIGGER update_posts_updated_at
    BEFORE UPDATE ON posts
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- 为分类表添加触发器
CREATE TRIGGER update_categories_updated_at
    BEFORE UPDATE ON categories
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- 为评论表添加触发器
CREATE TRIGGER update_comments_updated_at
    BEFORE UPDATE ON comments
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- 文章发布后自动设置 published_at
CREATE OR REPLACE FUNCTION set_post_published_at()
RETURNS TRIGGER AS $$
BEGIN
    IF NEW.status = 'Published' AND OLD.status != 'Published' AND NEW.published_at IS NULL THEN
        NEW.published_at = CURRENT_TIMESTAMP;
    END IF;
    RETURN NEW;
END;
$$ language 'plpgsql';

CREATE TRIGGER trigger_set_post_published_at
    BEFORE UPDATE ON posts
    FOR EACH ROW
    EXECUTE FUNCTION set_post_published_at();

-- =============================================
-- 10. 初始数据
-- =============================================

-- 默认管理员用户（密码: admin123，实际使用时需要 bcrypt 加密）
-- 密码哈希示例: $2a$11$... (bcrypt hashed "admin123")
INSERT INTO users (id, username, email, password_hash, display_name, role, is_active, email_verified)
VALUES (
    uuid_generate_v4(),
    'admin',
    'admin@one-blog.com',
    '$2a$11$N9qo8uLOickgx2ZMRZoMy.MqrqQzBZN0UfGNEsKYGsPwoe0LhQNOK',  -- bcrypt: admin123
    'Administrator',
    'Admin',
    TRUE,
    TRUE
);

-- 默认站点配置
INSERT INTO site_settings (key, value, description, is_public) VALUES
('site_name', 'one-blog', '站点名称', TRUE),
('site_description', 'A modern blog built with .NET and React', '站点描述', TRUE),
('site_logo', '/logo.png', '站点 Logo', TRUE),
('posts_per_page', '10', '每页文章数', TRUE),
('comments_enabled', 'true', '是否启用评论', TRUE),
('comment_moderation', 'true', '评论是否需要审核', TRUE),
('allow_anonymous_comments', 'true', '是否允许匿名评论', TRUE),
('google_analytics_id', NULL, 'Google Analytics ID', FALSE),
('smtp_host', NULL, 'SMTP 服务器地址', FALSE),
('smtp_port', '587', 'SMTP 端口', FALSE),
('smtp_username', NULL, 'SMTP 用户名', FALSE),
('smtp_password', NULL, 'SMTP 密码', FALSE);

-- 默认分类
INSERT INTO categories (name, slug, description) VALUES
('技术', 'tech', '技术文章、编程教程'),
('生活', 'life', '生活随笔、个人感悟'),
('随笔', 'notes', '随想杂记'),
('教程', 'tutorial', '教程、指南');

-- =============================================
-- 11. 权限和注释
-- =============================================

-- 为所有表添加注释
COMMENT ON TABLE users IS '用户表，存储注册用户基本信息';
COMMENT ON TABLE posts IS '文章表，存储博客文章内容';
COMMENT ON TABLE categories IS '分类表，文章分类';
COMMENT ON TABLE tags IS '标签表，文章标签';
COMMENT ON TABLE comments IS '评论表，文章评论';
COMMENT ON TABLE media_files IS '媒体文件表，存储上传的文件信息';
COMMENT ON TABLE site_settings IS '站点配置表，存储系统配置';
COMMENT ON TABLE post_versions IS '文章版本历史，用于内容回滚';
COMMENT ON TABLE refresh_tokens IS '刷新令牌表，用于 JWT Token 续期';
COMMENT ON TABLE user_login_logs IS '用户登录日志，安全审计';

COMMENT ON COLUMN posts.slug IS 'URL 友好的文章标识，唯一';
COMMENT ON COLUMN posts.content IS '文章正文，Markdown 格式';
COMMENT ON COLUMN posts.meta_title IS 'SEO 标题';
COMMENT ON COLUMN posts.meta_description IS 'SEO 描述';
COMMENT ON COLUMN comments.status IS '评论状态：待审核/已通过/已拒绝/垃圾';
COMMENT ON COLUMN users.role IS '用户角色：管理员/作者/读者';

-- =============================================
-- 12. 性能优化建议
-- =============================================

/*
生产环境建议：

1. 连接池配置 (appsettings.json):
   "ConnectionStrings": {
     "Default": "Host=localhost;Database=blogdb;Username=bloguser;Password=xxx;Maximum Pool Size=100;"
   }

2. 定期维护任务:
   - VACUUM ANALYZE 每周执行一次
   - REINDEX 每月执行一次

3. 备份策略:
   - 每日全量备份
   - 实时 WAL 归档

4. 监控指标:
   - 慢查询日志 (log_min_duration_statement = 1000)
   - 连接数监控
   - 锁等待监控
*/
