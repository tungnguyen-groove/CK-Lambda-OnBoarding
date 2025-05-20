namespace SnowflakeItemMaster.Application.Contracts.Persistence
{
    public interface IRepositoryManager
    {
        IItemMasterSourceLogRepository ItemMasterSourceLog { get; }

        Task SaveAsync();
    }
}