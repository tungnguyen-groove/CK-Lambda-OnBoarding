using Microsoft.EntityFrameworkCore;
using SnowflakeItemMaster.Application.Contracts.Persistence;
using SnowflakeItemMaster.Domain.Entities;

namespace SnowflakeItemMaster.Infrastructure.Repositories
{
    public class ItemMasterSourceLogRepository : RepositoryBase<ItemMasterSourceLog>, IItemMasterSourceLogRepository
    {
        public ItemMasterSourceLogRepository(RepositoryContext repositoryContext)
            : base(repositoryContext)
        {
        }

        public async Task<bool> UpdateSentStatusAsync(int id, bool isSent, string? error = null)
        {
            var entity = await FindByCondition(x => x.Id == id, true)
                .FirstOrDefaultAsync();

            if (entity == null)
                return false;

            entity.IsSentToSqs = isSent;
            if (error != null)
                entity.Errors = error;

            Update(entity);
            return true;
        }

        public void AddRange(IEnumerable<ItemMasterSourceLog> itemMasterSourceLogs)
        {
            foreach (var item in itemMasterSourceLogs)
            {
                Create(item);
            }
        }

        public async Task BatchUpdateSentStatusAsync(List<int> ids, bool sentStatus, string? error = null)
        {
            var items = await FindByCondition(x => ids.Contains(x.Id), true)
                .ToListAsync();

            foreach (var item in items)
            {
                item.IsSentToSqs = sentStatus;
                item.Errors = error;
                item.CreatedAt = DateTime.UtcNow;
                Update(item);
            }
        }
    }
} 