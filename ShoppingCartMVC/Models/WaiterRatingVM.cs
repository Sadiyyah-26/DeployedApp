using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ShoppingCartMVC.Models
{
    public class WaiterRatingVM
    {
        public string WaiterName { get; set; }
        public string WaiterEmail { get; set; }
        public string WaiterImage { get; set; }
        public double WaiterAvgRating { get; set; }
        public int rating { get; set; }
    }
}