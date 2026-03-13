# 构建阶段
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 复制项目文件
COPY ["src/OneReport/OneReport.csproj", "src/OneReport/"]
RUN dotnet restore "src/OneReport/OneReport.csproj"

# 复制源代码并构建
COPY . .
WORKDIR "/src/src/OneReport"
RUN dotnet build "OneReport.csproj" -c Release -o /app/build

# 发布阶段
FROM build AS publish
RUN dotnet publish "OneReport.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 运行阶段
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# 安装 ICU 以支持全球化（用于报表格式化）
RUN apt-get update && apt-get install -y --no-install-recommends \
    libc6-dev \
    libicu-dev \
    && rm -rf /var/lib/apt/lists/*

# 配置 Kestrel 和 ASP.NET Core
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# 创建非 root 用户
RUN adduser --disabled-password --gecos "" --home "/nonexistent" --shell "/sbin/nologin" --no-create-home --uid 1000 appuser

# 复制发布文件
COPY --from=publish /app/publish .

# 创建导出目录
RUN mkdir -p /app/wwwroot/exports && chown -R appuser:appuser /app

USER appuser

EXPOSE 8080

ENTRYPOINT ["dotnet", "OneReport.dll"]
