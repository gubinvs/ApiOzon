using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApiOzon.Models
{
    [Table("OzonDeliveryPoints")]
    public class OzonDeliveryPoint
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)] // ID контролирует сам Ozon
        [Column("DeliveryPointId")]
        public string DeliveryPointId { get; set; } = null!;

        [Required]
        [StringLength(50)]
        public string DeliveryPointNumber { get; set; } = null!;

        [Required]
        [StringLength(255)]
        public string Name { get; set; } = null!;

        [Required]
        public string Address { get; set; } = null!;

        public double Lat { get; set; }

        public double Lng { get; set; }

        public bool IsActive { get; set; }

        public int StoragePeriodDays { get; set; }

        public int FittingRoomsCount { get; set; }

        public bool IsBulky { get; set; }

        public long MaxWeightG { get; set; }

        public int MaxWidthMm { get; set; }

        public int MaxLengthMm { get; set; }

        public int MaxHeightMm { get; set; }

        [Column(TypeName = "decimal(18, 2)")] // Безопасное хранение денежных лимитов
        public decimal MaxPrice { get; set; }

        public long? ShipmentMethodId { get; set; }

        public DateTime LastUpdatedAt { get; set; }
    }
}
