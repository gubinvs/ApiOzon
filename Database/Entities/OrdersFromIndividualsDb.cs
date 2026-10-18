
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace ApiOzon.Entities
{
    public class OrdersFromIndividualsDb : DbContext
    {
        public OrdersFromIndividualsDb(){}

        [Key]
        public int Id {get; set;}

        public string GuidIdOrder {get; set;} = string.Empty;

        public string PhoneUser {get; set;} = string.Empty;

        public string StatusOrder {get; set;} = string.Empty;

        // Общая стоимость ордера
        public string? Amount {get; set;}

        // Дата оплаты заказа
        public string? DateTime {get; set;}

        public string? ProductName {get; set;}
    }
}