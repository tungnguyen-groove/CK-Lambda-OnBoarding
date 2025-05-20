using SnowflakeItemMaster.Application.Models;
using SnowflakeItemMaster.Domain.Entities;
using SnowflakeItemMaster.Domain.Providers;

namespace SnowflakeItemMaster.Application.Interfaces
{
    public interface ITransformService
    {
        ItemMasterSourceLog TransformItemPetalToItemMasterSourceLog(ItemPetal itemPetal);

        UnifiedItemModel MapToUnifiedItemMaster(ItemPetal itemPetal);
    }
}