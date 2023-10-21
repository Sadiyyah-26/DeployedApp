// Startup.cs
using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(ShoppingCartMVC.Startup))]

namespace ShoppingCartMVC
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.MapSignalR();
        }
    }
}
