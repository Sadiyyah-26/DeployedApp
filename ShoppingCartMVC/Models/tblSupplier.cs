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
        
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SupplierId { get; set; }

        public string SupplName { get; set; }

        public string SupplType { get; set; }

        public string ContactPerson { get; set; }
        public string ContactPersonPos { get; set; }
        public string ContactNum { get; set; }
        public string Email { get; set; }
        public string Tel { get; set; }
        public string CompanyRegNum { get; set; }
        public string PhysicalAddress { get; set; }
        public string Active { get; set; }

        
    }
}