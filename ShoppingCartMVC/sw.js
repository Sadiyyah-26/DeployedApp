self.addEventListener('push', function (event) {
    var options = {
        body: event.data.text(),
        icon: '@Url.Content("~/Images/notification.jpg")', // Provide the path to your notification icon
    };

    event.waitUntil(
        self.registration.showNotification('New Message', options)
    );
});