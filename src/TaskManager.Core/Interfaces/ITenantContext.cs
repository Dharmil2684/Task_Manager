namespace TaskManager.Core.Interfaces
{
    public interface ITenantContext
    {
        string CurrentTenantId { get; }
    }
}
