using System.Text.Json.Serialization;

namespace TaskManager.Models
{
    public abstract class BaseEntity
    {
        // Cosmos DB strictly requires the unique identifier to be lowercase "id"
        [JsonPropertyName("id")]
        public Guid Id { get; set; } = Guid.NewGuid();
    }
}
