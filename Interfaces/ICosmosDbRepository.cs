using Microsoft.Azure.Cosmos;

namespace TaskManager.Interfaces
{
    public interface ICosmosDbRepository<T> where T : class 
    {
        Task<T> GetItemAsync(string id);
        Task<IEnumerable<T>> GetItemsAsync(QueryDefinition queryString);
        Task AddItemAsync(T item);
        Task UpdateItemAsync(string id, T item);
        Task UpsertItemAsync(T item);
        Task DeleteItemAsync(string id);
    }
}
