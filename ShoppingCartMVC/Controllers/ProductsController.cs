using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ShoppingCartMVC.Models;
using System.IO;
using System.Data;
using System.Data.Entity;

namespace ShoppingCartMVC.Controllers
{
    public class ProductsController : Controller
    {
        dbOnlineStoreEntities db = new dbOnlineStoreEntities();

        #region showing all products for admin 

        public ActionResult Index()
        {
            var products = db.tblProducts.ToList();
            
            return View(products);
        }

        #endregion


        #region Admin Adds Products

        public ActionResult Create()
        {
            List<tblCategory> list = db.tblCategories.ToList();
            ViewBag.CatList = new SelectList(list, "CatId", "Name");

            var ingredients = db.tblIngredients
       .ToList() // Materialize the data
       .Select(m => new
       {
           ingName = m.Ing_Name,

       })
       .ToList();

            // Perform the conversion in-memory
            var ingrViewModel = ingredients.Select(m => new IngrVM
            {
                IngName = m.ingName,

            })
            .ToList();



            ViewBag.IngrNames = ingrViewModel;

            return View();
        }



        [HttpPost]
        public ActionResult Create(tblProduct p, HttpPostedFileBase Image, string[] selectedIngrs)
        {
            List<tblCategory> list = db.tblCategories.ToList();
            ViewBag.CatList = new SelectList(list, "CatId", "Name");

            if (ModelState.IsValid)
            {


                tblProduct pro = new tblProduct();
                pro.P_Name = p.P_Name;
                pro.Description = p.Description;
                pro.Unit = p.Unit;
                pro.Image = Image.FileName.ToString();
                pro.CatId = p.CatId;

                //image upload
                var folder = Server.MapPath("~/Uploads/");
                Image.SaveAs(Path.Combine(folder, Image.FileName.ToString()));

                db.tblProducts.Add(pro);

                if (selectedIngrs != null)
                {
                    foreach (var ingrName in selectedIngrs)
                    {
                        var ingredient = db.tblIngredients.FirstOrDefault(i => i.Ing_Name == ingrName);
                        if (ingredient != null)
                        {
                            var ingredientProduct = new IngredientProduct
                            {
                                ProID = pro.ProID,
                                Ing_ID = ingredient.Ing_ID,
                                /* Quantity = 1*/ // Adjust as needed
                            };
                            db.IngredientProducts.Add(ingredientProduct);
                        }
                    }
                }


                db.SaveChanges();

                return RedirectToAction("Index");
            }
            else
            {
                TempData["msg"] = "Product Not Upload";
            }
            return View();
        }


        #endregion


        #region Edit Products

        public ActionResult Edit(int id)
        {

            List<tblCategory> list = db.tblCategories.ToList();
            ViewBag.CatList = new SelectList(list, "CatId", "Name");

            var query = db.tblProducts.SingleOrDefault(m => m.ProID == id);

            List<tblIngredients> allIngredients = db.tblIngredients.ToList();
            List<int> selectedIngredientIds = db.IngredientProducts
        .Where(ip => ip.ProID == id)
        .Select(ip => ip.Ing_ID)
        .ToList();

            ViewBag.AllIngredients = allIngredients;
            ViewBag.SelectedIngredientIds = selectedIngredientIds;

            return View(query);
        }


        [HttpPost]
        public ActionResult Edit(tblProduct p, HttpPostedFileBase Image, int[] selectedIngrs)
        {
            List<tblCategory> list = db.tblCategories.ToList();
            ViewBag.CatList = new SelectList(list, "CatId", "Name");

           

            try
            {

                p.Image = Image.FileName.ToString();
                var folder = Server.MapPath("~/Uploads/");
                Image.SaveAs(Path.Combine(folder, Image.FileName.ToString()));
                db.Entry(p).State = EntityState.Modified;

                var existingIngredients = db.IngredientProducts.Where(ip => ip.ProID == p.ProID);
                db.IngredientProducts.RemoveRange(existingIngredients);

                // Add selected ingredients to the product
                if (selectedIngrs != null)
                {
                    foreach (var ingredientId in selectedIngrs)
                    {
                        db.IngredientProducts.Add(new IngredientProduct
                        {
                            ProID = p.ProID,
                            Ing_ID = ingredientId
                        });
                    }
                }

                db.SaveChanges();

            }
            catch (Exception ex)
            {
                TempData["msg"] = ex;
            }

            return RedirectToAction("Index");

        }

        #endregion


        #region Delete Product 

        public ActionResult Delete(int id)
        {
            var product = db.tblProducts.SingleOrDefault(m => m.ProID == id);

            // Update the foreign key column in the orders table to null
            foreach (var order in product.tblOrders.ToList())
            {
                order.ProID = null;
            }

            db.tblProducts.Remove(product);
            db.SaveChanges();


            return RedirectToAction("Index");

        }

        #endregion


        //extras 
        #region all extras

        public ActionResult ExIndex()
        {
            var query = db.tblExtras.ToList();
            return View(query);
        }

        #endregion

        #region adding extras 
        public ActionResult ExCreate()
        {
            List<tblCategory> list = db.tblCategories.ToList();
            ViewBag.CatList = new SelectList(list, "CatId", "Name");
            return View();
        }
        [HttpPost]
        public ActionResult ExCreate(tblExtras e)
        {
            List<tblCategory> list = db.tblCategories.ToList();
            ViewBag.CatList = new SelectList(list, "CatId", "Name");

            if (ModelState.IsValid)
            {
                tblExtras ex = new tblExtras();
                ex.exName = e.exName;
                ex.exCost = e.exCost;
                ex.CatId = e.CatId;
                db.tblExtras.Add(e);
                db.SaveChanges();
                return RedirectToAction("ExIndex");
            }
            else
            {
                TempData["msg"] = "Extra Not Added ";
            }
            return View();
        }

        #endregion

        #region edit extras
        public ActionResult ExEdit(int id)
        {
            List<tblCategory> list = db.tblCategories.ToList();
            ViewBag.CatList = new SelectList(list, "CatId", "Name");

            var query = db.tblExtras.SingleOrDefault(m => m.ExtraID == id);
            return View(query);
        }

        [HttpPost]
        public ActionResult ExEdit(tblExtras e)
        {
            List<tblCategory> list = db.tblCategories.ToList();
            ViewBag.CatList = new SelectList(list, "CatId", "Name");
            try
            {

                db.Entry(e).State = EntityState.Modified;
                db.SaveChanges();

                return RedirectToAction("ExIndex");
            }
            catch (Exception ex)
            {
                TempData["msg"] = ex;
            }
            return RedirectToAction("ExIndex");

        }
        #endregion

        #region delete extras
        public ActionResult ExDelete(int id)
        {
           var query=db.tblExtras.SingleOrDefault(m=>m.ExtraID == id);
            db.tblExtras.Remove(query);
            db.SaveChanges();

            return RedirectToAction("ExIndex");
        }
        #endregion

    }
}