using iTextSharp.text.pdf;
using iTextSharp.text;
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
    public class AdminHomeController : Controller
    {
        dbOnlineStoreEntities db=new dbOnlineStoreEntities();

        //List<AdminCart> Ali = new List<AdminCart>();

        #region Showing All Pending Supplier Stock Orders
        public ActionResult AllPendingOrders()//final changes
        {
            var orders = db.tblAdminOrders.Where(order => order.OrderStatus == "Pending").ToList();
            var ingredients = db.tblIngredients.ToList();

            var pendingIngredients = orders
                .Join(db.tblSuppliers,
                    order => order.SupplierId,
                    supplier => supplier.SupplierId,
                    (order, supplier) => new PendingOrdersVM
                    {
                        InvoiceID = order.InvoiceId,
                        SupplierId = supplier.SupplierId,
                        SupplName = supplier.SupplName,
                        Ing_ID = order.IngrID,
                        Ing_Name = ingredients.FirstOrDefault(ing => ing.Ing_ID == order.IngrID)?.Ing_Name,
                        IngImage = ingredients.FirstOrDefault(ing => ing.Ing_ID == order.IngrID)?.Ing_Image,
                        OrderedStockQty = order.Qty
                    })
                .ToList();



            TempData["PendingOrdersData"] = pendingIngredients;
            var groupedIngredients = pendingIngredients.GroupBy(info => info.SupplierId).ToList();

            return View(groupedIngredients);
        }

        #endregion

        public ActionResult PaymentSuccess()
        {
            return View();
        }

        #region Confirm Stock Availability with Supplier
        [HttpPost]
        public ActionResult ConfirmIngOrderWithSupplier(List<PendingOrdersVM> supplierGroup)
        {
            if (supplierGroup != null && supplierGroup.Count > 0)
            {
                if (ModelState.IsValid)
                {
                    // Group ingredients by InvoiceID and SupplierID
                    var groupedIngredients = supplierGroup.GroupBy(ing => new { ing.InvoiceID, ing.SupplierId });

                    foreach (var group in groupedIngredients)
                    {
                        int? invoiceId = group.Key.InvoiceID;
                        int supplierId = group.Key.SupplierId;
                        TempData["Invoice"] = invoiceId;

                        foreach (var ingredient in group)
                        {
                            // Find all orders with the same InvoiceID, SupplierId, and Ing_ID
                            var orders = db.tblAdminOrders
                                .Where(m => m.InvoiceId == invoiceId && m.SupplierId == supplierId && m.IngrID == ingredient.Ing_ID)
                                .ToList();

                            foreach (var order in orders)
                            {
                                bool isCheckboxChecked = ingredient.StockAvailabilityConfirmed;

                                if (isCheckboxChecked)
                                {
                                    order.OrderStatus = "Ordered";
                                    order.TblAdminInvoice.InvoiceDate = System.DateTime.Now;
                                }
                                else
                                {
                                    order.OrderStatus = "Cancelled";
                                }
                            }
                        }
                    }

                    // Save changes to all records
                    db.SaveChanges();
                }

                // Send invoice as PDF via email if any order is "Ordered"
                if (supplierGroup.Any(ing => ing.StockAvailabilityConfirmed))
                {
                    // Get the invoice data
                    AdminInv_VM invoiceData = GetInvoiceData(Convert.ToInt32(TempData["Invoice"]));

                    // Generate the PDF invoice
                    byte[] pdfBytes = GenerateInvoicePDF(invoiceData);

                    // Send the email with the PDF invoice
                    SendEmailWithInvoice(invoiceData.SupplName, Convert.ToInt32(TempData["Invoice"]), invoiceData.ContactPerson, invoiceData.ContactNum, invoiceData.ContactPersonPos, pdfBytes);
                }

                // Clear TempData to ensure it's not reused on subsequent requests
                TempData["PendingOrdersData"] = null;

                // Redirect to a success or confirmation page
                return RedirectToAction("PaymentSuccess");
            }
            else
            {
                // Handle the case where the supplierGroup is null or empty
                return View("Error");
            }
        }


        #endregion

        #region Generate Invoice as PDF
        #region Get Invoice Data
        private AdminInv_VM GetInvoiceData(int invoiceId)
        {
            int InvID = invoiceId; // Use the provided invoiceId parameter
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
                                    ao.OrderStatus,
                                    ao.OrderDate,
                                    i.Ing_Name,
                                    ao.Contact,
                                    ao.Address,
                                    ao.Unit,
                                    ao.Qty,
                                    ao.Total
                                }).ToList();

            var orderRecs = adminInvList.GroupBy(m => m.InvoiceId);

            // Create a ViewModel for the invoice
            AdminInv_VM objIVM = new AdminInv_VM();

            // You can aggregate data here if needed (e.g., summing up quantities)

            if (orderRecs.Any())
            {
                var order = orderRecs.First(); // Assuming there's only one invoice for the provided invoiceId

                // Assign common values to the ViewModel
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
                    OrderStatus = item.OrderStatus,
                    Ing_Name = item.Ing_Name,
                    Unit = item.Unit,
                    Qty = item.Qty,
                    Total = item.Total
                }).ToList();
            }

            return objIVM;
        }

        #endregion

        // Generate PDF invoice using iTextSharp
        private byte[] GenerateInvoicePDF(AdminInv_VM invoiceData)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                Document document = new Document(PageSize.A4, 50, 50, 25, 25);
                PdfWriter writer = PdfWriter.GetInstance(document, ms);
                document.Open();

                // Add content to the PDF document based on invoiceData
                // You can add content here to match your desired format and style
                // For example:
                // Company name and address
                document.Add(new iTextSharp.text.Paragraph("TURBO MEALS") { Alignment = Element.ALIGN_CENTER });
                document.Add(Chunk.NEWLINE);

                // Delivery address and contact
                document.Add(new iTextSharp.text.Paragraph("Delivery Address: " + invoiceData.Address));
                document.Add(new iTextSharp.text.Paragraph("Contact: " + invoiceData.Contact));
                document.Add(Chunk.NEWLINE);

                // Invoice information
                document.Add(new iTextSharp.text.Paragraph("Invoice #" + invoiceData.InvoiceId));
                document.Add(new iTextSharp.text.Paragraph("Payment Status: " + invoiceData.Payment) { Alignment = Element.ALIGN_RIGHT });
                document.Add(Chunk.NEWLINE);

                // Supplier information
                document.Add(new iTextSharp.text.Paragraph("Supplier Info:"));
                document.Add(new iTextSharp.text.Paragraph(invoiceData.SupplName) { Font = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14f, BaseColor.ORANGE) });
                document.Add(new iTextSharp.text.Paragraph(invoiceData.ContactPerson + " - " + invoiceData.ContactPersonPos));
                document.Add(new iTextSharp.text.Paragraph(invoiceData.ContactNum));
                document.Add(new iTextSharp.text.Paragraph(invoiceData.Email));
                document.Add(new iTextSharp.text.Paragraph(invoiceData.Tel));
                document.Add(new iTextSharp.text.Paragraph("Physical Address: " + invoiceData.PhysicalAddress));
                document.Add(Chunk.NEWLINE);

                // Invoice and order dates
                document.Add(new iTextSharp.text.Paragraph("Invoice Date: " + invoiceData.InvoiceDate));
                document.Add(new iTextSharp.text.Paragraph("Order Date: " + invoiceData.OrderDate));
                document.Add(Chunk.NEWLINE);

                // Order summary
                document.Add(new iTextSharp.text.Paragraph("Order Summary") { Font = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16f) });

                PdfPTable table = new PdfPTable(5);
                table.WidthPercentage = 100;

                table.AddCell(new PdfPCell(new Phrase("Order Status", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10f))));
                table.AddCell(new PdfPCell(new Phrase("Item Name", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10f))));
                table.AddCell(new PdfPCell(new Phrase("Unit Price", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10f))));
                table.AddCell(new PdfPCell(new Phrase("Quantity for Order", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10f))));
                table.AddCell(new PdfPCell(new Phrase("Sub-Total", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10f))));
                double grandTotal = 0;

                foreach (var item in invoiceData.Items)
                {
                    table.AddCell(item.OrderStatus.ToString());
                    table.AddCell(item.Ing_Name);
                    table.AddCell("R " + (item.Unit.HasValue ? ((decimal)item.Unit).ToString("0.00") : "N/A"));
                    table.AddCell(item.Qty.ToString());
                    table.AddCell("R " + (item.Unit.HasValue ? ((decimal)item.Total).ToString("0.00") : "N/A"));

                    if (item.OrderStatus != "Cancelled")
                    {
                        grandTotal += (double)item.Total;
                    }
                }

                document.Add(table);

                document.Add(Chunk.NEWLINE);

                // Total amount
                document.Add(new iTextSharp.text.Paragraph("Total Amount: R " + grandTotal.ToString("0.00")) { Alignment = Element.ALIGN_RIGHT });

                document.Close();
                return ms.ToArray();
            }
        }



        // Send email with invoice attachment
        private void SendEmailWithInvoice(string supplName, int invoiceID, string contactPerson, string contactNum, string contactPos, byte[] pdfBytes)
        {
            string recipientEmail = "turbostaff786@gmail.com"; // Replace with the recipient's email address
            var body = $@"Dear Turbo Meals Team,

            We acknowledge the receipt of your recent ingredient stock order. Please find the attached invoice for Invoice ID: {invoiceID}.

            Delivery will be made within 3 working days, and further notified to you on the day.

            Best regards,

            {contactPerson}
            {contactPos}
            {contactNum}";

            var message = new MailMessage();
            message.To.Add(new MailAddress(recipientEmail));
            message.From = new MailAddress("turbomeals123@gmail.com"); // Replace with your email address
            message.Subject = "Invoice for Stock Ordered";
            message.Body = body;
            //message.IsBodyHtml = true;

            using (var memoryStream = new MemoryStream(pdfBytes))
            {
                message.Attachments.Add(new Attachment(memoryStream, "Invoice.pdf"));
                using (var smtp = new SmtpClient())
                {
                    smtp.Send(message);
                }
            }
        }
        #endregion


        #region All Prep Staff Orders with Suppliers for Ingredient Stock 

        public ActionResult AdminGetAllOrderDetail()
        {
            var ordersWithIngredients = db.tblAdminOrders
                .Where(n => n.OrderStatus == "Ordered")
                .GroupBy(m => m.InvoiceId)
                .Select(group => new StockOrdersWithIngrVM
                {
                    InvoiceId = group.Key,
                    SupplierName = group.FirstOrDefault().TblSupplier.SupplName,
                    OrderDate = group.FirstOrDefault().OrderDate,
                    Ingredients = group.SelectMany(order => db.tblIngredients
                        .Where(ingredient => ingredient.Ing_ID == order.IngrID)
                        .Select(ingredient => ingredient.Ing_Name))
                        .ToList()
                })
                .ToList();

            return View(ordersWithIngredients);
        }


        #endregion


        #region Returned Orders with Supplier
        public ActionResult ReturnedOrdersList()
        {
            var ordersWithIngredients = db.tblAdminOrders
                .Where(n => n.OrderStatus == "Returned")
                .GroupBy(m => m.InvoiceId)
                .Select(group => new StockOrdersWithIngrVM
                {
                    InvoiceId = group.Key,
                    SupplierName = group.FirstOrDefault().TblSupplier.SupplName,
                    OrderDate = group.FirstOrDefault().OrderDate,
                    ReturnReason = group.FirstOrDefault().ReturnReason,
                    Ingredients = group.SelectMany(order => db.tblIngredients
                        .Where(ingredient => ingredient.Ing_ID == order.IngrID)
                        .Select(ingredient => ingredient.Ing_Name))
                        .ToList()
                })
                .ToList();

            return View(ordersWithIngredients);
        }

        [HttpPost]
        public ActionResult ReturnedOrdersList(int id)
        {
            var allOrders = db.tblAdminOrders.Where(m => m.InvoiceId == id && m.OrderStatus == "Returned").ToList();

            if (allOrders.Count > 0)
            {
                foreach (var order in allOrders)
                {
                    order.OrderStatus = "Ordered";
                    //order.OrderDate = System.DateTime.Now;
                    //order.TblAdminInvoice.InvoiceDate= System.DateTime.Now;
                }
                db.SaveChanges();
            }

            return RedirectToAction("AdminGetAllOrderDetail");

        }
        #endregion


        #region Received Orders List from Supplier
        public ActionResult ReceivedOrdersList()
        {
            var ordersWithIngredients = db.tblAdminOrders
                .Where(n => n.OrderStatus == "Received")
                .GroupBy(m => m.InvoiceId)
                .Select(group => new StockOrdersWithIngrVM
                {
                    InvoiceId = group.Key,
                    SupplierName = group.FirstOrDefault().TblSupplier.SupplName,
                    OrderDate = group.FirstOrDefault().OrderDate,
                    Ingredients = group.SelectMany(order => db.tblIngredients
                        .Where(ingredient => ingredient.Ing_ID == order.IngrID)
                        .Select(ingredient => ingredient.Ing_Name))
                        .ToList()
                })
                .ToList();

            return View(ordersWithIngredients);
        }
        #endregion


        #region  Confirm Delivery with Supplier

        public ActionResult SupplierConfirmOrder(int InvoiceID)
        {
            TempData["Inv"] = InvoiceID;
            var groupedOrders = db.tblAdminOrders
                .Where(m => m.InvoiceId == InvoiceID && m.OrderStatus == "Ordered")
                .GroupBy(n => n.InvoiceId)
                .ToList();
            return View(groupedOrders);
        }

        [HttpPost]
        public ActionResult SupplierConfirmOrder()
        {
            int id = (int)TempData["Inv"];
            var orders = db.tblAdminOrders
                .Where(m => m.InvoiceId == id && m.OrderStatus == "Ordered")
                .GroupBy(n => n.InvoiceId)
                .ToList();

            var updatePayment = db.tblAdminInvoices.FirstOrDefault(m => m.InvoiceId == id);

            foreach (var group in orders)
            {
                foreach (var orderInfo in group)
                {
                    // Get the conditions for the current order
                    var conditions = Request.Form.GetValues("inOrder[" + orderInfo.OrderId + "]");

                    if (conditions != null && conditions.Contains("Condition1") && conditions.Contains("Condition2") && conditions.Contains("Condition3"))
                    {
                        // Update the order status for this order as "Received"
                        orderInfo.OrderStatus = "Received";
                        int qty = (int)orderInfo.Qty;

                        // Update the corresponding ingredient
                        int ingredientId = (int)orderInfo.IngrID;
                        var ingredientEntity = db.tblIngredients.FirstOrDefault(i => i.Ing_ID == ingredientId);

                        if (ingredientEntity != null)
                        {
                            ingredientEntity.Ing_StockyQty += qty;
                            ingredientEntity.StockStatus = "In Stock";
                            db.Entry(ingredientEntity).State = EntityState.Modified;
                        }
                    }
                    else
                    {
                        string returnReason = "";

                        if (conditions != null && !conditions.Contains("Condition2") && !conditions.Contains("Condition3"))
                        {
                            returnReason = "Inventory received is damaged and quantity discrepancies";
                        }
                        else if (conditions != null && !conditions.Contains("Condition2"))
                        {
                            returnReason = "Inventory received is damaged/not fresh";
                        }
                        else
                        {
                            returnReason = "Quantity discrepancies";
                        }

                        // Update the order status for this order as "Returned"
                        orderInfo.OrderStatus = "Returned";
                        orderInfo.ReturnReason = returnReason;
                        db.Entry(orderInfo).State = EntityState.Modified;
                    }                   
                }

                if (group.All(orderInfo => orderInfo.OrderStatus == "Received"))
                {
                    updatePayment.Payment = "Paid";
                }
            }

            // Save changes to the database
            db.SaveChanges();

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
                                    ao.OrderStatus,
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
                    OrderStatus = item.OrderStatus,
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