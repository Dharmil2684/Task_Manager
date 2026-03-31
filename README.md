# TaskManager — Multi-Tenant SaaS

A production-ready .NET 10 Task Manager API built with Clean Architecture, Azure Cosmos DB, strict multi-tenant data isolation, and robust observability.

## Features

- **Clean Architecture:** Domain-driven application with clear separation of concerns (Core, Infrastructure, Services, API).
- **Multi-Tenancy:** Hardened vertical data isolation per tenant utilizing Cosmos DB partition keys.
- **Authentication & RBAC:** Secure JWT Bearer access mapped to robust Role-Based Access Control (TenantAdmin, Manager, Employee), with strict API endpoint rules and scoped REST visibility.
- **Resilient Persistence:** Azure Cosmos DB integration via the official .NET SDK, safeguarded by automatic Polly retry and circuit-breaker policies to combat `429 TooManyRequests`.
- **Security & Rate Limiting:** Global exception handling via `GlobalExceptionMiddleware` mapping structural errors cleanly (e.g. `AuthorizationException` mapping safely to 401 Unauthorized without stack trace leaks). Granular IP-based Rate Limiting on public-facing auth endpoints.
- **Email Notifications:** Asynchronous, dynamically-queued SendGrid integrations for tenant onboarding and secure user invitations.
- **Observability:** Centralised, structured JSON telemetry and logging powered by Serilog. Seamless transition between ANSI-colored local CLI traces and production-grade compact JSON outputs.
- **Containerization:** Highly optimized multi-stage `Dockerfile` operating strictly on port 8080 under a non-root Linux `.NET ASP.NET 10` user base, bundled with internalized container network `/health` probes.
- **CI/CD Built-In:** GitHub Actions automated pipelines execute deterministic `restore`, `build`, and `test` suites on every Pull Request or push to `main`.
- **Comprehensive Unit Testing:** A Moq/xUnit suite boasting high structural coverage across the Domain Application Services (AuthService, TaskService, UserService, InvitationService).

## Solution Structure

```
TaskManager.sln
├── src/
│   ├── TaskManager.Core            — Domain entities, DTOs, and interface contracts
│   ├── TaskManager.Infrastructure   — Cosmos DB persistence, external integrations (SendGrid)
│   ├── TaskManager.Services         — Application services (tenant boundaries, business logic)
│   └── TaskManager.API              — ASP.NET Core Web API (Controllers, Middleware, Serilog)
└── tests/
    └── TaskManager.Tests            — xUnit + Moq tests
```

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (For containerized deployment)
- [Azure Cosmos DB Emulator](https://learn.microsoft.com/en-us/azure/cosmos-db/local-emulator) (For local database simulation)

## Configuration

### Local Development — User Secrets

The API utilizes ASP.NET Core User Secrets to securely mount configurations off-disk during local debugging.

```bash
# Navigate to the API project
cd src/TaskManager.API

# Initialize user secrets (one-time)
dotnet user-secrets init

# Set the Cosmos DB emulator credentials
dotnet user-secrets set "CosmosDb:EndpointUri" "https://localhost:8081"
dotnet user-secrets set "CosmosDb:PrimaryKey" "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw=="

# Set JWT Security variables
dotnet user-secrets set "Jwt:SecretKey" "super-secret-key-at-least-32-characters-long!!"
dotnet user-secrets set "Jwt:Issuer" "TaskManagerAuthServer"
dotnet user-secrets set "Jwt:Audience" "TaskManagerWebClients"

# Build Frontend URL mapping and SendGrid Email key
dotnet user-secrets set "FrontendUrl" "http://localhost:3000"
dotnet user-secrets set "SendGrid:ApiKey" "your-sendgrid-api-key"
```

> **Note:** The Cosmso DB Key above is the well-known static Emulator key and is mathematically safe for local use.

## Build & Run

### 1. Locally via CLI
```bash
# Restore, build, and run xUnit tests
dotnet build TaskManager.sln
dotnet test TaskManager.sln

# Run the API with Hot Reload
dotnet watch run --project src/TaskManager.API
```
*Swagger UI is available at `http://localhost:<port>/` in Development environments.*

### 2. Locally via Docker

The service can be seamlessly run in complete isolation utilizing the optimized `Dockerfile`. 

```bash
# Build the production-ready image
docker build -t taskmanager-api:latest .

# Run the container, mapping host Port 8080 to container Port 8080
docker run -it --rm -p 8080:8080 \
  -e "CosmosDb:EndpointUri=YOUR_PROD_URI" \
  -e "CosmosDb:PrimaryKey=YOUR_PROD_KEY" \
  -e "Jwt:SecretKey=YOUR_SECRET_KEY" \
  -e "Jwt:Issuer=YOUR_ISSUER" \
  -e "Jwt:Audience=YOUR_AUDIENCE" \
  taskmanager-api:latest
```

## Production Deployment Context

1. **Host Restrictions**: `AllowedHosts` is restricted dynamically in `appsettings.Production.json`. Remember to attach your production URL before scaling up.
2. **Environment Overrides**: If deploying to Azure Container Apps or App Service, directly inject `CosmosDb__EndpointUri` as an App Setting variable, effectively superseding `.json` configurations.
3. **Log Aggregation**: The container natively streams Serilog JSON outputs onto STDOUT. If hosting on Azure Infrastructure, the orchestration plane will natively ingest and format these structured JSON packets directly into Azure Log Analytics or Application Insights.
