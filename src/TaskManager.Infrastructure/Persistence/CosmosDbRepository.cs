using Microsoft.Azure.Cosmos;
using Polly;
using Polly.Retry;
using TaskManager.Core.Interfaces;

namespace TaskManager.Infrastructure.Persistence
{
    public class CosmosDbRepository<T>(CosmosClient dbClient, string databaseName, string containerName, ITenantContext tenantContext) : ICosmosDbRepository<T> where T : class
    {
        private readonly Container _container = dbClient.GetContainer(databaseName, containerName);
        private readonly ITenantContext _tenantContext = tenantContext;
        
        // Define Polly exponential backoff policy for CosmosException 429 and 503
        private readonly AsyncRetryPolicy _retryPolicy = Policy
            .Handle<CosmosException>(ex => ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests || 
                                           ex.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

        public async Task AddItemAsync(T item, string? partitionKey = null, CancellationToken cancellationToken = default)
        {
            var resolvedPartitionKey = !string.IsNullOrEmpty(partitionKey) 
                ? partitionKey 
                : _tenantContext.CurrentTenantId;

            if (string.IsNullOrEmpty(resolvedPartitionKey))
            {
                throw new InvalidOperationException("PartitionKey must be provided or available in TenantContext.");
            }

            try
            {
                await _retryPolicy.ExecuteAsync(ct => 
                    _container.CreateItemAsync(item, new PartitionKey(resolvedPartitionKey), cancellationToken: ct), cancellationToken);
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                throw new InvalidOperationException("An item with the same identifier already exists.", ex);
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                throw new Exception("Too many requests. Please try again later.", ex);
            }
            catch (CosmosException ex)
            {
                throw new Exception($"Failed to add item to Cosmos DB. Status: {ex.StatusCode}", ex);
            }
        }

        public async Task DeleteItemAsync(string id, string partitionKey, CancellationToken cancellationToken = default)
        {
            await _retryPolicy.ExecuteAsync(ct => 
                _container.DeleteItemAsync<T>(id, new PartitionKey(partitionKey), cancellationToken: ct), cancellationToken);
        }

        public async Task<T?> GetItemAsync(string id, string partitionKey, CancellationToken cancellationToken = default)
        {
            try
            {
                ItemResponse<T> response = await _retryPolicy.ExecuteAsync(ct => 
                    _container.ReadItemAsync<T>(id, new PartitionKey(partitionKey), cancellationToken: ct), cancellationToken);
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task<IEnumerable<T>> GetItemsAsync(string query, Dictionary<string, object>? parameters = null, string? partitionKey = null, CancellationToken cancellationToken = default)
        {
            var queryDefinition = new QueryDefinition(query);

            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    queryDefinition = queryDefinition.WithParameter(param.Key, param.Value);
                }
            }

            // Scope query to the current tenant's partition if a partition key is available either from parameter or context
            var requestOptions = new QueryRequestOptions();
            var resolvedPartitionKey = !string.IsNullOrEmpty(partitionKey) 
                ? partitionKey 
                : _tenantContext.CurrentTenantId;
                
            if (string.IsNullOrEmpty(resolvedPartitionKey))
            {
                throw new InvalidOperationException("PartitionKey must be provided or available in TenantContext.");
            }

            requestOptions.PartitionKey = new PartitionKey(resolvedPartitionKey);

            var iterator = _container.GetItemQueryIterator<T>(queryDefinition, requestOptions: requestOptions);
            List<T> results = [];
            while (iterator.HasMoreResults)
            {
                var response = await _retryPolicy.ExecuteAsync(ct => 
                    iterator.ReadNextAsync(ct), cancellationToken);
                results.AddRange(response.ToList());
            }
            return results;
        }

        public async Task UpdateItemAsync(string id, T item, string partitionKey, CancellationToken cancellationToken = default)
        {
            await _retryPolicy.ExecuteAsync(ct => 
                _container.ReplaceItemAsync(item, id, new PartitionKey(partitionKey), cancellationToken: ct), cancellationToken);
        }

        public async Task UpsertItemAsync(T item, string? partitionKey = null, CancellationToken cancellationToken = default)
        {
            var resolvedPartitionKey = !string.IsNullOrEmpty(partitionKey) 
                ? partitionKey 
                : _tenantContext.CurrentTenantId;

            if (string.IsNullOrEmpty(resolvedPartitionKey))
            {
                throw new InvalidOperationException("PartitionKey must be provided or available in TenantContext.");
            }

            await _retryPolicy.ExecuteAsync(ct => 
                _container.UpsertItemAsync(item, new PartitionKey(resolvedPartitionKey), cancellationToken: ct), cancellationToken);
        }
    }
}
