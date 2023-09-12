using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ShoppingCartMVC.Models
{
    public class ReserveOrderViewModel
    {
        public List<Cart> CartItems { get; set; }
        public tblReservation Reservation { get; set; }
        public IEnumerable<SelectListItem> SeatNumberList { get; set; }
    }
}