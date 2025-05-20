using SnowflakeItemMaster.Application.Models;

namespace SnowflakeItemMaster.Application.Contracts.MessageQueue
{
    public interface IMessageQueueService
    {
        Task<(bool IsSuccess, string Message)> PublishItemAsync(UnifiedItemModel item);
    }
}