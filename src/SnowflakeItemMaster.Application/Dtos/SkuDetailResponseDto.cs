namespace SnowflakeItemMaster.Application.Dtos
{
    public class SkuDetailResponseDto
    {
        public string Sku { get; set; } = string.Empty;
        public bool IsValid { get; set; }
        public bool SentToSQS { get; set; }
        public string? Error { get; set; }
    }
}