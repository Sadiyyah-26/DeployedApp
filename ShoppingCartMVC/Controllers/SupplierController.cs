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

        #region showing suppliers for admin

        public ActionResult Index()
        {
            var query = db.tblSuppliers.ToList();

            return View(query);
        }

        #endregion


        #region add suppliers

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Create(tblSupplier c)
        {
            if (ModelState.IsValid)
            {
                tblSupplier supp = new tblSupplier();
                supp.Name = c.Name;
                db.tblSuppliers.Add(supp);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            else
            {
                TempData["msg"] = "Not Inserted Supplier";
            }
            return View();
        }

        #endregion


        #region edit supplier

        public ActionResult Edit(int id)
        {
            var query = db.tblSuppliers.SingleOrDefault(m => m.SupplierId == id);
            return View(query);
        }

        [HttpPost]
        public ActionResult Edit(tblSupplier c)
        {
            try
            {

                db.Entry(c).State = EntityState.Modified;
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

        #region delete supplier

        public ActionResult Delete(int id)
        {
            var query = db.tblSuppliers.SingleOrDefault(m => m.SupplierId == id);
            db.tblSuppliers.Remove(query);
            db.SaveChanges();
            return RedirectToAction("Index");
        }



        #endregion
    }
}