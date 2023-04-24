namespace ShoppingCartMVC.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.getallorders",
                c => new
                    {
                        InvoiceId = c.Int(nullable: false, identity: true),
                        UserId = c.Int(nullable: false),
                        User = c.String(),
                        Bill = c.Int(),
                        Payment = c.String(),
                        InvoiceDate = c.DateTime(),
                        Status = c.Byte(),
                    })
                .PrimaryKey(t => t.InvoiceId);
            
            CreateTable(
                "dbo.getallorderusers",
                c => new
                    {
                        InvoiceId = c.Int(nullable: false, identity: true),
                        UserId = c.Int(nullable: false),
                        User = c.String(),
                        Bill = c.Int(),
                        Payment = c.String(),
                        InvoiceDate = c.DateTime(),
                        Status = c.Byte(),
                    })
                .PrimaryKey(t => t.InvoiceId);
            
            CreateTable(
                "dbo.tblCategories",
                c => new
                    {
                        CatId = c.Int(nullable: false, identity: true),
                        Name = c.String(),
                    })
                .PrimaryKey(t => t.CatId);
            
            CreateTable(
                "dbo.tblProducts",
                c => new
                    {
                        ProID = c.Int(nullable: false, identity: true),
                        Name = c.String(),
                        Description = c.String(),
                        Unit = c.Int(),
                        Image = c.String(),
                        CatId = c.Int(),
                    })
                .PrimaryKey(t => t.ProID)
                .ForeignKey("dbo.tblCategories", t => t.CatId)
                .Index(t => t.CatId);
            
            CreateTable(
                "dbo.tblOrders",
                c => new
                    {
                        OrderId = c.Int(nullable: false, identity: true),
                        ProID = c.Int(),
                        Contact = c.String(),
                        Address = c.String(),
                        Unit = c.Int(),
                        Qty = c.Int(),
                        Total = c.Int(),
                        OrderDate = c.DateTime(),
                        InvoiceId = c.Int(),
                    })
                .PrimaryKey(t => t.OrderId)
                .ForeignKey("dbo.tblInvoices", t => t.InvoiceId)
                .ForeignKey("dbo.tblProducts", t => t.ProID)
                .Index(t => t.ProID)
                .Index(t => t.InvoiceId);
            
            CreateTable(
                "dbo.tblInvoices",
                c => new
                    {
                        InvoiceId = c.Int(nullable: false, identity: true),
                        UserId = c.Int(),
                        Bill = c.Int(),
                        Payment = c.String(),
                        InvoiceDate = c.DateTime(),
                        Status = c.Byte(),
                    })
                .PrimaryKey(t => t.InvoiceId)
                .ForeignKey("dbo.tblUsers", t => t.UserId)
                .Index(t => t.UserId);
            
            CreateTable(
                "dbo.tblUsers",
                c => new
                    {
                        UserId = c.Int(nullable: false, identity: true),
                        Name = c.String(),
                        Email = c.String(),
                        Password = c.String(),
                        RoleType = c.Int(),
                    })
                .PrimaryKey(t => t.UserId);
            
            CreateTable(
                "dbo.user_invoices",
                c => new
                    {
                        InvoiceId = c.Int(nullable: false, identity: true),
                        Customer = c.String(),
                        Bill = c.Int(),
                        Payment = c.String(),
                        InvoiceDate = c.DateTime(),
                    })
                .PrimaryKey(t => t.InvoiceId);
            
            CreateTable(
                "dbo.vw_getallproducts",
                c => new
                    {
                        ProID = c.Int(nullable: false, identity: true),
                        Name = c.String(),
                        Category = c.String(),
                        Description = c.String(),
                        Unit = c.Int(),
                        Image = c.String(),
                    })
                .PrimaryKey(t => t.ProID);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.tblOrders", "ProID", "dbo.tblProducts");
            DropForeignKey("dbo.tblInvoices", "UserId", "dbo.tblUsers");
            DropForeignKey("dbo.tblOrders", "InvoiceId", "dbo.tblInvoices");
            DropForeignKey("dbo.tblProducts", "CatId", "dbo.tblCategories");
            DropIndex("dbo.tblInvoices", new[] { "UserId" });
            DropIndex("dbo.tblOrders", new[] { "InvoiceId" });
            DropIndex("dbo.tblOrders", new[] { "ProID" });
            DropIndex("dbo.tblProducts", new[] { "CatId" });
            DropTable("dbo.vw_getallproducts");
            DropTable("dbo.user_invoices");
            DropTable("dbo.tblUsers");
            DropTable("dbo.tblInvoices");
            DropTable("dbo.tblOrders");
            DropTable("dbo.tblProducts");
            DropTable("dbo.tblCategories");
            DropTable("dbo.getallorderusers");
            DropTable("dbo.getallorders");
        }
    }
}
