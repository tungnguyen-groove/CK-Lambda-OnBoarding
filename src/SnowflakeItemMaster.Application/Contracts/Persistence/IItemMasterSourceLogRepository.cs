using SnowflakeItemMaster.Domain.Entities;

namespace SnowflakeItemMaster.Application.Contracts.Persistence
{
    public interface IItemMasterSourceLogRepository : IRepositoryBase<ItemMasterSourceLog>
    {
        Task<bool> UpdateSentStatusAsync(int id, bool isSent, string? error = null);
        void AddRange(IEnumerable<ItemMasterSourceLog> itemMasterSourceLogs);
        Task BatchUpdateSentStatusAsync(List<int> ids, bool sentStatus, string error = null);
    }
} 