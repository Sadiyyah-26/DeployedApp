//using ShoppingCartMVC.Models;
//using System;
//using System.Collections.Generic;
//using System.Data.Entity;
//using System.Linq;
//using System.Net;
//using System.Threading.Tasks;
//using System.Web;
//using System.Web.Mvc;

//namespace ShoppingCartMVC.Controllers
//{
//    public class tblSupplierOrdersController : Controller
//    {
//        private dbOnlineStoreEntities db = new dbOnlineStoreEntities();

//        #region showing all Stock Ordered Products
//        // GET: tblSupplierOrders
//        public async Task<ActionResult> Index()
//        {
            
//            var query = await db.tblSuppliersOrders
//                    .Include(t => t.TblProduct)
//                    .Include(t => t.tblSupplier)
//                    .Where(m => m.sOrderStatus == "Ordered")
//                    .ToListAsync();
//            return View(query);

//        }
//        #endregion

//        #region Showing Stock that have been returned to supplier
//        public async Task<ActionResult> ReturnIndex()
//        {
            
//            var query = await db.tblSuppliersOrders
//                    .Include(t => t.TblProduct)
//                    .Include(t => t.tblSupplier)
//                    .Where(m => m.sOrderStatus == "Returned")
//                    .ToListAsync();
//            return View(query);

//        }
//        #endregion

//        #region 
//        // GET: tblSupplierOrders/Details/5
//        public ActionResult Details(int? id)
//        {
//            if (id == null)
//            {
//                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
//            }
//            tblSupplierOrder tblSupplierOrder = db.tblSuppliersOrders.Find(id);
//            if (tblSupplierOrder == null)
//            {
//                return HttpNotFound();
//            }
//            return View(tblSupplierOrder);
//        }
//        #endregion

//        //Placing qty order with supplier 

//        //From low stock placing an order to stock up - Supplier name , Product name , qty to order

//        #region Ordering Stock
//        // GET: tblSupplierOrders/Create
//        public ActionResult Create(int id)
//        {
//            var wuery = db.tblProducts.SingleOrDefault(m => m.ProID == id);
//            TempData["Pro"] = id;
//            return View(wuery);
//        }

//        // POST: tblSupplierOrders/Create
//        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
//        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public ActionResult Create([Bind(Include = "OrderId,SupplierId,OrderDate,Total,sOrderStatus,ProID,sUnit,Qty")] tblSupplierOrder tblSupplierOrder, int qtyOrder)
//        {
//            tblProduct product = db.tblProducts.Find((int?)TempData["Pro"]);
//            int? suppID = product.SupplierId;

//            if (ModelState.IsValid)
//            {
//                tblSupplierOrder.ProID = (int?)TempData["Pro"];
//                tblSupplierOrder.SupplierId = suppID;
//                tblSupplierOrder.OrderDate = DateTime.Now;
//                tblSupplierOrder.QtyOrder = qtyOrder;
//                tblSupplierOrder.sUnit = tblSupplierOrder.SuppUnit();
//                tblSupplierOrder.Total = qtyOrder * tblSupplierOrder.sUnit;
//                tblSupplierOrder.sOrderStatus = tblSupplierOrder.checkStatus();


//                if (product != null)
//                {
                    

//                    if (tblSupplierOrder.sOrderStatus == "Received")
//                    {
//                        product.StockStatus = "In Stock";
//                    }
//                    else
//                    {
//                        product.StockStatus = "Stock Ordered";
//                    }


//                    // Save the changes to the products table
//                    db.Entry(product).State = EntityState.Modified;
//                }

//                // Add the tblSupplierOrder to the tblSuppliersOrders table
//                db.tblSuppliersOrders.Add(tblSupplierOrder);

//                // Save all the changes to the database
//                db.SaveChanges();

//                return RedirectToAction("Index");
//            }

            
//            return View(tblSupplierOrder);
//        }
//        #endregion

//        // Confirming order has been received from supplier

//        #region Confirming stock recieved from supplier
//        // GET: tblSupplierOrders/Edit/5
//        public ActionResult Edit(int? id)
//        {
//            TempData["Order"] = id;
//            if (id == null)
//            {
//                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
//            }
//            tblSupplierOrder tblSupplierOrder = db.tblSuppliersOrders.Find(id);
//            if (tblSupplierOrder == null)
//            {
//                return HttpNotFound();
//            }

//            return View(tblSupplierOrder);
//        }

//        // POST: tblSupplierOrders/Edit/5
//        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
//        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public ActionResult Edit([Bind(Include = "OrderId,SupplierId,OrderDate,Total,sOrderStatus,ProID,sUnit,Qty")] tblSupplierOrder tblSupplierOrder)
//        {
//            tblSupplierOrder suppOrder = db.tblSuppliersOrders.Find((int?)TempData["Order"]);
//            tblProduct product = db.tblProducts.Find(suppOrder.ProID);

//            if (ModelState.IsValid)
//            {

//                if (tblSupplierOrder.sOrderStatus == "Successful")
//                {
//                    suppOrder.TblProduct.Qty += suppOrder.QtyOrder;
//                    suppOrder.sOrderStatus = "Received";
//                    product.StockStatus = "In Stock";

//                }
//                else
//                if (tblSupplierOrder.sOrderStatus == "Unsuccessful")
//                {
//                    suppOrder.sOrderStatus = "Returned";
//                    product.StockStatus = "Waiting Stock";
//                }

               
//                db.SaveChanges();
//                return RedirectToAction("Index");
//            }
           
//            return View(tblSupplierOrder);
//        }

//        #endregion

//        // GET: tblSupplierOrders/Delete/5
//        public ActionResult Delete(int? id)
//        {
//            if (id == null)
//            {
//                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
//            }
//            tblSupplierOrder tblSupplierOrder = db.tblSuppliersOrders.Find(id);
//            if (tblSupplierOrder == null)
//            {
//                return HttpNotFound();
//            }
//            return View(tblSupplierOrder);
//        }

//        // POST: tblSupplierOrders/Delete/5
//        [HttpPost, ActionName("Delete")]
//        [ValidateAntiForgeryToken]
//        public ActionResult DeleteConfirmed(int id)
//        {
//            tblSupplierOrder tblSupplierOrder = db.tblSuppliersOrders.Find(id);
//            db.tblSuppliersOrders.Remove(tblSupplierOrder);
//            db.SaveChanges();
//            return RedirectToAction("Index");
//        }

//        protected override void Dispose(bool disposing)
//        {
//            if (disposing)
//            {
//                db.Dispose();
//            }
//            base.Dispose(disposing);
//        }
//    }
//}