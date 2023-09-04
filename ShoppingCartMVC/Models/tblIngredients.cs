using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace ShoppingCartMVC.Models
{
    public class tblIngredients
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Ing_ID { get; set; }

        public string Ing_Name { get; set; }
        public int Ing_UnitsUsed { get; set; } = 0;
        public string Ing_Image { get; set; }//L
        public int Ing_StockyQty { get; set; } = 0;
        public string StockStatus { get; set; } = "In Stock";
    }
}