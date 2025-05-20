using System.Text.Json.Serialization;

namespace SnowflakeItemMaster.Application.Models
{
    public class AlternateBarcode
    {
        [JsonPropertyName("value")]
        public string Value { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }
    }
}