using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace ShoppingCartMVC.Models
{
    public class tblAccProfile
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ProfileId { get; set; }
        public string userName { get; set; }
        public string userProfileImage { get; set; }
        public int UserID { get; set; }

        public virtual tblUser TblUser { get; set; }
    }
}