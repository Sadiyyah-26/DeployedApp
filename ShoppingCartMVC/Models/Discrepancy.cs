using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace ShoppingCartMVC.Models
{
    public class Discrepancy
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        public DateTime? Date { get; set; }

        public int InitalFloat { get; set; }

        public int ClosingBalance { get; set; }

        public int CountedBalance { get; set; }

        public int CashDiscrepancy { get; set; }
    }
}