# 后端 Dockerfile - .NET 8 Web API
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 复制项目文件
COPY backend/src/OneReport/OneReport.csproj ./
RUN dotnet restore

# 复制源代码
COPY backend/src/OneReport/ ./
RUN dotnet build -c Release -o /app/build

# 发布
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# 运行时镜像
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# 安装 ICU 库以支持全球化
RUN apt-get update && apt-get install -y --no-install-recommends \
    icu-devtools \
    && rm -rf /var/lib/apt/lists/*

# 创建非 root 用户
RUN adduser --disabled-password --gecos "" --home "/nonexistent" --shell "/sbin/nologin" --no-create-home --uid 1000 appuser

# 创建导出目录
RUN mkdir -p /app/wwwroot/exports && chown -R appuser:appuser /app

COPY --from=publish /app/publish .

USER appuser

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/api/system/health || exit 1

ENTRYPOINT ["dotnet", "OneReport.dll"]
