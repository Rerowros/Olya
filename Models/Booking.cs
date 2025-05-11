using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace App1.Models
{
    public class Booking
    {
        [Key]
        public int BookingId { get; set; }

        [Required]
        public DateTime CheckInDate { get; set; }

        [Required]
        public DateTime CheckOutDate { get; set; }

        public DateTime BookingDate { get; set; } = DateTime.UtcNow;

        [Required]
        public BookingStatus Status { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal TotalPrice { get; set; } // Может рассчитываться при сохранении

        // Foreign Keys
        public int GuestId { get; set; }
        public int RoomId { get; set; }

        // Navigation properties
        [ForeignKey("GuestId")]
        public virtual Guest? Guest { get; set; }

        [ForeignKey("RoomId")]
        public virtual Room? Room { get; set; }

        public virtual ICollection<BookedService> BookedServices { get; set; } = new List<BookedService>();
    }
}