using System.Text.Json.Serialization;

namespace SnowflakeItemMaster.Application.Dtos
{
    public class SkuRequestDto
    {
        [JsonPropertyName("skus")]
        public List<string> Skus { get; set; } = new();
    }
}