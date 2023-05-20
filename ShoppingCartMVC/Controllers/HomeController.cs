using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;
using ShoppingCartMVC.Models;
namespace ShoppingCartMVC.Controllers
{
    public class HomeController : Controller
    {
        /* Database Connection  */
        dbOnlineStoreEntities db = new dbOnlineStoreEntities();

        /* Add to Cart List use */
        List<Cart> li = new List<Cart>();

        #region Home Page in Showing All Products 

        public ActionResult Index()
        {

            if (TempData["cart"] != null)
            {
                int x = 0;

                List<Cart> li2 = TempData["cart"] as List<Cart>;
                foreach (var item in li2)
                {
                    x += item.bill;

                }
                TempData["total"] = x;
                TempData["item_count"] = li2.Count();
            }
            TempData.Keep();

            var query = db.tblProducts.ToList();
            return View(query);
        }

        #endregion

        #region Add To Cart

        public ActionResult AddtoCart(int id)
        {
            var query = db.tblProducts.Where(x => x.ProID == id).SingleOrDefault();
            return View(query);
        }

        [HttpPost]
        public ActionResult AddtoCart(int id, int qty)
        {
            tblProduct p = db.tblProducts.Where(x => x.ProID == id).SingleOrDefault();
            Cart c = new Cart();
            c.proid = id;
            c.proname = p.P_Name;
            c.price = Convert.ToInt32(p.Unit);
            c.qty = Convert.ToInt32(qty);
            c.bill = c.price * c.qty;
            if (TempData["cart"] == null)
            {
                li.Add(c);
                TempData["cart"] = li;
            }
            else
            {
                List<Cart> li2 = TempData["cart"] as List<Cart>;
                int flag = 0;
                foreach (var item in li2)
                {
                    if (item.proid == c.proid)
                    {
                        item.qty += c.qty;
                        item.bill += c.bill;
                        flag = 1;
                    }

                }
                if (flag == 0)
                {
                    li2.Add(c);
                    //new item
                }
                TempData["cart"] = li2;

            }

            TempData.Keep();

            return RedirectToAction("Index");
        }

        #endregion

        #region Remove Cart Item

        public ActionResult remove(int? id)
        {
            if (TempData["cart"] == null)
            {
                TempData.Remove("total");
                TempData.Remove("cart");
            }
            else
            {
                List<Cart> li2 = TempData["cart"] as List<Cart>;
                Cart c = li2.Where(x => x.proid == id).SingleOrDefault();
                li2.Remove(c);
                int s = 0;
                foreach (var item in li2)
                {
                    s += item.bill;
                }
                TempData["total"] = s;

            }

            return RedirectToAction("Index");
        }
        #endregion

        #region Checkout Code

        public ActionResult Checkout()
        {
            TempData.Keep();
            return View();
        }

        [HttpPost]
        public ActionResult Checkout(string contact, string address, string Method, string PayMethod) /*My chnages for delivery*/
        {
            if (ModelState.IsValid)
            {
                List<Cart> li2 = TempData["cart"] as List<Cart>;
                tblInvoice iv = new tblInvoice();
                iv.UserId = Convert.ToInt32(Session["uid"].ToString());
                iv.InvoiceDate = System.DateTime.Now;
                iv.Bill = (int)TempData["total"];
                iv.Payment = PayMethod;
                iv.DC_Method = Method; /*My chnages for delivery*/
                if (PayMethod == "PayPal")
                {
                    iv.Payment_Status = "Paid";
                }
                else
                {
                    iv.Payment_Status = "Pending";
                }

                db.tblInvoices.Add(iv);
                db.SaveChanges();


                //order book
                foreach (var item in li2)
                {
                    tblOrder od = new tblOrder();
                    od.ProID = item.proid;
                    od.Contact = contact;
                    od.Address = address;
                    od.Method = Method; /*My chnages for delivery*/
                    od.OrderDate = System.DateTime.Now;
                    od.InvoiceId = iv.InvoiceId;
                    od.Qty = item.qty;
                    od.Unit = item.price;
                    od.Total = item.bill;

                    db.tblOrders.Add(od);
                    db.SaveChanges();

                }
                TempData.Remove("total");
                TempData.Remove("cart");
                // TempData["msg"] = "Order Book Successfully!!";

                if (PayMethod == "PayPal")
                {
                    return Content("<script>" +
            "function callPayPal() {" +
            "window.location.href = 'https://www.sandbox.paypal.com/cgi-bin/webscr?cmd=_xclick&amount=" + iv.Bill.ToString() + "&business=sb-w3cyw20367505@business.example.com&item_name=FoodOrder&return=https://2023grp01a.azurewebsites.net/Home/Success';" +
            "}" +
            "callPayPal();" +
         "</script>");
                }
                else
                {
                    return RedirectToAction("Success", "Home"/*, new { id = @Session["uid"] }*/);
                }
            }


            TempData.Keep();
            return View();
        }

        #endregion


        #region All Orders for Admin 

        public ActionResult GetAllOrderDetail()
        {
            var query = db.tblOrders.ToList();
            return View(query);
        }

        #endregion

        #region  Confirm Order by Admin

        public ActionResult ConfirmOrder(int OrderId)
        {
            var query = db.tblOrders.SingleOrDefault(m => m.OrderId == OrderId);
            return View(query);
        }

        [HttpPost]
        public ActionResult ConfirmOrder(tblOrder o, string Status)
        {
            tblOrder tblOrder = db.tblOrders.Find(o.OrderId);
            tblOrder.TblInvoice.Status = Status;

            tblInvoice tblInvoice = db.tblInvoices.Find(o.OrderId);
            if (Status == "Order Collected")
            {

                tblInvoice.Payment_Status = "Paid";

            }

            db.Entry(tblOrder).State = EntityState.Modified;
            db.SaveChanges();
            db.Entry(tblInvoice).State = EntityState.Modified;
            db.SaveChanges();

            return RedirectToAction("GetAllOrderDetail");

        }

        #endregion

        #region Orders for Only User

        public ActionResult OrderDetail(int id)
        {
            var query = db.tblOrders.Where(m => m.TblInvoice.UserId == id).ToList();
            return View(query);
        }

        #endregion

        #region Deliveries for Driver
        public ActionResult DriverDeliveries(int id, tblOrder o)
        {
            var query = db.tblDrivers.Where(m => m.UserId == id).ToList();
            //var orders = db.tblOrders.ToList();
            //var count = 0;

            //using (var db = new dbOnlineStoreEntities())
            //{
            //    var match1 = db.tblDrivers.Any(n => n.UserId == id);

            //    if (match1)
            //    {
            //        foreach (var order in orders)
            //        {
            //            var value = db.tblDrivers
            //                .Where(t => t.OrderId == order.OrderId)
            //                .Select(t => t.TblOrder.TblInvoice.Status)
            //                .FirstOrDefault();

            //            if (value != null && value == "Out for Delivery")
            //            {
            //                count++;
            //            }
            //        }
            //    }

            //    // Reset the orderCount to 0 when a different user logs in
            //    if (TempData["CurrentUserId"] != null && (int)TempData["CurrentUserId"] != id)
            //    {
            //        count = 0;
            //    }

            //    // Store the current user's ID in TempData
            //    TempData["CurrentUserId"] = id;

            //    ViewBag.Orders = orders;
            //    ViewBag.OrderCount = count;

            return View(query);

        }


        #endregion

        #region  Get All Users 

        public ActionResult GetAllUser()
        {
            var query = db.tblUsers.ToList();
            return View(query);
        }

        #endregion

        #region Admin Assigns Driver

        public ActionResult AssignDriver(int OrderId, tblOrder o)
        {

            using (var db = new dbOnlineStoreEntities())
            {
                var dr = db.tblUsers.Where(u => u.RoleType == 3).ToList();

                var driverSelectList = dr.Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.UserId.ToString()
                });

                ViewData["UserId"] = driverSelectList;
            }

            if (o.OrderId == OrderId)
            {
                TempData["OrderID"] = o.OrderId;
            }

            using (var db = new dbOnlineStoreEntities())
            {
                var match1 = db.tblOrders.Any(n => n.OrderId == OrderId);
                var match2 = db.tblOrders.Any(m => m.OrderId == OrderId);

                if (match1)
                {

                    var value = db.tblOrders.Where(t => t.OrderId == o.OrderId).Select(t => t.OrderDate).FirstOrDefault();
                    if (value != null)
                    {
                        TempData["OrderDate"] = value;
                    }
                    else
                    {
                        TempData["OrderDate"] = "";
                    }

                }

                if (match2)
                {
                    var value = db.tblOrders.Where(t => t.OrderId == o.OrderId).Select(t => t.Address).FirstOrDefault();
                    if (value != null)
                    {
                        TempData["Address"] = value;
                    }
                    else
                    {
                        TempData["Address"] = "";
                    }
                }

            }

            return View();





        }

        [HttpPost]
        public ActionResult AssignDriver(Drivers d, tblUser u, int OrderId, tblOrder o)
        {

            using (var db = new dbOnlineStoreEntities())
            {
                var dri = db.tblUsers.Where(ut => ut.RoleType == 3).ToList();

                var driverSelectList = dri.Select(ut => new SelectListItem
                {
                    Text = ut.Name,
                    Value = ut.UserId.ToString()
                });

                ViewData["UserId"] = driverSelectList;
            }

            Drivers dr = new Drivers();

            using (var db = new dbOnlineStoreEntities())
            {
                var match = db.tblUsers.Any(n => n.UserId == d.UserId);

                if (match)
                {

                    var value = db.tblUsers.Where(t => t.UserId == d.UserId).Select(t => t.Name).FirstOrDefault();
                    if (value != null)
                    {
                        dr.DriverName = value;
                    }
                    else
                    {
                        dr.DriverName = "";
                    }

                }
            }



            dr.OrderId = OrderId;
            dr.UserId = d.UserId;

            using (var db = new dbOnlineStoreEntities())
            {
                var match1 = db.tblOrders.Any(n => n.OrderId == OrderId);
                var match2 = db.tblOrders.Any(m => m.OrderId == OrderId);

                if (match1)
                {

                    var value = db.tblOrders.Where(t => t.OrderId == o.OrderId).Select(t => t.OrderDate).FirstOrDefault();
                    if (value != null)
                    {
                        dr.OrderDate = (DateTime)value;
                    }

                }

                if (match2)
                {
                    var value = db.tblOrders.Where(t => t.OrderId == o.OrderId).Select(t => t.Address).FirstOrDefault();
                    if (value != null)
                    {
                        dr.DeliveryAddress = value;
                    }
                    else
                    {
                        dr.DeliveryAddress = "";
                    }
                }

            }


            db.tblDrivers.Add(dr);
            db.SaveChanges();

            tblOrder tblOrder = db.tblOrders.Find(OrderId);
            tblOrder.TblInvoice.Status = "Out for Delivery";

            db.Entry(tblOrder).State = EntityState.Modified;
            db.SaveChanges();

            return RedirectToAction("GetAllOrderDetail");
        }
        #endregion

        #region Invoice for  User

        public ActionResult Invoice(int id)
        {
            List<InvoiceVM> InvceVMList = new List<InvoiceVM>();

            var invList = (from o in db.tblOrders
                           join i in db.tblInvoices on o.InvoiceId equals i.InvoiceId
                           join u in db.tblUsers on i.UserId equals u.UserId
                           join pr in db.tblProducts on o.ProID equals pr.ProID
                           select new
                           {
                               i.InvoiceId,
                               i.InvoiceDate,
                               u.Name,
                               o.Address,
                               o.Contact,
                               pr.P_Name,
                               o.Qty,
                               o.Unit,
                               i.Payment_Status,
                               o.Total


                           }).ToList();




            foreach (var pro in invList)
            {

                if (pro.InvoiceId == id)
                {
                    //count += 1;
                    InvoiceVM objVM = new InvoiceVM();
                    objVM.InvoiceID = pro.InvoiceId;
                    objVM.InvoiceDate = pro.InvoiceDate;
                    objVM.Name = pro.Name;
                    objVM.Address = pro.Address;
                    objVM.Contact = pro.Contact;
                    objVM.Item = pro.P_Name;
                    objVM.Qty = pro.Qty;
                    objVM.Unit = pro.Unit;
                    objVM.Amount = pro.Total;
                    objVM.Payment_Status = pro.Payment_Status;
                    objVM.TotalAmount = pro.Total;
                    InvceVMList.Add(objVM);
                }
            }

            return View(InvceVMList);

        }


        #endregion

        #region Driver Views Individual Delivery Details
        public ActionResult DeliveryDetails(int OrderId)
        {
            TempData["oId"] = OrderId;
            var query = db.tblDrivers.SingleOrDefault(m => m.OrderId == OrderId);
            return View(query);
        }

        [HttpPost]
        public ActionResult DeliveryDetails(Drivers drivers, bool CashPaid)
        {

            int orderId = (int)TempData["oId"];
            tblOrder tblOrder = db.tblOrders.Find(orderId);
            tblOrder.TblInvoice.Status = "Order Delivered";

            tblInvoice tblInvoice = db.tblInvoices.Find(orderId);
            if (tblInvoice.Payment_Status == "Pending")
            {
                if (CashPaid)
                {
                    tblInvoice.Payment_Status = "Paid";
                }
                else
                {
                    tblInvoice.Payment_Status = "NPaid";
                }
            }


            db.Entry(tblOrder).State = EntityState.Modified;
            db.SaveChanges();
            db.Entry(tblInvoice).State = EntityState.Modified;
            db.SaveChanges();
            return RedirectToAction("DeliveryDetails", new { OrderId = orderId });

        }

        #endregion

        #region Admin Notify Customer for Collection

        public ActionResult NotifyCustomer(int OrderId)
        {
            TempData["order"] = OrderId;
            var query = db.tblOrders.SingleOrDefault(m => m.OrderId == OrderId);
            return View(query);
        }

        [HttpPost]
        public ActionResult NotifyCustomer(tblOrder o, bool orderReady)
        {
            int orderId = (int)TempData["order"];
            tblOrder tblOrder = db.tblOrders.Find(orderId);
            tblInvoice tblInvoice = db.tblInvoices.Find(orderId);
            tblOrder.TblInvoice.Status = "Ready for Collection";

            if (orderReady)
            {
                string oId = orderId.ToString();
                string toEmail = tblOrder.TblInvoice.TblUser.Email;
                string name = tblOrder.TblInvoice.TblUser.Name;

                var body = "Dear " + name + ",<br><br>" +
               "I'm contacting you on behalf of Turbo Meals, and I'm glad to inform you that your Order #" + orderId +
               " is now ready for collection.<br><br>" + "Please note, any outstanding payments may be made upon collection.<br><br>" +
                "Once your order is collected your Order Status will automatically be updated.<br><br>" +
               "Thank you for choosing Turbo Meals!";


                var message = new MailMessage();
                message.To.Add(new MailAddress(toEmail));
                message.From = new MailAddress("turbomeals123@gmail.com");
                message.Subject = "Your Turbo Meals Order Ready for Collection";
                message.Body = string.Format(body);
                message.IsBodyHtml = true;

                using (var smtp = new SmtpClient())
                {
                    smtp.Send(message);
                }
            }

            db.Entry(tblInvoice).State = EntityState.Modified;
            db.SaveChanges();
            return RedirectToAction("GetAllOrderDetail");
        }


        #endregion

        #region Directions for Driver
        public ActionResult Map(int OrderId)
        {
            TempData["oId2"] = OrderId;
            var query = db.tblDrivers.SingleOrDefault(m => m.OrderId == OrderId);
            return View(query);
        }
        #endregion

        #region Sending Delivery Notification
        public ActionResult SendMail(int OrderId)
        {
            TempData["oId2"] = OrderId;
            var query = db.tblDrivers.SingleOrDefault(m => m.OrderId == OrderId);
            return View(query);
        }

        [HttpPost]
        public ActionResult SendMail(Drivers dri, string delTime, string licenseNum)
        {
            int orderId = (int)TempData["oId2"];
            tblOrder tblOrder = db.tblOrders.Find(orderId);
            //TempData["id"] = new { id = @Session["uid"] };
            int id;


            if (Session["uid"] != null && int.TryParse(Session["uid"].ToString(), out id))
            {
                using (var db = new dbOnlineStoreEntities())
                {
                    var matchDr = db.tblUsers.Any(n => n.UserId == id);
                    if (matchDr)
                    {
                        var value = db.tblUsers.Where(t => t.UserId == id).Select(t => t.Name).FirstOrDefault();
                        if (value != null)
                        {
                            TempData["drName"] = value;
                        }
                        else
                        {
                            TempData["drName"] = "";
                        }

                    }
                }
            }
            else
            {
                TempData["drName"] = "A";
            }


            string toEmail = tblOrder.TblInvoice.TblUser.Email;
            string name = tblOrder.TblInvoice.TblUser.Name;
            string driverName = TempData["drName"].ToString();

            var body = "Dear " + name + ",<br><br>" +
                "I'm contacting you on behalf of Turbo Meals, my name is " + driverName +
                ", and I will be delivering your order placed with us at Turbo Meals.<br><br>" +
                "Your order delivery should arrive in " + delTime + ".<br><br>" +
                "Since we at Turbo Meals deeply value our customers and their safety we have taken an extra precaution.<br><br>" +
                "Please note my license number which will be used to deliver your order to you is as follows : " + licenseNum +
                "<br><br>See you soon! ";

            var message = new MailMessage();
            message.To.Add(new MailAddress(toEmail));
            message.From = new MailAddress("turbomeals123@gmail.com");
            message.Subject = "Your Turbo Meals Order Delivery";
            message.Body = string.Format(body);
            message.IsBodyHtml = true;

            using (var smtp = new SmtpClient())
            {
                smtp.Send(message);
            }


            return RedirectToAction("Map", new { OrderId = orderId });
        }
        #endregion

        #region Driver Collects Order
        public ActionResult DriverGetOrder(int OrderId)
        {
            TempData["oId"] = OrderId;
            var query = db.tblDrivers.SingleOrDefault(m => m.OrderId == OrderId);
            return View(query);
        }

        [HttpPost]
        public ActionResult DriverGetOrder(bool OrderCollected)
        {
            int orderId = (int)TempData["oId"];

            if (OrderCollected)
            {

                return RedirectToAction("Map", new { OrderId = orderId });
            }
            return View();
        }

        #endregion


        public ActionResult Success()
        {
            return View();
        }

        public ActionResult Contact()
        {
            return View();
        }

        public ActionResult ThankYou()
        {
            return View();
        }

        public ActionResult About()
        {
            return View();
        }

        public ActionResult Reviews()
        {
            return View();
        }

        public ActionResult Turbo_Deals()
        {
            return View();
        }

        private IEnumerable<SelectListItem> GetSeatOptions()  // v changes

        {
            return new List<SelectListItem>
    {
        new SelectListItem { Value = "1-2", Text = "1-2 seats" },
        new SelectListItem { Value = "3-4", Text = "3-4 seats" },
        new SelectListItem { Value = "5-6", Text = "5-6 seats" },
        new SelectListItem { Value = "6-8", Text = "6-8 seats" }

    };
        }
    }
}