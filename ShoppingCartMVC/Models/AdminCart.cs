using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ShoppingCartMVC.Models
{
    public class AdminCart
    {
        public int ingrid { get; set; }

        public string ingrname { get; set; }

        public int qty { get; set; }

        public double price { get; set; }

        public double bill { get; set; }
    }
}