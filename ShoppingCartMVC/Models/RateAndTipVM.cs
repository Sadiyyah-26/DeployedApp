using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ShoppingCartMVC.Models
{
    public class RateAndTipVM
    {
        public string DriverName { get; set; }
        public string DriverEmail { get; set; }
        public string DriverImage { get; set; }
        public double DriverAvgRating { get; set; }
        public double CustomTip { get; set; }
        public bool IsCustomTip { get; set; }
        public int rating { get; set; }
    }
}