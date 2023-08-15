using System.Linq;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
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
                    FormsAuthentication.SetAuthCookie(query.Email, false);
                    Session["User"] = query.Name;
                    return RedirectToAction("GetAllOrderDetail", "Home");
                }
                else if (query.RoleType == 2)
                {
                    Session["uid"] = query.UserId;
                    FormsAuthentication.SetAuthCookie(query.Email, false);
                    Session["User"] = query.Name;
                    return RedirectToAction("Index", "Home");
                }
                else if (query.RoleType == 3)
                {
                    Session["uid"] = query.UserId;
                    FormsAuthentication.SetAuthCookie(query.Email, false);
                    Session["User"] = query.Name;
                    return RedirectToAction("DriverDeliveries", "Home", new { id = @Session["uid"] });
                }
                else if (query.RoleType == 4)
                {
                    Session["uid"] = query.UserId;
                    FormsAuthentication.SetAuthCookie(query.Email, false);
                    Session["User"] = query.Name;
                    return RedirectToAction("PrepStaff", "Home", new { id = @Session["uid"] });
                }

            }
            else
            {
                TempData["msg"] = "Invalid Username or Password";
            }

            return View();
        }

        #endregion

        #region logout 

        public ActionResult Signout()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("Index", "Home");
        }

        #endregion
    }
}