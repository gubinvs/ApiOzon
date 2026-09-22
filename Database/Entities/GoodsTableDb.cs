

using System.ComponentModel.DataAnnotations;

namespace ApiOzon
{
    public class GoodsTableDb
    {
        [Key]
        public int Id {get; set;}

        public string ImgLinkPage {get; set;} = string.Empty;

        public string VendorCode {get; set;} = string.Empty;

        public string NameComponent {get; set;} = string.Empty;

        public string Manufacturer {get; set;} = string.Empty;

        public int Quantity {get; set;}

        public int DeliveryТime {get; set;}

        public int Price {get; set;}

        public int Bestseller {get; set;}

        public string Chapter {get; set;} = string.Empty;

        public string LinkPage {get; set;} = string.Empty;

        public string Guid {get; set;} = string.Empty;

        public string BasketImgPath {get; set;} = string.Empty;

        public string ImgLinkIconCard {get; set;} = string.Empty;

        public string ProductDescription {get; set;} = string.Empty; 
    }
}