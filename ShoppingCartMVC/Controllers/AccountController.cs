using System;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using RegistrationAndLogin.Models;
using ShoppingCartMVC.Models;

namespace ShoppingCartMVC.Controllers
{
    public class AccountController : Controller
    {

        dbOnlineStoreEntities db = new dbOnlineStoreEntities();

        #region user registration 

        public ActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Register(tblUser t)
        {
            tblUser u = new tblUser();
            if (ModelState.IsValid)
            {
                if (db.tblUsers.Any(o => o.Email == t.Email))
                {

                    TempData["msg"] = "This email address cannot be used. Please try again with a different email.";
                    t.Email = string.Empty;
                }
                else
                {
                    u.Name = t.Name;
                    u.Email = t.Email;
                    u.Password = t.Password;
                    u.RoleType = 2;

                    db.tblUsers.Add(u);
                    db.SaveChanges();

                    var accPoints = new UserPoints { UserID = u.UserId, PointBalance = 50 };
                    db.tblPoints.Add(accPoints);
                    db.SaveChanges();

                    var body = $"Dear {t.Name},<br /><br />Thank you for registering at Turbo Meals. Your registration was successful. You can now login with your credentials to access the Turbo Meals Site.<br /><br />";
                    var message = new MailMessage();
                    message.To.Add(new MailAddress(t.Email));
                    message.From = new MailAddress("turbomeals123@gmail.com"); // Replace with your email address
                    message.Subject = "Registration Confirmation";
                    message.Body = string.Format(body);
                    message.IsBodyHtml = true;


                    using (var smtp = new SmtpClient())
                    {
                        smtp.Send(message);
                    }
                    return RedirectToAction("Login", "Account");
                }


            }
            else
            {
                TempData["msg"] = "Sorry You Not Registered!!";
            }
            return View();
        }


        #endregion

        #region User Login

        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Login(tblUser t)
        {
            var query = db.tblUsers.SingleOrDefault(m => m.Email == t.Email && m.Password == t.Password);
            if (query != null)
            {
                if (query.RoleType == 1)
                {
                    Session["uid"] = query.UserId;
                    Session["UserRole"] = "Admin";

                    FormsAuthentication.SetAuthCookie(query.Email, false);
                    Session["User"] = query.Name;

                    var accProfile = db.TblAccProfiles.FirstOrDefault(m => m.UserID == query.UserId);

                    if (accProfile == null)
                    {
                        // No record with matching UserID found, so we create a new record
                        tblAccProfile ap = new tblAccProfile();

                        if (ModelState.IsValid)
                        {
                            ap.userName = query.Name;
                            ap.UserID = query.UserId;
                        }

                        db.TblAccProfiles.Add(ap);
                        db.SaveChanges();
                    }
                    else
                    {
                        Session["userProfile"] = accProfile.userProfileImage;
                    }


                    return RedirectToAction("GetAllOrderDetail", "Home");
                }
                else if (query.RoleType == 2)
                {
                    Session["uid"] = query.UserId;
                    Session["UserRole"] = "User";
                    FormsAuthentication.SetAuthCookie(query.Email, false);
                    Session["User"] = query.Name;

                    var accProfile = db.TblAccProfiles.FirstOrDefault(m => m.UserID == query.UserId);

                    if (accProfile == null)
                    {
                        // No record with matching UserID found, so we create a new record
                        tblAccProfile ap = new tblAccProfile();

                        if (ModelState.IsValid)
                        {
                            ap.userName = query.Name;
                            ap.UserID = query.UserId;
                        }

                        db.TblAccProfiles.Add(ap);
                        db.SaveChanges();
                    }
                    else
                    {
                        Session["userProfile"] = accProfile.userProfileImage;
                    }

                    return RedirectToAction("Index", "Home");
                }
                else if (query.RoleType == 3)
                {
                    Session["uid"] = query.UserId;
                    FormsAuthentication.SetAuthCookie(query.Email, false);
                    Session["User"] = query.Name;

                    var emp = db.tblEmployees.FirstOrDefault(m => m.UserID == query.UserId);

                    if (emp == null)
                    {
                        // No record with matching UserID found, so we create a new record
                        tblEmployee e = new tblEmployee();

                        if (ModelState.IsValid)
                        {
                            e.EmpName = query.Name;
                            e.UserID = query.UserId;
                        }

                        db.tblEmployees.Add(e);
                        db.SaveChanges();
                    }

                    return RedirectToAction("DriverDeliveries", "Home", new { id = @Session["uid"] });
                }
                else if (query.RoleType == 4)
                {
                    Session["uid"] = query.UserId;
                    FormsAuthentication.SetAuthCookie(query.Email, false);
                    Session["User"] = query.Name;

                    var emp = db.tblEmployees.FirstOrDefault(m => m.UserID == query.UserId);

                    if (emp == null)
                    {
                        // No record with matching UserID found, so we create a new record
                        tblEmployee e = new tblEmployee();

                        if (ModelState.IsValid)
                        {
                            e.EmpName = query.Name;
                            e.EmpImage = "NULL";
                            e.UserID = query.UserId;
                        }

                        db.tblEmployees.Add(e);
                        db.SaveChanges();
                    }
                    return RedirectToAction("PrepStaff", "Home", new { id = @Session["uid"] });

                }
                else if (query.RoleType == 5)
                {
                    Session["uid"] = query.UserId;
                    FormsAuthentication.SetAuthCookie(query.Email, false);
                    Session["User"] = query.Name;

                    var emp = db.tblEmployees.FirstOrDefault(m => m.UserID == query.UserId);

                    if (emp == null)
                    {
                        // No record with matching UserID found, so we create a new record
                        tblEmployee e = new tblEmployee();

                        if (ModelState.IsValid)
                        {
                            e.EmpName = query.Name;
                            e.UserID = query.UserId;
                        }

                        db.tblEmployees.Add(e);
                        db.SaveChanges();
                    }

                    return RedirectToAction("POSDashboard", "Home", new { id = @Session["uid"] });
                }

            }
            else
            {
                TempData["msg"] = "Invalid Username or Password";
            }

            return View();
        }

        #endregion

        #region Forget Password and Reset Password
        [NonAction]
        public void SendVerificationLinkEmail(int userID, string activationCode, string customerEmail)
        {
            string baseUrl = $"{Request.Url.Scheme}://{Request.Url.Authority}";
            string url = Url.Action("ResetPassword", "Account", new { id = userID, token = activationCode });
            string link = $"{baseUrl}{url}";

            var content = "Hi,<br/><br/> We got your request for the reset of your account password. Please click on the below link to reset your password: " +
                    "<a href=" + link + ">Reset Password</a>";



            var email = new MailMessage();
            email.To.Add(new MailAddress(customerEmail));
            email.From = new MailAddress("turbomeals123@gmail.com");
            email.Subject = "Reset Password for Turbo Meals Account";
            email.Body = content;
            email.IsBodyHtml = true;

            using (var smtp = new SmtpClient())
            {
                smtp.Send(email);
            }


        }

        public ActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public ActionResult ForgotPassword(string EmailID)
        {
            //Verify Email ID
            //Generate Reset password link
            //Send Email
            string message = "";

            var account = db.tblUsers.Where(u => u.Email.Equals(EmailID)).FirstOrDefault();
            if (account != null)
            {
                //Send email for reset password
                string resetCode = Guid.NewGuid().ToString();
                SendVerificationLinkEmail(account.UserId, resetCode, account.Email);
                account.ResetPasswordCode = resetCode;


                db.SaveChanges();
                message = "Reset password link has been sent to your email address.";
            }
            else
            {
                message = "Account not found";
            }

            ViewBag.Message = message;
            return View();
        }

        public ActionResult ResetPassword(int id, string token)
        {
            //Verify the reset password link
            //Find account associated with this link
            //redirect to reset password page

            var user = db.tblUsers.Where(a => a.UserId.Equals(id)).FirstOrDefault();
            if (user != null)
            {
                ResetPasswordModel model = new ResetPasswordModel();
                model.ResetCode = token;
                return View(model);
            }
            else
            {
                return HttpNotFound();
            }

        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ResetPassword(ResetPasswordModel model)
        {
            var message = "";
            if (ModelState.IsValid)
            {

                var user = db.tblUsers.Where(a => a.ResetPasswordCode.Equals(model.ResetCode)).FirstOrDefault();
                if (user != null)
                {
                    user.Password = model.NewPassword;
                    user.ResetPasswordCode = "";
                    //dc.Configuration.ValidateOnSaveEnabled = false;
                    db.SaveChanges();
                    message = "New password Updated Successfully!";
                }

            }
            else
            {
                message = "Something Invalid";
            }

            ViewBag.Message = message;
            return View(model);
        }
        #endregion

        #region logout 

        public ActionResult Signout()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("Index", "Home");
        }

        #endregion

        #region Driver and Waiter View Account Details
        public ActionResult UserDetails()
        {
            int userID = Convert.ToInt16(Session["uid"]);
            tblEmployee currentUser = db.tblEmployees.SingleOrDefault(u => u.UserID == userID);

            if (currentUser != null)
            {
                return View(currentUser);
            }
            else
            {
                // Handle the case where the user is not found (e.g., show an error message)
                return RedirectToAction("Index", "Home"); // Redirect to a different page if needed
            }
        }
        #endregion

        #region Driver and Waiter Edit Account

        public ActionResult EditAccount(int id)
        {
            TempData["id"] = id;
            var query = db.tblEmployees.SingleOrDefault(m => m.UserID == id);
            return View(query);
        }


        [HttpPost]
        public ActionResult EditAccount(tblEmployee em, HttpPostedFileBase Image)
        {
            int uID = Convert.ToInt16(TempData["id"]);

            try
            {
                // Fetch the current entity from the database
                var currentEntity = db.tblEmployees.FirstOrDefault(m => m.UserID == uID);

                if (currentEntity != null)
                {
                    if (Image != null)
                    {
                        // Update the EmpImage field only
                        currentEntity.EmpImage = Image.FileName.ToString();
                        var folder = Server.MapPath("~/Uploads/");
                        Image.SaveAs(Path.Combine(folder, Image.FileName.ToString()));
                    }

                    // Save changes
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                TempData["msg"] = ex;
            }

            return RedirectToAction("UserDetails");
        }

        #endregion

        #region Driver Rating
        public ActionResult RateAndTip(int InvoiceId)
        {
            var completed = db.tblOrders.SingleOrDefault(db => db.InvoiceId == InvoiceId);
            if (completed.Rated)
            {
                return View("CompletedRating");
            }
            else
            {


                TempData["i"] = InvoiceId;


                var query = from driver in db.tblDrivers
                            join employee in db.tblEmployees
                            on driver.UserId equals employee.UserID
                            where driver.TblOrder.InvoiceId == InvoiceId
                            select new
                            {
                                DriverName = employee.EmpName,
                                DriverEmail = employee.TblUser.Email,
                                DriverImage = employee.EmpImage,
                                DriverAvgRating = employee.avgRating
                            };

                var result = query.SingleOrDefault();

                if (result == null)
                {
                    return HttpNotFound();
                }

                string driverName = result.DriverName;
                string driverEmail = result.DriverEmail;
                string driverImage = result.DriverImage;
                double driverAvgRating = result.DriverAvgRating;
                int ratingProvided = 1;


                var vmodel = new RateAndTipVM
                {
                    DriverName = driverName,
                    DriverEmail = driverEmail,
                    DriverImage = driverImage,
                    DriverAvgRating = driverAvgRating,
                    rating = ratingProvided
                };
                return View(vmodel);
            }
        }


        [HttpPost]
        public ActionResult RateAndTip(RateAndTipVM model)
        {
            if (ModelState.IsValid)
            {
                int inv = (int)TempData["i"];
                var query = db.tblDrivers.SingleOrDefault(m => m.TblOrder.InvoiceId == inv);
                var completed = db.tblOrders.SingleOrDefault(db => db.InvoiceId == inv);
                var emp = db.tblEmployees.SingleOrDefault(u => u.UserID == query.UserId);
                var email = completed.TblInvoice.TblUser.Email;

                if (emp != null)
                {

                    if (model.IsCustomTip)
                    {

                        emp.Tips += model.CustomTip;
                    }
                    else
                    {

                        emp.Tips += 0;
                    }

                    //checking rating 
                    if (model.rating == 1)
                    {
                        emp.Rating1 += 1;
                    }
                    else
                    if (model.rating == 2)
                    {
                        emp.Rating2 += 1;
                    }
                    else
                    if (model.rating == 3)
                    {
                        emp.Rating3 += 1;
                    }
                    else
                        if (model.rating == 4)
                    {
                        emp.Rating4 += 1;
                    }
                    else
                    if (model.rating == 5)
                    {
                        emp.Rating5 += 1;
                    }

                    var sum = emp.Rating1 + emp.Rating2 + emp.Rating3 + emp.Rating4 + emp.Rating5;
                    emp.avgRating = sum / 5.00;

                    completed.Rated = true;

                    db.SaveChanges();

                    //send thank you email
                    var body = "Dear Turbo Meals Customer,<br/><br/>" +
                         "We are writing to express our gratitude for your recent feedback regarding our driver's performance. Your opinion is of great importance to us as it allows us to enhance the quality of the services we provide."
                         + "<br/><br/>We appreciate the time you've taken to share your thoughts with us, and please be assured that your feedback will be instrumental in our ongoing efforts to improve our services."
                         + "<br/><br/>If you have any further comments or suggestions, please do not hesitate to reach out. We are always eager to hear from you."
                         + "<br/><br/>Thank you again for your valuable input."
                         + "<br/><br/>Kind regards,<br/>The Turbo Meals Family";

                    var message = new MailMessage();
                    message.To.Add(new MailAddress(email));
                    message.From = new MailAddress("turbomeals123@gmail.com"); // Replace with your email address
                    message.Subject = "Turbo Meals Driver Rating Completed";
                    message.Body = body;
                    message.IsBodyHtml = true;

                    using (var smtp = new SmtpClient())
                    {
                        smtp.Send(message);
                    }

                    if (model.IsCustomTip)
                    {
                        return Content("<script>" +
                                               "function callPayPal() {" +
                                               "window.location.href = 'https://www.sandbox.paypal.com/cgi-bin/webscr?cmd=_xclick&amount=" + model.CustomTip.ToString() + "&business=sb-w3cyw20367505@business.example.com&item_name=DriverTip&return=https://2023grp01a.azurewebsites.net/Account/CompletedRating';" +
                                               "}" +
                                               "callPayPal();" +
                                               "</script>");
                    }


                }
                else
                {
                    return HttpNotFound();
                }
            }
            return View("CompletedRating");
        }


        #endregion

        #region Waiter Rating
        public ActionResult WaiterRating(string OrderNo)
        {
            TempData["wOrderNo"] = OrderNo;
            var completedRating = db.TblInStoreOrders.SingleOrDefault(m => m.OrderNumber == OrderNo);

            if (completedRating.waiterRated)
            {
                return View("CompletedRating");
            }
            else
            {
                TempData["OrderNo"] = OrderNo;

                var qry = from inStore in db.TblInStoreOrders
                          join employee in db.tblEmployees
                          on inStore.WaiterID equals employee.UserID
                          where inStore.OrderNumber == OrderNo
                          select new
                          {
                              WaiterName = employee.EmpName,
                              WaiterEmail = employee.TblUser.Email,
                              WaiterImage = employee.EmpImage,
                              WaiterAvgRating = employee.avgRating

                          };

                var result = qry.SingleOrDefault();

                if (result == null)
                {
                    return HttpNotFound();
                }

                string waiterName = result.WaiterName;
                string waiterEmail = result.WaiterEmail;
                string waiterImage = result.WaiterImage;
                double AvgRating = result.WaiterAvgRating;
                int ratingProvided = 1;



                var vmodel = new WaiterRatingVM
                {
                    WaiterName = waiterName,
                    WaiterEmail = waiterEmail,
                    WaiterImage = waiterImage,
                    WaiterAvgRating = AvgRating,
                    rating = ratingProvided
                };

                return View(vmodel);
            }
        }

        [HttpPost]
        public ActionResult WaiterRating(WaiterRatingVM vm)
        {
            if (ModelState.IsValid)
            {
                string order = (string)TempData["wOrderNo"];

                var qry = from inStore in db.TblInStoreOrders
                          join employee in db.tblEmployees
                          on inStore.WaiterID equals employee.UserID
                          where inStore.OrderNumber == order
                          select new
                          {
                              WaiterName = employee.EmpName,
                              WaiterEmail = employee.TblUser.Email,
                              WaiterImage = employee.EmpImage,
                              WaiterAvgRating = employee.avgRating

                          };

                var result = qry.SingleOrDefault();
                var emp = db.tblEmployees.FirstOrDefault(m => m.EmpName == result.WaiterName);
                var completedRating = db.TblInStoreOrders.SingleOrDefault(m => m.OrderNumber == order);
                var email = completedRating.Email;

                if (emp != null)
                {
                    //tips
                    if (vm.IsCustomTip)
                    {

                        emp.Tips += vm.CustomTip;
                    }
                    else
                    {

                        emp.Tips += 0;
                    }

                    //rating
                    if (vm.rating == 1)
                    {
                        emp.Rating1 += 1;
                    }
                    else
                      if (vm.rating == 2)
                    {
                        emp.Rating2 += 1;
                    }
                    else
                      if (vm.rating == 3)
                    {
                        emp.Rating3 += 1;
                    }
                    else
                      if (vm.rating == 4)
                    {
                        emp.Rating4 += 1;
                    }
                    else
                      if (vm.rating == 5)
                    {
                        emp.Rating5 += 1;
                    }

                    var sum = emp.Rating1 + emp.Rating2 + emp.Rating3 + emp.Rating4 + emp.Rating5;
                    emp.avgRating = sum / 5.00;

                    completedRating.waiterRated = true;

                    db.SaveChanges();

                    if (vm.IsCustomTip)
                    {
                        return Content("<script>" +
                                               "function callPayPal() {" +
                                               "window.location.href = 'https://www.sandbox.paypal.com/cgi-bin/webscr?cmd=_xclick&amount=" + vm.CustomTip.ToString() + "&business=sb-w3cyw20367505@business.example.com&item_name=DriverTip&return=https://2023grp01a.azurewebsites.net/Account/CompletedRating';" +
                                               "}" +
                                               "callPayPal();" +
                                               "</script>");
                    }
                }

                //send thank you email
                var body = "Dear Turbo Meals Customer,<br/><br/>" +
                     "We are writing to express our gratitude for your recent feedback regarding our waiter's performance. Your opinion is of great importance to us as it allows us to enhance the quality of the services we provide."
                     + "<br/><br/>We appreciate the time you've taken to share your thoughts with us, and please be assured that your feedback will be instrumental in our ongoing efforts to improve our services."
                     + "<br/><br/>If you have any further comments or suggestions, please do not hesitate to reach out. We are always eager to hear from you."
                     + "<br/><br/>Thank you again for your valuable input."
                     + "<br/><br/>Kind regards,<br/>The Turbo Meals Family";

                var message = new MailMessage();
                message.To.Add(new MailAddress(email));
                message.From = new MailAddress("turbomeals123@gmail.com"); // Replace with your email address
                message.Subject = "Turbo Meals Waiter Rating Completed";
                message.Body = body;
                message.IsBodyHtml = true;

                using (var smtp = new SmtpClient())
                {
                    smtp.Send(message);
                }

            }
            return View("CompletedRating");
        }

        #endregion

        #region Admin and User View Profile and edit account
        public ActionResult AccProfile()
        {
            int userID = Convert.ToInt16(Session["uid"]);
            tblAccProfile user = db.TblAccProfiles.SingleOrDefault(m => m.UserID == userID);

            if (user != null)
            {
                return View(user);
            }
            else
            {
                // Handle the case where the user is not found (e.g., show an error message)
                return RedirectToAction("Index", "Home"); // Redirect to a different page if needed
            }

        }


        public ActionResult profileEdit()
        {
            int userID = Convert.ToInt16(Session["uid"]);
            TempData["id"] = userID;
            var query = db.TblAccProfiles.SingleOrDefault(m => m.UserID == userID);
            return View(query);
        }

        [HttpPost]
        public ActionResult profileEdit(tblAccProfile ap, HttpPostedFileBase Image)
        {
            int id = Convert.ToInt16(TempData["id"]);

            try
            {
                // Fetch the current entity from the database
                var currentEntity = db.TblAccProfiles.FirstOrDefault(m => m.UserID == id);

                if (currentEntity != null)
                {
                    if (Image != null)
                    {
                        // Update the EmpImage field only
                        currentEntity.userProfileImage = Image.FileName.ToString();
                        var folder = Server.MapPath("~/Uploads/");
                        Image.SaveAs(Path.Combine(folder, Image.FileName.ToString()));
                    }

                    // Save changes
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                TempData["msg"] = ex;
            }
            return RedirectToAction("AccProfile");
        }

        #endregion

        public ActionResult Thanks()
        {
            return View();
        }

        public ActionResult CompletedRating()
        {
            return View();
        }


    }
}