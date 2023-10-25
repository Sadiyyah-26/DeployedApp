using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace ShoppingCartMVC.Models
{
    public class tblInStoreOrder
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OrderId { get; set; }

        public int? BookingId { get; set; }

        public string OrderNumber { get; set; }

        public DateTime? OrderDateTime { get; set; }

        public string WaiterName { get; set; } = "None";

        public int WaiterID { get; set; }

        public string ProductName { get; set; }

        public int? Unit { get; set; }

        public int? Qty { get; set; }

        public int? Total { get; set; }

        public int? Change { get; set; }

        public int? Amountgiven { get; set; }
        public string Method { get; set; }

        public string PayMethod { get; set; } = "Pending";

        public string Status { get; set; } = "Preparing";

        public string TableNumber { get; set; } = "None";

        public string CellNumber { get; set; }

        public string Email { get; set; }

        public bool waiterRated { get; set; } = false;

        public double? Tip { get; set; }

        public DateTime? ReservedDate { get; set; }

        public DateTime? ReservedTime { get; set; }

        public virtual tblReservation TblReservation { get; set; }

        public virtual tblUser User { get; set; }
    }
}
