using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace ShoppingCartMVC.Models
{
    public class tblExtras
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ExtraID { get; set; }

        public string exName { get; set; }
        public string exCost { get; set; }

        public int? CatId { get; set; }

        public virtual tblCategory tblCategory { get; set; }
    }
}