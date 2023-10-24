using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace ShoppingCartMVC.Controllers
{
    public class BotController : Controller
    {
        // Define an endpoint for receiving messages from the user
        [HttpPost]
        public Task<ActionResult> PostMessage()
        {
            // Get the incoming message from the user
            string userMessage = Request.Form["userMessage"];

            // Process the user message using the chatbot
            string botResponse = ProcessUserMessage(userMessage);

            // Return the bot's response
            return Task.FromResult<ActionResult>(Content(botResponse));
        }

        private string ProcessUserMessage(string message)
        {
            string response = string.Empty;

            // Normalize the user's message to lowercase for case-insensitive matching
            string userMessageLower = message.ToLower();

            // Check for keywords and generate responses
            if (userMessageLower.Contains("order") || userMessageLower == "1")
            {
                response = "Before you order, make sure to:<br/><br/>1. Create an account by clicking on the register link.<br/>2. After registering and logging in, go to the Menu page.<br/>3. Click on an item to see its price and description.<br/>4. If you want to order it, select the quantity and any extras, and then click the ADD TO CART button.<br/><br/>After adding items to your cart, remember:<br/>1. You'll see the count in the top right corner, next to a trolley icon.<br/>2. You can continue adding items as needed.<br/>3. When you're ready, navigate to the checkout page by clicking on the cart/trolley icon.<br/><br/>On the checkout page, you can:<br/>1. Review all the details of the items in your cart.<br/>2. Enter your personal information.<br/>3. You can apply a 50-point discount if you have one.<br/>4. When you're ready, click on the CHECKOUT button to complete your order. Happy shopping!";
            }

            else if (userMessageLower.Contains("2"))
            {
                response = "After successfully placing your order, you can effortlessly monitor its progress by navigating to the 'Order History' tab. There, you will find a comprehensive display of all order-related information. In case you have yet to make the payment, your status will be marked as 'Pending' until the payment is settled.<br/><br/>Additionally, if you wish to cancel your order, you can do so by clicking on the 'View Placed Order' button. You also have the convenience of accessing and downloading your invoice. It is advisable to regularly review your order status to stay informed about the preparation and availability of your meal, especially if you plan to collect it.<br/><br/>Furthermore, if you have chosen food delivery, you will receive an email with details, including the estimated delivery time, vehicle number plate, and the name of the driver. It's essential to keep track of your emails for these updates to ensure a smooth experience.";
            }
            else if (userMessageLower.Contains("3"))
            {
                response = "After receiving your meal, you have the option to request a refund in case you are unsatisfied. To initiate this process, navigate to the 'Refund' tab and select the refund request button. Please provide a clear and concise reason for your refund request, and you can also upload supporting images as proof.<br/><br/>Your refund request will be forwarded to the restaurant's management for review. They will carefully assess your request and make a determination on its approval or denial. You can anticipate a response within 3-5 business days.<br/><br/>If your request is approved, your refund will be promptly processed.";
            }
            else if (userMessageLower.Contains("4"))
            {
                response = "To make a table reservation at our restaurant, please visit the 'Reservation' tab and provide the required information. If your desired date and time are available, you will receive a confirmation email along with a unique booking ID. Simply present this ID to the restaurant staff to validate your reservation.<br/><br/>If you wish to reserve a table while also ensuring your meal is prepared in advance, you can follow a similar process as when placing an order.However, when you reach the checkout page, select the 'RESERVE ORDER' button and provide the necessary details. An email will be generated to confirm your reservation.";
            }
            else if (userMessageLower.Contains("5"))
            {
                response = "Please navigate to our Contact us page to find out more information on how to get in touch with us";
            }
            else
            {
                response = "If you require additional assistance, please do not hesitate to reach out to us through email or by giving us a call.";
            }
            return response;
        }
        public ActionResult Bot()
        {
            return View();
        }
    }
}