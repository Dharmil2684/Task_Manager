using Microsoft.Azure.Cosmos;
using TaskManager.Interfaces;
using TaskManager.Models;
using TaskManager.Data;
using TaskManager.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var cosmosEndpoint = builder.Configuration["CosmosDb:EndpointUri"];
var cosmos = builder.Configuration["CosmosDb:PrimaryKey"];
var databaseName = builder.Configuration["CosmosDb:DatabaseName"];

if (string.IsNullOrWhiteSpace(cosmosEndpoint) || string.IsNullOrWhiteSpace(cosmos))
{
    throw new InvalidOperationException("Critical Cosmos DB connection details are missing from configuration.");
}

if (string.IsNullOrWhiteSpace(databaseName))
{
    throw new InvalidOperationException("The CosmosDb:DatabaseName configuration is missing. The application cannot start.");
}

builder.Services.AddSingleton<CosmosClient>(sp => new CosmosClient(cosmosEndpoint, cosmos));
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantContext, TenantContext>();

builder.Services.AddScoped<ICosmosDbRepository<TaskItem>>(sp =>
{
    var client = sp.GetRequiredService<CosmosClient>();
    var tenantContext = sp.GetRequiredService<ITenantContext>();
    return new CosmosDbRepository<TaskItem>(client, databaseName, "Tasks", tenantContext);
});

builder.Services.AddScoped<ICosmosDbRepository<TaskManager.Models.User>>(sp =>
{
    var client = sp.GetRequiredService<CosmosClient>();
    var tenantContext = sp.GetRequiredService<ITenantContext>();
    return new CosmosDbRepository<TaskManager.Models.User>(client, databaseName, "Users", tenantContext);
});

builder.Services.AddScoped<ICosmosDbRepository<Tenant>>(sp =>
{
    var client = sp.GetRequiredService<CosmosClient>();
    var tenantContext = sp.GetRequiredService<ITenantContext>();
    return new CosmosDbRepository<Tenant>(client, databaseName, "Tenants", tenantContext);
});

builder.Services.AddControllers();
// Swagger UI Add services to generate OpenAPI documents
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Enable Swagger (only in development ideally)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
        c.RoutePrefix = string.Empty;
    });
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
