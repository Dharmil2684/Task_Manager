using System;
using System.Text.Json.Serialization;

namespace TaskManager.Models
{
    public class TaskItem : BaseEntity
    {
        [JsonPropertyName("tenantId")]
        public Guid TenantId { get; set; } // The Partition Key

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public WorkTaskStatus Status { get; set; } = WorkTaskStatus.Pending;

        // Links to the User.Id of the assigned employee
        [JsonPropertyName("assigneeId")]
        public Guid AssigneeId { get; set; }

        [JsonPropertyName("dueDate")]
        public DateTime DueDate { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}