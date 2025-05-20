namespace SnowflakeItemMaster.Application.Dtos
{
    public class SkuProcessingResponseDto
    {
        public int TotalSKUs { get; set; }
        public int ValidSKUs { get; set; }
        public int InvalidSKUs { get; set; }
        public int SqsSuccess { get; set; }
        public int SqsFailed { get; set; }
        public int LogSaved { get; set; }
        public List<SkuDetailResponseDto> Details { get; set; } = new();

        public static SkuProcessingResponseDto CreateError(string error)
        {
            return new SkuProcessingResponseDto
            {
                Details = new List<SkuDetailResponseDto>
                {
                    new SkuDetailResponseDto { Error = error }
                }
            };
        }
    }
}