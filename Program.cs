using Microsoft.Azure.Cosmos;
using TaskManager.Core.Interfaces;
using TaskManager.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var cosmosEndpoint = builder.Configuration["CosmosDb:EndpointUri"];
var cosmos = builder.Configuration["CosmosDb:PrimaryKey"];
var databaseName = builder.Configuration["CosmosDb:DatabaseName"];

var cosmosClient = new CosmosClient(cosmosEndpoint, cosmos);

builder.Services.AddSingleton<ICosmosDbRepository<TaskManager.Core.Models.TaskItem>>(
    new CosmosDbRepository<TaskManager.Core.Models.TaskItem>(cosmosClient, databaseName, "Tasks"));

builder.Services.AddSingleton<ICosmosDbRepository<TaskManager.Core.Models.User>>(
    new CosmosDbRepository<TaskManager.Core.Models.User>(cosmosClient, databaseName, "Users"));

builder.Services.AddSingleton<ICosmosDbRepository<TaskManager.Core.Models.Tenant>>(
    new CosmosDbRepository<TaskManager.Core.Models.Tenant>(cosmosClient, databaseName, "Tenants"));

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
