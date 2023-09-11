using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ShoppingCartMVC.Models
{
    public class SelectedProductModel
    {
        public string OrderNumber { get; set; }
        public string Product { get; set; }
        public int UnitPrice { get; set; }
        public int Quantity { get; set; }
        public int TotalPrice { get; set; }
        public string ProductName { get; internal set; }
        public int? Qty { get; internal set; }
        public int? Total { get; internal set; }
    }
}