ARG BUILD_CONFIGURATION=Release

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS restore
WORKDIR /src
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 \
    NUGET_XMLDOC_MODE=skip

COPY ["global.json", "Directory.Build.props", "Directory.Packages.props", "Task_Management_BE.sln", "./"]
COPY ["src/Application/Application.csproj", "src/Application/"]
COPY ["src/Domain/Domain.csproj", "src/Domain/"]
COPY ["src/Infrastructure/Infrastructure.csproj", "src/Infrastructure/"]
COPY ["src/Web/Web.csproj", "src/Web/"]
COPY ["src/NotificationWorker/NotificationWorker.csproj", "src/NotificationWorker/"]

RUN --mount=type=cache,id=nuget-cache,target=/root/.nuget/packages \
    dotnet restore Task_Management_BE.sln

FROM restore AS publish-web
ARG BUILD_CONFIGURATION=Release
COPY . .
RUN --mount=type=cache,id=nuget-cache,target=/root/.nuget/packages \
    dotnet publish src/Web/Web.csproj \
      -c $BUILD_CONFIGURATION \
      -o /app/publish \
      --self-contained false \
      -p:UseAppHost=false

FROM restore AS publish-worker
ARG BUILD_CONFIGURATION=Release
COPY . .
RUN --mount=type=cache,id=nuget-cache,target=/root/.nuget/packages \
    dotnet publish src/NotificationWorker/NotificationWorker.csproj \
      -c $BUILD_CONFIGURATION \
      -o /app/publish \
      --self-contained false \
      -p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS web
WORKDIR /app
RUN addgroup -S appgroup && adduser -S appuser -G appgroup
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Development
COPY --from=publish-web /app/publish .
USER appuser
ENTRYPOINT ["dotnet", "Task_Management_BE.dll"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS worker
WORKDIR /app
RUN addgroup -S appgroup && adduser -S appuser -G appgroup
COPY --from=publish-worker /app/publish .
USER appuser
ENTRYPOINT ["dotnet", "NotificationWorker.dll"]
