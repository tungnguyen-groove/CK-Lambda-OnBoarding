using System.Text.Json.Serialization;

namespace SnowflakeItemMaster.Application.Models
{
    public class Category
    {
        [JsonPropertyName("path")]
        public string Path { get; set; }

        [JsonPropertyName("source")]
        public string Source { get; set; }
    }
}