using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ShoppingCartMVC.Models
{
    public class IngSupplVM
    {
        public string IngredientName { get; set; }
        public List<SupplierIngredients> Suppliers { get; set; }
        public double Ing_UnitCost { get; set; }
    }
}