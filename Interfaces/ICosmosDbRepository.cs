namespace TaskManager.Core.Interfaces
{
    public interface ICosmosDbRepository<T> where T : class 
    {
        Task<T> GetItemAsync(string id, string partitionKey);
        Task<IEnumerable<T>> GetItemsAsync(string queryString);
        Task AddItemAsync(T item, string partitionKey);
        Task UpdateItemAsync(string id, T item, string partitionKey);
        Task DeleteItemAsync(string id, string partitionKey);
    }
}
