ARG BUILD_CONFIGURATION=Release

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS restore
WORKDIR /src

COPY ["global.json", "Directory.Build.props", "Directory.Packages.props", "Task_Management_BE.sln", "./"]
COPY ["src/Application/Application.csproj", "src/Application/"]
COPY ["src/Domain/Domain.csproj", "src/Domain/"]
COPY ["src/Infrastructure/Infrastructure.csproj", "src/Infrastructure/"]
COPY ["src/Web/Web.csproj", "src/Web/"]

RUN dotnet restore Task_Management_BE.sln

FROM restore AS publish
ARG BUILD_CONFIGURATION=Release

COPY . .

RUN dotnet publish src/Web/Web.csproj -c $BUILD_CONFIGURATION -o /app/publish -p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Development

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "Task_Management_BE.dll"]
