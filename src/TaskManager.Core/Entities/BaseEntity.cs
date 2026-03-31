using System.Text.Json.Serialization;

namespace TaskManager.Core.Entities
{
    public abstract class BaseEntity
    {
        // Cosmos DB strictly requires the unique identifier to be lowercase "id"
        [JsonPropertyName("id")]
        [Newtonsoft.Json.JsonProperty("id")]
        public Guid Id { get; init; } = Guid.NewGuid();
    }
}
