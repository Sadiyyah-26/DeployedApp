using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace ShoppingCartMVC.Models
{
    public class tblAdminOrder
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OrderId { get; set; }

        public int? IngrID { get; set; }

        public int? SupplierId { get; set; }

        public string Contact { get; set; }

        public string Address { get; set; }

        public int? Unit { get; set; }

        public int? Qty { get; set; }

        public int? Total { get; set; }

        public DateTime? OrderDate { get; set; }

        public string OrderStatus { get; set; } = "Ordered";

        public string ReturnReason { get; set; } = "";

        public int? InvoiceId { get; set; }

        public virtual tblIngredients tblIngredients { get; set; }

        public virtual tblAdminInvoice TblAdminInvoice { get; set; }
        public virtual tblSupplier TblSupplier { get; set; }

    }
}