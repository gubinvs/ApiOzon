namespace ApiOzon
{
    public class OzonDeliveryPointDb
    {
        public int Id { get; set; }

        public long DeliveryPointId { get; set; }

        public string? DeliveryPointNumber { get; set; }

        public string? Name { get; set; }

        public string? Address { get; set; }

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }

        public bool IsActive { get; set; }

        public int? StoragePeriodDays { get; set; }

        public int? FittingRoomsCount { get; set; }

        public bool IsBulky { get; set; }

        public int? MaxWeightG { get; set; }

        public int? MaxWidthMm { get; set; }

        public int? MaxLengthMm { get; set; }

        public int? MaxHeightMm { get; set; }

        public decimal? MaxPrice { get; set; }

        public long? ShipmentMethodId { get; set; }
    }
}