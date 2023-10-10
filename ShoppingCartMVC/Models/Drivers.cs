using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using System.Web;

namespace ShoppingCartMVC.Models
{
    public class Drivers
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int DriverId { get; set; }

        public string DriverName { get; set; }
        public string DriverEmail { get; set; }
        public string DriverImage { get; set; }
        public double DriverRating { get; set; }
        public double DriverTips { get; set; }

        public int OrderId { get; set; }
        public virtual tblOrder TblOrder { get; set; }

        [DisplayName("DriverUserID")]
        public int UserId { get; set; }
        public virtual tblEmployee Employee { get; set; }

        public string DeliveryAddress { get; set; }

        public DateTime OrderDate { get; set; }
    }
}