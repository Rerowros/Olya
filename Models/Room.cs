using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace App1.Models
{
    public class Room
    {
        [Key]
        public int RoomId { get; set; }

        [Required]
        [MaxLength(10)]
        public string RoomNumber { get; set; } = string.Empty;

        public int Floor { get; set; }

        [Required]
        public RoomStatus Status { get; set; }

        // Foreign Key
        public int RoomCategoryId { get; set; }

        // Navigation property
        [ForeignKey("RoomCategoryId")]
        public virtual RoomCategory? RoomCategory { get; set; }

        // Navigation property
        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}