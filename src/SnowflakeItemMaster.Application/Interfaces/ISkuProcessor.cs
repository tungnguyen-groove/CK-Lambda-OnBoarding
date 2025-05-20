using SnowflakeItemMaster.Application.Dtos;
using SnowflakeItemMaster.Application.Models;

namespace SnowflakeItemMaster.Application.Interfaces
{
    public interface ISkuProcessor
    {
        Task<SkuProcessingResponseDto> ProcessSkusAsync(SkuRequestDto request);
    }
}