using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ShoppingCartMVC.Models
{
    public class IngQtyVM
    {
        public int IngredientID { get; set; }
        public string IngredientName { get; set; }
        public decimal Quantity { get; set; }
        public string StdQty_UnitMeaseurement { get; set; }
    }
}