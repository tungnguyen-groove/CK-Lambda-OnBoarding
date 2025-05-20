using SnowflakeItemMaster.Domain.Entities;

namespace SnowflakeItemMaster.Application.Models
{
    public class ProcessedItemResult
    {
        public ItemMasterSourceLog? SourceLog { get; set; }
        public UnifiedItemModel? UnifiedModel { get; set; }
        public bool IsValid { get; set; }
    }
}