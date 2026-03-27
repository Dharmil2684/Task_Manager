using System;
using System.Text.Json.Serialization;

namespace TaskManager.Models
{
    public class Tenant : BaseEntity
    {
        [JsonPropertyName("companyName")]
        public string CompanyName { get; set; } = string.Empty;

        [JsonPropertyName("subscriptionTier")]
        public string SubscriptionTier { get; set; } = "Free"; // Useful for future SaaS billing

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
