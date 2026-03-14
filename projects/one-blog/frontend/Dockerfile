# 前端 Dockerfile - Nginx 托管
# 构建阶段
FROM node:20-alpine AS build
WORKDIR /app

# 复制 package.json 和 package-lock.json
COPY package*.json ./

# 安装依赖
RUN npm ci

# 复制源代码
COPY . .

# 构建生产版本
RUN npm run build

# 生产阶段 - Nginx
FROM nginx:alpine

# 安装 curl 用于健康检查
RUN apk add --no-cache curl

# 删除默认配置
RUN rm /etc/nginx/conf.d/default.conf

# 复制自定义 Nginx 配置
COPY nginx.conf /etc/nginx/conf.d/

# 从构建阶段复制构建产物
COPY --from=build /app/dist /usr/share/nginx/html

# 创建非 root 用户运行 Nginx
RUN adduser -D -H -u 1000 nginxuser && \
    chown -R nginxuser:nginxuser /usr/share/nginx/html && \
    chown -R nginxuser:nginxuser /var/cache/nginx && \
    chown -R nginxuser:nginxuser /var/log/nginx && \
    chown -R nginxuser:nginxuser /etc/nginx/conf.d

# 修改 Nginx 运行用户
RUN sed -i 's/user  nginx;/user nginxuser;/' /etc/nginx/nginx.conf

# 健康检查
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:80/ || exit 1

# 暴露端口
EXPOSE 80

# 启动 Nginx
CMD ["nginx", "-g", "daemon off;"]
