using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ShoppingCartMVC.Models
{
    public class BillViewModel
    {
        public int BillId { get; set; }

        public string OrderNumber { get; set; }

        public string WaiterName { get; set; }

        public string TableNumber { get; set; }
        public List<SelectedProductModel> Products { get; internal set; }
        public int TotalAmount { get; internal set; }
        public string ProductName { get; internal set; }
        public int? Qty { get; internal set; }
        public int? Total { get; internal set; }

        public string PaymentMethod { get; internal set; }
    }
}