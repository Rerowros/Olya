using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

namespace App1.Models
{
    public class Guest : INotifyPropertyChanged
    {
        private int guestId;
        [Key]
        public int GuestId
        {
            get => guestId;
            set => SetField(ref guestId, value);
        }

        private string firstName = string.Empty;
        [Required]
        [MaxLength(100)]
        public string FirstName
        {
            get => firstName;
            set => SetField(ref firstName, value);
        }

        private string lastName = string.Empty;
        [Required]
        [MaxLength(100)]
        public string LastName
        {
            get => lastName;
            set => SetField(ref lastName, value);
        }

        private string? phoneNumber;
        [MaxLength(20)]
        public string? PhoneNumber
        {
            get => phoneNumber;
            set => SetField(ref phoneNumber, value);
        }

        private string? email;
        [MaxLength(255)]
        [EmailAddress]
        public string? Email
        {
            get => email;
            set => SetField(ref email, value);
        }

        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }
    }
}