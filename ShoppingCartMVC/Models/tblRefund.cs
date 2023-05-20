using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace ShoppingCartMVC.Models
{
    public class tblRefund
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RefundId { get; set; }
        public string EmailID { get; set; }
        public int OrderId { get; set; }
        public virtual ICollection<tblOrder> tblOrders { get; set; }
        public DateTime RefundRequestDate { get; set; }
        public string RefundReason { get; set; }
        public string Image { get; set; }
        public string RefundStatus { get; set; }

        public string checkStatus()
        {
            string sts = "";
            if (RefundStatus == "Successful")
            {
                sts = "Refund Complete";
            }
            else if (RefundStatus == "Unsuccessful")
            {
                sts = "Refund Not Cleared";
            }
            else
            {
                sts = "Refund Requested";
            }
            return sts;
        }
    }
}