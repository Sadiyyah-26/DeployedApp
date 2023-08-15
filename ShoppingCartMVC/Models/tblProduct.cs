using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace ShoppingCartMVC.Models
{
    public partial class tblProduct
    {
        public tblProduct()
        {
            this.tblOrders = new HashSet<tblOrder>();
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ProID { get; set; }
        public string P_Name { get; set; }
        public string Description { get; set; }
        public int? Unit { get; set; }
        public string Image { get; set; }
        public int? CatId { get; set; }
        public int? SupplierId { get; set; }
        public int? Qty { get; set; }
        public string StockStatus { get; set; } = "In Stock";

        public virtual tblCategory tblCategory { get; set; }
        public virtual tblSupplier tblSupplier { get; set; }
        public virtual ICollection<tblOrder> tblOrders { get; set; }
    }
}