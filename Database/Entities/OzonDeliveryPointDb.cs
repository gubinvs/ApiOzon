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


// CREATE TABLE `ozonDeliveryPoints` (
//     `Id` INT NOT NULL AUTO_INCREMENT,
//     `DeliveryPointId` BIGINT NOT NULL,
//     `DeliveryPointNumber` VARCHAR(255) NULL,
//     `Name` VARCHAR(255) NULL,
//     `Address` TEXT NULL,
//     `Latitude` DOUBLE NULL,
//     `Longitude` DOUBLE NULL,
//     `IsActive` TINYINT(1) NOT NULL,
//     PRIMARY KEY (`Id`),
//     UNIQUE KEY `UX_OzonDeliveryPoints_DeliveryPointId` (`DeliveryPointId`)
// ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
