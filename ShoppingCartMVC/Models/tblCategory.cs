using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace ShoppingCartMVC.Models
{
    public partial class tblCategory
    {
        public tblCategory()
        {
            this.TblProducts = new HashSet<tblProduct>();
        }
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CatId { get; set; }

        public string Name { get; set; }

        public virtual ICollection<tblProduct> TblProducts { get; set; }
    }
}