using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using ShoppingCartMVC.Models;

namespace ShoppingCartMVC.Controllers
{
    public class tblRefundsController : Controller
    {
        private dbOnlineStoreEntities db = new dbOnlineStoreEntities();

        // GET: tblRefunds
        public ActionResult Index()
        {
            return View(db.tblRefunds.ToList());
        }

        // GET: tblRefunds/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            tblRefund tblRefund = db.tblRefunds.Find(id);
            if (tblRefund == null)
            {
                return HttpNotFound();
            }
            return View(tblRefund);
        }

        // GET: tblRefunds/Create
        public ActionResult Create()
        {
            List<tblOrder> list = db.tblOrders.ToList();
            ViewBag.OrderList = new SelectList(list, "OrderId", "OrderId");
            return View();
        }

        // POST: tblRefunds/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "RefundId,EmailID,OrderId,RefundRequestDate,RefundReason,Image,RefundStatus")] tblRefund tblRefund, HttpPostedFileBase Image, int orderId)
        {
            List<tblOrder> list = db.tblOrders.ToList();
            ViewBag.OrderList = new SelectList(list, "OrderId", "OrderId");

            if (ModelState.IsValid)
            {
                
                string subject = "Refund Request Confirmation";
                string body = "Your request for a refund was recieved.<br><br>" +
                    "The manager will review your request and you will have a response within 3-5 business days.";
                string emailID = tblRefund.EmailID;

                WebMail.Send(emailID, subject, body, null, null, null, true, null, null, null, null, null, null);

                tblRefund.RefundRequestDate = DateTime.Now;
                tblRefund.Image = Image.FileName.ToString();
                tblRefund.RefundStatus = tblRefund.checkStatus();

                var folder = Server.MapPath("~/Uploads/");
                Image.SaveAs(Path.Combine(folder, Image.FileName.ToString()));

                db.tblRefunds.Add(tblRefund);
                db.SaveChanges();

                var order = db.tblOrders.FirstOrDefault(o => o.OrderId == tblRefund.OrderId);
                if (order != null)
                {
                    order.RefundStatus = tblRefund.RefundStatus;
                    db.SaveChanges();
                }


                return RedirectToAction("Refund", "Home", new { id = @Session["uid"] });
            }

            return View(tblRefund);
        }

        // GET: tblRefunds/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            tblRefund tblRefund = db.tblRefunds.Find(id);
            if (tblRefund == null)
            {
                return HttpNotFound();
            }
            return View(tblRefund);
        }

        // POST: tblRefunds/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "RefundId,EmailID,OrderId,RefundRequestDate,RefundReason,Image,RefundStatus")] tblRefund tblRefund)
        {
            if (ModelState.IsValid)
            {
                var originalRefund = db.tblRefunds.Find(tblRefund.RefundId);
                if (originalRefund != null)
                {
                    bool statusChanged = originalRefund.RefundStatus != tblRefund.RefundStatus;

                    originalRefund.RefundStatus = tblRefund.RefundStatus;

                    db.Entry(originalRefund).State = EntityState.Modified;
                    db.SaveChanges();

                    if (statusChanged)
                    {
                        var order = db.tblOrders.FirstOrDefault(o => o.OrderId == tblRefund.OrderId);
                        if (order != null)
                        {
                            order.RefundStatus = tblRefund.RefundStatus;
                            db.Entry(order).State = EntityState.Modified;
                            db.SaveChanges();
                        }

                        string emailSubject;
                        string emailBody;
                        string orderNum = originalRefund.OrderId.ToString();
                        

                        if (tblRefund.RefundStatus == "Successful")
                        {
                            emailSubject = "Refund Request Approved";
                            emailBody = "Dear Turbo Meals customer,<br><br>Your refund request for Order Number #"+orderNum+" has been approved.<br><br> Please note your refund will be processed soon.";
                        }
                        else if (tblRefund.RefundStatus == "Unsuccessful")
                        {
                            emailSubject = "Refund Request Denied";
                            emailBody = "Dear Turbo Meals customer,<br><br>We regret to inform you that your refund request for Order Number # "+orderNum+" has been denied.<br><br>For any further queries please send a reply to this email and we will gladly get back to you within 24 hours.";
                        }
                        else
                        {
                            // Handle other refund status values if needed
                            return RedirectToAction("Index");
                        }

                        // Send email to the user
                        string emailID = tblRefund.EmailID;
                        WebMail.Send(emailID, emailSubject, emailBody, null, null, null, true, null, null, null, null, null, null);
                    }
                }

                return RedirectToAction("Index");
            }

            return View(tblRefund);
        }



        // GET: tblRefunds/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            tblRefund tblRefund = db.tblRefunds.Find(id);
            if (tblRefund == null)
            {
                return HttpNotFound();
            }
            return View(tblRefund);
        }

        // POST: tblRefunds/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            tblRefund tblRefund = db.tblRefunds.Find(id);
            db.tblRefunds.Remove(tblRefund);
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
    }
}
