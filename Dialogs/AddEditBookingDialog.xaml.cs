using App1.Models;
using App1.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace App1.Dialogs
{
    // Диалог для добавления или редактирования бронирования
    public sealed partial class AddEditBookingDialog : ContentDialog, INotifyPropertyChanged
    {
        public Booking CurrentBooking { get; private set; }
        private readonly DatabaseService _dbService;
        private readonly bool _isEditMode;

        // Списки для выпадающих списков гостей и номеров
        public ObservableCollection<Guest> Guests { get; } = new();
        public ObservableCollection<Room> AvailableRooms { get; } = new();

        // Выбранный гость
        private Guest? _selectedGuest;
        public Guest? SelectedGuest
        {
            get => _selectedGuest;
            set
            {
                if (SetField(ref _selectedGuest, value))
                {
                    UpdateBookingDetails();
                }
            }
        }

        // Выбранный номер
        private Room? _selectedRoom;
        public Room? SelectedRoom
        {
            get => _selectedRoom;
            set
            {
                if (SetField(ref _selectedRoom, value))
                {
                    UpdateBookingDetails();
                }
            }
        }

        // Дата заезда
        private DateTimeOffset _checkInDate;
        public DateTimeOffset CheckInDate 
        {
            get => _checkInDate;
            set
            {
                if (SetField(ref _checkInDate, value))
                {
                    HandleDateChange();
                }
            }
        }

        // Дата выезда
        private DateTimeOffset _checkOutDate;
        public DateTimeOffset CheckOutDate
        {
            get => _checkOutDate;
            set
            {
                 if (SetField(ref _checkOutDate, value))
                 {
                     HandleDateChange();
                 }
            }
        }

        // Текст с итоговой ценой
        private string _totalPriceDisplay = "Цена не рассчитана";
        public string TotalPriceDisplay
        {
            get => _totalPriceDisplay;
            private set => SetField(ref _totalPriceDisplay, value);
        }

        // Текст ошибки валидации
        private string _validationError = string.Empty;
        public string ValidationError
        {
            get => _validationError;
            private set => SetField(ref _validationError, value);
        }

        // Видимость текста ошибки
        private Visibility _validationErrorVisibility = Visibility.Collapsed;
        public Visibility ValidationErrorVisibility
        {
             get => _validationErrorVisibility;
             private set => SetField(ref _validationErrorVisibility, value);
        }

        // Конструктор для добавления
        public AddEditBookingDialog(DatabaseService dbService)
        {
            this.InitializeComponent();
            _dbService = dbService;
            _isEditMode = false;
            this.Title = "Новое бронирование";
            _checkInDate = DateTimeOffset.Now.Date;
            _checkOutDate = DateTimeOffset.Now.Date.AddDays(1);
            CurrentBooking = new Booking
            {
                CheckInDate = _checkInDate.DateTime,
                CheckOutDate = _checkOutDate.DateTime,
                Status = BookingStatus.Подтверждено
            };
        }

        // Конструктор для редактирования
        public AddEditBookingDialog(DatabaseService dbService, Booking bookingToEdit)
        {
            this.InitializeComponent();
            _dbService = dbService;
            CurrentBooking = bookingToEdit;
            _isEditMode = true;
            this.Title = $"Редактировать бронирование №{bookingToEdit.BookingId}";
            _checkInDate = new DateTimeOffset(bookingToEdit.CheckInDate);
            _checkOutDate = new DateTimeOffset(bookingToEdit.CheckOutDate);
        }

        // Загружаем данные при открытии окна
        private async void ContentDialog_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadAndSetInitialDataAsync();
        }

        // Загружаем гостей и номера
        private async Task LoadAndSetInitialDataAsync()
        {
            try
            {
                var guestsData = await _dbService.GetAllGuestsAsync();
                Guests.Clear();
                foreach (var guest in guestsData.OrderBy(g => g.LastName).ThenBy(g => g.FirstName)) Guests.Add(guest);

                // Если редактируем, выбираем нужного гостя
                if (_isEditMode && CurrentBooking.GuestId > 0)
                {
                    SelectedGuest = Guests.FirstOrDefault(g => g.GuestId == CurrentBooking.GuestId);
                }

                // Загружаем доступные номера
                await LoadAvailableRoomsAsync();

                // Обновляем цену
                UpdatePriceDisplay();
            }
            catch (Exception ex)
            {
                ShowValidationError($"Ошибка загрузки данных: {ex.Message}");
            }
        }

        // Загружаем доступные номера
        private async Task LoadAvailableRoomsAsync()
        {
            RoomComboBox.IsEnabled = false;
            AvailableRooms.Clear();
            int originalRoomId = _isEditMode ? CurrentBooking.RoomId : 0;
            Room? originalRoom = null;

            try
            {
                var available = await _dbService.GetAvailableRoomsAsync(CheckInDate.DateTime, CheckOutDate.DateTime);
                foreach (var room in available.OrderBy(r => r.RoomNumber)) AvailableRooms.Add(room);

                // Если редактируем, добавляем исходный номер, если его нет в списке
                if (originalRoomId > 0 && !AvailableRooms.Any(r => r.RoomId == originalRoomId))
                {
                    originalRoom = await _dbService.GetRoomByIdAsync(originalRoomId);
                    if (originalRoom != null)
                    {
                        AvailableRooms.Insert(0, originalRoom);
                    }
                }

                // Выбираем нужный номер
                 if(originalRoomId > 0)
                 {
                     SelectedRoom = AvailableRooms.FirstOrDefault(r => r.RoomId == originalRoomId);
                 }
                 else
                 {
                      SelectedRoom = null;
                 }
            }
            catch (Exception ex)
            {
                 ShowValidationError($"Не удалось загрузить доступные номера: {ex.Message}");
            }
            finally
            {
                RoomComboBox.IsEnabled = true;
            }
        }

        // Обработка изменения дат
        private async void HandleDateChange()
        {
             if (CheckOutDate < CheckInDate)
             {
                 _checkOutDate = CheckInDate.AddDays(1);
                 OnPropertyChanged(nameof(CheckOutDate));
             }
             await LoadAvailableRoomsAsync();
        }

        // Обновляем данные бронирования при выборе гостя или номера
        private void UpdateBookingDetails()
        {
            if (CurrentBooking == null) return;

            if (SelectedGuest != null)
            {
                CurrentBooking.GuestId = SelectedGuest.GuestId;
            }
             if (SelectedRoom != null)
            {
                CurrentBooking.RoomId = SelectedRoom.RoomId;
            }
            UpdatePriceDisplay();
        }

        // Считаем и показываем цену
        private void UpdatePriceDisplay()
        {
             if (SelectedRoom?.RoomCategory != null && CheckOutDate > CheckInDate)
             {
                 int nights = (CheckOutDate.Date - CheckInDate.Date).Days;
                 if (nights <= 0 && CheckOutDate.DateTime > CheckInDate.DateTime) nights = 1;

                 if (nights > 0)
                 {
                    decimal roomPrice = SelectedRoom.RoomCategory.BasePricePerNight;
                    CurrentBooking.TotalPrice = nights * roomPrice;
                    TotalPriceDisplay = $"Примерная цена: {CurrentBooking.TotalPrice:C} ({nights} ночей)";
                 }
                 else
                 {
                    CurrentBooking.TotalPrice = 0;
                    TotalPriceDisplay = "Проверьте даты";
                 }
             }
             else
             {
                 CurrentBooking.TotalPrice = 0;
                 TotalPriceDisplay = "Цена не рассчитана";
             }
        }

        // Кнопка "ОК" — проверяем данные
        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            bool isValid = ValidateInput();
            if (!isValid)
            {
                args.Cancel = true;
                return;
            }

            // Обновляем данные бронирования перед закрытием окна
            CurrentBooking.GuestId = SelectedGuest?.GuestId ?? 0;
            CurrentBooking.RoomId = SelectedRoom?.RoomId ?? 0;
            CurrentBooking.CheckInDate = CheckInDate.DateTime;
            CurrentBooking.CheckOutDate = CheckOutDate.DateTime;

            if (CurrentBooking.GuestId == 0 || CurrentBooking.RoomId == 0)
            {
                 ShowValidationError("Необходимо выбрать гостя и номер.");
                 args.Cancel = true;
                 return;
            }

            HideValidationError();
        }

        // Проверяем правильность введённых данных
        private bool ValidateInput()
        {
            HideValidationError();
            if (SelectedGuest == null) return ShowValidationError("Пожалуйста, выберите гостя.");
            if (SelectedRoom == null) return ShowValidationError("Пожалуйста, выберите номер.");
            if (CheckOutDate <= CheckInDate) return ShowValidationError("Дата выезда должна быть позже даты заезда.");
            return true;
        }

        // Показываем ошибку
        private bool ShowValidationError(string message)
        {
            ValidationError = message;
            ValidationErrorVisibility = Visibility.Visible;
            return false;
        }
        // Скрываем ошибку
        private void HideValidationError()
        {
            ValidationErrorVisibility = Visibility.Collapsed;
            ValidationError = string.Empty;
        }

        // Реализация INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Помогает обновлять значения и уведомлять об изменениях
        private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}