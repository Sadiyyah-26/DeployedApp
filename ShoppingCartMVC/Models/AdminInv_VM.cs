using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace ShoppingCartMVC.Models
{
    public class AdminInv_VM
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Inv_VM_ID { get; set; }

        //tblAdminInv
        public int InvoiceId { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public string Payment { get; set; }


        //tblUser
        public string Name { get; set; }

        //tblSupplier
        public string SupplName { get; set; }

        public string ContactPerson { get; set; }
        public string ContactPersonPos { get; set; }
        public string ContactNum { get; set; }
        public string Email { get; set; }
        public string Tel { get; set; }
        public string PhysicalAddress { get; set; }

        //tblAdminOrder
        public int OrderId { get; set; }

        public string Contact { get; set; }

        public string Address { get; set; }

        public int? Unit { get; set; }

        public int? Qty { get; set; }

        public int? Total { get; set; }

        public DateTime? OrderDate { get; set; }

        public string OrderStatus { get; set; }

        //tblIngredients
        public string Ing_Name { get; set; }

        public List<Item> Items { get; set; }

        public class Item
        {
            public int OrderId { get; set; }
            public string OrderStatus { get; set; }
            public string Ing_Name { get; set; }
            public int? Unit { get; set; }
            public int? Qty { get; set; }
            public int? Total { get; set; }

        }

    }
}