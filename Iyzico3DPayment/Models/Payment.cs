using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Iyzico3DPayment.Models
{
    public class Payment
    {
        public int Id { get; set; }

        [Required]
        public string ConversationId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal PaidAmount { get; set; }

        public int InstallmentCount { get; set; }

        [MaxLength(50)]
        public string CardHolderName { get; set; }

        [MaxLength(20)]
        public string CardNumberMasked { get; set; }

        [MaxLength(2)]
        public string ExpireMonth { get; set; }

        [MaxLength(4)]
        public string ExpireYear { get; set; }

        [MaxLength(10)]
        public string CvvHash { get; set; }

        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public int? ApiConfigurationId { get; set; }

        [ForeignKey("ApiConfigurationId")]
        public virtual ApiConfiguration ApiConfiguration { get; set; }
    }
}