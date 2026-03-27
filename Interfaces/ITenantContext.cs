namespace TaskManager.Interfaces
{
    public interface ITenantContext 
    {
        string CurrentTenantId { get; }
    }
}
