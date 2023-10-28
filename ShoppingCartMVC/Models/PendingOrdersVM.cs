using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ShoppingCartMVC.Models
{
    public class PendingOrdersVM
    {
        public int? InvoiceID { get; set; }
        public int SupplierId { get; set; }
        public string SupplName { get; set; }
        public int? Ing_ID { get; set; }
        public string Ing_Name { get; set; }
        public string IngImage { get; set; }
        public int? OrderedStockQty { get; set; }
        public bool StockAvailabilityConfirmed { get; set; }
    }
}