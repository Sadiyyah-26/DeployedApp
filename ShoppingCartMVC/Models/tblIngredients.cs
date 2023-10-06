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
        public decimal Ing_StandardQty { get; set; } = 1; // standard qty per unit
        public string StdQty_UnitMeaseurement { get; set; }
        public string Ing_Image { get; set; }
        public decimal Ing_StockyQty { get; set; } = 0;
        public string StockStatus { get; set; } = "In Stock";
    }
}