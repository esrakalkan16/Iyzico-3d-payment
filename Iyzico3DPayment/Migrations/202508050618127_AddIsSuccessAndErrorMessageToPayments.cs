namespace Iyzico3DPayment.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddIsSuccessAndErrorMessageToPayments : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Payments", "IsSuccess", c => c.Boolean(nullable: false));
            AddColumn("dbo.Payments", "ErrorMessage", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Payments", "ErrorMessage");
            DropColumn("dbo.Payments", "IsSuccess");
        }
    }
}
