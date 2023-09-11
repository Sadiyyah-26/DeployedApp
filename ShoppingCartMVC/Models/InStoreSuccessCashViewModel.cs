using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ShoppingCartMVC.Models
{
    public class InStoreSuccessCashViewModel
    {
        public string OrderNumber { get; set; }
        public DateTime OrderDateTime { get; set; }
        public List<SelectedProductModel> Products { get; set; }
        public int TotalAmount { get; set; }
    }
}