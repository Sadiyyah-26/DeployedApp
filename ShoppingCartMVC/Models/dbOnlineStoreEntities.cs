using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Reflection.Emit;

namespace ShoppingCartMVC.Models
{
    public class dbOnlineStoreEntities : DbContext
    {
        public dbOnlineStoreEntities() : base("name=dbOnlineStoreEntities")
        {
        }

       
        public virtual DbSet<tblCategory> tblCategories { get; set; }
        public virtual DbSet<tblInvoice> tblInvoices { get; set; }
        public virtual DbSet<tblOrder> tblOrders { get; set; }
        public virtual DbSet<tblProduct> tblProducts { get; set; }
        public virtual DbSet<tblUser> tblUsers { get; set; }
        
        public virtual DbSet<Drivers> tblDrivers { get; set; }
        public virtual DbSet<tblReservation> tblReservations { get; set; }
        public virtual DbSet<tblRefund> tblRefunds { get; set; }
        public virtual DbSet<tblExtras> tblExtras { get; set; }
        public virtual DbSet<tblSupplier> tblSuppliers { get; set; }
        
        public virtual DbSet<tblIngredients> tblIngredients { get; set; }
        public virtual DbSet<IngredientProduct> IngredientProducts { get; set; }
        public virtual DbSet<SupplierIngredients> SupplierIngredients { get; set; }

        public virtual DbSet<tblAdminOrder> tblAdminOrders { get; set; }
        public virtual DbSet<tblAdminInvoice> tblAdminInvoices { get; set; }

        public virtual DbSet<UserPoints> tblPoints { get; set; }

        public virtual DbSet<AccountPoints> tblAccPoints { get; set; }
        public virtual DbSet<tblDonations> tblDonations { get; set; }

        public virtual DbSet<tblInStoreOrder> TblInStoreOrders { get; set; }
        public virtual DbSet<tblBill> TblBills { get; set; }

        public virtual DbSet<tblEmployee> tblEmployees { get; set; }
        
        public virtual DbSet<tblAccProfile> TblAccProfiles { get; set; }

        public virtual DbSet<tblCashFloat> tblCashFloats { get; set; }

        public virtual DbSet<Transactions> tblTransactions { get; set; }
        public virtual DbSet<Discrepancy> tblDiscrepancy { get; set; }



    }
}

