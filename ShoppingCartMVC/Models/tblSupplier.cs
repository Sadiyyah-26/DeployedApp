using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace ShoppingCartMVC.Models
{
    public class tblSupplier
    {
        public tblSupplier()
        {
            this.TblProducts = new HashSet<tblProduct>();
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SupplierId { get; set; }

        public string Name { get; set; }

        public virtual ICollection<tblProduct> TblProducts { get; set; }
    }
}