// ...existing code...
using App1.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using Windows.Storage; // Add this namespace
using BCrypt.Net;

namespace App1.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Guest> Guests { get; set; }
        public DbSet<RoomCategory> RoomCategories { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<BookedService> BookedServices { get; set; }
        
        
        // Настраиваем подключение к SQLite
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // Получаем путь к локальной папке данных приложения
                string dbPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "hotel_app.db");
                System.Diagnostics.Debug.WriteLine($"Database path: {dbPath}"); // Вывод пути для отладки
                optionsBuilder.UseSqlite($"Data Source={dbPath}");
            }
        }


        // (Необязательно) Начальное заполнение данными (Seeding)
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // --- Категории номеров ---
            modelBuilder.Entity<RoomCategory>().HasData(
                new RoomCategory { RoomCategoryId = 1, Name = "Стандарт", Description = "Стандартный номер", Capacity = 2, BasePricePerNight = 5000.00m },
                new RoomCategory { RoomCategoryId = 2, Name = "Люкс", Description = "Люкс", Capacity = 4, BasePricePerNight = 12000.00m },
                new RoomCategory { RoomCategoryId = 3, Name = "Эконом", Description = "Эконом", Capacity = 1, BasePricePerNight = 2000.00m }
            );

             // --- Номера ---
            modelBuilder.Entity<Room>().HasData(
                new Room { RoomId = 1, RoomNumber = "101", Floor = 1, Status = RoomStatus.Свободно, RoomCategoryId = 1 },
                new Room { RoomId = 2, RoomNumber = "102", Floor = 1, Status = RoomStatus.Занято, RoomCategoryId = 1 }, // Занят
                new Room { RoomId = 3, RoomNumber = "201", Floor = 2, Status = RoomStatus.Свободно, RoomCategoryId = 2 },
                new Room { RoomId = 4, RoomNumber = "202", Floor = 2, Status = RoomStatus.Уборка, RoomCategoryId = 3 } // Уборка
            );

            // --- Услуги ---
             modelBuilder.Entity<Service>().HasData(
                new Service { ServiceId = 1, Name = "Завтрак", Description = "Континентальный завтрак", Price = 350.00m },
                new Service { ServiceId = 2, Name = "Прачечная", Description = "Стирка и глажка", Price = 205.00m },
                new Service { ServiceId = 3, Name = "Wi-Fi", Description = "Беспроводной интернет", Price = 0.00m },
                new Service { ServiceId = 4, Name = "Парковка", Description = "Место на парковке", Price = 500.00m }
            );

            // --- Пользователи ---
            // Пароли хешированы с использованием BCrypt.Net.BCrypt.HashPassword(plainPassword)
            // Plain passwords for seeding: "admin_password", "reception_password"
            modelBuilder.Entity<User>().HasData(
                new User { UserId = 1, Username = "admin", PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"), FullName = "Администратор Системы", Role = UserRole.Administrator },
                new User { UserId = 2, Username = "reception", PasswordHash = BCrypt.Net.BCrypt.HashPassword("reception123"), FullName = "Иван Петров", Role = UserRole.Receptionist }
            );
            
            // --- Гости ---
            modelBuilder.Entity<Guest>().HasData(
                new Guest { GuestId = 1, FirstName = "Алиса", LastName = "Иванова", PhoneNumber = "+79001112233", Email = "alice@example.com" },
                new Guest { GuestId = 2, FirstName = "Борис", LastName = "Смирнов", PhoneNumber = "+79114445566", Email = "boris@sample.org" },
                new Guest { GuestId = 3, FirstName = "Виктор", LastName = "Кузнецов", PhoneNumber = "+79227778899" } // Без email
            );

            // --- Бронирования ---
            // Обратите внимание: RoomId = 2 соответствует номеру 102, который помечен как Занято
            var booking1Date = DateTime.UtcNow.AddDays(-5);
            modelBuilder.Entity<Booking>().HasData(
                new Booking
                {
                    BookingId = 1,
                    CheckInDate = booking1Date.Date,
                    CheckOutDate = booking1Date.AddDays(3).Date,
                    BookingDate = booking1Date.AddDays(-1), // Забронировали за день до заезда
                    Status = BookingStatus.Проверено, // Гость сейчас в отеле
                    TotalPrice = 165.00m, // (50 * 3 ночи) + 15 завтрак
                    GuestId = 1, // Алиса Иванова
                    RoomId = 2    // Номер 102 (Standard)
                }
            );

            var booking2Date = DateTime.UtcNow.AddDays(10);
             modelBuilder.Entity<Booking>().HasData(
                 new Booking
                 {
                     BookingId = 2,
                     CheckInDate = booking2Date.Date,
                     CheckOutDate = booking2Date.AddDays(7).Date,
                     BookingDate = DateTime.UtcNow, // Забронировали сегодня
                     Status = BookingStatus.Подтверждено, // Бронь подтверждена
                     TotalPrice = 840.00m, // (120 * 7 ночей)
                     GuestId = 2, // Борис Смирнов
                     RoomId = 3    // Номер 201 (Suite)
                 }
             );

            // --- Заказанные услуги ---
            modelBuilder.Entity<BookedService>().HasData(
                // Услуга для бронирования 1 (Алиса Иванова)
                new BookedService
                {
                    BookedServiceId = 1,
                    Quantity = 1,
                    DateProvided = booking1Date.AddDays(1), // Завтрак на следующий день после заезда
                    BookingId = 1,
                    ServiceId = 1 // Завтрак
                }
                // Можно добавить больше услуг для других бронирований, если нужно
                // new BookedService { BookedServiceId = 2, Quantity = 1, DateProvided = booking2Date.AddDays(1), BookingId = 2, ServiceId = 4 } // Парковка для Бориса
            );
        }
    }
}