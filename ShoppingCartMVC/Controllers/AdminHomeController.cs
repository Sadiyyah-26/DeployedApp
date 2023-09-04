using ShoppingCartMVC.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ShoppingCartMVC.Controllers
{
    public class AdminHomeController : Controller
    {
        dbOnlineStoreEntities db=new dbOnlineStoreEntities();

        List<AdminCart> Ali = new List<AdminCart>();

        #region Home Page in showing all Supplier Ingredients 

        public ActionResult Index(int id)
        {

            if (TempData["admincart"] != null)
            {
                double x = 0;

                List<AdminCart> Ali2 = TempData["admincart"] as List<AdminCart>;
                foreach (var item in Ali2)
                {
                    x += item.bill;

                }
                TempData["admintotal"] = x;
                TempData["admin_item_count"] = Ali2.Count();
            }
            TempData.Keep();

            var qry = db.tblSuppliers.SingleOrDefault(m => m.SupplierId == id);

            var suppliedIngredients = db.SupplierIngredients
     .Where(si => si.SupplierId == id)
     .Join(db.tblIngredients, si => si.Ing_ID, i => i.Ing_ID, (si, i) => new IngrVM { ID = si.Ing_ID, IngName = i.Ing_Name, Ing_Image = i.Ing_Image, Ing_UnitCost = si.Ing_UnitCost })
     .ToList();


            ViewBag.ingrList = suppliedIngredients;
            return View(qry);
        }

        #endregion


        #region Prep Staff Add to Cart

        public ActionResult AdminAddtoCart(int id)
        {
            var query = db.tblIngredients.Where(x => x.Ing_ID == id).SingleOrDefault();
            var ingCost = db.SupplierIngredients.SingleOrDefault(n => n.Ing_ID == id)?.Ing_UnitCost;
            TempData["unit"] = ingCost;
            var supID = db.SupplierIngredients.SingleOrDefault(m => m.Ing_ID == id)?.SupplierId;
            TempData["sID"] = supID;
            return View(query);
        }

        [HttpPost]
        public ActionResult AdminAddtoCart(int id, int qty)
        {
            tblIngredients p = db.tblIngredients.Where(x => x.Ing_ID == id).SingleOrDefault();
            var ingCost = db.SupplierIngredients.SingleOrDefault(n => n.Ing_ID == id)?.Ing_UnitCost;
            var supID = db.SupplierIngredients.SingleOrDefault(m => m.Ing_ID == id)?.SupplierId;

            AdminCart ac = new AdminCart();
            ac.ingrid = id;
            ac.ingrname = p.Ing_Name;
            ac.price = (double)ingCost;
            ac.qty = Convert.ToInt32(qty);
            ac.bill = ac.price * ac.qty;
            if (TempData["admincart"] == null)
            {
                Ali.Add(ac);
                TempData["admincart"] = Ali;
            }
            else
            {
                List<AdminCart> Ali2 = TempData["admincart"] as List<AdminCart>;
                int flag = 0;
                foreach (var item in Ali2)
                {
                    if (item.ingrid == ac.ingrid)
                    {
                        item.qty += ac.qty;
                        item.bill += ac.bill;
                        flag = 1;
                    }

                }
                if (flag == 0)
                {
                    Ali2.Add(ac);
                    //new item
                }
                TempData["admincart"] = Ali2;

            }

            TempData.Keep();
            TempData["sID"] = supID;
            return RedirectToAction("Index", new { id = supID });
        }

        #endregion


        #region Prep Staff Remove Cart Ingredients

        public ActionResult remove(int? id)
        {
            if (TempData["admincart"] == null)
            {
                TempData.Remove("admintotal");
                TempData.Remove("admincart");
            }
            else
            {
                List<AdminCart> Ali2 = TempData["admincart"] as List<AdminCart>;
                AdminCart ac = Ali2.Where(x => x.ingrid == id).SingleOrDefault();
                Ali2.Remove(ac);
                double s = 0;
                foreach (var item in Ali2)
                {
                    s += item.bill;
                }
                TempData["admintotal"] = s;

            }
            var supID = db.SupplierIngredients.SingleOrDefault(m => m.Ing_ID == id)?.SupplierId;
            TempData["sID"] = supID;
            return RedirectToAction("Index", new { id = supID });
        }
        #endregion


        #region Prep Staff Checkout Code

        public ActionResult AdminCheckout()
        {
            TempData["Supplier"] = TempData["sID"];
            TempData.Keep();
            return View();
        }

        [HttpPost]
        public ActionResult AdminCheckout(string contact, string address)
        {

            //DateTime currentTime = DateTime.Now;
            //DateTime cutoffStartTime = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, 0, 0, 0); // set the cutoff start time to 12:00 AM
            //DateTime cutoffEndTime = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, 8, 0, 0); // set the cutoff end time to 8:00 AM

            //if (currentTime >= cutoffStartTime && currentTime < cutoffEndTime) // check if the current time is after or equal to the cutoff start time and before the cutoff end time
            //{
            //    // throw an error if it's cutOffTime
            //    return RedirectToAction("Error", "Home");
            //}
            //else
            //{
                // perform some action if it's not cutOffTime
                // ...
                if (ModelState.IsValid)
                {
                    List<AdminCart> Ali2 = TempData["admincart"] as List<AdminCart>;
                    tblAdminInvoice aiv = new tblAdminInvoice();
                    aiv.UserId = Convert.ToInt32(Session["uid"].ToString());
                    aiv.InvoiceDate = System.DateTime.Now;
                    aiv.Bill = (double)TempData["admintotal"];
                    aiv.Payment = "PayPal";
                    aiv.SupplierId = (int)TempData["Supplier"];

                    db.tblAdminInvoices.Add(aiv);
                    db.SaveChanges();
                    TempData["id"] = aiv.InvoiceId;
                    //order book
                    foreach (var item in Ali2)
                    {
                        tblAdminOrder od = new tblAdminOrder();
                        od.IngrID = item.ingrid;
                        od.Contact = contact;
                        od.Address = address;
                        od.OrderDate = System.DateTime.Now;
                        od.InvoiceId = aiv.InvoiceId;
                        od.Qty = item.qty;
                        od.Unit = Convert.ToInt32(item.price);
                        od.Total = Convert.ToInt32(item.bill);
                        od.SupplierId = (int)TempData["Supplier"];
                        db.tblAdminOrders.Add(od);
                    }

                    db.SaveChanges();
                    TempData.Remove("admintotal");
                    TempData.Remove("admincart");
                    // TempData["msg"] = "Order Book Successfully!!";
                    //relook at paypal link entering order shouldnt be coming up
                    return Content("<script>" +
                "function callPayPal() {" +
                "var InvID = " + (int)TempData["id"] + ";" +
                "window.location.href = 'https://www.sandbox.paypal.com/cgi-bin/webscr?cmd=_xclick&amount=" + aiv.Bill.ToString() + "&business=sb-w3cyw20367505@business.example.com&item_name=FoodOrder&return=https://localhost:44377/AdminHome/AdminInvoice?InvID=' + InvID;" +
                "}" +
                "callPayPal();" +
             "</script>");

                }

            TempData.Keep();
            return View();

        }

        #endregion


        #region All Prep Staff Orders with Suppliers for Ingredient Stock 

        public ActionResult AdminGetAllOrderDetail()
        {
            var query = db.tblAdminOrders
                .Where(n => n.OrderStatus == "Ordered")
                .GroupBy(m => m.InvoiceId)
                .Select(o => o.FirstOrDefault())
                .ToList();

            return View(query);
        }

        #endregion


        #region Returned Orders with Supplier
        public ActionResult ReturnedOrdersList()
        {
            var query = db.tblAdminOrders
                .Where(n => n.OrderStatus == "Returned")
                .GroupBy(m => m.InvoiceId)
                .Select(o => o.FirstOrDefault())
                .ToList();

            return View(query);
        }

        [HttpPost]
        public ActionResult ReturnedOrdersList(int id)
        {
            var query = db.tblAdminOrders.Find(id);
            if (query != null)
            {
                query.OrderStatus = "Ordered";
                query.OrderDate = System.DateTime.Now;
            }
            db.SaveChanges();
            return RedirectToAction("AdminGetAllOrderDetail");
        }
        #endregion


        #region Received Orders List from Supplier
        public ActionResult ReceivedOrdersList()
        {
            var query = db.tblAdminOrders
                .Where(n => n.OrderStatus == "Received")
                .GroupBy(m => m.InvoiceId)
                .Select(o => o.FirstOrDefault())
                .ToList();

            return View(query);
        }
        #endregion


        #region  Confirm Order with Supplier

        public ActionResult SupplierConfirmOrder(int InvoiceID)
        {
            TempData["Inv"] = InvoiceID;
            var query = db.tblAdminOrders.FirstOrDefault(m => m.InvoiceId == InvoiceID);
            return View(query);
        }

        [HttpPost]
        public ActionResult SupplierConfirmOrder(tblAdminOrder o, string[] inOrder)
        {
            int InvoiceId = (int)TempData["Inv"];


            var ordersSameInvoiceId = db.tblAdminOrders.Where(order => order.InvoiceId == InvoiceId).ToList();

            if (ordersSameInvoiceId.Any())
            {
                if (inOrder != null && inOrder.Contains("Condition1") && inOrder.Contains("Condition2") && inOrder.Contains("Condition3"))
                {

                    foreach (var orderToUpdate in ordersSameInvoiceId)
                    {
                        orderToUpdate.OrderStatus = "Received";
                        int qty = (int)orderToUpdate.Qty;
                        db.Entry(orderToUpdate).State = EntityState.Modified;

                        int ingredientId = (int)orderToUpdate.IngrID;


                        var ingredient = db.tblIngredients.FirstOrDefault(i => i.Ing_ID == ingredientId);

                        if (ingredient != null)
                        {
                            ingredient.Ing_StockyQty += qty;
                            ingredient.StockStatus = "In Stock";
                            // Handle the found Ingredient record
                        }


                    }
                }
                else
                {
                    string returnReason = "";
                    if ((!inOrder.Contains("Condition2")) && (!inOrder.Contains("Condition3")))
                    {
                        returnReason = "Inventory recieved is damaged and quantity discrepancies";
                    }
                    else
                    if (!inOrder.Contains("Condition2"))
                    {
                        returnReason = "Inventory received is damaged/not fresh";
                    }
                    else
                    {
                        returnReason = "Quantity discrepancies";
                    }
                    // Conditions are not met, update the status for each order
                    foreach (var orderToUpdate in ordersSameInvoiceId)
                    {
                        orderToUpdate.OrderStatus = "Returned";
                        orderToUpdate.ReturnReason = returnReason;
                        db.Entry(orderToUpdate).State = EntityState.Modified;
                    }
                }

                db.SaveChanges();
            }


            return RedirectToAction("AdminGetAllOrderDetail");
        }


        #endregion


        #region Prep Staff Invoice
        public ActionResult AdminInvoice()
        {
            int InvID = Convert.ToInt32(Request.QueryString["InvID"]);
            List<AdminInv_VM> AinvList = new List<AdminInv_VM>();

            var adminInvList = (from ao in db.tblAdminOrders
                                join ai in db.tblAdminInvoices on ao.InvoiceId equals ai.InvoiceId
                                join s in db.tblSuppliers on ai.SupplierId equals s.SupplierId
                                join i in db.tblIngredients on ao.IngrID equals i.Ing_ID
                                where ai.InvoiceId == InvID
                                select new
                                {
                                    ai.InvoiceId,
                                    ai.InvoiceDate,
                                    ai.Payment,
                                    s.SupplName,
                                    s.ContactPerson,
                                    s.ContactPersonPos,
                                    s.ContactNum,
                                    s.Email,
                                    s.Tel,
                                    s.PhysicalAddress,
                                    ao.OrderId,
                                    ao.OrderDate,
                                    i.Ing_Name,
                                    ao.Contact,
                                    ao.Address,
                                    ao.Unit,
                                    ao.Qty,
                                    ao.Total

                                }).ToList();

            var orderRecs = adminInvList.GroupBy(m => m.InvoiceId);

            foreach (var order in orderRecs)
            {
                // Create a ViewModel for each group
                AdminInv_VM objIVM = new AdminInv_VM();

                // You can aggregate data here if needed (e.g., summing up quantities)

                // Assign common values to the ViewModel (since they are the same for all records in the group)
                objIVM.InvoiceId = order.Key; // Group Key is the InvoiceId
                objIVM.InvoiceDate = order.First().InvoiceDate;
                objIVM.OrderDate = order.First().OrderDate;
                objIVM.Payment = order.First().Payment;
                objIVM.SupplName = order.First().SupplName;
                objIVM.ContactPerson = order.First().ContactPerson;
                objIVM.ContactPersonPos = order.First().ContactPersonPos;
                objIVM.ContactNum = order.First().ContactNum;
                objIVM.Email = order.First().Email;
                objIVM.Tel = order.First().Tel;
                objIVM.PhysicalAddress = order.First().PhysicalAddress;

                objIVM.Contact = order.First().Contact;
                objIVM.Address = order.First().Address;



                // Create a list of items within the invoice
                objIVM.Items = order.Select(item => new AdminInv_VM.Item
                {
                    OrderId = item.OrderId,
                    Ing_Name = item.Ing_Name,
                    Unit = item.Unit,
                    Qty = item.Qty,
                    Total = item.Total
                }).ToList();

                AinvList.Add(objIVM);
            }

            return View(AinvList);
        }
        #endregion



    }
}