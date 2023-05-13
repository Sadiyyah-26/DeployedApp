using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace ShoppingCartMVC.Models
{
    public class InvoiceVM
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int InvoiceVM_ID { get; set; }

        public virtual tblInvoice TblInvoice { get; set; }
        public int InvoiceID { get; set; }
        public string Name { get; set; }
        public string Contact { get; set; }
        public string Address { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public string Item { get; set; }
        public int? Qty { get; set; }
        public int? Unit { get; set; }
        public int? Amount { get; set; }
        public string Payment_Status { get; set; }
        public int? TotalAmount { get; set; }
    }
}