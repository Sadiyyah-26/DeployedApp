using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace ShoppingCartMVC.Models
{
    public class AccountPoints
    {
        public AccountPoints(int userId, string name, string email, double pointBalance, int? roleType)
        {
            UserId = userId;
            Name = name;
            Email = email;
            PointBalance = pointBalance;
            RoleType = roleType;
        }

        public AccountPoints()
        {
            //Contructor for enumerables
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public double PointBalance { get; set; }
        public int? RoleType { get; set; }
    }
}