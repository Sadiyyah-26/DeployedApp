using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ShoppingCartMVC.Models
{
    public class IngProVm
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public List<IngQtyVM> Ingredients { get; set; }
    }
}