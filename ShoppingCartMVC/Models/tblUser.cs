using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace ShoppingCartMVC.Models
{
    public partial class tblUser
    {
        public tblUser()
        {
            this.tblInvoices = new HashSet<tblInvoice>();
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }      
        public string Password { get; set; }
        public int? RoleType { get; set; }

        public virtual ICollection<tblInvoice> tblInvoices { get; set; }
    }
}