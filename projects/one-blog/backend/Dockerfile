# 后端 Dockerfile - .NET 8 运行时
# 构建阶段
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 复制项目文件
COPY backend/DotnetBlog.csproj ./
RUN dotnet restore

# 复制源代码
COPY backend/. ./

# 构建发布版本
RUN dotnet publish -c Release -o /app/publish --no-restore

# 运行时阶段
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# 安装健康检查工具
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# 创建非 root 用户
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

# 从构建阶段复制发布文件
COPY --from=build /app/publish .

# 暴露端口
EXPOSE 8080

# 健康检查
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# 启动应用
ENTRYPOINT ["dotnet", "DotnetBlog.dll"]
