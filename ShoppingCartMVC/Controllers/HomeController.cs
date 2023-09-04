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
            TempData.Keep();

            var query = db.tblProducts.ToList();
            return View(query);
        }

        #endregion

        #region Add To Cart

        public ActionResult AddtoCart(int id)
        {
          
            var query = db.tblProducts.Where(x => x.ProID == id).SingleOrDefault();
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

            return View(query);
        }

       

        [HttpPost]
        public ActionResult AddtoCart(int id, int qty, string[] selectedExtras)
        {
            tblProduct p = db.tblProducts.Where(x => x.ProID == id).SingleOrDefault();

            //if (qty > p.Qty)
            //{
            //    ModelState.AddModelError("", "The selected quantity exceeds the available quantity.");
            //    return View(p);
            //}

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
            TempData.Keep();
            return View();
        }

        [HttpPost]
        public ActionResult Checkout(string contact, string address, string Method, string PayMethod) //v changes
        {
            if (ModelState.IsValid)
            {
                List<Cart> li2 = TempData["cart"] as List<Cart>;
                tblInvoice iv = new tblInvoice();
                iv.UserId = Convert.ToInt32(Session["uid"].ToString());
                iv.InvoiceDate = System.DateTime.Now;
                iv.Bill = (int)TempData["total"];
                iv.Payment = PayMethod;
                iv.DC_Method = Method;

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


                    //var product = db.tblProducts.SingleOrDefault(p => p.ProID == item.proid);
                    //if (product != null)
                    //{
                    //    product.Qty -= item.qty;

                    //}

                    //var proTable = db.tblProducts.ToList();

                    //foreach (var record in proTable)
                    //{
                    //    if ((record.Qty < 50) && (record.StockStatus == "In Stock"))
                    //    {
                    //        record.StockStatus = "Low Stock";
                    //        //SendQuantityAlertEmail(product);
                    //    }
                    //    db.Entry(record).State = EntityState.Modified;
                    //}

                    db.SaveChanges();
                }

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
                        "window.location.href = 'https://www.sandbox.paypal.com/cgi-bin/webscr?cmd=_xclick&amount=" + iv.Bill.ToString() + "&business=sb-w3cyw20367505@business.example.com&item_name=FoodOrder&return=https://2023grp01a.azurewebsites.net/Home/Success';" +
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
            var query = db.tblOrders.Where(m => m.TblInvoice.UserId == id).ToList();
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
            return View(query);
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
                               o.Extras,
                               o.ExtrasCost,
                               o.Method,
                               i.Time_CD,
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
                    objVM.Extras = pro.Extras;
                    objVM.ExtrasCost = pro.ExtrasCost;
                    objVM.Method = pro.Method;
                    objVM.CD_Time = pro.Time_CD;
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