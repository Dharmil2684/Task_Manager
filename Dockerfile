# Stage 1: Build and Publish
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and restore as distinct layers
COPY TaskManager.sln ./
COPY src/TaskManager.Core/TaskManager.Core.csproj src/TaskManager.Core/
COPY src/TaskManager.Infrastructure/TaskManager.Infrastructure.csproj src/TaskManager.Infrastructure/
COPY src/TaskManager.Services/TaskManager.Services.csproj src/TaskManager.Services/
COPY src/TaskManager.API/TaskManager.API.csproj src/TaskManager.API/
COPY tests/TaskManager.Tests/TaskManager.Tests.csproj tests/TaskManager.Tests/

RUN dotnet restore

# Copy everything else and build the release API
COPY src/ src/
WORKDIR /src/src/TaskManager.API
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Run
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Install curl for HEALTHCHECK
USER root
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Switch to standard non-root App user
USER app

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .

# Health check to ensure the container is responsive
HEALTHCHECK --interval=30s --timeout=3s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "TaskManager.API.dll"]
