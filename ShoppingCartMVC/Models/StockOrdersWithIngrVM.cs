using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ShoppingCartMVC.Models
{
    public class StockOrdersWithIngrVM
    {
        public int? InvoiceId { get; set; }
        public string SupplierName { get; set; }
        public DateTime? OrderDate { get; set; }
        public List<string> Ingredients { get; set; }
        public string ReturnReason { get; set; }
    }
}