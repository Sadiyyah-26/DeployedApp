using ShoppingCartMVC.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ProgressBar;
using System.Xml.Linq;
using iTextSharp.text.pdf;
using iTextSharp.text;

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

            var viewModelList = new List<IngProVm>();

            foreach (var group in groupedIng)
            {
                var product = group.First().TblProduct;

                var ingredientViewModels = group.Select(item => new IngQtyVM
                {
                    IngredientID = item.TblIngredients.Ing_ID,
                    IngredientName = item.TblIngredients.Ing_Name,
                    Quantity = item.Ing_QtyPerPro,
                    StdQty_UnitMeaseurement = item.TblIngredients.StdQty_UnitMeaseurement
                }).ToList();

                var viewModel = new IngProVm
                {
                    ProductID = product.ProID,
                    ProductName = product.P_Name,
                    Ingredients = ingredientViewModels
                };

                viewModelList.Add(viewModel);
            }

            return View(viewModelList);
        }


        [HttpPost]
        public ActionResult UpdateIngredientQuantities(int productId, List<int> ingredientId, List<decimal> quantity)
        {
            // Retrieve the product and associated ingredients from the database.
            var product = db.IngredientProducts.Where(m => m.ProID == productId).ToList();

            if (product == null)
            {
                return HttpNotFound();
            }

            // Loop through the posted ingredient quantities and update the database.
            for (int i = 0; i < ingredientId.Count; i++)
            {
                var ingredientProduct = product.FirstOrDefault(item => item.Ing_ID == ingredientId[i]);
                if (ingredientProduct != null)
                {
                    ingredientProduct.Ing_QtyPerPro = quantity[i];
                }
            }

            // Save changes to the database.
            db.SaveChanges();

            // Redirect to a success page or back to the product list.
            return RedirectToAction("IngProIndex");
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
                //ing.Ing_UnitsUsed = i.Ing_UnitsUsed;
                ing.Ing_StandardQty=i.Ing_StandardQty;

                if (Image!=null)
                {
                    ing.Ing_Image = Image.FileName.ToString();
                }
               
                ing.Ing_StockyQty = i.Ing_StockyQty;
                ing.StdQty_UnitMeaseurement = i.StdQty_UnitMeaseurement;

                //image upload
                if (Image!=null)
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
        public ActionResult UpdateUnitCost(int ingId, int supplierID)
        {
            TempData["ingId"] = ingId;
            TempData["SupplierId"] = supplierID;
            var supplierIngredient = db.SupplierIngredients.SingleOrDefault(si => si.Ing_ID == ingId && si.SupplierId == supplierID);
            return View(supplierIngredient);
        }

        [HttpPost]
        public ActionResult UpdateUnitCostPost(SupplierIngredients updatedSupplierIngredient)
        {
            // Retrieve the IDs from TempData
            int ingredientId = (int)TempData["ingId"];
            int supplierId = (int)TempData["SupplierId"];

            // Update the unit cost in the database for the specified supplier-ingredient combination
            var existingSupplierIngredient = db.SupplierIngredients.SingleOrDefault(si => si.Ing_ID == ingredientId && si.SupplierId == supplierId);

            if (existingSupplierIngredient != null)
            {
                existingSupplierIngredient.Ing_UnitCost = updatedSupplierIngredient.Ing_UnitCost;
                db.SaveChanges();
            }

            // Redirect to a success page or return a view as needed
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
                    !db.tblAdminOrders.Any(order =>
                        order.IngrID == info.Ing_ID &&
                         order.OrderStatus=="Pending"&&
                         order.SupplierId == info.SupplierId ))
                .ToList();

            // Group the results by supplier
            var groupedIngredients = lowStockIngredients.GroupBy(info => info.SupplierId).ToList();

            return View(groupedIngredients);
        }

        #endregion

        #region Specify qty,totals for Low stock ingredients to create purchase order
        public ActionResult LowStockForSupplier(int supplierId)
        {
            // Query to load low stock ingredients for the specific supplier
            var lowStockIngredientsForSupplier = db.SupplierIngredients
                .Join(db.tblIngredients,
                    si => si.Ing_ID,
                    i => i.Ing_ID,
                    (si, i) => new LowStockVM
                    {
                        SupplierId = si.SupplierId,
                        Ing_ID = i.Ing_ID,
                        Ing_Name = i.Ing_Name,
                        StockQty = i.Ing_StockyQty,
                        StockStatus = i.StockStatus,
                        IngImage = i.Ing_Image,
                        SupplName = si.TblSupplier.SupplName,
                        Quantity = 0, // Initialize quantity to 0
                        UnitCost = si.Ing_UnitCost // Assign unit cost
                    })
                .Where(info => info.SupplierId == supplierId && info.StockStatus == "Low Stock" &&
                    !db.tblAdminOrders.Any(order => order.IngrID == info.Ing_ID && order.OrderStatus == "Ordered"))
                .ToList();

            var purchaseOrderModel = new PurchaseOrderVM
            {
                Contact = "", // Initialize with default values
                Address = "", // Initialize with default values
                LowStockItems = lowStockIngredientsForSupplier
            };

            ViewBag.SupplierName = lowStockIngredientsForSupplier.FirstOrDefault()?.SupplName;
            TempData["suppID"] = supplierId;
            return View(purchaseOrderModel);
        }

        #endregion


        //purchase order
        #region Create Purchase Order



        [HttpPost]
        public ActionResult CreatePurchaseOrder(PurchaseOrderVM model)
        {
            string contact = model.Contact;
            string address = model.Address;
            List<LowStockVM> lowStockItems = model.LowStockItems;

            var orderDate = DateTime.Now;
            var orderStatus = "Pending";
            int supplierId = (int)TempData["suppID"];

            tblAdminInvoice aiv = new tblAdminInvoice();
            aiv.UserId = Convert.ToInt32(Session["uid"].ToString());
            aiv.InvoiceDate = System.DateTime.Now;
            aiv.Bill = 0;
            aiv.Payment = "Pending";
            aiv.SupplierId = supplierId;

            db.tblAdminInvoices.Add(aiv);
            db.SaveChanges();

            List<tblAdminOrder> newOrders = new List<tblAdminOrder>();

            foreach (var item in lowStockItems)
            {
                var ingID = item.Ing_ID;
                decimal quantity = item.Quantity;
                double unitCost = item.UnitCost;
                decimal totalCost = (decimal)unitCost * quantity;

                var newOrder = new tblAdminOrder
                {
                    IngrID = ingID,
                    SupplierId = supplierId,
                    Contact = contact,
                    Address = address,
                    InvoiceId = aiv.InvoiceId,
                    Unit = (int)unitCost,
                    Qty = (int)quantity,
                    Total = (int)totalCost,
                    OrderDate = orderDate,
                    OrderStatus = orderStatus,
                };

                newOrders.Add(newOrder);
            }

            // Assuming you have a context named 'db', add the new orders to the database
            db.tblAdminOrders.AddRange(newOrders);
            db.SaveChanges();


            byte[] pdfBytes = GeneratePDF(model, model.LowStockItems, aiv.InvoiceId, aiv.SupplierId);

            var supplInfo = db.tblSuppliers.FirstOrDefault(m => m.SupplierId == aiv.SupplierId);

            // Send the email with the invoice attachment
            SendEmailWithPurchaseOrder(supplInfo.SupplName, pdfBytes);

            Session["PurchaseOrder"] = model;
            // Return a response, e.g., a success message.
            return RedirectToAction("PurchaseOrder", new { invID = aiv.InvoiceId, SupplID = aiv.SupplierId });
        }

        #endregion

        #region Purchase order view for staff
        public ActionResult PurchaseOrder(int invID, int SupplID)
        {
            PurchaseOrderVM purchaseOrder = Session["PurchaseOrder"] as PurchaseOrderVM;
            var SupplInfo = db.tblSuppliers.FirstOrDefault(m => m.SupplierId == SupplID);
            ViewBag.InvID = invID;
            ViewBag.SupplierInfo = SupplInfo;
            return View(purchaseOrder);

        }
        #endregion


        #region Generate pdf for purchase order and send mail
        private byte[] GeneratePDF(PurchaseOrderVM purchaseOrder, List<LowStockVM> lowStockItems, int invID, int SupplID)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                Document document = new Document(PageSize.A4, 50, 50, 25, 25);
                PdfWriter writer = PdfWriter.GetInstance(document, ms);
                document.Open();

                // Company name and address
                document.Add(new iTextSharp.text.Paragraph("TURBO MEALS") { Alignment = Element.ALIGN_CENTER });
                document.Add(iTextSharp.text.Chunk.NEWLINE);

                // Delivery address and contact
                document.Add(new iTextSharp.text.Paragraph("Delivery Address: " + purchaseOrder.Address));
                document.Add(new iTextSharp.text.Paragraph("Contact: " + purchaseOrder.Contact));
                document.Add(iTextSharp.text.Chunk.NEWLINE);

                document.Add(new iTextSharp.text.Paragraph("Purchase Order: #" + invID));
                document.Add(iTextSharp.text.Chunk.NEWLINE);
                string supplierName = "";
                string contactPerson = "";
                string contactPos = "";
                string contactNum = "";
                string contactEmail = "";
                string supplAddress = "";
                string tel = "";
                foreach (var item in lowStockItems)
                {
                    // Access the fields from the LowStockVM item
                    int ingID = item.Ing_ID;
                    decimal quantity = item.Quantity;
                    double unitCost = item.UnitCost;
                    decimal totalCost = item.TotalCost;

                    // Access supplier information using the SupplierId

                    var supplier = db.tblSuppliers.FirstOrDefault(s => s.SupplierId == SupplID);

                    // Supplier information

                    if (supplier != null)
                    {
                        // You can access supplier data like supplier.SupplName, supplier.ContactPerson, etc.
                        supplierName = supplier.SupplName;
                        contactPerson = supplier.ContactPerson;
                        contactPos = supplier.ContactPersonPos;
                        contactNum = supplier.ContactNum;
                        contactEmail = supplier.Email;
                        supplAddress = supplier.PhysicalAddress;
                        tel = supplier.Tel;

                    }


                }

                // Supplier information
                document.Add(new iTextSharp.text.Paragraph("Supplier Info:"));
                document.Add(new iTextSharp.text.Paragraph(supplierName) { Font = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14f, BaseColor.ORANGE) });
                document.Add(new iTextSharp.text.Paragraph(contactPerson + " - " + contactPos));
                document.Add(new iTextSharp.text.Paragraph(contactNum + "/" + tel));
                document.Add(new iTextSharp.text.Paragraph(contactEmail));
                document.Add(new iTextSharp.text.Paragraph("Physical Address: " + supplAddress));
                document.Add(iTextSharp.text.Chunk.NEWLINE);


                // Order summary
                document.Add(new iTextSharp.text.Paragraph("Order Summary") { Font = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16f) });

                PdfPTable table = new PdfPTable(4);
                table.WidthPercentage = 100;

                //table.AddCell(new PdfPCell(new Phrase("Order No.", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10f))));
                table.AddCell(new PdfPCell(new Phrase("Item Name", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10f))));
                table.AddCell(new PdfPCell(new Phrase("Unit Price", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10f))));
                table.AddCell(new PdfPCell(new Phrase("Quantity for Order", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10f))));
                table.AddCell(new PdfPCell(new Phrase("Sub-Total", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10f))));

                decimal grandTotal = 0;
                foreach (var item in purchaseOrder.LowStockItems)
                {
                    decimal quantity = item.Quantity;
                    double unitCost = item.UnitCost;
                    decimal totalCost = (decimal)unitCost * quantity;

                    var ing = db.tblIngredients.FirstOrDefault(m => m.Ing_ID == item.Ing_ID);

                    //table.AddCell(item.Ing_ID.ToString());
                    table.AddCell(ing.Ing_Name);
                    table.AddCell("R " + (item.UnitCost).ToString("0.00"));
                    table.AddCell(item.Quantity.ToString());
                    table.AddCell("R " + (totalCost).ToString("0.00"));

                    grandTotal += totalCost;
                }

                document.Add(table);

                document.Add(iTextSharp.text.Chunk.NEWLINE);

                // Total amount
                document.Add(new iTextSharp.text.Paragraph("Total Amount: R " + grandTotal.ToString("0.00")) { Alignment = Element.ALIGN_RIGHT });

                document.Close();
                return ms.ToArray();
            }
        }

        //send pdf copy of purchase order to supplier
        private void SendEmailWithPurchaseOrder(string supplName, byte[] pdfBytes)
        {
            string recipientEmail = "jacedoe123@gmail.com"; // Replace with the recipient's email address
            var body = $@"Dear {supplName},

            I hope this email finds you well. We are pleased to inform you that we would like to place a purchase order.

            Payment Terms: On delivery

            Delivery Date: Effective within 3 days of response

            Please find attached a detailed Purchase Order document (PDF) for your reference. It contains all the necessary information, including item descriptions, quantities, prices, and delivery instructions.

            We kindly request that you review the purchase order and confirm your acceptance by replying to this email at your earliest convenience. If you have any questions or require any further information, please do not hesitate to contact us at 031-456-2548.

            Once we receive your confirmation, we will proceed with the necessary steps to process this order. We anticipate a smooth and timely transaction and look forward to receiving your products.

            Thank you for your prompt attention to this matter. We appreciate your continued partnership and dedication to providing us with high-quality products and services.

            Sincerely,
            John Doe
            Kitchen Manager
            Turbo Meals
            031-456-2547";


            var message = new MailMessage();
            message.To.Add(new MailAddress(recipientEmail));
            message.From = new MailAddress("turbomeals123@gmail.com"); // Replace with your email address
            message.Subject = "Purchase Order for Turbo Meals";
            message.Body = body;
            //message.IsBodyHtml = true;

            using (var memoryStream = new MemoryStream(pdfBytes))
            {
                message.Attachments.Add(new Attachment(memoryStream, "TurboMeals_PurchaseOrder.pdf"));
                using (var smtp = new SmtpClient())
                {
                    smtp.Send(message);
                }
            }
        }
        #endregion

        //include delete ingredient code


    }
}