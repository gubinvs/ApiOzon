

using System.ComponentModel.DataAnnotations;

namespace ApiOzon
{
    public class WarehouseDb
    {
        [Key]
        public int Id {get; set;}

        public string GuidIdProduct {get; set;} = string.Empty;

        public string Name {get; set;} = string.Empty;

        public int Quantity {get; set;}
    }
}