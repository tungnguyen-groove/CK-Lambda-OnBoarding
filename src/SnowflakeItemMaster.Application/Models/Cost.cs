using System.Text.Json.Serialization;

namespace SnowflakeItemMaster.Application.Models
{
    public class Cost
    {
        [JsonPropertyName("value")]
        public decimal Value { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; }
    }
}