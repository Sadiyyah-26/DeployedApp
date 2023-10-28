using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace ShoppingCartMVC.Models
{
    public class LowStockVM
    {
  
        public int SupplierId { get; set; }
        public string SupplName { get; set; }
        public int Ing_ID { get; set; }
        public string Ing_Name { get; set; }
        public string IngImage { get; set; }
        public decimal StockQty { get; set; }
        public string StockStatus { get; set; }

        public decimal Quantity { get; set; }
        public double UnitCost { get; set; }
        public decimal TotalCost { get; set; }
    }
}