using App1.Models;
using App1.Services;
using App1.Dialogs; 
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace App1.Pages
{
    public sealed partial class BookingsPage : Page
    {
        public ObservableCollection<Booking> Bookings { get; private set; } = new();
        private readonly DatabaseService _dbService = new DatabaseService();

        public BookingsPage()
        {
            this.InitializeComponent();
            if (BookingsListView != null)
            {
                BookingsListView.SelectionChanged += BookingsListView_SelectionChanged;
            }
            UpdateButtonStates();
        }

        public void LoadData(ObservableCollection<Booking> bookings)
        {

            Bookings = bookings;

            if (BookingsListView != null) BookingsListView.ItemsSource = Bookings;

            if (BookingsListView != null) BookingsListView.SelectedItem = null;
            UpdateButtonStates();
        }

        private Booking? GetSelectedBooking()
        {
            return BookingsListView?.SelectedItem as Booking;
        }

        private void BookingsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateButtonStates();
        }

        private void UpdateButtonStates()
        {
            var selectedBooking = GetSelectedBooking();
            bool isSelected = selectedBooking != null;

            EditBookingButton.IsEnabled = isSelected;
            CancelBookingButton.IsEnabled = isSelected && selectedBooking?.Status != BookingStatus.Отменено && selectedBooking?.Status != BookingStatus.Выписано;
            CheckInButton.IsEnabled = isSelected && selectedBooking?.Status == BookingStatus.Подтверждено && selectedBooking?.CheckInDate.Date <= DateTime.Today;
            CheckOutButton.IsEnabled = isSelected && selectedBooking?.Status == BookingStatus.Проверено;
        }
        
        private async void CheckAvailability_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CheckAvailabilityDialog(_dbService);
            dialog.XamlRoot = this.Content?.XamlRoot;
            if (dialog.XamlRoot == null) return;

            await dialog.ShowAsync();
        }

        private async void NewBooking_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddEditBookingDialog(_dbService); 
            dialog.XamlRoot = this.Content?.XamlRoot;
            if (dialog.XamlRoot == null) return;

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary && dialog.CurrentBooking != null)
            {
                var newBooking = dialog.CurrentBooking;
                try
                {
                    newBooking.BookingDate = DateTime.UtcNow;
                    newBooking.Status = BookingStatus.Подтверждено;
                    
                    await _dbService.AddBookingAsync(newBooking); 

                    
                    var addedBooking = await _dbService.GetBookingByIdAsync(newBooking.BookingId);
                    if (addedBooking != null)
                    {
                        Bookings.Add(addedBooking); 
                        BookingsListView.SelectedItem = addedBooking; 
                    }
                    else
                    {
                        await ShowErrorDialogAsync("Бронирование добавлено, но не удалось обновить список.");
                    }
                }
                catch (DbUpdateException dbEx) 
                {
                    System.Diagnostics.Debug.WriteLine($"DbUpdateException: {dbEx.InnerException?.Message ?? dbEx.Message}");
                    await ShowErrorDialogAsync($"Не удалось сохранить бронирование в базе данных: {dbEx.InnerException?.Message ?? dbEx.Message}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Exception: {ex}");
                    await ShowErrorDialogAsync($"Не удалось добавить бронирование: {ex.Message}");
                }
            }
        }

        private async void EditBooking_Click(object sender, RoutedEventArgs e)
        {
            var selectedBooking = GetSelectedBooking();
            if (selectedBooking == null)
            {
                await ShowInfoDialogAsync("Выберите бронирование для редактирования.");
                return;
            }

            try
            {
                var bookingToEdit = await _dbService.GetBookingByIdAsync(selectedBooking.BookingId);
                if (bookingToEdit == null)
                {
                    await ShowErrorDialogAsync("Не удалось загрузить данные выбранного бронирования. Возможно, оно было удалено.");
                    Bookings.Remove(selectedBooking); 
                    return;
                }

                var dialog = new AddEditBookingDialog(_dbService, bookingToEdit); 
                dialog.XamlRoot = this.Content?.XamlRoot;
                 if (dialog.XamlRoot == null) return;

                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary && dialog.CurrentBooking != null)
                {
                    var updatedBookingData = dialog.CurrentBooking;

                    bookingToEdit.GuestId = updatedBookingData.GuestId;
                    bookingToEdit.RoomId = updatedBookingData.RoomId;
                    bookingToEdit.CheckInDate = updatedBookingData.CheckInDate;
                    bookingToEdit.CheckOutDate = updatedBookingData.CheckOutDate;
                    bookingToEdit.TotalPrice = updatedBookingData.TotalPrice;

                    await _dbService.UpdateBookingAsync(bookingToEdit);

                    await RefreshBookingInList(selectedBooking.BookingId);
                }
            }
            catch (DbUpdateException dbEx)
            {
                System.Diagnostics.Debug.WriteLine($"DbUpdateException: {dbEx.InnerException?.Message ?? dbEx.Message}");
                await ShowErrorDialogAsync($"Не удалось сохранить изменения в базе данных: {dbEx.InnerException?.Message ?? dbEx.Message}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception: {ex}");
                await ShowErrorDialogAsync($"Не удалось обновить бронирование: {ex.Message}");
            }
        }

        private async void CancelBooking_Click(object sender, RoutedEventArgs e)
        {
            var selectedBooking = GetSelectedBooking();
            if (selectedBooking == null)
            {
                await ShowInfoDialogAsync("Выберите бронирование для отмены.");
                return;
            }

            if (selectedBooking.Status == BookingStatus.Отменено || selectedBooking.Status == BookingStatus.Выписано)
            {
                await ShowInfoDialogAsync("Это бронирование уже отменено или завершено.");
                return;
            }

            var confirmDialog = new ContentDialog
            {
                Title = "Отмена бронирования",
                Content = $"Вы уверены, что хотите отменить бронирование №{selectedBooking.BookingId} на имя {selectedBooking.Guest?.LastName ?? "N/A"} (Номер: {selectedBooking.Room?.RoomNumber ?? "N/A"})?",
                PrimaryButtonText = "Да, отменить",
                CloseButtonText = "Нет",
                XamlRoot = this.Content?.XamlRoot,
                DefaultButton = ContentDialogButton.Close
            };

            if (confirmDialog.XamlRoot == null) return;
            var result = await confirmDialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                try
                {
                    var bookingToCancel = await _dbService.GetBookingByIdAsync(selectedBooking.BookingId);
                    if (bookingToCancel == null)
                    {
                        await ShowErrorDialogAsync("Не удалось найти бронирование для отмены. Возможно, оно было удалено.");
                        Bookings.Remove(selectedBooking);
                        return;
                    }

                    if (bookingToCancel.Status == BookingStatus.Отменено || bookingToCancel.Status == BookingStatus.Выписано)
                    {
                         await ShowInfoDialogAsync("Бронирование уже было отменено или завершено.");
                         await RefreshBookingInList(selectedBooking.BookingId);
                         return;
                    }

                    bookingToCancel.Status = BookingStatus.Отменено;
                    await _dbService.UpdateBookingAsync(bookingToCancel);

                    if (selectedBooking.Status == BookingStatus.Проверено && bookingToCancel.RoomId > 0)
                    {
                         var room = await _dbService.GetRoomByIdAsync(bookingToCancel.RoomId);
                         if(room != null && room.Status == RoomStatus.Занято)
                         {
                            room.Status = RoomStatus.Уборка;
                            await _dbService.UpdateRoomAsync(room);
                         }
                    }

                    await RefreshBookingInList(selectedBooking.BookingId);
                }
                catch (Exception ex)
                {
                    await ShowErrorDialogAsync($"Не удалось отменить бронирование: {ex.Message}");
                }
            }
        }

        private async void CheckIn_Click(object sender, RoutedEventArgs e)
        {
            var selectedBooking = GetSelectedBooking();
            if (selectedBooking == null)
            {
                await ShowInfoDialogAsync("Выберите бронирование для регистрации заезда.");
                return;
            }

            if (selectedBooking.Status != BookingStatus.Подтверждено)
            {
                await ShowInfoDialogAsync("Зарегистрировать заезд можно только для подтвержденных бронирований.");
                return;
            }
            if (selectedBooking.CheckInDate.Date > DateTime.Today)
            {
                await ShowInfoDialogAsync($"Регистрация заезда возможна не ранее {selectedBooking.CheckInDate:d}.");
                return;
            }

            var confirmDialog = new ContentDialog
            {
                Title = "Регистрация заезда",
                Content = $"Зарегистрировать заезд для бронирования №{selectedBooking.BookingId}?\nГость: {selectedBooking.Guest?.LastName ?? "N/A"}\nНомер: {selectedBooking.Room?.RoomNumber ?? "N/A"}",
                PrimaryButtonText = "Зарегистрировать",
                CloseButtonText = "Отмена",
                XamlRoot = this.Content?.XamlRoot
            };
            if (confirmDialog.XamlRoot == null) return;
            var result = await confirmDialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                try
                {
                    var bookingToCheckIn = await _dbService.GetBookingByIdAsync(selectedBooking.BookingId);
                    if (bookingToCheckIn == null)
                    {
                        await ShowErrorDialogAsync("Не удалось найти бронирование. Возможно, оно было удалено.");
                        Bookings.Remove(selectedBooking);
                        return;
                    }
                     if (bookingToCheckIn.Status != BookingStatus.Подтверждено)
                    {
                         await ShowInfoDialogAsync("Статус бронирования изменился. Невозможно зарегистрировать заезд.");
                         await RefreshBookingInList(selectedBooking.BookingId);
                         return;
                    }

                     var room = await _dbService.GetRoomByIdAsync(bookingToCheckIn.RoomId);
                     if (room == null)
                     {
                         await ShowErrorDialogAsync($"Ошибка: Номер {bookingToCheckIn.Room?.RoomNumber ?? bookingToCheckIn.RoomId.ToString()} не найден.");
                         return;
                     }
                     if (room.Status != RoomStatus.Свободно && room.Status != RoomStatus.Уборка) 
                     {
                         await ShowErrorDialogAsync($"Номер {room.RoomNumber} сейчас не свободен (статус: {room.Status}). Заезд невозможен.");
                         return;
                     }

                    bookingToCheckIn.Status = BookingStatus.Проверено;
                    await _dbService.UpdateBookingAsync(bookingToCheckIn);

                    room.Status = RoomStatus.Занято;
                    await _dbService.UpdateRoomAsync(room);

                    await RefreshBookingInList(selectedBooking.BookingId);
                }
                catch (Exception ex)
                {
                    await ShowErrorDialogAsync($"Ошибка при регистрации заезда: {ex.Message}");
                }
            }
        }

        private async void CheckOut_Click(object sender, RoutedEventArgs e)
        {
            var selectedBooking = GetSelectedBooking();
            if (selectedBooking == null)
            {
                await ShowInfoDialogAsync("Выберите бронирование для регистрации выезда.");
                return;
            }

            if (selectedBooking.Status != BookingStatus.Проверено) 
            {
                await ShowInfoDialogAsync("Зарегистрировать выезд можно только для гостей, которые зарегистрировали заезд.");
                return;
            }


            var confirmDialog = new ContentDialog
            {
                Title = "Регистрация выезда",
                Content = $"Зарегистрировать выезд для бронирования №{selectedBooking.BookingId}?\nГость: {selectedBooking.Guest?.LastName ?? "N/A"}\nНомер: {selectedBooking.Room?.RoomNumber ?? "N/A"}\n\nНе забудьте проверить и выставить окончательный счет.",
                PrimaryButtonText = "Зарегистрировать выезд",
                CloseButtonText = "Отмена",
                XamlRoot = this.Content?.XamlRoot
            };
            if (confirmDialog.XamlRoot == null) return;
            var result = await confirmDialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                try
                {
                    var bookingToCheckOut = await _dbService.GetBookingByIdAsync(selectedBooking.BookingId);
                    if (bookingToCheckOut == null)
                    {
                        await ShowErrorDialogAsync("Не удалось найти бронирование. Возможно, оно было удалено.");
                        Bookings.Remove(selectedBooking);
                        return;
                    }
                     if (bookingToCheckOut.Status != BookingStatus.Проверено)
                    {
                         await ShowInfoDialogAsync("Статус бронирования изменился. Невозможно зарегистрировать выезд.");
                         await RefreshBookingInList(selectedBooking.BookingId);
                         return;
                    }

                    bookingToCheckOut.Status = BookingStatus.Выписано; 
                    await _dbService.UpdateBookingAsync(bookingToCheckOut);

                    var room = await _dbService.GetRoomByIdAsync(bookingToCheckOut.RoomId);
                    if (room != null)
                    {
                        room.Status = RoomStatus.Уборка; 
                        await _dbService.UpdateRoomAsync(room);
                    }

                    await RefreshBookingInList(selectedBooking.BookingId);
                    
                }
                catch (Exception ex)
                {
                    await ShowErrorDialogAsync($"Ошибка при регистрации выезда: {ex.Message}");
                }
            }
        }

        private async Task RefreshBookingInList(int bookingId)
        {
             var index = Bookings.ToList().FindIndex(b => b.BookingId == bookingId);
             if (index != -1)
             {
                 var refreshedBooking = await _dbService.GetBookingByIdAsync(bookingId);
                 if (refreshedBooking != null)
                 {
                     Bookings[index] = refreshedBooking; 
                     BookingsListView.SelectedItem = refreshedBooking;
                 }
                 else
                 {
                     Bookings.RemoveAt(index); 
                 }
             }
              UpdateButtonStates(); 
        }

        private async Task ShowErrorDialogAsync(string message)
        {
            ContentDialog errorDialog = new ContentDialog
            {
                Title = "Ошибка",
                Content = message,
                CloseButtonText = "Ок",
                XamlRoot = this.Content?.XamlRoot
            };
            if (errorDialog.XamlRoot != null) await errorDialog.ShowAsync();
        }

        private async Task ShowInfoDialogAsync(string message)
        {
            ContentDialog infoDialog = new ContentDialog
            {
                Title = "Информация",
                Content = message,
                CloseButtonText = "Ок",
                XamlRoot = this.Content?.XamlRoot
            };
            if (infoDialog.XamlRoot != null) await infoDialog.ShowAsync();
        }
    }
}
