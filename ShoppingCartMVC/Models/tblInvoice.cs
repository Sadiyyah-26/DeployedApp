using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace ShoppingCartMVC.Models
{
    public partial class tblInvoice
    {
        public tblInvoice()
        {
            this.TblOrders = new HashSet<tblOrder>();
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int InvoiceId { get; set; }

        public int UserId { get; set; }

        public int? Bill { get; set; }

        public string Payment { get; set; }

        [DisplayName("Method")] /*My chnages for delivery*/
        public string DC_Method { get; set; } = "Delivery";

        public DateTime? InvoiceDate { get; set; }

        public string Status { get; set; } = "Order Placed";

        public string Payment_Status { get; set; } = "Paid";

        public DateTime? Time_CD { get; set; }

        public virtual tblUser TblUser { get; set; }

        public virtual ICollection<tblOrder> TblOrders { get; set; }
    }
}