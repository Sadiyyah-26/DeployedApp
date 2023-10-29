using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace ShoppingCartMVC.Models
{
    public class Transactions
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int TransactionID { get; set; }

        public int FloatID { get; set; }

        public DateTime? TransactionTime { get; set; }

        public string Transaction { get; set; }

        public int? InStoreOrderID { get; set; }

        public int? OnlineOrderID { get; set; }

        public int? UserID { get; set; }

        public string UserName { get; set; }

        public int Current { get; set; }

        public int Credit { get; set; }

        public int GivenAmt { get; set; }

        public int Debit { get; set; }

        public int ClosingBalance { get; set; }

        public virtual tblInStoreOrder InStore { get; set; }

        public virtual tblOrder Order { get; set; }

        public virtual tblUser User { get; set; }

        public virtual tblCashFloat Float { get; set; }
    }
}