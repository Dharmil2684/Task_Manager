using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskManager.Core.Interfaces;
using TaskManager.Infrastructure.Persistence;
using Entities = TaskManager.Core.Entities;

namespace TaskManager.Infrastructure
{
    /// <summary>
    /// Registers all Infrastructure-layer services into the DI container.
    /// </summary>
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var cosmosEndpoint = configuration["CosmosDb:EndpointUri"];
            var cosmosKey = configuration["CosmosDb:PrimaryKey"];
            var databaseName = configuration["CosmosDb:DatabaseName"];

            if (string.IsNullOrWhiteSpace(cosmosEndpoint) || string.IsNullOrWhiteSpace(cosmosKey))
            {
                throw new InvalidOperationException("Critical Cosmos DB connection details are missing from configuration.");
            }

            if (string.IsNullOrWhiteSpace(databaseName))
            {
                throw new InvalidOperationException("The CosmosDb:DatabaseName configuration is missing. The application cannot start.");
            }

            // Register External Services
            services.AddTransient<IEmailService, Email.SendGridEmailService>();
            
            // Register CosmosClient as singleton
            services.AddSingleton<CosmosClient>(sp => new CosmosClient(cosmosEndpoint, cosmosKey, new CosmosClientOptions
            {
                SerializerOptions = new CosmosSerializationOptions
                {
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                },
                MaxRetryAttemptsOnRateLimitedRequests = 3,
                MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(10)
            }));

            // Register DB Initializer
            services.AddSingleton<CosmosDbInitializer>(sp => 
            {
                var client = sp.GetRequiredService<CosmosClient>();
                return new CosmosDbInitializer(client, databaseName);
            });

            // Register repositories
            services.AddScoped<ICosmosDbRepository<Entities.TaskItem>>(sp =>
            {
                var client = sp.GetRequiredService<CosmosClient>();
                var tenantContext = sp.GetRequiredService<ITenantContext>();
                return new CosmosDbRepository<Entities.TaskItem>(client, databaseName, "Tasks", tenantContext);
            });

            services.AddScoped<ICosmosDbRepository<Entities.User>>(sp =>
            {
                var client = sp.GetRequiredService<CosmosClient>();
                var tenantContext = sp.GetRequiredService<ITenantContext>();
                return new CosmosDbRepository<Entities.User>(client, databaseName, "Users", tenantContext);
            });

            services.AddScoped<ICosmosDbRepository<Entities.Tenant>>(sp =>
            {
                var client = sp.GetRequiredService<CosmosClient>();
                var tenantContext = sp.GetRequiredService<ITenantContext>();
                return new CosmosDbRepository<Entities.Tenant>(client, databaseName, "Tenants", tenantContext);
            });

            services.AddScoped<ICosmosDbRepository<Entities.InvitationToken>>(sp =>
            {
                var client = sp.GetRequiredService<CosmosClient>();
                var tenantContext = sp.GetRequiredService<ITenantContext>();
                return new CosmosDbRepository<Entities.InvitationToken>(client, databaseName, "InvitationTokens", tenantContext);
            });

            return services;
        }
    }
}
