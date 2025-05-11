using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace App1.Models
{
    public class BookedService
    {
        [Key]
        public int BookedServiceId { get; set; }

        public int Quantity { get; set; } = 1;

        public DateTime DateProvided { get; set; } = DateTime.UtcNow;

        // Foreign Keys
        public int BookingId { get; set; }
        public int ServiceId { get; set; }

        // Navigation properties
        [ForeignKey("BookingId")]
        public virtual Booking? Booking { get; set; }

        [ForeignKey("ServiceId")]
        public virtual Service? Service { get; set; }
    }
}