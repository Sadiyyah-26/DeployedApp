using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace ShoppingCartMVC.Models
{
    public class SupplierIngredients
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SupplIng_ID { get; set; }

        public int SupplierId { get; set; }

        public int Ing_ID { get; set; }

        public double Ing_UnitCost { get; set; } = 0;
        public virtual tblSupplier TblSupplier { get; set; }
        public virtual tblIngredients TblIngredients { get; set; }
    }
}