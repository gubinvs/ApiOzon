

using System.Text.Json.Serialization;

namespace ApiOzon
{
    public class OzonStocksResponse
    {
        public List<Products>? Present { get; set; }

        public bool HasNext {get; set;}

        public string Cursor {get; set;} = string.Empty;
    }

    public class Products
    {
        public int Sku {get; set;}
        public string VendorCode {get; set;} = string.Empty;

        public int ProductId {get; set;}
        
        public int WarehouseId {get; set;}

        public int Present {get; set;}

        public int Reserved {get; set;}

    }
}
