using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ShoppingCartMVC.Models
{
    public partial class tblReservation
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int BookingId { get; set; }

        public string Customer_Name { get; set; }
        public string Mail { get; set; }

        public string Number { get; set; }

        public DateTime? Date { get; set; }

        public DateTime? Time { get; set; }

        public string Seating { get; set; }
        public IEnumerable<SelectListItem> SeatNumberList { get; set; }

    }
}
