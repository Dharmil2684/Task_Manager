using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

namespace TaskManager.Infrastructure
{
    public class CosmosDbInitializer
    {
        private readonly CosmosClient _client;
        private readonly string _databaseName;

        public CosmosDbInitializer(CosmosClient client, string databaseName)
        {
            _client = client;
            _databaseName = databaseName;
        }

        public async Task InitializeAsync()
        {
            // Create database
            DatabaseResponse databaseResponse = await _client.CreateDatabaseIfNotExistsAsync(_databaseName);
            Database database = databaseResponse.Database;

            // Create container for Tenants
            await database.CreateContainerIfNotExistsAsync(
                id: "Tenants",
                partitionKeyPath: "/id"
            );
        
            // Create container for Users
            await database.CreateContainerIfNotExistsAsync(
                id: "Users",
                partitionKeyPath: "/tenantId"
            );
        
            // Create container for Tasks
            await database.CreateContainerIfNotExistsAsync(
                id: "Tasks",
                partitionKeyPath: "/tenantId"
            );
        
            // Create container for InvitationTokens
            await database.CreateContainerIfNotExistsAsync(
                id: "InvitationTokens",
                partitionKeyPath: "/tenantId"
    );
        }
    }
}
