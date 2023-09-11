using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using System.Web;

namespace ShoppingCartMVC.Models
{
    public class tblBill
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int BillId { get; set; }

        public int OrderId { get; set; }

        public string OrderNumber { get; set; }

        public DateTime? OrderDateTime { get; set; }
        public string WaiterName { get; set; } = "None";

        public string ProductName { get; set; }


        public int? Unit { get; set; }

        public int? Qty { get; set; }

        public int? Total { get; set; }

        public string TableNumber { get; set; } = "None";
        public string Method { get; set; }

        public string PayMethod { get; set; } = "Pending";

        public virtual tblInStoreOrder TblInStoreOrder { get; set; }
    }
}
