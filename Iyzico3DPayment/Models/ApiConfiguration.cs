using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Iyzico3DPayment.Models
{
    
        public class ApiConfiguration
        {
            public int Id { get; set; }

            [Required]
            public string ApiKey { get; set; }

            [Required]
            public string SecretKey { get; set; }

            [Required]
            public string BaseUrl { get; set; }
        }
    }
