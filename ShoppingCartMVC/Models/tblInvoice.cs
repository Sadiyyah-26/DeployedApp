using System;
using System.Collections.Generic;
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

        public DateTime? InvoiceDate { get; set; }

        public byte? Status { get; set; }

        public virtual tblUser TblUser { get; set; }

        public virtual ICollection<tblOrder> TblOrders { get; set; }
    }
}