using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Web.UI.WebControls;
using ShoppingCartMVC.Models;

namespace ShoppingCartMVC.Controllers
{
    public class DonationsController : Controller
    {
        private dbOnlineStoreEntities db = new dbOnlineStoreEntities();

        #region User Donation Index
        // GET: Donations
        public ActionResult Index()
        {
            int sum = 0;

            try
            {
                sum = db.tblDonations.Select(t => t.DonationAmount).Sum();
            }catch(Exception e)
            {
                //catch null error
            }
            ViewBag.TotalDonations = 106422 + sum;
            return View(db.tblDonations.ToList());
        }
        #endregion

        #region Donation Details Admin
        // GET: Donations/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            tblDonations tblDonations = db.tblDonations.Find(id);
            if (tblDonations == null)
            {
                return HttpNotFound();
            }
            return View(tblDonations);
        }
        #endregion

        #region Make Donation
        // GET: Donations/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Donations/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "DonationId,DonCharityOrg,DonEmail,DonationAmount")] tblDonations tblDonations)
        {
            if (ModelState.IsValid)
            {
                db.tblDonations.Add(tblDonations);
                db.SaveChanges();
              int amt=  tblDonations.DonationAmount;

                return Content("<script>" +
                        "function callPayPal() {" +
                        "window.location.href = 'https://www.sandbox.paypal.com/cgi-bin/webscr?cmd=_xclick&amount=" + amt.ToString() + "&business=sb-w3cyw20367505@business.example.com&item_name=Donation&return=https://2023grp01a.azurewebsites.net/Donations/DonationSuccess';" +
                        "}" +
                        "callPayPal();" +
                        "</script>");
            }

            return View(tblDonations);
        }
        #endregion

        public ActionResult DonationSuccess()
        {
            return View();
        }

        #region Donation List Admin
        public ActionResult DonationDetails()
        {
            var query = db.tblDonations.ToList();
            return View(query);
        }
        #endregion

        #region Edit
        // GET: Donations/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            tblDonations tblDonations = db.tblDonations.Find(id);
            if (tblDonations == null)
            {
                return HttpNotFound();
            }
            return View(tblDonations);
        }

      
        // POST: Donations/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "DonationId,DonCharityOrg,DonEmail,DonationAmount")] tblDonations tblDonations)
        {
            if (ModelState.IsValid)
            {
                db.Entry(tblDonations).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(tblDonations);
        }
        #endregion

        #region Delete
        // GET: Donations/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            tblDonations tblDonations = db.tblDonations.Find(id);
            if (tblDonations == null)
            {
                return HttpNotFound();
            }
            return View(tblDonations);
        }

        // POST: Donations/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            tblDonations tblDonations = db.tblDonations.Find(id);
            db.tblDonations.Remove(tblDonations);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
        #endregion
    }
}
