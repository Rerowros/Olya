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
    // Диалог для проверки доступности номеров
    public sealed partial class CheckAvailabilityDialog : ContentDialog, INotifyPropertyChanged
    {
        private readonly DatabaseService _dbService;

        // Дата заезда
        private DateTimeOffset _checkInDate = DateTimeOffset.Now.Date;
        public DateTimeOffset CheckInDate
        {
            get => _checkInDate;
            set => SetField(ref _checkInDate, value);
        }

        // Дата выезда
        private DateTimeOffset _checkOutDate = DateTimeOffset.Now.Date.AddDays(1);
        public DateTimeOffset CheckOutDate
        {
            get => _checkOutDate;
            set => SetField(ref _checkOutDate, value);
        }

        // Категории номеров
        public ObservableCollection<RoomCategory> RoomCategories { get; } = new();
        // Доступные номера
        public ObservableCollection<Room> AvailableRooms { get; } = new();

        // Выбранная категория
        private RoomCategory? _selectedCategory;
        public RoomCategory? SelectedCategory
        {
            get => _selectedCategory;
            set => SetField(ref _selectedCategory, value);
        }

        // Текст статуса
        private string _statusText = "Введите даты и нажмите 'Найти'";
        public string StatusText
        {
            get => _statusText;
            set => SetField(ref _statusText, value);
        }

        // Флаг загрузки
        private bool _isLoading = false;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if(SetField(ref _isLoading, value))
                {
                    OnPropertyChanged(nameof(IsFindButtonEnabled));
                }
            }
        }
        // Кнопка "Найти" активна, если не идет загрузка
        public bool IsFindButtonEnabled => !IsLoading;

        public CheckAvailabilityDialog(DatabaseService dbService)
        {
            this.InitializeComponent();
            _dbService = dbService;
            this.Loaded += Dialog_Loaded;
        }

        // Загружаем категории при открытии окна
        private async void Dialog_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadCategoriesAsync();
        }

        // Получаем список категорий из базы
        private async Task LoadCategoriesAsync()
        {
            try
            {
                var categories = await _dbService.GetAllRoomCategoriesAsync();
                RoomCategories.Clear();
                foreach (var cat in categories.OrderBy(c => c.Name)) RoomCategories.Add(cat);
            }
            catch (Exception ex)
            {
                StatusText = $"Ошибка загрузки категорий: {ex.Message}";
            }
        }

        // Поиск доступных номеров
        private async void FindButton_Click(object sender, RoutedEventArgs e)
        {
            AvailableRooms.Clear();
            IsLoading = true;
            StatusText = "Поиск...";

            if (CheckOutDate <= CheckInDate)
            {
                StatusText = "Ошибка: Дата выезда должна быть позже даты заезда.";
                IsLoading = false;
                return;
            }

            try
            {
                int categoryId = SelectedCategory?.RoomCategoryId ?? 0;
                var rooms = await _dbService.GetAvailableRoomsAsync(CheckInDate.DateTime, CheckOutDate.DateTime, categoryId);

                if (rooms.Any())
                {
                    foreach (var room in rooms.OrderBy(r=>r.RoomNumber)) AvailableRooms.Add(room);
                    StatusText = $"Найдено доступных номеров: {rooms.Count}";
                }
                else
                {
                    StatusText = "Свободных номеров на выбранные даты не найдено.";
                }
            }
            catch (Exception ex)
            {
                StatusText = $"Ошибка при поиске: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        // Реализация INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}