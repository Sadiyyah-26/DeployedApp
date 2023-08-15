using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace ShoppingCartMVC.Models
{
    public class tblSupplierOrder
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OrderId { get; set; }
        public int? SupplierId { get; set; }
        public DateTime? OrderDate { get; set; }
        public int? Total { get; set; }
        public string sOrderStatus { get; set; }

        public int? ProID { get; set; }

        public int? sUnit { get; set; }

        public int? QtyOrder { get; set; }


        public virtual tblProduct TblProduct { get; set; }
        public virtual tblSupplier tblSupplier { get; set; }


        public string checkStatus()
        {
            string sts = "";
            if (sOrderStatus == "Successful")
            {
                sts = "Received";
            }
            else if (sOrderStatus == "Unsuccessful")
            {
                sts = "Returned";
            }
            else
            {
                sts = "Ordered";
            }
            return sts;
        }
        public int? SuppUnit()
        {
            dbOnlineStoreEntities db= new dbOnlineStoreEntities();
            var uAmt = (from a in db.tblProducts
                        where a.ProID == ProID
                        select a.Unit).Single();

            return (uAmt - (uAmt / 2));
        }

        public int? getTotal()
        {
            return QtyOrder * SuppUnit();
        }
    }
}