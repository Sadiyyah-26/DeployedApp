using ShoppingCartMVC.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ShoppingCartMVC.Controllers
{
    public class SupplierController : Controller
    {
        dbOnlineStoreEntities db=new dbOnlineStoreEntities();

        #region Showing Suppliers for Prep Staff

        public ActionResult Index()
        {
            var query = db.tblSuppliers.ToList();

            return View(query);
        }

        #endregion


        #region Add Suppliers

        public ActionResult Create()
        {
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
        public ActionResult Create(tblSupplier c,string supplType, string Active, string[] selectedIngrs, Dictionary<string, string> unitCosts)
        {
            if (ModelState.IsValid)
            {
                tblSupplier supp = new tblSupplier();
                supp.SupplName = c.SupplName;
                supp.SupplType = supplType;
                supp.ContactPerson = c.ContactPerson;
                supp.ContactPersonPos = c.ContactPersonPos;
                supp.ContactNum = c.ContactNum;
                supp.Email = c.Email;
                supp.Tel = c.Tel;
                supp.CompanyRegNum = c.CompanyRegNum;
                supp.PhysicalAddress = c.PhysicalAddress;
                supp.Active = Active;
                db.tblSuppliers.Add(supp);

                foreach (var ingrName in selectedIngrs)
                {
                    var ingredient = db.tblIngredients.FirstOrDefault(i => i.Ing_Name == ingrName);
                    if (ingredient != null)
                    {
                        double unitCost = 0;
                        if (unitCosts.ContainsKey(ingrName))
                        {
                            double.TryParse(unitCosts[ingrName], out unitCost);
                        }

                        var ingredientProduct = new SupplierIngredients
                        {
                            SupplierId = supp.SupplierId,
                            Ing_ID = ingredient.Ing_ID,

                            Ing_UnitCost = unitCost
                        };


                        db.SupplierIngredients.Add(ingredientProduct);


                    }
                }

                db.SaveChanges();
                return RedirectToAction("Index");
            }
            else
            {
                TempData["msg"] = "Supplier Not Inserted ";
            }
            return View();
        }

        #endregion


        #region Edit Supplier

        public ActionResult Edit(int id)
        {
            var query = db.tblSuppliers.SingleOrDefault(m => m.SupplierId == id);

            List<tblIngredients> allIngredients = db.tblIngredients.ToList();

            List<int> selectedIngredientIds = db.SupplierIngredients
        .Where(ip => ip.SupplierId == id)
        .Select(ip => ip.Ing_ID)
        .ToList();



            ViewBag.AllIngredients = allIngredients;
            ViewBag.SelectedIngredientIds = selectedIngredientIds;

            return View(query);
        }

        [HttpPost]
        public ActionResult Edit(tblSupplier c, int[] selectedIngrs)
        {
            try
            {

                db.Entry(c).State = EntityState.Modified;

                var existingIngredients = db.SupplierIngredients.Where(ip => ip.SupplierId == c.SupplierId);
                db.SupplierIngredients.RemoveRange(existingIngredients);

                // Add selected ingredients to the product
                if (selectedIngrs != null)
                {
                    foreach (var ingredientId in selectedIngrs)
                    {
                        db.SupplierIngredients.Add(new SupplierIngredients
                        {
                            SupplierId = c.SupplierId,
                            Ing_ID = ingredientId,

                        });
                    }
                }

                db.SaveChanges();

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["msg"] = ex;
            }
            return RedirectToAction("Index");
        }

        #endregion

        #region Delete Supplier

        public ActionResult Delete(int id)
        {
            var query = db.tblSuppliers.SingleOrDefault(m => m.SupplierId == id);
            db.tblSuppliers.Remove(query);
            db.SaveChanges();
            return RedirectToAction("Index");
        }



        #endregion

        #region Supplier Details
        public ActionResult Details(int id)
        {
            var selectedIngredients = db.SupplierIngredients
     .Where(ip => ip.SupplierId == id)
     .Join(db.tblIngredients, ip => ip.Ing_ID, ing => ing.Ing_ID, (ip, ing) => ing.Ing_Name)
     .ToList();

            ViewBag.SelectedIngredientNames = selectedIngredients;
            var query = db.tblSuppliers.SingleOrDefault(m => m.SupplierId == id);
            return View(query);
        }
        #endregion


    }
}