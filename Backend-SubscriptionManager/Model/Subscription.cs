using System.ComponentModel.DataAnnotations;
using System;

namespace Backend_SubscriptionManager.Model
{
    public class Subscription
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public string Category { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Cost { get; set; } = 0;

        public DateTime RenewalDate { get; set; }

        public string? InvoiceFilePath { get; set; } // To store file path

        public DateTime CreatedAt { get; set; }
        public string UserEmail { get; set; }
    }

}
