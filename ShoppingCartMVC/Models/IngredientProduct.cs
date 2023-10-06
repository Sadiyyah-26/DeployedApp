using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace ShoppingCartMVC.Models
{
    public class IngredientProduct
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IngPro_ID { get; set; }
        public int ProID { get; set; }
        public int Ing_ID { get; set; }
        public decimal Ing_QtyPerPro { get; set; } = 0;
        public virtual tblProduct TblProduct { get; set; }
        public virtual tblIngredients TblIngredients { get; set; }
    }
}