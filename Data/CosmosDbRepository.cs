using Microsoft.Azure.Cosmos;
using TaskManager.Interfaces;


namespace TaskManager.Data
{
    public class CosmosDbRepository<T> (CosmosClient dbClient, string databaseName, string containerName, ITenantContext tenantContext) : ICosmosDbRepository<T> where T : class
    {
        private readonly Container _container = dbClient.GetContainer(databaseName, containerName);
        private readonly ITenantContext _tenantContext = tenantContext;

        public async Task AddItemAsync(T item)
        {
            var partitionKey = new PartitionKey(_tenantContext.CurrentTenantId);
            await _container.CreateItemAsync(item, partitionKey);
        }

        public async Task DeleteItemAsync(string id)
        {
            var partitionKey = new PartitionKey(_tenantContext.CurrentTenantId);
            await _container.DeleteItemAsync<T>(id, partitionKey);
        }

        public async Task<T> GetItemAsync(string id)
        {
            try 
            {
                var partitionKey = new PartitionKey(_tenantContext.CurrentTenantId);
                ItemResponse<T> response = await _container.ReadItemAsync<T>(id, partitionKey);
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }
        public async Task<IEnumerable<T>> GetItemsAsync(QueryDefinition queryDefinition)
        {
            var query = _container.GetItemQueryIterator<T>(queryDefinition);
            List<T> results = [];
            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();
                results.AddRange(response.ToList());
            }
            return results;
        }

        public async Task UpdateItemAsync(string id, T item)
        {
            var partitionKey = new PartitionKey(_tenantContext.CurrentTenantId);
            await _container.ReplaceItemAsync(item, id, partitionKey);
        }

        public async Task UpsertItemAsync(T item)
        {
            var partitionKey = new PartitionKey(_tenantContext.CurrentTenantId);
            await _container.UpsertItemAsync(item, partitionKey);
        }
    }
}
