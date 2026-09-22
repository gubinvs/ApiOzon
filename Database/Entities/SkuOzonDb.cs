

using System.ComponentModel.DataAnnotations;

namespace ApiOzon
{
    public class SkuOzonDb
    {
        [Key]
        public int Id {get; set;}

        public string GuidIdProduct {get; set;} = string.Empty;

        public string SkuOzon {get; set;} = string.Empty;
    }
}