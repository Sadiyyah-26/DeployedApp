using Microsoft.AspNet.SignalR;
using ShoppingCartMVC.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ShoppingCartMVC.SignalR.hubs
{
    public class ChatHub : Hub
    {
        private string GetUserProfileImage(string senderName)
        {
            var context = HttpContext.Current;

            if (context != null)
            {
                string baseUrl = context.Request.Url.GetLeftPart(UriPartial.Authority);
                string imageFolder = "Uploads"; // Update with your image folder name

                using (var dbContext = new dbOnlineStoreEntities()) // Replace with your actual DbContext
                {
                    var user = dbContext.TblAccProfiles.FirstOrDefault(p => p.userName == senderName);
                    if (user != null && !string.IsNullOrEmpty(user.userProfileImage))
                    {
                        // Build the complete URL to the image
                        string imageUrl = $"{baseUrl}/{imageFolder}/{user.userProfileImage}";

                        return imageUrl;
                    }
                }
            }

            // Return a default profile image URL if the user's profile is not found
            return "https://cdn.pixabay.com/photo/2015/10/05/22/37/blank-profile-picture-973460_1280.png";
        }

        public void Send(string senderName, string message)
        {
            string userProfileImage = GetUserProfileImage(senderName);
            Clients.All.addNewMessageToPage(senderName, message, userProfileImage);
        }
    }

}