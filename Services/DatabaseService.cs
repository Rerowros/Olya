using App1.Data;
using App1.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BCrypt.Net;

namespace App1.Services
{
    public class DatabaseService
    {
        // --- Guest CRUD ---

        public async Task AddGuestAsync(Guest guest)
        {
            using (var db = new AppDbContext())
            {
                db.Guests.Add(guest);
                await db.SaveChangesAsync();
            }
        }

        public async Task<Guest?> GetGuestByIdAsync(int guestId)
        {
            using (var db = new AppDbContext())
            {
                return await db.Guests.FindAsync(guestId);
            }
        }

         public async Task<List<Guest>> GetAllGuestsAsync()
        {
            using (var db = new AppDbContext())
            {
                return await db.Guests.ToListAsync();
            }
        }

        public async Task UpdateGuestAsync(Guest guest)
        {
            using (var db = new AppDbContext())
            {
                db.Entry(guest).State = EntityState.Modified;
                await db.SaveChangesAsync();
            }
        }

        public async Task DeleteGuestAsync(int guestId)
        {
            using (var db = new AppDbContext())
            {
                var guest = await db.Guests.FindAsync(guestId);
                if (guest != null)
                {
                    db.Guests.Remove(guest);
                    await db.SaveChangesAsync();
                }
            }
        }

        // --- Booking CRUD ---

        public async Task AddBookingAsync(Booking booking)
        {
             using (var db = new AppDbContext())
             {
                 // Можно добавить логику расчета TotalPrice здесь перед сохранением
                 db.Bookings.Add(booking);
                 await db.SaveChangesAsync();
             }
        }

         public async Task<Booking?> GetBookingByIdAsync(int bookingId)
        {
            using (var db = new AppDbContext())
            {
                // Включаем связанные данные Guest и Room
                return await db.Bookings
                             .Include(b => b.Guest)
                             .Include(b => b.Room)
                                 .ThenInclude(r => r!.RoomCategory) // Включаем категорию номера
                             .Include(b => b.BookedServices)
                                 .ThenInclude(bs => bs.Service) // Включаем заказанные услуги
                             .FirstOrDefaultAsync(b => b.BookingId == bookingId);
            }
        }

        public async Task<List<Booking>> GetBookingsForDateRangeAsync(DateTime start, DateTime end)
        {
             using (var db = new AppDbContext())
             {
                 return await db.Bookings
                              .Include(b => b.Guest)
                              .Include(b => b.Room)
                              .Where(b => b.CheckInDate < end && b.CheckOutDate > start) // Пересекающиеся бронирования
                              .ToListAsync();
             }
        }

         public async Task UpdateBookingAsync(Booking booking)
        {
            using (var db = new AppDbContext())
            {
                db.Entry(booking).State = EntityState.Modified;
                await db.SaveChangesAsync();
            }
        }

         public async Task DeleteBookingAsync(int bookingId)
        {
            using (var db = new AppDbContext())
            {
                var booking = await db.Bookings.FindAsync(bookingId);
                if (booking != null)
                {
                    // Возможно, потребуется удалить связанные BookedServices вручную,
                    // если не настроено каскадное удаление
                    var bookedServices = await db.BookedServices
                                                 .Where(bs => bs.BookingId == bookingId)
                                                 .ToListAsync();
                    db.BookedServices.RemoveRange(bookedServices);

                    db.Bookings.Remove(booking);
                    await db.SaveChangesAsync();
                }
            }
        }
         
         // --- Room CRUD ---

        public async Task AddRoomAsync(Room room)
        {
            using (var db = new AppDbContext())
            {
                db.Rooms.Add(room);
                await db.SaveChangesAsync();
            }
        }

        public async Task<Room?> GetRoomByIdAsync(int roomId)
        {
            using (var db = new AppDbContext())
            {
                // Включаем категорию номера
                return await db.Rooms
                             .Include(r => r.RoomCategory)
                             .FirstOrDefaultAsync(r => r.RoomId == roomId);
            }
        }

        public async Task<List<Room>> GetAllRoomsAsync()
        {
            using (var db = new AppDbContext())
            {
                // Включаем категорию номера
                return await db.Rooms
                             .Include(r => r.RoomCategory)
                             .ToListAsync();
            }
        }

        public async Task UpdateRoomAsync(Room room)
        {
            using (var db = new AppDbContext())
            {
                db.Entry(room).State = EntityState.Modified;
                await db.SaveChangesAsync();
            }
        }

        public async Task DeleteRoomAsync(int roomId)
        {
            using (var db = new AppDbContext())
            {
                var room = await db.Rooms.FindAsync(roomId);
                if (room != null)
                {
                    // Проверка на связанные бронирования перед удалением (опционально)
                    var hasBookings = await db.Bookings.AnyAsync(b => b.RoomId == roomId);
                    if (hasBookings)
                    {
                        // Можно выбросить исключение или изменить статус комнаты вместо удаления
                        throw new InvalidOperationException("Невозможно удалить комнату с активными бронированиями.");
                    }
                    db.Rooms.Remove(room);
                    await db.SaveChangesAsync();
                }
            }
        }
         
        // --- RoomCategory CRUD ---

        public async Task AddRoomCategoryAsync(RoomCategory category)
        {
            using (var db = new AppDbContext())
            {
                db.RoomCategories.Add(category);
                await db.SaveChangesAsync();
            }
        }

        public async Task<RoomCategory?> GetRoomCategoryByIdAsync(int categoryId)
        {
            using (var db = new AppDbContext())
            {
                return await db.RoomCategories.FindAsync(categoryId);
            }
        }

        public async Task<List<RoomCategory>> GetAllRoomCategoriesAsync()
        {
            using (var db = new AppDbContext())
            {
                return await db.RoomCategories.ToListAsync();
            }
        }

        public async Task UpdateRoomCategoryAsync(RoomCategory category)
        {
            using (var db = new AppDbContext())
            {
                db.Entry(category).State = EntityState.Modified;
                await db.SaveChangesAsync();
            }
        }

        public async Task DeleteRoomCategoryAsync(int categoryId)
        {
            using (var db = new AppDbContext())
            {
                var category = await db.RoomCategories.FindAsync(categoryId);
                if (category != null)
                {
                    // Проверка на связанные комнаты перед удалением (опционально)
                    var hasRooms = await db.Rooms.AnyAsync(r => r.RoomCategoryId == categoryId);
                    if (hasRooms)
                    {
                        throw new InvalidOperationException("Невозможно удалить категорию, к которой привязаны комнаты.");
                    }
                    db.RoomCategories.Remove(category);
                    await db.SaveChangesAsync();
                }
            }
        }
        
        // --- Service CRUD ---

        public async Task AddServiceAsync(Service service)
        {
            using (var db = new AppDbContext())
            {
                db.Services.Add(service);
                await db.SaveChangesAsync();
            }
        }

        public async Task<Service?> GetServiceByIdAsync(int serviceId)
        {
            using (var db = new AppDbContext())
            {
                return await db.Services.FindAsync(serviceId);
            }
        }

        public async Task<List<Service>> GetAllServicesAsync()
        {
            using (var db = new AppDbContext())
            {
                return await db.Services.ToListAsync();
            }
        }

        public async Task UpdateServiceAsync(Service service)
        {
            using (var db = new AppDbContext())
            {
                db.Entry(service).State = EntityState.Modified;
                await db.SaveChangesAsync();
            }
        }

        public async Task DeleteServiceAsync(int serviceId)
        {
            using (var db = new AppDbContext())
            {
                var service = await db.Services.FindAsync(serviceId);
                if (service != null)
                {
                    // Проверка на связанные BookedServices перед удалением (опционально)
                    var hasBookedServices = await db.BookedServices.AnyAsync(bs => bs.ServiceId == serviceId);
                    if (hasBookedServices)
                    {
                        throw new InvalidOperationException("Невозможно удалить услугу, которая была заказана.");
                    }
                    db.Services.Remove(service);
                    await db.SaveChangesAsync();
                }
            }
        }
        
        // --- BookedService CRUD ---

        public async Task AddBookedServiceAsync(BookedService bookedService)
        {
            using (var db = new AppDbContext())
            {
                db.BookedServices.Add(bookedService);
                await db.SaveChangesAsync();
            }
        }

        public async Task<List<Booking>> GetAllBookingsAsync()
        {
            using (var db = new AppDbContext())
            {
                return await db.Bookings
                    .Include(b => b.Guest)
                    .Include(b => b.Room)
                    .ThenInclude(r => r!.RoomCategory)
                    .Include(b => b.BookedServices)
                    .ThenInclude(bs => bs.Service)
                    .ToListAsync();
            }
        }
        
        public async Task<BookedService?> GetBookedServiceByIdAsync(int bookedServiceId)
        {
            using (var db = new AppDbContext())
            {
                // Включаем связанную услугу и бронирование (опционально)
                return await db.BookedServices
                             .Include(bs => bs.Service)
                             .Include(bs => bs.Booking)
                             .FirstOrDefaultAsync(bs => bs.BookedServiceId == bookedServiceId);
            }
        }

        public async Task<List<BookedService>> GetBookedServicesByBookingIdAsync(int bookingId)
        {
            using (var db = new AppDbContext())
            {
                // Включаем связанную услугу
                return await db.BookedServices
                             .Include(bs => bs.Service)
                             .Where(bs => bs.BookingId == bookingId)
                             .ToListAsync();
            }
        }

        public async Task UpdateBookedServiceAsync(BookedService bookedService)
        {
            using (var db = new AppDbContext())
            {
                db.Entry(bookedService).State = EntityState.Modified;
                await db.SaveChangesAsync();
            }
        }

        public async Task DeleteBookedServiceAsync(int bookedServiceId)
        {
            using (var db = new AppDbContext())
            {
                var bookedService = await db.BookedServices.FindAsync(bookedServiceId);
                if (bookedService != null)
                {
                    db.BookedServices.Remove(bookedService);
                    await db.SaveChangesAsync();
                }
            }
        }

        // --- User CRUD ---

        public async Task AddUserAsync(User user, string plainPassword)
        {
            using (var db = new AppDbContext())
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(plainPassword);
                db.Users.Add(user);
                await db.SaveChangesAsync();
            }
        }
        public async Task<User?> GetUserByIdAsync(int userId)
        {
            using (var db = new AppDbContext())
            {
                return await db.Users.FindAsync(userId);
            }
        }
         public async Task<User?> GetUserByUsernameAsync(string username)
        {
            using (var db = new AppDbContext())
            {
                return await db.Users.FirstOrDefaultAsync(u => u.Username == username);
            }
        }
        public async Task<List<User>> GetAllUsersAsync()
        {
            using (var db = new AppDbContext())
            {
                return await db.Users.ToListAsync();
            }
        }

        // This method updates user details BUT NOT the password.
        // Use UpdateUserPasswordAsync for password changes.
        public async Task UpdateUserAsync(User user)
        {
            using (var db = new AppDbContext())
            {
                var existingUser = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == user.UserId);
                if (existingUser == null)
                {
                    throw new KeyNotFoundException($"Пользователь с ID {user.UserId} не найден.");
                }

                // Preserve the existing password hash. Password changes are handled by UpdateUserPasswordAsync.
                user.PasswordHash = existingUser.PasswordHash; 
                
                db.Entry(user).State = EntityState.Modified;
                await db.SaveChangesAsync();
            }
        }

        // Method for safely updating password. Expects a new PLAIN password.
        public async Task UpdateUserPasswordAsync(int userId, string newPlainPassword)
        {
            using (var db = new AppDbContext())
            {
                var user = await db.Users.FindAsync(userId);
                if (user != null)
                {
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPlainPassword);
                    await db.SaveChangesAsync();
                }
                 else
                {
                    throw new KeyNotFoundException($"Пользователь с ID {userId} не найден.");
                }
            }
        }
        public async Task DeleteUserAsync(int userId)
        {
            using (var db = new AppDbContext())
            {
                var user = await db.Users.FindAsync(userId);
                if (user != null)
                {
                    db.Users.Remove(user);
                    await db.SaveChangesAsync();
                }
            }
        }

        // Method for verifying a user's password. Returns the User object on success, null otherwise.
        public async Task<User?> VerifyUserPasswordAsync(string username, string providedPassword)
        {
            using (var db = new AppDbContext())
            {
                var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (user == null)
                {
                    return null; // User not found
                }
                
                if (BCrypt.Net.BCrypt.Verify(providedPassword, user.PasswordHash))
                {
                    return user; // Password is correct
                }
                return null; // Password incorrect
            }
        }

        
        // --- Другие полезные методы ---

        public async Task<List<Room>> GetAvailableRoomsAsync(DateTime checkIn, DateTime checkOut, int categoryId = 0)
        {
            using (var db = new AppDbContext())
            {
                // Находим ID номеров, забронированных на пересекающиеся даты
                var bookedRoomIds = await db.Bookings
                    // This condition finds ANY booking (except cancelled) that overlaps the search dates
                    .Where(b => b.CheckInDate < checkOut && b.CheckOutDate > checkIn && b.Status != BookingStatus.Отменено)
                    .Select(b => b.RoomId)
                    .Distinct()
                    .ToListAsync();

                // Запрашиваем номера, которые НЕ входят в список забронированных
                var query = db.Rooms
                    .Include(r => r.RoomCategory) // Включаем категорию
                    // This excludes ANY room found in bookedRoomIds, regardless of the booking's status (as long as it's not Cancelled)
                    .Where(r => !bookedRoomIds.Contains(r.RoomId) && r.Status != RoomStatus.ТехОбслуживание);

                // ... filter by category ...

                return await query.ToListAsync();
            }
        }

    }
}