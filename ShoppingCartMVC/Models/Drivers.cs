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

        public int OrderId { get; set; }
        public virtual tblOrder TblOrder { get; set; }

        [DisplayName("DriverUserID")]
        public int UserId { get; set; }
        public virtual tblUser User { get; set; }

        public string DeliveryAddress { get; set; }

        public DateTime OrderDate { get; set; }
    }
}