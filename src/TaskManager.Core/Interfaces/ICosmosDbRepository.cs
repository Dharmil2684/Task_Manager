namespace TaskManager.Core.Interfaces
{
    public interface ICosmosDbRepository<T> where T : class
    {
        Task<T?> GetItemAsync(string id, string partitionKey, CancellationToken cancellationToken = default);
        Task<IEnumerable<T>> GetItemsAsync(string query, Dictionary<string, object>? parameters = null, CancellationToken cancellationToken = default);
        Task AddItemAsync(T item, CancellationToken cancellationToken = default);
        Task UpdateItemAsync(string id, T item, string partitionKey, CancellationToken cancellationToken = default);
        Task UpsertItemAsync(T item, CancellationToken cancellationToken = default);
        Task DeleteItemAsync(string id, string partitionKey, CancellationToken cancellationToken = default);
    }
}
