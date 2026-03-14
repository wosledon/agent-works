# 前端 Dockerfile - React + Vite
FROM node:20-alpine AS build
WORKDIR /app

# 安装依赖
COPY frontend/package*.json ./
RUN npm ci --only=production=false

# 复制源代码
COPY frontend/ ./

# 构建
RUN npm run build

# 生产环境使用 Nginx
FROM nginx:alpine AS production

# 复制构建产物
COPY --from=build /app/dist /usr/share/nginx/html

# 复制 Nginx 配置
COPY frontend/nginx.conf /etc/nginx/conf.d/default.conf

# 健康检查
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD wget --quiet --tries=1 --spider http://localhost:80 || exit 1

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
