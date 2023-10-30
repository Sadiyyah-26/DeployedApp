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
using System.Windows;
using System.Net;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using System.Threading.Tasks;


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

                //changes final
                int? cartTotal = 0;

                if (Method == "Delivery")
                {
                    cartTotal = (int)TempData["total"] + 30;
                }
                else
                {
                    cartTotal = (int)TempData["total"];
                }

                tblInvoice iv = new tblInvoice();
                iv.UserId = Convert.ToInt32(Session["uid"].ToString());
                iv.InvoiceDate = System.DateTime.Now;
                //iv.Bill = (int)TempData["total"];

                if (RedeemPoints == "No" || RedeemPoints == null) { iv.Bill = cartTotal; }
                else if (RedeemPoints == "Yes")
                {
                    iv.Bill = Convert.ToInt32(cartTotal * 0.5);

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
                        item.PointBalance += (double)cartTotal * 0.05;
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
                        item.PointBalance += (double)cartTotal * 0.05;
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
                var body = $"<html><body>" +
             $"<p>Dear {userName},</p><br /><br />" +
             $"<p>Your order was placed successfully.</p><br /><br />" +
             $"<p>You will be notified when your order is ready.</p><br /><br />";

                if (iv.Payment == "Cash")
                {
                    body += $"<p>Please note that your payment is pending. Kindly complete the payment when you receive your order.</p><br /><br />";
                }
                else if (iv.Payment == "PayPal")
                {
                    body += $"<p>Your payment has been successfully processed.</p><br /><br />";
                }

                body += $"<p><strong>Order Details:</strong></p>";

                foreach (var item in li2)
                {
                    tblProduct product = db.tblProducts.Find(item.proid);
                    string productName = product.P_Name;
                    int quantity = item.qty;
                    int Price = (int)product.Unit;
                    int bill = item.bill;

                    body += $"<p>Item: {productName}</p>" +
                            $"<p>Quantity: {quantity}</p>" +
                            $"<p>Unit Price: R{Price}.00</p>" +
                            $"<p>Total Bill: R{bill}.00</p><br /><br />";
                }

                body += $"</body></html>";

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
            var query = db.tblOrders.Where(m => m.OrderReady == "Ready")
                .GroupBy(m => m.InvoiceId)
                .Select(m => m.FirstOrDefault()).ToList();
            return View(query);
        }

        #endregion

        #region  Confirm Order by Admin

        public ActionResult ConfirmOrder(int? InvoiceId)
        {
            TempData["orderNum"] = InvoiceId;
            var query = db.tblOrders.Where(m => m.InvoiceId == InvoiceId)
                .GroupBy(m => m.InvoiceId).ToList();
            return View(query);
        }
        [HttpPost]
        public ActionResult ConfirmOrder(string Status)
        {
            int id = (int)TempData["orderNum"];
            tblInvoice tblInvoice = db.tblInvoices.Find(id);
            tblInvoice.Status = Status;

            if (Status == "Order Collected")
            {
                tblInvoice.Payment_Status = "Paid";
                tblInvoice.Time_CD = DateTime.Now;

                // Get the existing cash float for the current date
                DateTime date = DateTime.Now.Date;
                var existingCashFloat = db.tblCashFloats
                    .Where(c => c.Date == date)
                    .FirstOrDefault();

                if (existingCashFloat != null)
                {
                    // Check if the Payment field for the order is "Cash"
                    if (tblInvoice.Payment == "Cash")
                    {
                        Transactions transaction = new Transactions
                        {
                            FloatID = existingCashFloat.FloatID, // Get the existing cash float for the day
                            Transaction = "Collection",
                            InStoreOrderID = null, // Set as needed
                            OnlineOrderID = id, // Set the OnlineOrderID as the invoice ID
                            TransactionTime = DateTime.Now,
                            UserID = Convert.ToInt16(Session["uid"]), // Replace with the actual User ID
                            UserName = Session["User"] as string, // Replace with the actual username
                            Current = existingCashFloat.Amount, // Use the existing cash float amount
                            Credit = (int)tblInvoice.Bill, // Set credit as the bill amount
                            GivenAmt = 0,
                            Debit = 0,
                            ClosingBalance = existingCashFloat.Amount + (int)tblInvoice.Bill
                        };

                        db.tblTransactions.Add(transaction);
                        existingCashFloat.Amount = existingCashFloat.Amount + (int)tblInvoice.Bill;
                    }
                }
            }

            db.Entry(tblInvoice).State = EntityState.Modified;
            db.SaveChanges();

            tblOrder tblOrder = db.tblOrders.Find(id);
            string oId = id.ToString();
            string toEmail = tblInvoice.TblUser.Email;
            string name = tblInvoice.TblUser.Name;

            var body = "Dear " + name + ",<br><br>" +
            "Your Order with Invoice #" + oId + " has been collected from Turbo Meals.<br><br>Payment Status: Paid.<br><br>Thank you for choosing Turbo Meals!";

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
            var query = db.tblDrivers.Where(m => m.UserId == id)
                .GroupBy(m => m.TblOrder.InvoiceId)
                .Select(m => m.FirstOrDefault()).ToList();
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

        public ActionResult AssignDriver(int? InvId, tblOrder o)
        {
            var drivers = db.tblUsers
                .Where(u => u.RoleType == 3)
                .Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.UserId.ToString()
                })
                .ToList();

            var viewModel = new AssignDriverVM
            {
                DriverSelectList = drivers,
                InvoiceId = InvId
            };

           
            var firstMatchingOrder = db.tblOrders
                .Where(t => t.InvoiceId == InvId)
                .FirstOrDefault();

            if (firstMatchingOrder != null)
            {
                viewModel.OrderDate = firstMatchingOrder.OrderDate;
                viewModel.Address = firstMatchingOrder.Address;
            }

            viewModel.SelectedUserId = int.Parse(drivers.FirstOrDefault()?.Value);

            return View(viewModel);
        }

        [HttpPost]
        public ActionResult AssignDriver(int? InvId, AssignDriverVM vm)
        {
            if (ModelState.IsValid)
            {
                
                int selectedDriverId = vm.SelectedUserId;

                string selectedName = "";

                if (vm.SelectedUserId != 0) 
                {
                    int selectedUserId = vm.SelectedUserId; // Get the selected driver's ID

                    
                    var selectedDriver = db.tblUsers.SingleOrDefault(u => u.UserId == selectedUserId);

                    if (selectedDriver != null)
                    {
                        selectedName = selectedDriver.Name; // Get the name of the selected driver
                    }
                }

               // Retrieve all order records with the same InvoiceId
                var ordersWithSameInvoice = db.tblOrders
                    .Where(o => o.InvoiceId == InvId)
                    .ToList();

                foreach (var order in ordersWithSameInvoice)
                {
                    // Create a new driver record and associate it with the order
                    var driver = new Drivers
                    {
                        UserId = selectedDriverId,
                        OrderId = order.OrderId,
                        DriverName = selectedName, 
                        OrderDate = (DateTime)order.OrderDate, 
                        DeliveryAddress = order.Address 
                    };

                    db.tblDrivers.Add(driver);

                   
                    order.TblInvoice.Status = "Out for Delivery";
                }

               
                db.SaveChanges();

                
                return RedirectToAction("GetAllOrderDetail");
            }
            return View(vm);
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
        public ActionResult DeliveryDetails(int InvoiceID)
        {
            TempData["oId"] = InvoiceID;
            var driverOrdersSameInv = db.tblDrivers
       .Where(driver => driver.TblOrder.InvoiceId == InvoiceID)
       .ToList();


            var groupedOrders = driverOrdersSameInv
                .GroupBy(driver => driver.TblOrder.InvoiceId)
                .ToList();

            return View(groupedOrders);
        }

        [HttpPost]
        public ActionResult DeliveryDetails(Drivers drivers, bool CashPaid)
        {

            int invoiceId = (int)TempData["oId"];
            var qry = db.tblOrders.Where(m => m.InvoiceId == invoiceId).ToList();

            string payment = "";
            string userEmail = "";
            string userName = "";
            string userAddress = "";
            if (qry.Any())
            {
                var firstOrder = qry.First();

                userEmail = firstOrder.TblInvoice.TblUser.Email;
                userName = firstOrder.TblInvoice.TblUser.Name;
                userAddress = firstOrder.Address;
                foreach (var i in qry)
                {
                    i.TblInvoice.Status = "Order Delivered";
                    i.TblInvoice.Time_CD = System.DateTime.Now;

                    if (i.TblInvoice.Payment_Status == "Pending")
                    {
                        if (CashPaid)
                        {
                            i.TblInvoice.Payment_Status = "Paid";
                        }
                        else
                        {
                            i.TblInvoice.Payment_Status = "NPaid";
                        }

                        payment = "Please note, your Payment Status has now been updated to Paid.<br><br>";
                    }
                    payment = "Payment Status: Paid<br><br>";
                }

                db.SaveChanges();


                DateTime date = DateTime.Now.Date;
                var existingCashFloat = db.tblCashFloats
                    .Where(c => c.Date == date)
                    .FirstOrDefault();



                if (existingCashFloat != null)
                {
                    // Get the existing float amount for the "current" balance
                    int currentBalance = existingCashFloat.Amount;

                    // Calculate the closing balance by adding credit (bill) to the current balance
                    int closingBalance = currentBalance + (int)firstOrder.TblInvoice.Bill;

                    // Create a new transaction record for delivery
                    Transactions transaction = new Transactions
                    {
                        FloatID = existingCashFloat.FloatID, // Get the existing cash float for the day
                        Transaction = "Delivery",
                        InStoreOrderID = null, // Set as needed
                        OnlineOrderID = invoiceId, // Set the OnlineOrderID as the invoice ID
                        TransactionTime = DateTime.Now,
                        UserID = Convert.ToInt16(Session["uid"]), // Replace with the actual User ID
                        UserName = userName, // Use the user's name from the order
                        Current = currentBalance, // Use the current balance
                        Credit = (int)firstOrder.TblInvoice.Bill, // Set credit as the bill amount
                        GivenAmt = 0,
                        Debit = 0,
                        ClosingBalance = closingBalance
                    };

                    db.tblTransactions.Add(transaction);

                    // Update the existing cash float's amount to the closing balance
                    existingCashFloat.Amount = closingBalance;
                }

            }
            db.SaveChanges();
            string oId = invoiceId.ToString();
            string toEmail = userEmail;
            string name = userName;
            string address = userAddress;


            string baseUrl = $"{Request.Url.Scheme}://{Request.Url.Authority}";
            string url = Url.Action("RateAndTip", "Account", new { InvoiceId = TempData["oId"] });
            string link = $"{baseUrl}{url}";


            var body = "Dear " + name + ",<br><br>" +
                "Your Order with Invoice #" + oId + " has been successfully delivered to you at " + address + ".<br><br>" + payment +
                "Thank you for choosing Turbo Meals!<br>" +
                "Click here to rate and tip your driver: " + link;


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

            return RedirectToAction("DeliveryDetails", new { InvoiceID = invoiceId });

        }
        #endregion


        #region Admin Notify Customer for Collection

        public ActionResult NotifyCustomer(int InvoiceID)
        {
            TempData["order"] = InvoiceID;
            var query = db.tblOrders.Where(m => m.InvoiceId == InvoiceID)
                .GroupBy(m => m.InvoiceId).ToList();
            return View(query);
        }

        [HttpPost]
        public ActionResult NotifyCustomer(tblOrder o, bool orderReady)
        {
            int orderId = (int)TempData["order"];
            
            tblInvoice tblInvoice = db.tblInvoices.Find(orderId);
            tblInvoice.Status = "Ready for Collection";

            if (orderReady)
            {
                string oId = orderId.ToString();
                string toEmail = tblInvoice.TblUser.Email;
                string name = tblInvoice.TblUser.Name;

                var body = "Dear " + name + ",<br><br>" +
               "I'm contacting you on behalf of Turbo Meals, and I'm glad to inform you that your Order with invoice number #" + orderId +
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
        public ActionResult Map(int InvoiceID)
        {
            var firstDriverOrder = db.tblDrivers
        .Where(driver => driver.TblOrder.InvoiceId == InvoiceID)
        .FirstOrDefault();

            
            if (firstDriverOrder != null)
            {               
                return View(firstDriverOrder);
            }
            return View();         
        }
        #endregion

        #region Sending Delivery Notification
        public ActionResult SendMail(int InvoiceId)
        {
            TempData["oId2"] = InvoiceId;
            var query = db.tblDrivers.Where(m => m.TblOrder.InvoiceId == InvoiceId).FirstOrDefault();
            return View(query);
        }

        [HttpPost]
        public ActionResult SendMail(Drivers dri, string delTime, string licenseNum)
        {
            int orderId = (int)TempData["oId2"];
            tblOrder tblOrder = db.tblOrders.Find(orderId);
           
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


            return RedirectToAction("DeliveryDetails", new { InvoiceID = orderId });
        }
        #endregion

        #region Driver Collects Order
        public ActionResult DriverGetOrder(int InvoiceID)
        {
            TempData["oId"] = InvoiceID;

            var driverOrdersSameInv = db.tblDrivers
        .Where(driver => driver.TblOrder.InvoiceId == InvoiceID)
        .ToList();


            var groupedOrders = driverOrdersSameInv
                .GroupBy(driver => driver.TblOrder.InvoiceId)
                .ToList();
           
            return View(groupedOrders);
        }

        [HttpPost]
        public ActionResult DriverGetOrder(bool OrderCollected)
        {
            int invID = (int)TempData["oId"];

            if (OrderCollected)
            {
                return RedirectToAction("Map", new { InvoiceID = invID });
            }
            return View();
        }

        #endregion

        #region New Orders Go to Staff for Prep
        public ActionResult PrepStaff()
        {
            var query = db.tblOrders.Where(m => m.OrderReady == "Being Prepared").ToList();
            return View(query);

        }

        #endregion

        #region Staff Makes Order Ready
        [HttpPost]
        public ActionResult PrepOrder(int InvoiceId)
        {
            var Order = db.tblOrders.Where(m => m.InvoiceId == InvoiceId);
            if (Order != null)
            {

                var relatedOrders = db.tblOrders.Where(o => o.InvoiceId == InvoiceId).ToList();
                foreach (var rec in relatedOrders)
                {
                    rec.OrderReady = "Ready";

                    //update stock qty
                    var ingPro = db.IngredientProducts.Where(ip => ip.ProID == rec.ProID).ToList();

                    foreach (var i in ingPro)
                    {
                        var ingID = i.Ing_ID;

                        var ingr = db.tblIngredients.SingleOrDefault(m => m.Ing_ID == ingID);
                      

                        decimal qtyToReduce = 0;
                        if (ingr != null)
                        {
                           

                            qtyToReduce = (decimal)(i.Ing_QtyPerPro * ingr.Ing_StandardQty * rec.Qty);
                            ingr.Ing_StockyQty -= qtyToReduce;

                            if ((ingr.Ing_StockyQty < 50) && (ingr.StockStatus == "In Stock"))
                            {
                                ingr.StockStatus = "Low Stock";
                                //send qty alert email 
                                string ing = ingr.Ing_Name;
                                string qty = Convert.ToString(ingr.Ing_StockyQty);                               

                                string baseUrl = $"{Request.Url.Scheme}://{Request.Url.Authority}";
                                string url = Url.Action("LowStock", "Ingredients");
                                string link = $"{baseUrl}{url}";

                                var content = $"The Stock Quantity for the following Ingredient has dropped below 50 and requires restocking.<br/><br/> ";
                                content += "Ingredient: " + ing + " current quantity is " + qty + "<br/>Click here to view low stock ingredients: <a href=" + link + ">Low Stock</a>"; ;



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


                }

                db.SaveChanges();
            }
            db.SaveChanges();
            return RedirectToAction("PrepStaff", "Home", new { id = @Session["uid"] });
        }
        #endregion

        #region Customer Cancels Order
        public ActionResult CancelOrder(int? InvoiceId)
        {
            TempData["o"] = InvoiceId;
            var query = db.tblOrders.Where(m => m.InvoiceId == InvoiceId)
                .GroupBy(m => m.InvoiceId).ToList();
            return View(query);
        }

        [HttpPost]
        public ActionResult CancelOrder()
        {
            int invID = (int)TempData["o"];
            tblInvoice tblInvoice = db.tblInvoices.Find(invID);
            tblInvoice.Status = "Cancelled";

            string oId = invID.ToString();
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
           "Your Order with Invoice #" + invID + " has successfully been cancelled as per your request.<br><br>" +
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


        public ActionResult SuccessBill()
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
            int deliveryInvoiceCount = GetDeliveryCount();
            decimal totalInStore = GetTotalInStoreSalesAmount();
            decimal totalOnline = GetTotalOnlineSalesAmount();
            int takeawayCount = GetTakeawayCount();
            int driverCount = GetDriverCountFromDatabase();
            int roleType5Count = GetRoleType5CountFromDatabase();
            int roleType4Count = GetRoleType4CountFromDatabase();
            int roleType1Count = GetRoleType1CountFromDatabase();
            int roleType2Count = GetRoleType2CountFromDatabase();
            int supplierCount = GetSupplierCountFromDatabase();
            decimal totalDonationAmount = GetTotalDonationAmount();
            decimal totalOrderAmount = GetTotalOrderAmount();
            decimal netTotalSales = GetNetTotalSalesAmount();
            int reservationCount = GetReservationCount();


            ViewBag.UserCount = userCount;
            ViewBag.OrderCount = orderCount;
            ViewBag.POSCount = posCount;
            ViewBag.ProductCount = productCount;
            ViewBag.TotalSales = totalSales;
            ViewBag.TotalInStore = totalInStore;
            ViewBag.TotalOnline = totalOnline;
            ViewBag.DeliveryInvoiceCount = deliveryInvoiceCount;
            ViewBag.TakeawayCount = takeawayCount;
            ViewBag.DriverCount =driverCount;
            ViewBag.RoleType5Count = roleType5Count;
            ViewBag.RoleType4Count = roleType4Count;
            ViewBag.RoleType1Count = roleType1Count;
            ViewBag.RoleType2Count = roleType2Count;
            ViewBag.SupplierInvoice = supplierCount;
            ViewBag.Donations = totalDonationAmount;
            ViewBag.TotalOrder = totalOrderAmount;
            ViewBag.Profit = netTotalSales;
            ViewBag.Reservation = reservationCount;


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

        public int GetDeliveryCount()
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
                // Count all orders with DC_Method as "Collection" from tblInvoice
                int takeawayCount = context.tblInvoices
                    .Count(i => i.DC_Method == "Collection");

                // Count all Takeaway orders from InstoreOrders
                int instoreTakeawayCount = context.TblInStoreOrders
                    .Count(o => o.Method == "Takeaway");

                return takeawayCount + instoreTakeawayCount;
            }
        }

        public int GetDriverCountFromDatabase()
        {
            using (var context = new dbOnlineStoreEntities())
            {
                int driverCount = context.tblUsers.Where(u => u.RoleType == 3).Count();
                return driverCount;
            }
        }


        public int GetRoleType5CountFromDatabase()
        {
            using (var context = new dbOnlineStoreEntities())
            {
                int roleType5Count = context.tblUsers.Where(u => u.RoleType == 5).Count();
                return roleType5Count;
            }
        }


        public int GetRoleType4CountFromDatabase()
        {
            using (var context = new dbOnlineStoreEntities())
            {
                int roleType4Count = context.tblUsers.Where(u => u.RoleType == 4).Count();
                return roleType4Count;
            }
        }

        public int GetRoleType1CountFromDatabase()
        {
            using (var context = new dbOnlineStoreEntities())
            {
                int roleType1Count = context.tblUsers.Where(u => u.RoleType == 1).Count();
                return roleType1Count;
            }
        }

        public int GetRoleType2CountFromDatabase()
        {
            using (var context = new dbOnlineStoreEntities())
            {
                int roleType2Count = context.tblUsers.Where(u => u.RoleType == 2).Count();
                return roleType2Count;
            }
        }

        public int GetSupplierCountFromDatabase()
        {
            using (var context = new dbOnlineStoreEntities()) // Replace YourDbContext with the actual name of your DbContext class
            {
                int supplierCount = context.tblSuppliers.Count();
                return supplierCount;
            }
        }

        public decimal GetTotalDonationAmount()
        {
            using (var context = new dbOnlineStoreEntities()) // Replace YourDbContext with your actual DbContext class
            {
                decimal totalDonationAmount = context.tblDonations.Sum(d => d.DonationAmount);
                return totalDonationAmount;
            }
        }

        public decimal GetTotalOrderAmount()
        {
            using (var context = new dbOnlineStoreEntities()) // Replace YourDbContext with your actual DbContext class
            {
                decimal totalOrderAmount = context.tblAdminOrders
                    .Where(o => o.Total.HasValue)
                    .Sum(o => o.Total.Value);

                return totalOrderAmount;
            }
        }


        public decimal GetNetTotalSalesAmount()
        {
            decimal totalSales = GetTotalSalesAmount();
            decimal totalStockExpenses = GetTotalOrderAmount();

            decimal netTotalSales = totalSales - totalStockExpenses;

            return netTotalSales;
        }

        public int GetReservationCount()
        {
            using (var context = new dbOnlineStoreEntities()) // Replace with your actual DbContext class
            {
                int reservationCount = context.tblReservations.Count();

                return reservationCount;
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
        public ActionResult ConfirmInStore(List<SelectedProductModel> selectedProducts, string tableNumber, bool isDineIn, string paymentMethod, int? changeDue)
        {
            if (selectedProducts == null || selectedProducts.Count == 0)
            {
                return RedirectToAction("Index");
            }

            string waiterName = Session["User"] as string;
            int waiterID = Convert.ToInt16(Session["uid"]);
            string payMethod = Request.Form["paymentOption"];
            DateTime orderDateTime = DateTime.Now;
            string cellN = Request.Form["inputCell"];
            string emailU = Request.Form["inputEmail"];
            int changeDueValue = changeDue ?? 0;

            // Initialize totalAmount, closingBalance, and previousClosingBalance
            int totalAmount = 0;
            int closingBalance = 0;
            //int? previousClosingBalance = null;
            DateTime currentDate = DateTime.Now.Date;

            // Check if there is a record for the current date in tblCashFloat
            var cashFloatEntry = db.tblCashFloats.FirstOrDefault(cf => cf.Date == currentDate);

            if (cashFloatEntry != null)
            {
                // Use the existing float amount for the "current" balance
                int currentBalance = cashFloatEntry.Amount;
                int initialFloatAmount = currentBalance;

                // Deduct the changeDueValue from the current day's cash float
                cashFloatEntry.Amount -= changeDueValue;

                // Save changes to the database
                db.SaveChanges();

                // Calculate the closing balance using the initial float amount
                closingBalance = initialFloatAmount - totalAmount;
            }
            else
            {
                TempData["SuccessMessage"] = "";
            }

            string orderNumber = GenerateOrderNumber();

            // Iterate through selected products to create orders and transactions
            var ordersToInsert = new List<tblInStoreOrder>();
            var billsToInsert = new List<tblBill>();
            var transactionsToInsert = new List<Transactions>();

            foreach (var product in selectedProducts)
            {
                string method = isDineIn ? "Dine-in" : "Takeaway";

                // Update the cash float
                if (cashFloatEntry != null)
                {
                    cashFloatEntry.Amount = closingBalance + totalAmount;
                    db.SaveChanges();
                }

                var order = new tblInStoreOrder
                {
                    OrderNumber = orderNumber,
                    OrderDateTime = orderDateTime,
                    WaiterName = waiterName,
                    WaiterID = waiterID,
                    ProductName = product.Product,
                    Unit = product.UnitPrice,
                    Qty = product.Quantity,
                    Total = product.TotalPrice,
                    Method = method,
                    PayMethod = isDineIn ? null : paymentMethod,
                    CellNumber = cellN,
                    Email = emailU,
                    Status = "Preparing",
                    TableNumber = string.IsNullOrEmpty(tableNumber) ? "NONE" : tableNumber,
                    ReservedDate = null,
                    ReservedTime = null,
                    Change = changeDueValue,
                    Amountgiven = changeDueValue + product.TotalPrice
                };

                db.TblInStoreOrders.Add(order);

                if (!isDineIn)
                {
                    // Use the existing float amount for the "current" balance
                    int currentBalance = cashFloatEntry != null ? cashFloatEntry.Amount : 0; // Default to 0 if no existing cash float

                    // Create a transaction for Takeaway orders
                    var transaction = new Transactions
                    {
                        FloatID = cashFloatEntry != null ? cashFloatEntry.FloatID : 0,
                        Transaction = method,
                        InStoreOrderID = order.OrderId,
                        OnlineOrderID = null,
                        TransactionTime = DateTime.Now,
                        UserID = waiterID,
                        UserName = waiterName,
                        Current = currentBalance, // Use the current balance as the "current" amount
                        Credit = product.TotalPrice,
                        GivenAmt = changeDueValue + product.TotalPrice,
                        Debit = changeDueValue,
                        ClosingBalance = currentBalance + product.TotalPrice // Calculate closing balance
                    };

                    db.tblTransactions.Add(transaction);
                    db.SaveChanges();

                    totalAmount += product.TotalPrice; // Update the total amount
                }

                var bill = new tblBill
                {
                    OrderId = order.OrderId,
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
            }

            // Update the cash float for the last transaction if it exists
            if (cashFloatEntry != null)
            {
                cashFloatEntry.Amount = closingBalance + totalAmount;
                db.SaveChanges();
            }

            db.SaveChanges();

            if (!isDineIn && paymentMethod == "Card")
            {

                string returnURL = "https://2023grp01a.azurewebsites.net/Home/InStoreSuccess"; // Change this URL to match your actual URL

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
        public ActionResult TrackReservations(DateTime? selectedDate)
        {
            using (var db = new dbOnlineStoreEntities())
            {
                // Retrieve all reservations
                var allReservations = db.tblReservations.ToList();

                // Filter by selected date on the client side
                var reservations = selectedDate.HasValue
                    ? allReservations.Where(r => r.Date.HasValue && r.Date.Value.Date == selectedDate.Value.Date).ToList()
                    : allReservations;

                return View(reservations);
            }
        }
        #endregion

        #region Generate Output
        [HttpPost]
        public ActionResult GenerateOutput(DateTime date)
        {
            // Redirect to TrackReservations action with the selected date as a parameter
            return RedirectToAction("TrackReservations", new { selectedDate = date });
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

        #region OpenTable
        public ActionResult OpenTable()
        {
            var model = new tblReservation();
            model.SeatNumberList = GetSeatOptions();
            ViewBag.Submitted = false;

            return View(model);
        }
        [HttpPost]
        public ActionResult OpenTable(tblReservation reservation)
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

        #endregion

        #region Order Ready In Store

        private async Task SendOrderReadyEmail(string OrderNumber, string message, string email)
        {
            //sending sms to notify order is ready

            string accountSid = "ACb88a86596a0f96d4a39e78710f29cd46";
            string authToken = "60ce9beec8d9c41f0eebf37bb485c6c5";
            string fromPhoneNumber = "+13308879147";

            TwilioClient.Init(accountSid, authToken);

            var smsMessage = MessageResource.Create(
                body: message,
                from: new Twilio.Types.PhoneNumber(fromPhoneNumber),
                to: new Twilio.Types.PhoneNumber("+27606854298")
            );


            //sending email for waiter rating 
            string baseUrlW = $"{Request.Url.Scheme}://{Request.Url.Authority}";
            string urlW = Url.Action("WaiterRating", "Account", new { OrderNo = OrderNumber });
            string linkW = $"{baseUrlW}{urlW}";

            var body = "Dear Turbo Meals Customer, <br/><br/>"
                + "We value your feedback and would appreciate you leaving a rating for your waiter,using the following link: <a href=" + linkW + ">Waiter Rating</a>"
                + "<br/><br/>Thank you for helping us improve our services." + "<br/><br/>Kind regards<br/>Turbo Meals Family.";


            var Emessage = new MailMessage();
            Emessage.To.Add(new MailAddress(email));
            Emessage.From = new MailAddress("turbomeals123@gmail.com"); // Replace with your email address
            Emessage.Subject = "Turbo Meals Waiter Rating";
            Emessage.Body = body;
            Emessage.IsBodyHtml = true;

            using (var smtp = new SmtpClient())
            {
                await smtp.SendMailAsync(Emessage);
            }

        }


        [HttpPost]
        public async Task<ActionResult> MarkAsReady(int OrderId)
        {
            var order = db.TblInStoreOrders.Find(OrderId);


            if (order != null)
            {
                var relatedOrders = db.TblInStoreOrders.Where(o => o.OrderNumber == order.OrderNumber).ToList();
                foreach (var relatedOrder in relatedOrders)
                {
                    relatedOrder.Status = "Ready";

                    //update stock qty
                    var ingPro = db.IngredientProducts.Where(ip => ip.TblProduct.P_Name == relatedOrder.ProductName).ToList();

                    foreach (var i in ingPro)
                    {
                        var ingID = i.Ing_ID;

                        var ingr = db.tblIngredients.SingleOrDefault(m => m.Ing_ID == ingID);
                       
                        decimal qtyToReduce = 0;
                        if (ingr != null)
                        {
                            //qtyToReduce = ingr.Ing_UnitsUsed * (int)relatedOrder.Qty;
                            qtyToReduce = (decimal)(i.Ing_QtyPerPro * ingr.Ing_StandardQty * relatedOrder.Qty);
                            ingr.Ing_StockyQty -= qtyToReduce;


                            if ((ingr.Ing_StockyQty < 50) && (ingr.StockStatus == "In Stock"))
                            {
                                ingr.StockStatus = "Low Stock";
                                //send qty alert email 
                                string ing = ingr.Ing_Name;
                                string qty = Convert.ToString(ingr.Ing_StockyQty);
                               
                                string baseUrl = $"{Request.Url.Scheme}://{Request.Url.Authority}";
                                string url = Url.Action("LowStock", "Ingredients");
                                string link = $"{baseUrl}{url}";

                                var content = $"The Stock Quantity for the following Ingredient has dropped below 50 and requires restocking.<br/><br/> ";
                                content += "Ingredient: " + ing + " current quantity is " + qty + "<br/>Click here to view low stock ingredients: <a href=" + link + ">Low Stock</a>";



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

                }

                db.SaveChanges();

                string message = "Dear Turbo Meals Customer,\nYour order: " + order.OrderNumber + " is now ready.\n";
                if (order.Method == "Takeaway")
                {
                    message += "You can collect order from Collections.\n";
                    if (order.PayMethod == "Cash")
                    {
                        message += "Please note, payment is pending and may be done upon collection.\n";
                    }
                    message += "We hope you enjoy your meal, and hope to see you again soon.";
                }
                else
                {
                    message += "Your waiter " + order.WaiterName + " will be bringing your order to your table, Table " + order.TableNumber + " shortly. Thank you for your patience.\n";
                    message += "\nWe hope you enjoy your meal.\n\n\n";
                    message += "We would greatly appreciate you taking a minute and leaving a rating of your waiter, using the link sent to your email address.";
                }



                // Send the order ready confirmation email
                await SendOrderReadyEmail(order.OrderNumber, message, order.Email);
            }

            return RedirectToAction("PrepInStoreOrders");
        }
        #endregion

        #region Update Payment Method
        [HttpPost]
        public ActionResult UpdatePaymentMethod(string orderNumber, string paymentMethod, int? changeDue, double? tipAmount)
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

                        // Update the change amount that was calculated in the view
                        order.Change = changeDue;
                        order.Tip = tipAmount;
                    }

                    // Save changes to the database
                    dbContext.SaveChanges();

                    if (paymentMethod.Equals("Card", StringComparison.OrdinalIgnoreCase))
                    {
                        string returnURL = "https://2023grp01a.azurewebsites.net/Home/SuccessBill"; // Change this URL to match your actual URL

                        // Redirect to PayPal with the calculated total amount and return URL
                        return Content("<script>" +
                            "function callPayPal() {" +
                            "window.location.href = 'https://www.sandbox.paypal.com/cgi-bin/webscr?cmd=_xclick&amount=" + "&business=sb-w3cyw20367505@business.example.com&item_name=FoodOrder&return=" + returnURL + "';" +
                            "}" +
                            "callPayPal();" +
                            "</script>");

                    }

                    // Get the existing cash float for the current date
                    DateTime currentDate = DateTime.Now.Date;
                    var existingCashFloat = dbContext.tblCashFloats.FirstOrDefault(cf => cf.Date == currentDate);

                    if (existingCashFloat != null)
                    {
                        // Calculate the total amount based on all bills with the same order number
                        int totalAmount = dbContext.TblBills
                            .Where(b => b.OrderNumber == orderNumber)
                            .Sum(b => (int)b.Total);

                        // Use the existing float amount for the "current" balance
                        int currentBalance = existingCashFloat.Amount;

                        // Create a new transaction record for Dine-in
                        Transactions transaction = new Transactions
                        {
                            FloatID = existingCashFloat.FloatID,
                            Transaction = "Dine-in",
                            InStoreOrderID = orders[0].OrderId, // Set as needed
                            OnlineOrderID = null,
                            TransactionTime = DateTime.Now,
                            UserID = orders[0].WaiterID, // Assuming waiter's ID is used as the User ID
                            UserName = orders[0].WaiterName, // Use the waiter's name from the order
                            Current = currentBalance, // Use the existing float amount for the day
                            Credit = totalAmount, // Set credit as the total amount from bills
                            GivenAmt = 0, // Include the change due in the given amount
                            Debit = changeDue.GetValueOrDefault(0),
                            ClosingBalance = currentBalance + totalAmount // Calculate closing balance
                        };

                        dbContext.tblTransactions.Add(transaction);

                        // Update the existing cash float's amount to the closing balance
                        existingCashFloat.Amount = currentBalance + totalAmount;
                        dbContext.SaveChanges();

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
            /*Bitmap resizedImage = new Bitmap(qrCodeImage, new Size(200, 200));*/ // Adjust the size as needed

            // Convert the resized image to a base64 string
            using (MemoryStream stream = new MemoryStream())
            {
                //resizedImage.Save(stream, ImageFormat.Png);
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
        new SelectListItem { Value = "1", Text = "TableNum 1 ( 2 seats)" },
        new SelectListItem { Value = "2", Text = "TableNum 2 ( 4 seats)" },
        new SelectListItem { Value = "3", Text = "TableNum 3 ( 4 seats)" },
        new SelectListItem { Value = "4", Text = "TableNum 4 ( 4 seats)" },
        new SelectListItem { Value = "5", Text = "TableNum 5 ( 2 seats)" },
        new SelectListItem { Value = "6", Text = "TableNum 6 ( 5 seats)" },
        new SelectListItem { Value = "7", Text = "TableNum 7 ( 2 seats)" },
        new SelectListItem { Value = "8", Text = "TableNum 8 ( 6 seats)" },
        new SelectListItem { Value = "9", Text = "TableNum 9 ( 2 seats)" },
        new SelectListItem { Value = "10", Text = "TableNum 10 ( 2 seats)" },
        new SelectListItem { Value = "11", Text = "TableNum 11 ( 8 seats)" },
        new SelectListItem { Value = "12", Text = "TableNum 12 ( 2 seats)" },
        new SelectListItem { Value = "13", Text = "TableNum 13 ( 4 seats)" },
        new SelectListItem { Value = "14", Text = "TableNum 14 ( 2 seats)" },
        new SelectListItem { Value = "15", Text = "TableNum 15 ( 4 seats)" }

    };
        }
        #endregion

        #region CompleteReservation
        public ActionResult CompleteReservation(int bookingId)
        {
            using (var dbContext = new dbOnlineStoreEntities())
            {
                try
                {
                    // Find and delete all occurrences of the bookingId in the tblReservations table
                    var reservationsToDelete = dbContext.tblReservations.Where(r => r.BookingId == bookingId).ToList();
                    dbContext.tblReservations.RemoveRange(reservationsToDelete);

                    // Find and delete all occurrences of the bookingId in the TblInStoreOrders table
                    var ordersToDelete = dbContext.TblInStoreOrders.Where(o => o.BookingId == bookingId).ToList();
                    dbContext.TblInStoreOrders.RemoveRange(ordersToDelete);

                    // Save changes to the database
                    dbContext.SaveChanges();
                }
                catch (Exception ex)
                {
                    // Handle any exceptions
                    ViewBag.ErrorMessage = "An error occurred while completing the reservation: " + ex.Message;
                    // You might want to log the error or take other actions here
                }
            }

            // Redirect back to the Dine-In page
            return RedirectToAction("TrackReservations");
        }
        #endregion


        #region ReserveOrder
        private List<Cart> FetchCartItems()
        {
            // Retrieve the cart items from TempData
            List<Cart> cartItems = TempData["cart"] as List<Cart>;

            if (cartItems == null)
            {
                // Handle the case where cart items are not found in TempData
                // You can return an empty list, throw an exception, or handle it as needed.
                cartItems = new List<Cart>(); // For example, return an empty list
            }

            return cartItems;
        }

        [HttpGet]
        public ActionResult ReserveOrder()
        {
            // Fetch cart items (you need to implement this logic based on your setup)
            var cartItems = FetchCartItems(); // Implement this method

            var reservation = new tblReservation();
            reservation.SeatNumberList = GetSeatOptions();

            var model = new ReserveOrderViewModel
            {
                CartItems = cartItems,
                Reservation = reservation,
            };

            ViewBag.Submitted = false;

            return View(model);
        }


        private string Generate()
        {
            Random rand = new Random();
            string orderNumber;

            // Generate a random 3-digit order number and ensure it's unique
            do
            {
                orderNumber = rand.Next(100, 1000).ToString();
            } while (db.TblInStoreOrders.Any(o => o.OrderNumber == orderNumber));

            return orderNumber;
        }

        [HttpPost]
        public ActionResult ReserveOrder(ReserveOrderViewModel model, string PaymentMethod)
        {
            bool reservationSuccessful = true; // Flag to track if the reservation was successful

            if (ModelState.IsValid)
            {
                try
                {
                    // Save reservation details to tblReservation
                    var reservation = new tblReservation
                    {
                        Customer_Name = model.Reservation.Customer_Name,
                        Mail = model.Reservation.Mail,
                        Number = model.Reservation.Number,
                        Date = model.Reservation.Date,
                        Time = model.Reservation.Time,
                        Seating = model.Reservation.Seating,
                        // Populate other reservation fields as needed
                    };

                    // Make sure to populate SeatNumberList again here
                    reservation.SeatNumberList = GetSeatOptions();

                    // Save the reservation to the database
                    db.tblReservations.Add(reservation);
                    db.SaveChanges();

                    // Access and process dynamically generated cart items
                    foreach (var cartItem in model.CartItems)
                    {
                        // Save each item's details to tblInStoreOrder or process as needed
                        var order = new tblInStoreOrder
                        {
                            BookingId = reservation.BookingId, // Use the BookingId from the saved reservation
                            OrderNumber = Generate(),
                            OrderDateTime = DateTime.Now,
                            ProductName = cartItem.proname,
                            Unit = cartItem.price,
                            Qty = cartItem.qty,
                            Total = cartItem.price,
                            Method = "Dine-in",
                            PayMethod = PaymentMethod,
                            TableNumber = reservation.Seating,
                            ReservedDate = reservation.Date,
                            ReservedTime = reservation.Time
                        };

                        // Save the order to the database
                        db.TblInStoreOrders.Add(order);
                    }

                    db.SaveChanges();
                    var body = $"Dear {reservation.Customer_Name},<br /><br />Your reservation was successful. Table Number {reservation.Seating} is reserved for you on this date {(reservation.Date.HasValue ? reservation.Date.Value.ToShortDateString() : string.Empty)} and time {(reservation.Time.HasValue ? reservation.Time.Value.ToString("hh:mm tt") : "")},<br><br>Your Booking ID is {reservation.BookingId}. We hope you enjoy our services at Turbo Meals";

                    // Add cart items to the email body
                    body += "<br /><br />Your Order Details:<br />";
                    foreach (var cartItem in model.CartItems)
                    {
                        body += $"<strong>Item: </strong>{cartItem.proname}<br />";
                        body += $"<strong>Quantity: </strong>{cartItem.qty}<br />";
                        body += $"<strong>Price: </strong>R {cartItem.price}<br />";
                        body += $"<strong>Total: </strong>R {cartItem.bill}<br /><br />";
                    }

                    body += "If you have any queries, drop us an email (turbomeals123@gmail.com)";

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
                catch (Exception)
                {
                    // Handle the exception (e.g., log it)
                    reservationSuccessful = false;
                }
            }

            // Continue with the normal functionality
            if (reservationSuccessful)
            {
                // Reservation was successful, show a success message
                return RedirectToAction("ReserveSuccess_Cash");
            }
            else
            {
                // Reservation was not successful due to an exception, you can handle it accordingly
                // For example, you can display an error message to the user
                ViewBag.ErrorMessage = "An error occurred while making the reservation. Please try again later.";
            }

            // If there are validation errors, return the view with errors
            return View(model);
        }
        #endregion

     

        #region AdminHandling

        public ActionResult AdminCashFloat()
        {
            return View();
        }

        [HttpPost]
        public ActionResult SaveCashFloatAdmin(tblCashFloat cashFloat)
        {
            if (ModelState.IsValid)
            {
                using (var context = new dbOnlineStoreEntities())
                {
                    cashFloat.Date = DateTime.Now.Date; // Set the current date (without time)

                    // Check if there's a cash float for the current day
                    var existingCashFloat = context.tblCashFloats
                        .Where(c => c.Date == cashFloat.Date)
                        .FirstOrDefault();

                    if (existingCashFloat != null)
                    {
                        // Update the cash float for the current day
                        existingCashFloat.Amount = cashFloat.Amount;
                    }
                    else
                    {
                        // If there's no cash float for the current day, add a new record
                        context.tblCashFloats.Add(cashFloat);
                    }

                    context.SaveChanges();
                }

                return RedirectToAction("AdminCashFLoat");
            }

            return View("AdminCashFloat", cashFloat);
        }



        #endregion

        #region WaiterCashDrawer

        public ActionResult CashDrawer()
        {
            // Get the last record from tblCashFloat (you need to implement this logic)
            var lastCashFloat = db.tblCashFloats.OrderByDescending(cf => cf.Date).FirstOrDefault();

            return View(lastCashFloat);
        }
        
        public JsonResult GetCurrentFloatAmount()
        {
            using (var dbContext = new dbOnlineStoreEntities())
            {
                // Fetch the current float amount from the database
                var currentFloat = dbContext.tblCashFloats
                    .Where(cf => cf.Date == DateTime.Today)
                    .FirstOrDefault();

                if (currentFloat != null)
                {
                    return Json(new { amount = currentFloat.Amount }, JsonRequestBehavior.AllowGet);
                }
            }

            // If there's no data or an error occurs, provide a default amount
            return Json(new { amount = 0.00 }, JsonRequestBehavior.AllowGet);
        }

        //public ActionResult CashDrawer()
        //{
        //    using (var context = new dbOnlineStoreEntities())
        //    {
        //        // Retrieve the cash float for the current day
        //        var currentDay = DateTime.Now.Date;
        //        var cashFloat = context.tblCashFloats
        //            .Where(c => c.Date == currentDay)
        //            .FirstOrDefault();

        //        if (cashFloat != null)
        //        {
        //            // Pass the cash float for the current day to the view
        //            return View(cashFloat);
        //        }
        //    }

        //    // If there's no cash float for the current day, pass a new CashFloat object
        //    return View(new tblCashFloat());
        //}

        //[HttpPost]
        //public ActionResult SaveCashFloat(tblCashFloat cashFloat)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        using (var context = new dbOnlineStoreEntities())
        //        {
        //            cashFloat.Date = DateTime.Now.Date; // Set the current date (without time)

        //            // Check if there's a cash float for the current day
        //            var existingCashFloat = context.tblCashFloats
        //                .Where(c => c.Date == cashFloat.Date)
        //                .FirstOrDefault();

        //            if (existingCashFloat != null)
        //            {
        //                // Update the cash float for the current day
        //                existingCashFloat.Amount = cashFloat.Amount;
        //            }
        //            else
        //            {
        //                // If there's no cash float for the current day, add a new record
        //                context.tblCashFloats.Add(cashFloat);
        //            }

        //            context.SaveChanges();
        //        }

        //        return RedirectToAction("CashDrawer");
        //    }

        //    return View("CashDrawer", cashFloat);
        //}
        public ActionResult Transactions()
        {
            List<Transactions> transaction;

            using (var context = new dbOnlineStoreEntities())
            {
                transaction = context.tblTransactions.ToList();
            }

            return View(transaction);
        }

        //public ActionResult ShortageFloat()
        //{
        //    return View();
        //}

        #endregion

        # region Withdrawals
        public ActionResult Withdrawals()
        {

            using (var dbContext = new dbOnlineStoreEntities()) // Replace 'YourDbContext' with your actual DbContext class
            {
                var transactions = dbContext.tblTransactions.ToList(); // Fetch all transactions
                return View(transactions);
            }
        }

        [HttpPost]
        public ActionResult WithdrawTips(int? OrderID, string WithdrawalType, int? OperatingExpenses, int? ChangeTakenOut)
        {
            if (WithdrawalType == "Employee Tips")
            {
                // Get all employees
                var employees = db.tblEmployees.ToList();

                // Calculate the total tips to withdraw
                double totalTips = employees.Sum(e => e.Tips);

                // Create a new transaction record for tips withdrawal
                var currentDate = DateTime.Now.Date;
                var existingFloat = db.tblCashFloats.FirstOrDefault(cf => cf.Date == currentDate);

                if (existingFloat != null)
                {
                    var lastTransaction = db.tblTransactions
                        .OrderByDescending(t => t.TransactionID)
                        .FirstOrDefault();

                    var closingBalance = lastTransaction != null
                        ? lastTransaction.ClosingBalance
                        : existingFloat.Amount; // Use the existing float amount for the day

                    string userName = Session["User"] as string;
                    int userID = Convert.ToInt16(Session["uid"]);

                    // Create a new transaction record for tips withdrawal
                    var transaction = new Transactions
                    {
                        FloatID = existingFloat.FloatID,
                        Transaction = "Tips", // Set the transaction type to "Tips"
                        OnlineOrderID = null,
                        InStoreOrderID = OrderID,
                        TransactionTime = DateTime.Now,
                        UserID = userID,
                        UserName = userName,
                        Current = existingFloat.Amount, // Current balance is the float amount for the day
                        Credit = 0,
                        Debit = (int)totalTips, // Debit is the total tips
                        ClosingBalance = existingFloat.Amount - (int)totalTips // Calculate closing balance
                    };

                    db.tblTransactions.Add(transaction);

                    // Update employee tips to 0
                    foreach (var employee in employees)
                    {
                        employee.Tips = 0;
                    }

                    db.SaveChanges();

                    // Update the float amount for the day
                    existingFloat.Amount = transaction.ClosingBalance;
                    db.SaveChanges();
                }
            }
            else if (WithdrawalType == "Refunds")
            {
                // Ensure OrderID matches a record in tblInstore
                var inStoreOrder = db.TblInStoreOrders.SingleOrDefault(o => o.OrderId == OrderID && o.PayMethod == "Cash");

                if (inStoreOrder != null)
                {
                    // Get the total for the selected InStoreOrder
                    int totalRefund = (int)inStoreOrder.Total;

                    // Rest of the logic is similar to Employee Tips withdrawal
                    var currentDate = DateTime.Now.Date;
                    var existingFloat = db.tblCashFloats.FirstOrDefault(cf => cf.Date == currentDate);

                    if (existingFloat != null)
                    {
                        var lastTransaction = db.tblTransactions
                            .OrderByDescending(t => t.TransactionID)
                            .FirstOrDefault();

                        var closingBalance = lastTransaction != null
                            ? lastTransaction.ClosingBalance
                            : existingFloat.Amount;

                        string userName = Session["User"] as string;
                        int userID = Convert.ToInt16(Session["uid"]);

                        var transaction = new Transactions
                        {
                            FloatID = existingFloat.FloatID,
                            Transaction = "Refunds", // Set the transaction type to "Refunds"
                            OnlineOrderID = null,
                            InStoreOrderID = OrderID,
                            TransactionTime = DateTime.Now,
                            UserID = userID,
                            UserName = userName,
                            Current = existingFloat.Amount,
                            Credit = 0,
                            Debit = totalRefund, // Debit is the total refund amount
                            ClosingBalance = existingFloat.Amount - totalRefund
                        };

                        db.tblTransactions.Add(transaction);

                        // Update the float amount for the day
                        existingFloat.Amount = transaction.ClosingBalance;
                        db.SaveChanges();
                    }
                }
                else
                {
                    TempData["OrderNotFound"] = "Order not found";
                    return RedirectToAction("WithDrawals");
                }
            }
            else if (WithdrawalType == "Operating Expenses")
            {
                if (OperatingExpenses.HasValue)
                {
                    // Get the existing float for the day
                    var currentDate = DateTime.Now.Date;
                    var existingFloat = db.tblCashFloats.FirstOrDefault(cf => cf.Date == currentDate);

                    if (existingFloat != null)
                    {
                        // Calculate the closing balance
                        int currentBalance = existingFloat.Amount;
                        int debit = OperatingExpenses.Value;
                        int closingBalance = currentBalance - debit;

                        // Create a new transaction record for operating expenses withdrawal
                        string userName = Session["User"] as string;
                        int userID = Convert.ToInt16(Session["uid"]);

                        var transaction = new Transactions
                        {
                            FloatID = existingFloat.FloatID,
                            Transaction = "Operating Expenses", // Set the transaction type to "Operating Expenses"
                            OnlineOrderID = null,
                            InStoreOrderID = OrderID, // Set as needed
                            TransactionTime = DateTime.Now,
                            UserID = userID,
                            UserName = userName, // Use the user's name from the order
                            Current = currentBalance, // Use the current balance
                            Credit = 0,
                            Debit = debit, // Debit is the operating expenses amount
                            ClosingBalance = closingBalance
                        };

                        db.tblTransactions.Add(transaction);

                        // Update the float amount for the day
                        existingFloat.Amount = closingBalance;
                        db.SaveChanges();
                    }
                }
                else
                {
                    TempData["InvalidAmount"] = "Please enter a valid operating expenses amount.";
                }
            }
            else if (WithdrawalType == "DeliveryChange")
            {
                if (ChangeTakenOut.HasValue)
                {
                    // Get the existing float for the day
                    var currentDate = DateTime.Now.Date;
                    var existingFloat = db.tblCashFloats.FirstOrDefault(cf => cf.Date == currentDate);

                    if (existingFloat != null)
                    {
                        // Calculate the closing balance
                        int currentBalance = existingFloat.Amount;
                        int debit = ChangeTakenOut.Value;
                        int closingBalance = currentBalance - debit;

                        // Create a new transaction record for "Change Taken Out for Delivery/Other"
                        string userName = Session["User"] as string;
                        int userID = Convert.ToInt16(Session["uid"]);

                        var transaction = new Transactions
                        {
                            FloatID = existingFloat.FloatID,
                            Transaction = "Delivery Change", // Set the transaction type
                            OnlineOrderID = null,
                            InStoreOrderID = OrderID, // Set as needed
                            TransactionTime = DateTime.Now,
                            UserID = userID,
                            UserName = userName, // Use the user's name from the order
                            Current = currentBalance, // Use the current balance
                            Credit = 0,
                            Debit = debit, // Debit is the change taken out amount
                            ClosingBalance = closingBalance
                        };

                        db.tblTransactions.Add(transaction);

                        // Update the float amount for the day
                        existingFloat.Amount = closingBalance;
                        db.SaveChanges();
                    }
                }
                else
                {
                    TempData["InvalidAmount"] = "Please enter a valid change taken out amount.";
                }
            }
            // Handle other withdrawal types here

            return RedirectToAction("Withdrawals");
        }


        #endregion
        #region Discrepancy
        public ActionResult Discrepancy()
        {
            return View();
        }

        [HttpPost]
        public ActionResult CreateDiscrepancy(Discrepancy model)
        {
            if (ModelState.IsValid)
            {
                // Fetch the current day's FloatID and calculate initial and closing balances
                int floatID = FindFloatIDForCurrentDay(model.Date);
                int initialBalance = FindInitialBalanceForFloatID(floatID);
                int closingBalance = FindClosingBalanceForFloatID(floatID);

                // Calculate Cash Discrepancy
                int cashDiscrepancy = closingBalance - model.CountedBalance;

                // Create and save the Discrepancy record with calculated Cash Discrepancy
                var discrepancy = new Discrepancy
                {
                    Date = model.Date,
                    InitalFloat = initialBalance,
                    ClosingBalance = closingBalance,
                    CountedBalance = model.CountedBalance,
                    CashDiscrepancy = cashDiscrepancy
                };

                // Save the Discrepancy record to your database
                db.tblDiscrepancy.Add(discrepancy);
                db.SaveChanges();

                // Redirect to a success page or perform other actions
                return RedirectToAction("DiscrepancyCreated");
            }

            // If there are validation errors, redisplay the form with error messages
            return View(model);
        }

        // Other controller actions

        // Helper methods to find FloatID, initial balance, and closing balance
        private int FindFloatIDForCurrentDay(DateTime? selectedDate)
        {
            if (selectedDate.HasValue)
            {
                // Find the FloatID for the current day based on the selected date
                var selectedDateValue = selectedDate.Value;
                var firstTransactionForDay = db.tblTransactions
                    .OrderBy(t => t.TransactionTime)
                    .FirstOrDefault(t => DbFunctions.TruncateTime(t.Float.Date) == DbFunctions.TruncateTime(selectedDateValue));

                if (firstTransactionForDay != null)
                {
                    return firstTransactionForDay.FloatID;
                }
            }

            return 0; // Return 0 or any other default value if not found
        }



        private int FindInitialBalanceForFloatID(int floatID)
        {
            // Find the initial balance based on the first transaction for the given FloatID
            var initialTransaction = db.tblTransactions
                .Where(t => t.FloatID == floatID)
                .OrderBy(t => t.TransactionTime)
                .FirstOrDefault();

            if (initialTransaction != null)
            {
                return initialTransaction.Current;
            }

            return 0; // Return 0 or any other default value if not found
        }


        private int FindClosingBalanceForFloatID(int floatID)
        {
            var closingTransaction = db.tblTransactions
                .Where(t => t.FloatID == floatID)
                .OrderByDescending(t => t.TransactionTime)
                .FirstOrDefault();

            if (closingTransaction != null)
            {
                return closingTransaction.ClosingBalance;
            }

            return 0; // Return 0 or any other default value if not found
        }
        #endregion
    }
}