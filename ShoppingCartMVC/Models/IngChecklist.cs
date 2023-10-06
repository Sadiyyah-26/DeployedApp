using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ShoppingCartMVC.Models
{
    public class IngChecklist
    {
        public int OrderId { get; set; }
        public List<string> Conditions { get; set; }
    }
}