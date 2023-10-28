using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ShoppingCartMVC.Models
{
    public class PurchaseOrderVM
    {
        public List<LowStockVM> LowStockItems { get; set; }
        public string Contact { get; set; }
        public string Address { get; set; }
    }

    
}