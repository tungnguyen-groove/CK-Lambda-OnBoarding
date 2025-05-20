using SnowflakeItemMaster.Domain.Providers;

namespace SnowflakeItemMaster.Application.Contracts.Provider
{
    public interface IItemPetalRepository
    {
        Task<List<ItemPetal>> GetItemsBySkusAsync(IEnumerable<string> skus);
        Task<List<ItemPetal>> GetItemsWithLimitAsync(DateTime fromDate, DateTime toDate, int limit = 100);
        Task<List<ItemPetal>> GetItemsWithLimitAsync(int limit, int hours = 1);
    }
}