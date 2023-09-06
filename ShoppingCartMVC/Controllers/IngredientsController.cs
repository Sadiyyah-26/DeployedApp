using ShoppingCartMVC.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;

namespace ShoppingCartMVC.Controllers
{
    public class IngredientsController : Controller
    {
        dbOnlineStoreEntities db=new dbOnlineStoreEntities();

        #region All Ingredients View
        // GET: Ingredients
        public ActionResult IngIdex()
        {
            var allIngr = db.tblIngredients.ToList();

            return View(allIngr);
        }
        #endregion


        #region Ingredients for each Product View
        public ActionResult IngProIndex()
        {
            var all = db.IngredientProducts.ToList();
            var groupedIng = all.GroupBy(m => m.ProID).ToList();
            return View(groupedIng);
        }
        #endregion


        #region Supplier for each Ingredient View
        public ActionResult SupplIngIndex()
        {
            var ing = db.SupplierIngredients.ToList();
            return View(ing);
        }
        #endregion


        #region Add Ingredients
        public ActionResult AddIngr()
        {
            return View();
        }

        [HttpPost]
        public ActionResult AddIngr(tblIngredients i, HttpPostedFileBase Image)
        {
            if (ModelState.IsValid)
            {
                tblIngredients ing = new tblIngredients();
                ing.Ing_Name = i.Ing_Name;
                ing.Ing_UnitsUsed = i.Ing_UnitsUsed;
                if(Image!=null)
                {
                    ing.Ing_Image = Image.FileName.ToString();
                }
               
                ing.Ing_StockyQty = i.Ing_StockyQty;


                //image upload
                if(Image!=null)
                {
                    var folder = Server.MapPath("~/Uploads/");
                    Image.SaveAs(Path.Combine(folder, Image.FileName.ToString()));
                }

                if (ing.Ing_StockyQty == 0)
                {
                    ing.StockStatus = "Low Stock";

                    var content = $"New ingredient has been added to the system.<br/><br/> ";
                    content += "Ingredient: " + ing.Ing_Name + "  " + " current stock quantity is 0.<br/><br/>";
                    content += "*Reminder to assign a Supplier from existing Supplier list for this ingredient/add a new Supplier for ingredient, and place Stock order.";



                    var email = new MailMessage();
                    email.To.Add(new MailAddress("Turbostaff786@gmail.com"));
                    email.From = new MailAddress("turbomeals123@gmail.com");
                    email.Subject = "New Ingredient Added!";
                    email.Body = content;
                    email.IsBodyHtml = true;

                    using (var smtp = new SmtpClient())
                    {
                        smtp.Send(email);
                    }
                }

                db.tblIngredients.Add(ing);
                db.SaveChanges();



                return RedirectToAction("IngIdex");
            }
            else
            {
                TempData["msg"] = "Ingredient Not Inserted ";
            }
            return View();
        }

        #endregion


        #region Edit Ingredient Info
        public ActionResult EditIngr(int id)
        {
            var query = db.tblIngredients.SingleOrDefault(m => m.Ing_ID == id);
            return View(query);
        }

        [HttpPost]
        public ActionResult EditIngr(tblIngredients i, HttpPostedFileBase Image)
        {
            try
            {
                if(Image!=null)
                {
                    i.Ing_Image = Image.FileName.ToString();
                    var folder = Server.MapPath("~/Uploads/");
                    Image.SaveAs(Path.Combine(folder, Image.FileName.ToString()));
                }
               


                if ((i.Ing_StockyQty < 50) && (i.StockStatus == "In Stock"))
                {
                    i.StockStatus = "Low Stock";
                }

                db.Entry(i).State = EntityState.Modified;
                db.SaveChanges();

                return RedirectToAction("IngIdex");
            }
            catch (Exception ex)
            {
                TempData["msg"] = ex;
            }
            return RedirectToAction("IngIdex");
        }
        #endregion


        #region Update Supplier Ingredient Unit Cost
        public ActionResult UpdateUnitCost(int id)
        {
            TempData["id"] = id;
            var qry = db.SupplierIngredients.SingleOrDefault(m => m.Ing_ID == id);
            return View(qry);
        }

        [HttpPost]
        public ActionResult UpdateUnitCost(double unitCost)
        {
            // Retrieve the id from TempData and cast it to an int
            if (TempData["id"] != null && int.TryParse(TempData["id"].ToString(), out int id))
            {
                SupplierIngredients qry = db.SupplierIngredients.SingleOrDefault(m => m.Ing_ID == id);

                if (qry != null)
                {
                    qry.Ing_UnitCost = unitCost;
                    db.SaveChanges();
                }
            }

            return RedirectToAction("SupplIngIndex");
        }



        #endregion

        #region Showing Low Stock Ingredients for Kitchen Staff
        //Ingrediemts that are low for customer ordering and needs topup
        public ActionResult LowStock()
        {
            var lowStockIngredients = db.SupplierIngredients
        .Join(db.tblIngredients,
              si => si.Ing_ID,
              i => i.Ing_ID,
              (si, i) => new
              {
                  si.SupplierId,
                  si.Ing_ID,
                  i.StockStatus,
                  i.Ing_Name,
                  i.Ing_StockyQty,
                  i.Ing_Image
              })
        .Join(db.tblSuppliers,
              info => info.SupplierId,
              s => s.SupplierId,
              (info, s) => new LowStockVM
              {
                  SupplierId = info.SupplierId,
                  Ing_ID = info.Ing_ID,
                  Ing_Name = info.Ing_Name,
                  StockQty = info.Ing_StockyQty,
                  StockStatus = info.StockStatus,
                  IngImage = info.Ing_Image,
                  SupplName = s.SupplName
              })
        .Where(info => info.StockStatus == "Low Stock" &&
                   !db.tblAdminOrders.Any(order => order.IngrID == info.Ing_ID && order.OrderStatus == "Ordered"))
        .ToList();

            return View(lowStockIngredients);
        }

        #endregion

        //include delete ingredient code


    }
}