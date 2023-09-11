using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;
using ShoppingCartMVC.Models;
using QRCoder;

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

            //get the users point balance
            if (Session["uid"] != null)
            {
                try
                {
                    int userID = Convert.ToInt32(Session["uid"].ToString());
                    if (userID > 0)
                    {
                        var value = (from e in db.tblPoints
                                     where e.UserID == userID
                                     select e.PointBalance).SingleOrDefault();
                        ViewBag.TotalPoints = value;
                    }
                }
                catch (Exception)
                {

                    throw;
                }
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

            //extras
            int? catIdToMatch = query.CatId;

            var extrasWithCost = db.tblExtras
        .Where(m => m.CatId == catIdToMatch)
        .ToList() 
        .Select(m => new
        {
            extName = m.exName,
            extCost = m.exCost
        })
        .ToList();

            
            var extrasWithCostViewModel = extrasWithCost.Select(m => new ExtraVM
            {
                ExName = m.extName,
                ExCost = decimal.Parse(m.extCost)
            })
            .ToList();



            ViewBag.ExtraNames = extrasWithCostViewModel;

            //points
            if (Session["uid"] != null)
            {
                try
                {
                    int userID = Convert.ToInt32(Session["uid"].ToString());
                    if (userID > 0)
                    {
                        var value = (from e in db.tblPoints
                                     where e.UserID == userID
                                     select e.PointBalance).SingleOrDefault();
                        TempData["points"] = value;
                        ViewBag.TotalPoints = value;
                    }
                }
                catch (Exception)
                {

                    throw;
                }
            }
            TempData.Keep();

            return View(query);
        }

       

        [HttpPost]
        public ActionResult AddtoCart(int id, int qty, string[] selectedExtras)
        {
            //points
            if (Session["uid"] != null)
            {
                try
                {
                    int userID = Convert.ToInt32(Session["uid"].ToString());
                    if (userID > 0)
                    {
                        var value = (from e in db.tblPoints
                                     where e.UserID == userID
                                     select e.PointBalance).SingleOrDefault();
                        TempData["points"] = value;
                        ViewBag.TotalPoints = value;
                    }
                }
                catch (Exception)
                {

                    throw;
                }
            }
            TempData.Keep();

            //retrive info
            tblProduct p = db.tblProducts.Where(x => x.ProID == id).SingleOrDefault();


            Cart c = new Cart();
            c.proid = id;
            c.proname = p.P_Name;
            c.price = Convert.ToInt32(p.Unit);
            c.qty = Convert.ToInt32(qty);


            if (selectedExtras == null || selectedExtras.Length == 0)
            {
                selectedExtras = new string[] { "None" };
            }


            c.extras = string.Join(",", selectedExtras);

            // Calculate the add-on total price
            int addonTotalPrice = 0;

            foreach (var extra in selectedExtras)
            {
                tblExtras tblExtra = db.tblExtras.Where(e => e.exName == extra).SingleOrDefault();

                if (tblExtra != null)
                {
                    addonTotalPrice += Convert.ToInt32(tblExtra.exCost);
                }
            }
            c.extrasCost = Convert.ToInt32(addonTotalPrice);

            c.bill = (c.price * c.qty) + addonTotalPrice;

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
            if (Session["uid"] != null)
            {
                try
                {
                    int userID = Convert.ToInt32(Session["uid"].ToString());
                    if (userID > 0)
                    {
                        var value = (from e in db.tblPoints
                                     where e.UserID == userID
                                     select e.PointBalance).SingleOrDefault();
                        TempData["points"] = value;
                        ViewBag.TotalPoints = value;
                    }
                }
                catch (Exception)
                {

                    throw;
                }
            }
            TempData.Keep();
            return View();
        }

        [HttpPost]
        public ActionResult Checkout(string contact, string address, string Method, string PayMethod, string RedeemPoints) //v changes
        {
            if (ModelState.IsValid)
            {
                List<Cart> li2 = TempData["cart"] as List<Cart>;
                tblInvoice iv = new tblInvoice();
                iv.UserId = Convert.ToInt32(Session["uid"].ToString());
                iv.InvoiceDate = System.DateTime.Now;
                //iv.Bill = (int)TempData["total"];

                if (RedeemPoints == "No" || RedeemPoints == null) { iv.Bill = (int)TempData["total"]; }
                else if (RedeemPoints == "Yes")
                {
                    iv.Bill = Convert.ToInt32((int)TempData["total"] * 0.5);

                    //Remove points from the user's account
                    UserPoints uPoints = new UserPoints();
                    //Find the user
                    int userID = Convert.ToInt32(Session["uid"].ToString());
                    var cols = db.tblPoints.Where(w => w.UserID == userID);
                    foreach (var item in cols)
                    {
                        //Add 5% of the total bill as points to their account
                        item.PointBalance -= Convert.ToDouble(iv.Bill);
                    }
                    db.SaveChanges();
                }




                iv.Payment = PayMethod;
                iv.DC_Method = Method;

                if (PayMethod == "PayPal")
                {
                    iv.Payment_Status = "Paid";

                    //Add points to the user's account
                    UserPoints uPoints = new UserPoints();
                    //Find the user
                    int userID = Convert.ToInt32(Session["uid"].ToString());
                    var cols = db.tblPoints.Where(w => w.UserID == userID);
                    foreach (var item in cols)
                    {
                        //Add 5% of the total bill as points to their account
                        item.PointBalance += (int)TempData["total"] * 0.05;
                    }
                    db.SaveChanges();
                }
                else if (PayMethod == "Cash")
                {
                    iv.Payment_Status = "Pending";

                    //Add points to the user's account
                    UserPoints uPoints = new UserPoints();
                    //Find the user
                    int userID = Convert.ToInt32(Session["uid"].ToString());
                    var cols = db.tblPoints.Where(w => w.UserID == userID);
                    foreach (var item in cols)
                    {
                        //Add 5% of the total bill as points to their account
                        item.PointBalance += (int)TempData["total"] * 0.05;
                    }
                    db.SaveChanges();
                }

                db.tblInvoices.Add(iv);
                db.SaveChanges();

                // Retrieve user information
                tblUser user = db.tblUsers.Find(iv.UserId);
                string userEmail = user.Email;
                string userName = user.Name;

                // Order book
                foreach (var item in li2)
                {
                    tblOrder od = new tblOrder();
                    od.ProID = item.proid;
                    od.Contact = contact;
                    od.Address = address;
                    od.Method = Method;
                    od.OrderDate = System.DateTime.Now;
                    od.InvoiceId = iv.InvoiceId;
                    od.Qty = item.qty;
                    od.Unit = item.price;
                    od.Total = item.bill;
                    od.OrderReady = "Being Prepared";
                    od.Extras = item.extras;
                    od.ExtrasCost = item.extrasCost;
                    db.tblOrders.Add(od);                  

                }
                db.SaveChanges();

                // Send email to user
                var body = $"Dear {userName},<br /><br />Your order was placed successfully.<br><br> You will be notified when your order is ready.<br><br>";

                if (iv.Payment == "Cash")
                {
                    body += "Please note that your payment is pending. Kindly complete the payment when you recieve your order.";
                }
                else if (iv.Payment == "PayPal")
                {
                    body += "Your payment has been successfully processed.";
                }

                body += "<br /><br />Order Details:<br />";

                foreach (var item in li2)
                {
                    tblProduct product = db.tblProducts.Find(item.proid);
                    string productName = product.P_Name;
                    int quantity = item.qty;
                    int Price = (int)product.Unit;
                    int bill = item.bill;

                    body += $"Item: {productName}<br />" +
                            $"Quantity: {quantity}<br />" +
                            $"UnitPrice: {Price}<br />" +
                            $"Total Bill: {bill}<br /><br />";
                }

                var message = new MailMessage();
                message.To.Add(new MailAddress(userEmail));
                message.From = new MailAddress("turbomeals123@gmail.com"); // Replace with your email address
                message.Subject = "Order Confirmation";
                message.Body = body;
                message.IsBodyHtml = true;

                using (var smtp = new SmtpClient())
                {
                    smtp.Send(message);
                }


                TempData.Remove("total");
                TempData.Remove("cart");

                if (PayMethod == "PayPal")
                {
                    return Content("<script>" +
                        "function callPayPal() {" +
                        "window.location.href = 'https://www.sandbox.paypal.com/cgi-bin/webscr?cmd=_xclick&amount=" + iv.Bill.ToString() + "&business=sb-w3cyw20367505@business.example.com&item_name=FoodOrder&return=https://localhost:44377//Home/Success';" +
                        "}" +
                        "callPayPal();" +
                        "</script>");
                }
                else
                {
                    return RedirectToAction("Success", "Home");
                }
            }

            TempData.Keep();
            return View();
        }
        #endregion

        public ActionResult Error(string message)
        {
            return View();
        }

        #region All Orders for Admin 

        public ActionResult GetAllOrderDetail()
        {
            var query = db.tblOrders.Where(m => m.OrderReady == "Ready").ToList();
            return View(query);
        }

        #endregion

        #region  Confirm Order by Admin

        public ActionResult ConfirmOrder(int OrderId)
        {
            TempData["orderNum"] = OrderId;
            var query = db.tblOrders.SingleOrDefault(m => m.OrderId == OrderId);
            return View(query);
        }

        [HttpPost]
        public ActionResult ConfirmOrder( string Status)
        {
            int id = (int)TempData["orderNum"];
            tblInvoice tblInvoice = db.tblInvoices.Find(id);
            tblInvoice.Status = Status;
            if (Status== "Order Collected")
            {
                
                tblInvoice.Payment_Status = "Paid";
                tblInvoice.Time_CD = DateTime.Now;
            }
            db.Entry(tblInvoice).State = EntityState.Modified;
            db.SaveChanges();

            tblOrder tblOrder = db.tblOrders.Find(id);
            string oId = id.ToString();
            string toEmail = tblInvoice.TblUser.Email;
            string name = tblInvoice.TblUser.Name;


            var body = "Dear " + name + ",<br><br>" +
           "Your Order #" + oId + " has been collected from Turbo Meals.<br><br>Payment Status: Paid.<br><br>Thank you for choosing Turbo Meals!";



            var message = new MailMessage();
            message.To.Add(new MailAddress(toEmail));
            message.From = new MailAddress("turbomeals123@gmail.com");
            message.Subject = "Turbo Meals Order Collected";
            message.Body = string.Format(body);
            message.IsBodyHtml = true;

            using (var smtp = new SmtpClient())
            {
                smtp.Send(message);
            }

            return RedirectToAction("GetAllOrderDetail");

        }

        #endregion

        #region Orders for Only User

        public ActionResult OrderDetail(int id)
        {
            var query = db.tblOrders.Where(m => m.TblInvoice.UserId == id)
               .GroupBy(i => i.InvoiceId)
                .Select(m => m.FirstOrDefault()).ToList();
            return View(query);
        }

        #endregion

        #region Deliveries for Driver
        public ActionResult DriverDeliveries(int id)
        {
            var query = db.tblDrivers.Where(m => m.UserId == id).ToList();
            return View(query);

        }


        #endregion

        #region  Get All Users 

        public ActionResult GetAllUser()
        {
            var query = db.tblUsers.ToList();
            List<AccountPoints> accPoints = new List<AccountPoints>();
            foreach (var item in query)
            {
                var value = (from e in db.tblPoints
                             where e.UserID == item.UserId
                             select e.PointBalance).SingleOrDefault();
                accPoints.Add(new AccountPoints(item.UserId, item.Name, item.Email, value, item.RoleType));
            }
            var list = accPoints.ToList();
            return View(list);
        }

        #endregion

        #region Admin Assigns Driver

        public ActionResult AssignDriver(int OrderId, tblOrder o)
        {
                var dr = db.tblUsers.Where(u => u.RoleType == 3).ToList();

                var driverSelectList = dr.Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.UserId.ToString()
                });

                ViewData["UserId"] = driverSelectList;
            

            if (o.OrderId == OrderId)
            {
                TempData["OrderID"] = o.OrderId;
            }
       
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

            

            return View();
        }

        [HttpPost]
        public ActionResult AssignDriver(Drivers d, tblUser u, int OrderId, tblOrder o)
        {

            
                var dri = db.tblUsers.Where(ut => ut.RoleType == 3).ToList();

                var driverSelectList = dri.Select(ut => new SelectListItem
                {
                    Text = ut.Name,
                    Value = ut.UserId.ToString()
                });

                ViewData["UserId"] = driverSelectList;
            

            Drivers dr = new Drivers();

            
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
            



            dr.OrderId = OrderId;
            dr.UserId = d.UserId;

           
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
            if (Session["uid"] != null)
            {
                try
                {
                    int userID = Convert.ToInt32(Session["uid"].ToString());
                    if (userID > 0)
                    {
                        var value = (from e in db.tblPoints
                                     where e.UserID == userID
                                     select e.PointBalance).SingleOrDefault();
                        TempData["points"] = value;
                        ViewBag.TotalPoints = value;
                    }
                }
                catch (Exception)
                {

                    throw;
                }
            }
            TempData.Keep();



            var invList = (from o in db.tblOrders
                           join i in db.tblInvoices on o.InvoiceId equals i.InvoiceId
                           join u in db.tblUsers on i.UserId equals u.UserId
                           join pr in db.tblProducts on o.ProID equals pr.ProID
                           where i.InvoiceId == id
                           group new
                           {
                               i.InvoiceId,
                               i.InvoiceDate,
                               u.Name,
                               o.Address,
                               o.Contact,
                               pr.P_Name,
                               o.Qty,
                               o.Unit,
                               o.Extras,
                               o.ExtrasCost,
                               o.Method,
                               i.Time_CD,
                               i.Payment_Status,
                               o.Total
                           } by i.InvoiceId into grouped
                           select new InvoiceVM
                           {
                               InvoiceID = grouped.Key,
                               InvoiceDate = grouped.FirstOrDefault().InvoiceDate,
                               Name = grouped.FirstOrDefault().Name,
                               Address = grouped.FirstOrDefault().Address,
                               Contact = grouped.FirstOrDefault().Contact,
                               Item = grouped.FirstOrDefault().P_Name,
                               Qty = grouped.FirstOrDefault().Qty,
                               Unit = grouped.FirstOrDefault().Unit,
                               Amount = grouped.FirstOrDefault().Total,
                               Extras = grouped.FirstOrDefault().Extras,
                               ExtrasCost = grouped.FirstOrDefault().ExtrasCost,
                               Method = grouped.FirstOrDefault().Method,
                               CD_Time = grouped.FirstOrDefault().Time_CD,
                               Payment_Status = grouped.FirstOrDefault().Payment_Status,

                               ExtraInfo = grouped.Select(info => new InvoiceVM.Info
                               {
                                   Item = info.P_Name,
                                   Qty = info.Qty,
                                   Unit = info.Unit,
                                   Amount = info.Total,
                                   Extras = info.Extras,
                                   ExtrasCost = info.ExtrasCost

                               }).ToList()
                           }).ToList();



            return View(invList);

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

            string payment = "";
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
                payment = "Please note, your Payment Status has now been updated to Paid.<br><br>";

            }
            payment = "Payment Status: Paid<br><br>";

            db.Entry(tblOrder).State = EntityState.Modified;
            db.SaveChanges();
            db.Entry(tblInvoice).State = EntityState.Modified;
            db.SaveChanges();

            string oId = orderId.ToString();
            string toEmail = tblOrder.TblInvoice.TblUser.Email;
            string name = tblOrder.TblInvoice.TblUser.Name;
            string address = tblOrder.Address;

            var body = "Dear " + name + ",<br><br>" +
           "Your Order #" + orderId + " has been successfully delivered to you at " + address + ".<br><br>" + payment +
           "Thank you for choosing Turbo Meals!";


            var message = new MailMessage();
            message.To.Add(new MailAddress(toEmail));
            message.From = new MailAddress("turbomeals123@gmail.com");
            message.Subject = "Turbo Meals Order Delivered";
            message.Body = string.Format(body);
            message.IsBodyHtml = true;

            using (var smtp = new SmtpClient())
            {
                smtp.Send(message);
            }

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
                message.Subject = "Turbo Meals Order Ready for Collection";
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
            else
            {
                TempData["drName"] = "Driver";
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

        #region New Orders Go to Staff for Prep
        public ActionResult PrepStaff(int id)
        {
            var query = db.tblOrders.Where(m => m.OrderReady == "Being Prepared").ToList();
            return View(query);

        }

        #endregion

        #region Staff Makes Order Ready
        public ActionResult PrepOrder(int OrderId)
        {
            TempData["cusOrder"] = OrderId;
            var query = db.tblOrders.SingleOrDefault(m => m.OrderId == OrderId);
            return View(query);
        }

        [HttpPost]
        public ActionResult PrepOrder(bool Ready)
        {
            int order = (int)TempData["cusOrder"];
            tblOrder tblOrder = db.tblOrders.Find(order);
            if (Ready)
            {
                tblOrder.OrderReady = "Ready";
            }

            db.Entry(tblOrder).State = EntityState.Modified;

            //update stock qty
            var ingPro = db.IngredientProducts.Where(ip => ip.ProID == tblOrder.ProID).ToList();

            foreach (var i in ingPro)
            {
                var ingID = i.Ing_ID;

                var ingr = db.tblIngredients.SingleOrDefault(m => m.Ing_ID == ingID);
                var supplIngr = db.SupplierIngredients.SingleOrDefault(m => m.Ing_ID == ingID);

                int qtyToReduce = 0;
                if (ingr != null)
                {
                    qtyToReduce = ingr.Ing_UnitsUsed * (int)tblOrder.Qty;
                    ingr.Ing_StockyQty -= qtyToReduce;

                    if ((ingr.Ing_StockyQty < 50) && (ingr.StockStatus == "In Stock"))
                    {
                        ingr.StockStatus = "Low Stock";
                        //send qty alert email 
                        string ing = ingr.Ing_Name;
                        string qty = Convert.ToString(ingr.Ing_StockyQty);
                        string suppl = supplIngr.TblSupplier.SupplName;
                        //test for link redirect
                        int supID = supplIngr.SupplierId;

                        string baseUrl = $"{Request.Url.Scheme}://{Request.Url.Authority}";
                        string url = Url.Action("Index", "AdminHome", new { id = supID });
                        string link = $"{baseUrl}{url}";

                        var content = $"The Stock Quantity for the following Ingredient has dropped below 50 and requires restocking.<br/><br/> ";
                        content += "Ingredient: " + ing + " supplied by " + suppl + " current quantity has dropped to " + qty + "<br/>Click here to place order with Supplier: " + link; ;



                        var email = new MailMessage();
                        email.To.Add(new MailAddress("turbostaff786@gmail.com"));
                        email.From = new MailAddress("turbomeals123@gmail.com");
                        email.Subject = "Low Stock Alert!";
                        email.Body = content;
                        email.IsBodyHtml = true;

                        using (var smtp = new SmtpClient())
                        {
                            smtp.Send(email);
                        }

                    }

                }
                db.Entry(ingr).State = EntityState.Modified;
            }//foreach ingr

            db.SaveChanges();
            return RedirectToAction("PrepStaff", "Home", new { id = @Session["uid"] });
        }
        #endregion

        #region Customer Cancels Order
        public ActionResult CancelOrder(int OrderId)
        {
            TempData["o"] = OrderId;
            var query = db.tblOrders.SingleOrDefault(m => m.OrderId == OrderId);

            return View(query);
        }

        [HttpPost]
        public ActionResult CancelOrder()
        {
            int orderId = (int)TempData["o"];
            tblInvoice tblInvoice = db.tblInvoices.Find(orderId);
            tblInvoice.Status = "Cancelled";

            string oId = orderId.ToString();
            string toEmail = tblInvoice.TblUser.Email;
            string name = tblInvoice.TblUser.Name;
            string total = tblInvoice.Bill.ToString();
            string payMethod = tblInvoice.DC_Method;
            string refund = "";
            if (tblInvoice.Payment_Status == "Pending")
            {
                refund = "Since you have opted for Cash on " + payMethod + " there is no refund due to you." +
                "<br><br>Hope to have you choose Turbo Meals in the future.";

            }
            else
            {
                refund = "Please note, your refund of R" + total + ".00 will be processed and credited to your account within 48 hours.<br><br>Hope to have you choose Turbo Meals in the future.";
            }

            var body = "Dear " + name + ",<br><br>" +
           "Your Order #" + orderId + " has successfully been cancelled as per your request.<br><br>" +
            refund;


            var message = new MailMessage();
            message.To.Add(new MailAddress(toEmail));
            message.From = new MailAddress("turbomeals123@gmail.com");
            message.Subject = "Turbo Meals Order Cancellation";
            message.Body = string.Format(body);
            message.IsBodyHtml = true;

            using (var smtp = new SmtpClient())
            {
                smtp.Send(message);
            }

            db.Entry(tblInvoice).State = EntityState.Modified;
            db.SaveChanges();
            return RedirectToAction("OrderDetail", "Home", new { id = @Session["uid"] });
        }

        #endregion

        public ActionResult Success()
        {
            return View();
        }

        public ActionResult HomePage()
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


        public ActionResult Reviews()
        {
            return View();
        }

        public ActionResult Turbo_Deals()
        {
            return View();
        }

        public ActionResult Refund(int id)
        {
            var query = db.tblOrders.Where(m => m.TblInvoice.UserId == id).ToList();
            return View(query);
        }

        #region Dashboard
        public ActionResult Dashboard()
        {
            int userCount = GetUserCountFromDatabase(); // Implement this function to get user count
            int orderCount = GetTotalOrderCount();
            int posCount = GetTotalPOSsales();// Get total order count
            int productCount = GetTotalProducts();
            decimal totalSales = GetTotalSalesAmount();
            decimal totalInStore = GetTotalInStoreSalesAmount();
            decimal totalOnline = GetTotalOnlineSalesAmount();
            int deliveryInvoiceCount = GetDeliveryInvoiceCount();
            int takeawayCount = GetTakeawayCount();

            ViewBag.UserCount = userCount;
            ViewBag.OrderCount = orderCount;
            ViewBag.POSCount = posCount;
            ViewBag.ProductCount = productCount;
            ViewBag.TotalSales = totalSales;
            ViewBag.TotalInStore = totalInStore;
            ViewBag.TotalOnline = totalOnline;
            ViewBag.DeliveryInvoiceCount = deliveryInvoiceCount;
            ViewBag.TakeawayCount = takeawayCount;

            return View();
        }

        public int GetUserCountFromDatabase()
        {
            using (var context = new dbOnlineStoreEntities())
            {
                int UserCount = context.tblUsers.Count();

                return UserCount;
            }
        }

        public int GetTotalOrderCount()
        {
            using (var context = new dbOnlineStoreEntities())
            {
                int orderCount = context.tblOrders.Count();

                return orderCount;
            }
        }
        public int GetTotalPOSsales()
        {
            using (var context = new dbOnlineStoreEntities())
            {
                int PosCount = context.TblInStoreOrders.GroupBy(o => o.OrderNumber)
                                                      .Count();
                return PosCount;
            }
        }
        public int GetTotalProducts()
        {
            using (var context = new dbOnlineStoreEntities())
            {
                int productCount = context.tblProducts.Count();

                return productCount;
            }
        }

        public decimal GetTotalInStoreSalesAmount()
        {
            using (var context = new dbOnlineStoreEntities())
            {
                // Calculate the sum of total fields in tblInStoreOrder
                decimal totalInStore = context.TblInStoreOrders.Sum(o => o.Total) ?? 0; ;

                return totalInStore;
            }
        }

        public decimal GetTotalOnlineSalesAmount()
        {
            using (var context = new dbOnlineStoreEntities())
            {
                // Calculate the sum of total fields in tblInStoreOrder
                decimal totalOnline = context.tblOrders.Sum(o => o.Total) ?? 0; ;

                return totalOnline;
            }
        }

        public decimal GetTotalSalesAmount()
        {
            using (var context = new dbOnlineStoreEntities())
            {

                decimal totalSalesInStore = context.TblInStoreOrders.Sum(o => o.Total) ?? 0;
                decimal totalSalesOnline = context.tblOrders.Sum(o => o.Total) ?? 0;
                decimal totalSales = totalSalesInStore + totalSalesOnline;

                return totalSales;
            }
        }

        public int GetDeliveryInvoiceCount()
        {
            using (var context = new dbOnlineStoreEntities())
            {
                int deliveryInvoiceCount = context.tblInvoices.Count(i => i.DC_Method == "Delivery");

                return deliveryInvoiceCount;
            }
        }

        public int GetTakeawayCount()
        {
            using (var context = new dbOnlineStoreEntities())
            {
                int takeawayCount = context.tblInvoices
                    .Count(i => i.DC_Method == "Collection" && i.TblOrders.Any(o => o.Method == "Takeaway"));

                // To count unique order numbers for takeaway records
                var takeawayOrderNumbers = context.TblInStoreOrders
                    .Where(o => o.Method == "Takeaway")
                    .Select(o => o.OrderNumber)
                    .Distinct();

                int uniqueTakeawayCount = takeawayOrderNumbers.Count();

                return takeawayCount + uniqueTakeawayCount;
            }
        }

        #endregion

        #region POSDashboard
        public ActionResult POSDashboard()
        {
            return View();
        }
        #endregion

        #region Waiter Menu
        public ActionResult WaiterMenu()
        {
            var categories = db.tblCategories.Include("TblProducts").ToList();
            ViewBag.Categories = categories;

            var products = db.tblProducts.ToList();
            return View(products);
        }
        #endregion

        #region Generate Order
        private string GenerateOrderNumber()
        {
            string orderNumber;

            do
            {
                orderNumber = new Random().Next(1, 1000).ToString("D3"); // Generate a new order number.

            } while (OrderNumberExists(orderNumber)); // Check if the generated order number already exists in the database.

            return orderNumber;
        }

        private bool OrderNumberExists(string orderNumber)
        {
            var existingOrder = db.TblInStoreOrders.FirstOrDefault(o => o.OrderNumber == orderNumber);
            return existingOrder != null;
        }
        [HttpPost]
        public ActionResult ConfirmInStore(List<SelectedProductModel> selectedProducts, string tableNumber, bool isDineIn, string paymentMethod)
        {
            if (selectedProducts == null || selectedProducts.Count == 0)
            {
                return RedirectToAction("Index");
            }


            string waiterName = Session["User"] as string;


            string payMethod = Request.Form["paymentOption"];

            DateTime orderDateTime = DateTime.Now;


            var ordersToInsert = new List<tblInStoreOrder>();


            var billsToInsert = new List<tblBill>();


            int totalAmount = (int)selectedProducts.Sum(product => product.Total);

            string orderNumber = GenerateOrderNumber();


            foreach (var product in selectedProducts)
            {
                string method = isDineIn ? "Dine-in" : "Takeaway";

                var order = new tblInStoreOrder
                {
                    OrderNumber = orderNumber,
                    OrderDateTime = orderDateTime,
                    WaiterName = waiterName,
                    ProductName = product.Product,
                    Unit = product.UnitPrice,
                    Qty = product.Quantity,
                    Total = product.TotalPrice,
                    Method = method,
                    PayMethod = isDineIn ? null : paymentMethod,
                    Status = "Preparing",
                    TableNumber = string.IsNullOrEmpty(tableNumber) ? "NONE" : tableNumber,
                    ReservedDate = null,
                    ReservedTime = null
                };


                db.TblInStoreOrders.Add(order);
                db.SaveChanges();


                int orderId = order.OrderId;


                var bill = new tblBill
                {
                    OrderId = orderId,
                    OrderNumber = order.OrderNumber,
                    OrderDateTime = order.OrderDateTime,
                    WaiterName = order.WaiterName,
                    ProductName = order.ProductName,
                    Unit = order.Unit,
                    Qty = order.Qty,
                    Total = order.Total,
                    TableNumber = order.TableNumber,
                    Method = order.Method,
                    PayMethod = order.PayMethod ?? "Pending"
                };


                db.TblBills.Add(bill);
                db.SaveChanges();
            }

            if (!isDineIn && paymentMethod == "Card")
            {

                string returnURL = "https://localhost:44377/Home/InStoreSuccess"; // Change this URL to match your actual URL

                // Redirect to PayPal with the calculated total amount and return URL
                return Content("<script>" +
                    "function callPayPal() {" +
                    "window.location.href = 'https://www.sandbox.paypal.com/cgi-bin/webscr?cmd=_xclick&amount=" + totalAmount.ToString() + "&business=sb-w3cyw20367505@business.example.com&item_name=FoodOrder&return=" + returnURL + "';" +
                    "}" +
                    "callPayPal();" +
                    "</script>");


            }
            if (isDineIn)
            {

                return RedirectToAction("InStoreSuccess");
            }
            else
            {
                var lastBill = db.TblBills.OrderByDescending(b => b.OrderDateTime).FirstOrDefault();


                if (lastBill != null)
                {

                    var viewModel = new InStoreSuccessCashViewModel
                    {
                        OrderNumber = lastBill.OrderNumber,
                        OrderDateTime = (DateTime)lastBill.OrderDateTime,
                        Products = db.TblBills
                            .Where(b => b.OrderNumber == lastBill.OrderNumber)
                            .Select(b => new SelectedProductModel
                            {
                                ProductName = b.ProductName,
                                Qty = b.Qty,
                                Total = b.Total
                            })
                            .ToList(),
                        TotalAmount = (int)db.TblBills
                            .Where(b => b.OrderNumber == lastBill.OrderNumber)
                            .Sum(b => b.Total)
                    };

                    return View("InStoreSuccessCash", viewModel);
                }
                else
                {

                    return RedirectToAction("Index");
                }
            }
        }
        #endregion

        #region Generate Bill
        public ActionResult GenerateBill(string orderNumber)
        {
            using (var dbContext = new dbOnlineStoreEntities())
            {
                // Retrieve all products with the given 'orderNumber'
                var products = dbContext.TblInStoreOrders
                    .Where(o => o.OrderNumber == orderNumber)
                    .Select(o => new SelectedProductModel
                    {
                        ProductName = o.ProductName,
                        Qty = o.Qty,
                        Total = o.Total
                    })
                    .ToList();

                // Calculate the total for all related order numbers
                var totalAmount = dbContext.TblInStoreOrders
                    .Where(o => o.OrderNumber == orderNumber)
                    .Sum(o => o.Total);

                var paymentMethod = dbContext.TblInStoreOrders
                    .Where(o => o.OrderNumber == orderNumber)
                    .Select(o => o.PayMethod)
                    .FirstOrDefault();

                var viewModel = new BillViewModel
                {
                    OrderNumber = orderNumber,
                    Products = products,
                    TotalAmount = (int)totalAmount,
                    PaymentMethod = paymentMethod // Corrected property name
                };
                // Create a view model and populate it with the product data and total amount
                return View("GenerateBill", viewModel);
            }
        }
        #endregion

        #region Track Reservation
        public ActionResult TrackReservations()
        {
            using (var db = new dbOnlineStoreEntities())
            {
                var reservations = db.tblReservations.ToList();
                return View(reservations);
            }
        }
        #endregion

        #region Generate Output
        [HttpPost]
        public ActionResult GenerateOutput(DateTime date)
        {
            using (var db = new dbOnlineStoreEntities())
            {
                // Retrieve reservations for the selected date
                ViewBag.SelectedDateReservations = db.tblReservations
                    .Where(r => r.Date == date.Date)
                    .ToList();

                // Return to the TrackReservations view
                return View("TrackReservations", db.tblReservations.ToList());
            }
        }
        #endregion

        #region Dine In
        public ActionResult DineIn()
        {
            // Assuming you have a DbContext named "db" and a DbSet for orders named "TblInStoreOrders"
            var dineInOrders = db.TblInStoreOrders.Where(order => order.Method == "Dine-in").ToList();

            return View(dineInOrders);
        }
        #endregion

        #region In Store
        public ActionResult InStoreSuccess()
        {
            var lastBill = db.TblBills.OrderByDescending(b => b.OrderDateTime).FirstOrDefault();

            // Check if a bill was found
            if (lastBill != null)
            {
                // Prepare the data to be sent to the view
                var viewModel = new InStoreSuccessCashViewModel
                {
                    OrderNumber = lastBill.OrderNumber,
                    OrderDateTime = (DateTime)lastBill.OrderDateTime,
                    Products = db.TblBills
                        .Where(b => b.OrderNumber == lastBill.OrderNumber)
                        .Select(b => new SelectedProductModel
                        {
                            ProductName = b.ProductName,
                            Qty = b.Qty,
                            Total = b.Total
                        })
                        .ToList(),
                    TotalAmount = (int)db.TblBills
                        .Where(b => b.OrderNumber == lastBill.OrderNumber)
                        .Sum(b => b.Total)
                };

                return View("InStoreSuccessCash", viewModel);
            }
            else
            {
                // Handle the case where no order is found (e.g., display an error message)
                return RedirectToAction("Index");
            }
        }


        public ActionResult InStoreSuccessCash()
        {
            return View();
        }
        #endregion

        #region Reserve Table
        public ActionResult ReserveTable()
        {
            var model = new tblReservation();
            model.SeatNumberList = GetSeatOptions();
            ViewBag.Submitted = false;

            return View(model);
        }
        [HttpPost]
        public ActionResult ReserveTable(tblReservation reservation)
        {
            tblReservation g = new tblReservation();
            // Check if the input fields are not blank
            if (string.IsNullOrWhiteSpace(reservation.Mail) ||
                string.IsNullOrWhiteSpace(reservation.Number) ||
                string.IsNullOrWhiteSpace(reservation.Seating))
            {
                ModelState.AddModelError("", "Please fill in all the required fields.");
            }
            else if (ModelState.IsValid)
            {
                // Check if the date is from Monday to Friday
                if (reservation.Date.HasValue && reservation.Date.Value.DayOfWeek >= DayOfWeek.Monday && reservation.Date.Value.DayOfWeek <= DayOfWeek.Friday)
                {
                    // Check if the time is between 9am and 3pm
                    if (reservation.Time.HasValue && reservation.Time.Value.TimeOfDay >= new TimeSpan(8, 0, 0) && reservation.Time.Value.TimeOfDay <= new TimeSpan(18, 0, 0))
                    {
                        using (var context = new dbOnlineStoreEntities())
                        {
                            // Check if the seat is available for the chosen date and time
                            int count = context.tblReservations
                                .Where(r => r.Date == reservation.Date && r.Time == reservation.Time)
                                .Count();
                            if (count >= 2)
                            {
                                ModelState.AddModelError("", "Sorry, this seat is already booked twice for the chosen date and time.");
                                // Re-populate the SeatNumberList dropdown with the updated model
                                reservation.SeatNumberList = GetSeatOptions();
                                ViewBag.Submitted = false;
                                return View(reservation);
                            }

                            // Check if the seat is available for the chosen date and has not been booked more than twice
                            count = context.tblReservations
                                .Where(r => r.Date == reservation.Date && r.Seating == reservation.Seating)
                                .Count();
                            if (count >= 2)
                            {
                                ModelState.AddModelError("", "Sorry, this seating option is already booked twice for the chosen date.");
                                // Re-populate the SeatNumberList dropdown with the updated model
                                reservation.SeatNumberList = GetSeatOptions();
                                ViewBag.Submitted = false;
                                return View(reservation);
                            }
                            g.BookingId = reservation.BookingId;
                            g.Customer_Name = reservation.Customer_Name;
                            g.Mail = reservation.Mail;
                            g.Number = reservation.Number;
                            g.Seating = reservation.Seating;
                            g.Date = reservation.Date;
                            g.Time = reservation.Time;
                            context.tblReservations.Add(reservation);
                            context.SaveChanges();
                            var body = $"Dear {reservation.Customer_Name},<br /><br />Your reservation was successful. Seating for {reservation.Seating} people is reserved for you on this date {(reservation.Date.HasValue ? reservation.Date.Value.ToShortDateString() : string.Empty)} and time {(reservation.Time.HasValue ? reservation.Time.Value.ToString("hh:mm tt") : "")},<br><br>Your Booking ID is {reservation.BookingId}. We hope you enjoy our services at Turbo Meals" +
                                $" If you have any queries, drop us an email (turbomeals123@gmail.com)";
                            var message = new MailMessage();
                            message.To.Add(new MailAddress(reservation.Mail));
                            message.From = new MailAddress("turbomeals123@gmail.com"); // Replace with your email address
                            message.Subject = "Reservation Confirmation";
                            message.Body = string.Format(body);
                            message.IsBodyHtml = true;


                            using (var smtp = new SmtpClient())
                            {
                                smtp.Send(message);
                            }
                        }
                        TempData["Submitted"] = true;

                        return RedirectToAction("Reserve_SuccessCard");
                    }
                    else
                    {
                        ModelState.AddModelError("", "Invalid time selection. Reservations are only available between 8am and 6pm.");
                    }
                }
                else
                {
                    ModelState.AddModelError("", "Invalid date selection. Reservations are only available from Monday to Friday.");
                }
            }

            // Re-populate the SeatNumberList dropdown with the updated model
            reservation.SeatNumberList = GetSeatOptions();
            ViewBag.Submitted = false;
            return View(reservation);
        }
        #endregion


        #region Staff In Store Order 
        public ActionResult ReadyOrders()
        {
            var readyOrders = db.TblInStoreOrders.Where(m => m.Status == "Ready").ToList();
            return View(readyOrders);
        }

        public ActionResult PrepInStoreOrders()
        {
            var orders = db.TblInStoreOrders.Where(m => m.Status == "Preparing").ToList();
            return View(orders);
        }

        public ActionResult PrepInStoreOrder(int OrderId)
        {

            var selectedOrder = db.TblInStoreOrders.FirstOrDefault(o => o.OrderId == OrderId);

            if (selectedOrder == null)
            {

                return RedirectToAction("PrepInStoreOrders");
            }

            var ordersWithSameOrderNumber = db.TblInStoreOrders.Where(o => o.OrderNumber == selectedOrder.OrderNumber).ToList();

            return View(ordersWithSameOrderNumber);
        }

        [HttpPost]
        public ActionResult MarkAsReady(int OrderId)
        {

            var order = db.TblInStoreOrders.Find(OrderId);

            if (order != null)
            {

                var relatedOrders = db.TblInStoreOrders.Where(o => o.OrderNumber == order.OrderNumber);
                foreach (var relatedOrder in relatedOrders)
                {
                    relatedOrder.Status = "Ready";
                }
                
                db.SaveChanges();
            }

            return RedirectToAction("PrepInStoreOrders");
        }
        #endregion

        #region Update Payment Method
        [HttpPost]
        public ActionResult UpdatePaymentMethod(string orderNumber, string paymentMethod)
        {
            using (var dbContext = new dbOnlineStoreEntities())
            {
                // Find all bills and orders with the given order number
                var bills = dbContext.TblBills.Where(b => b.OrderNumber == orderNumber).ToList();
                var orders = dbContext.TblInStoreOrders.Where(o => o.OrderNumber == orderNumber).ToList();

                if (bills.Count > 0 && orders.Count > 0)
                {
                    // Update the payment method for all bills and orders with the same order number
                    foreach (var bill in bills)
                    {
                        bill.PayMethod = paymentMethod;
                    }

                    foreach (var order in orders)
                    {
                        order.PayMethod = paymentMethod;
                    }

                    // Save changes to the database
                    dbContext.SaveChanges();

                    if (paymentMethod.Equals("Card", StringComparison.OrdinalIgnoreCase))
                    {
                        string returnURL = "https://localhost:44377/Home/POSDashboard"; // Change this URL to match your actual URL

                        // Redirect to PayPal with the calculated total amount and return URL
                        return Content("<script>" +
                            "function callPayPal() {" +
                            "window.location.href = 'https://www.sandbox.paypal.com/cgi-bin/webscr?cmd=_xclick&amount=" + "&business=sb-w3cyw20367505@business.example.com&item_name=FoodOrder&return=" + returnURL + "';" +
                            "}" +
                            "callPayPal();" +
                            "</script>");

                    }
                }
            }

            return RedirectToAction("GenerateBill", new { orderNumber });
        }
        #endregion

        #region Complete Order
        public ActionResult CompleteOrder(string orderNumber)
        {
            using (var dbContext = new dbOnlineStoreEntities())
            {
                // Find and delete all occurrences of the order number in the bill table
                var billsToDelete = dbContext.TblBills.Where(b => b.OrderNumber == orderNumber).ToList();
                dbContext.TblBills.RemoveRange(billsToDelete);

                // Find and delete all occurrences of the order number in the order table
                var ordersToDelete = dbContext.TblInStoreOrders.Where(o => o.OrderNumber == orderNumber).ToList();
                dbContext.TblInStoreOrders.RemoveRange(ordersToDelete);

                // Save changes to the database
                dbContext.SaveChanges();
            }

            // Redirect back to the Dine-In page
            return RedirectToAction("DineIn");
        }
        #endregion

        #region Reservation
        public ActionResult Reservations()
        {
            return View();
        }

        public ActionResult Reserve_SuccessCard()
        {
            return View();
        }

        public ActionResult ReserveSuccess_Cash()
        {
            return View();
        }


       

        public ActionResult CheckoutReservation(int reservationId)
        {
            using (var context = new dbOnlineStoreEntities())
            {
                var reservation = context.tblReservations.FirstOrDefault(r => r.BookingId == reservationId);
                if (reservation != null)
                {
                    return View(reservation);
                }
            }

            return RedirectToAction("MakeReservation", "Home");
        }

        public ActionResult GetAllReservations()  //v changes
        {
            List<tblReservation> reservations;

            using (var context = new dbOnlineStoreEntities())
            {
                reservations = context.tblReservations.ToList();
            }

            return View(reservations);
        }


        public ActionResult MakeReservation()  //V changes
        {
            var model = new tblReservation();
            model.SeatNumberList = GetSeatOptions();
            ViewBag.Submitted = false;

            return View(model);
        }


        public ActionResult CheckInCustomer()
        {
            return View();
        }
        public ActionResult CheckIn()
        {
            return View();
        }

        [HttpPost]
        public ActionResult VerifyBooking(string bookingId)
        {
            using (var context = new dbOnlineStoreEntities())
            {
                int convertedBookingId = int.Parse(bookingId);
                var reservation = context.tblReservations.FirstOrDefault(r => r.BookingId == convertedBookingId);
                if (reservation != null)
                {
                    ViewBag.SuccessMessage = "Checked in successfully!";
                    ViewBag.QRCode = GenerateQRCode(convertedBookingId.ToString());


                    // Pass the reservation to the view
                    ViewBag.Reservation = reservation;

                    return View("CheckinCustomers"); // Assuming your view name is "CheckInCustomer"
                }
                else
                {
                    ViewBag.ErrorMessage = "Invalid booking ID. Please try again.";
                }
            }

            return View("CheckIn");
        }
        private string GenerateQRCode(string bookingId)
        {
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(bookingId, QRCodeGenerator.ECCLevel.Q);
            QRCode qrCode = new QRCode(qrCodeData);
            Bitmap qrCodeImage = qrCode.GetGraphic(10); // Adjust the size of the QR code as needed

            // Resize the QR code image
            Bitmap resizedImage = new Bitmap(qrCodeImage, new Size(200, 200)); // Adjust the size as needed

            // Convert the resized image to a base64 string
            using (MemoryStream stream = new MemoryStream())
            {
                resizedImage.Save(stream, ImageFormat.Png);
                byte[] qrCodeBytes = stream.ToArray();
                string base64Image = Convert.ToBase64String(qrCodeBytes);
                return "data:image/png;base64," + base64Image;
            }
        }

        [HttpPost]
        public ActionResult MakeReservation(tblReservation reservation)  //v changes 
        {

            tblReservation g = new tblReservation();
            // Check if the input fields are not blank
            if (string.IsNullOrWhiteSpace(reservation.Mail) ||
                string.IsNullOrWhiteSpace(reservation.Number) ||
                string.IsNullOrWhiteSpace(reservation.Seating))
            {
                ModelState.AddModelError("", "Please fill in all the required fields.");
            }
            else if (ModelState.IsValid)
            {
                // Check if the date is from Monday to Friday
                if (reservation.Date.HasValue && reservation.Date.Value.DayOfWeek >= DayOfWeek.Monday && reservation.Date.Value.DayOfWeek <= DayOfWeek.Friday)
                {
                    // Check if the time is between 9am and 3pm
                    if (reservation.Time.HasValue && reservation.Time.Value.TimeOfDay >= new TimeSpan(8, 0, 0) && reservation.Time.Value.TimeOfDay <= new TimeSpan(18, 0, 0))
                    {
                        using (var context = new dbOnlineStoreEntities())
                        {
                            // Check if the seat is available for the chosen date and time
                            int count = context.tblReservations
                                .Where(r => r.Date == reservation.Date && r.Time == reservation.Time)
                                .Count();
                            if (count >= 2)
                            {
                                ModelState.AddModelError("", "Sorry, this seat is already booked twice for the chosen date and time.");
                                // Re-populate the SeatNumberList dropdown with the updated model
                                reservation.SeatNumberList = GetSeatOptions();
                                ViewBag.Submitted = false;
                                return View(reservation);
                            }

                            // Check if the seat is available for the chosen date and has not been booked more than twice
                            count = context.tblReservations
                                .Where(r => r.Date == reservation.Date && r.Seating == reservation.Seating)
                                .Count();
                            if (count >= 2)
                            {
                                ModelState.AddModelError("", "Sorry, this seating option is already booked twice for the chosen date.");
                                // Re-populate the SeatNumberList dropdown with the updated model
                                reservation.SeatNumberList = GetSeatOptions();
                                ViewBag.Submitted = false;
                                return View(reservation);
                            }
                            g.BookingId = reservation.BookingId;
                            g.Customer_Name = reservation.Customer_Name;
                            g.Mail = reservation.Mail;
                            g.Number = reservation.Number;
                            g.Seating = reservation.Seating;
                            g.Date = reservation.Date;
                            g.Time = reservation.Time;
                            context.tblReservations.Add(reservation);
                            context.SaveChanges();
                            var body = $"Dear {reservation.Customer_Name},<br /><br />Your reservation was successful. Seating for {reservation.Seating} people is reserved for you on this date {(reservation.Date.HasValue ? reservation.Date.Value.ToShortDateString() : string.Empty)} and time {(reservation.Time.HasValue ? reservation.Time.Value.ToString("hh:mm tt") : "")},<br><br>Your Booking ID is {reservation.BookingId}. We hope you enjoy our services at Turbo Meals" +
                                $" If you have any queries, drop us an email (turbomeals123@gmail.com)";
                            var message = new MailMessage();
                            message.To.Add(new MailAddress(reservation.Mail));
                            message.From = new MailAddress("turbomeals123@gmail.com"); // Replace with your email address
                            message.Subject = "Reservation Confirmation";
                            message.Body = string.Format(body);
                            message.IsBodyHtml = true;


                            using (var smtp = new SmtpClient())
                            {
                                smtp.Send(message);
                            }
                        }
                        TempData["Submitted"] = true;

                        return RedirectToAction("CheckoutReservation", "Home", new { reservationId = reservation.BookingId });
                    }
                    else
                    {
                        ModelState.AddModelError("", "Invalid time selection. Reservations are only available between 8am and 6pm.");
                    }
                }
                else
                {
                    ModelState.AddModelError("", "Invalid date selection. Reservations are only available from Monday to Friday.");
                }
            }

            // Re-populate the SeatNumberList dropdown with the updated model
            reservation.SeatNumberList = GetSeatOptions();
            ViewBag.Submitted = false;
            return View(reservation);
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
        #endregion

    }
}