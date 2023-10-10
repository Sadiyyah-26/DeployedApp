using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace ShoppingCartMVC.Models
{
    public class tblEmployee
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int EmpId { get; set; }

        public string EmpName { get; set; }
        public string EmpImage { get; set; }
        public int Rating1 { get; set; } = 0;
        public int Rating2 { get; set; } = 0;
        public int Rating3 { get; set; } = 0;
        public int Rating4 { get; set; } = 0;
        public int Rating5 { get; set; } = 0;
        public double avgRating { get; set; } = 0;
        public double Tips { get; set; } = 0;
        public int UserID { get; set; }

        public virtual tblUser TblUser { get; set; }
    }
}