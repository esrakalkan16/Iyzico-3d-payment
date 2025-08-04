using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Dynamic;
using System.Linq;
using System.Web;

namespace Iyzico3DPayment.Models
{
   
        public class ApplicationDbContext : DbContext
        {
            public ApplicationDbContext() : base("DefaultConnection")  {}

        public DbSet<Payment> Payments { get; set; }
        public DbSet<ApiConfiguration> ApiConfigurations { get; set; }
    }
    }
