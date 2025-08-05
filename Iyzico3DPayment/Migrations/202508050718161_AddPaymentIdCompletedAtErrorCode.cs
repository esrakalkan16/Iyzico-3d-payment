namespace Iyzico3DPayment.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddPaymentIdCompletedAtErrorCode : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Payments", "PaymentId", c => c.String(maxLength: 100));
            AddColumn("dbo.Payments", "ErrorCode", c => c.String(maxLength: 50));
            AddColumn("dbo.Payments", "CompletedAt", c => c.DateTime());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Payments", "CompletedAt");
            DropColumn("dbo.Payments", "ErrorCode");
            DropColumn("dbo.Payments", "PaymentId");
        }
    }
}
