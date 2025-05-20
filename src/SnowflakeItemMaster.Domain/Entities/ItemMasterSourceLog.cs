using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SnowflakeItemMaster.Domain.Entities
{
    [Table("item_master_source_log")]
    public class ItemMasterSourceLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("sku")]
        [StringLength(100)]
        public string Sku { get; set; }

        [Required]
        [Column("source_model", TypeName = "json")]
        public string SourceModel { get; set; }

        [Required]
        [Column("validation_status")]
        [StringLength(50)]
        public string ValidationStatus { get; set; }

        [Required]
        [Column("common_model", TypeName = "json")]
        public string CommonModel { get; set; } = "{}";

        [Column("errors")]
        public string? Errors { get; set; }

        [Column("is_sent_to_sqs")]
        public bool IsSentToSqs { get; set; }

        [Required]
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}