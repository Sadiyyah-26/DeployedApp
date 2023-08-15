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
            var query = db.tblProducts.ToList();
            return View(query);
        }

        #endregion


        #region products add for admin

        public ActionResult Create()
        {
            List<tblCategory> list = db.tblCategories.ToList();
            ViewBag.CatList = new SelectList(list, "CatId", "Name");
            return View();
        }



        [HttpPost]
        public ActionResult Create(tblProduct p, HttpPostedFileBase Image)
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


        #region edit products

        public ActionResult Edit(int id)
        {

            List<tblCategory> list = db.tblCategories.ToList();
            ViewBag.CatList = new SelectList(list, "CatId", "Name");

            var query = db.tblProducts.SingleOrDefault(m => m.ProID == id);

            return View(query);
        }


        [HttpPost]
        public ActionResult Edit(tblProduct p, HttpPostedFileBase Image)
        {
            List<tblCategory> list = db.tblCategories.ToList();
            ViewBag.CatList = new SelectList(list, "CatId", "Name");

            try
            {

                p.Image = Image.FileName.ToString();
                var folder = Server.MapPath("~/Uploads/");
                Image.SaveAs(Path.Combine(folder, Image.FileName.ToString()));
                db.Entry(p).State = EntityState.Modified;
                db.SaveChanges();

            }
            catch (Exception ex)
            {
                TempData["msg"] = ex;
            }

            return RedirectToAction("Index");

        }

        #endregion


        #region delete product 

        public ActionResult Delete(int id)
        {
            var query = db.tblProducts.SingleOrDefault(m => m.ProID == id);
            db.tblProducts.Remove(query);

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