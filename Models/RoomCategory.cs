using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace App1.Models
{
    public class RoomCategory
    {
        [Key]
        public int RoomCategoryId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public int Capacity { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal BasePricePerNight { get; set; }

        // Navigation property
        public virtual ICollection<Room> Rooms { get; set; } = new List<Room>();
    }
}