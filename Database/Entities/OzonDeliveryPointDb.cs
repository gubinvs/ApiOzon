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
    }
}