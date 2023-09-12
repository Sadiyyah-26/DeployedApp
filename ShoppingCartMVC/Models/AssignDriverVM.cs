using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ShoppingCartMVC.Models
{
    public class AssignDriverVM
    {
        public List<SelectListItem> DriverSelectList { get; set; }

        
        public int? InvoiceId { get; set; }

        public DateTime? OrderDate { get; set; }

        public string Address { get; set; }
        public int SelectedUserId { get; set; }
    }
}