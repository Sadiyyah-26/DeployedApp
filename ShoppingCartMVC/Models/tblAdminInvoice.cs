using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace ShoppingCartMVC.Models
{
    public class tblAdminInvoice
    {
        public tblAdminInvoice()
        {
            this.TblAdminOrders = new HashSet<tblAdminOrder>();
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int InvoiceId { get; set; }

        public int UserId { get; set; }

        public int SupplierId { get; set; }

        public double? Bill { get; set; }

        public string Payment { get; set; }

        public DateTime? InvoiceDate { get; set; }

        public virtual tblUser TblUser { get; set; }


        public virtual ICollection<tblAdminOrder> TblAdminOrders { get; set; }

    }
}