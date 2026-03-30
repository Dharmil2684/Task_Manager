# TaskManager — Multi-Tenant SaaS

A .NET 10 Task Manager API built with Clean Architecture, Cosmos DB, and multi-tenant isolation.

## Solution Structure

```
TaskManager.sln
├── src/
│   ├── TaskManager.Core            — Domain entities, enums, and interface contracts
│   ├── TaskManager.Infrastructure   — Cosmos DB persistence, external integrations
│   ├── TaskManager.Services         — Application services (tenant resolution, etc.)
│   └── TaskManager.API              — ASP.NET Core Web API (composition root)
└── tests/
    └── TaskManager.Tests            — xUnit + Moq tests
```

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Azure Cosmos DB Emulator](https://learn.microsoft.com/en-us/azure/cosmos-db/local-emulator) (for local development)

## Configuration

### Local Development — User Secrets

The Cosmos DB connection credentials are **stored in appsettings.json**. Use .NET User Secrets for local development:

```bash
# Navigate to the API project
cd src/TaskManager.API

# Initialize user secrets (one-time)
dotnet user-secrets init

# Set the Cosmos DB emulator credentials
dotnet user-secrets set "CosmosDb:EndpointUri" "https://localhost:8081"
dotnet user-secrets set "CosmosDb:PrimaryKey" "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw=="
```

> **Note:** The key above is the well-known Cosmos DB Emulator key and is safe for local use. Never use it in production.

### CI / Production — Environment Variables or Azure Key Vault

For CI pipelines and production deployments, provide configuration via:

**Option A: Environment Variables**
```bash
export CosmosDb__EndpointUri="https://your-cosmos-account.documents.azure.com:443/"
export CosmosDb__PrimaryKey="<your-production-key>"
```

**Option B: Azure Key Vault**
Integrate Azure Key Vault in `Program.cs`:
```csharp
builder.Configuration.AddAzureKeyVault(
    new Uri("https://your-keyvault.vault.azure.net/"),
    new DefaultAzureCredential());
```

### Host Restrictions

- **Development**: `AllowedHosts` is set to `*` (wildcard) in `appsettings.json`.
- **Production**: `AllowedHosts` is restricted in `appsettings.Production.json`. Update the domain values before deploying.

## Build & Run

```bash
# Restore, build, and run tests
dotnet build TaskManager.sln
dotnet test TaskManager.sln

# Run the API
dotnet run --project src/TaskManager.API
```

Swagger UI is available at `http://localhost:5046/` in Development mode.
