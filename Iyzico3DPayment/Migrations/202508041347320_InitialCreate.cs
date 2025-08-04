namespace Iyzico3DPayment.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ApiConfigurations",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        ApiKey = c.String(nullable: false),
                        SecretKey = c.String(nullable: false),
                        BaseUrl = c.String(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Payments",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        ConversationId = c.String(nullable: false),
                        PaidAmount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        InstallmentCount = c.Int(nullable: false),
                        CardHolderName = c.String(maxLength: 50),
                        CardNumberMasked = c.String(maxLength: 20),
                        ExpireMonth = c.String(maxLength: 2),
                        ExpireYear = c.String(maxLength: 4),
                        CvvHash = c.String(maxLength: 10),
                        Status = c.String(),
                        CreatedAt = c.DateTime(nullable: false),
                        ApiConfigurationId = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.ApiConfigurations", t => t.ApiConfigurationId)
                .Index(t => t.ApiConfigurationId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Payments", "ApiConfigurationId", "dbo.ApiConfigurations");
            DropIndex("dbo.Payments", new[] { "ApiConfigurationId" });
            DropTable("dbo.Payments");
            DropTable("dbo.ApiConfigurations");
        }
    }
}
