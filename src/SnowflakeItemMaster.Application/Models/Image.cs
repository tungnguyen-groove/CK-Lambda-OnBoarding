using System.Text.Json.Serialization;

namespace SnowflakeItemMaster.Application.Models
{
    public class Image
    {
        [JsonPropertyName("sizeType")]
        public string SizeType { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }
    }
}