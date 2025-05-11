using System;
using App1.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls; 
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using App1.Data;
using Microsoft.EntityFrameworkCore;

namespace App1.Pages
{
    public sealed partial class RoomsPage
    {
        // Разрешаем установку свойства извне или из этого класса
        public ObservableCollection<Room> Rooms { get; private set; } = new();
        private static readonly CultureInfo RussianCulture = new CultureInfo("ru-RU");

        // Для отслеживания текущей сортировки
        private string _currentSortColumn = string.Empty;
        private bool _isAscending = true;

        public RoomsPage() => InitializeComponent();

        public void LoadData(ObservableCollection<Room> rooms)
        {
            Rooms = rooms;
            // Сброс сортировки при загрузке новых данных
            _currentSortColumn = string.Empty;
            _isAscending = true;
            Bindings.Update();
        }

        public static string FormatPrice(decimal price)
        {
            return price.ToString("C", RussianCulture);
        }

        private void SortColumn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not string sortProperty) return;

            // Определяем направление сортировки
            if (_currentSortColumn == sortProperty)
            {
                _isAscending = !_isAscending; // Меняем направление
            }
            else
            {
                _currentSortColumn = sortProperty;
                _isAscending = true; // По умолчанию - по возрастанию
            }

            // Сортировка с использованием LINQ
            IOrderedEnumerable<Room> sortedRooms;
            switch (sortProperty)
            {
                // Обработка вложенных свойств
                case "RoomCategory.Name":
                    sortedRooms = _isAscending
                        ? Rooms.OrderBy(r => r.RoomCategory?.Name)
                        : Rooms.OrderByDescending(r => r.RoomCategory?.Name);
                    break;
                case "RoomCategory.Capacity":
                     sortedRooms = _isAscending
                        ? Rooms.OrderBy(r => r.RoomCategory?.Capacity ?? 0)
                        : Rooms.OrderByDescending(r => r.RoomCategory?.Capacity ?? 0);
                    break;
                case "RoomCategory.BasePricePerNight":
                     sortedRooms = _isAscending
                        ? Rooms.OrderBy(r => r.RoomCategory?.BasePricePerNight ?? 0m)
                        : Rooms.OrderByDescending(r => r.RoomCategory?.BasePricePerNight ?? 0m);
                    break;
                // Обработка прямых свойств
                case "RoomNumber":
                    sortedRooms = _isAscending ? Rooms.OrderBy(r => r.RoomNumber) : Rooms.OrderByDescending(r => r.RoomNumber);
                    break;
                 case "Floor":
                    sortedRooms = _isAscending ? Rooms.OrderBy(r => r.Floor) : Rooms.OrderByDescending(r => r.Floor);
                    break;
                case "Status":
                    sortedRooms = _isAscending ? Rooms.OrderBy(r => r.Status) : Rooms.OrderByDescending(r => r.Status);
                    break;
                default:
                    return; // Неизвестное свойство для сортировки
            }

            // Обновляем коллекцию
            Rooms = new ObservableCollection<Room>(sortedRooms);
            // Уведомляем привязку об изменении всей коллекции
            Bindings.Update(); // Или реализуйте INotifyPropertyChanged для свойства Rooms
        }

private async void AddRoom_Click(object sender, RoutedEventArgs e)
        {
            await ShowRoomDialogAsync(null);
        }

        private async void EditRoom_Click(object sender, RoutedEventArgs e)
        {
            // Получаем выбранную комнату
            var selectedRoom = RoomsListView.SelectedItem as Room;
            if (selectedRoom == null)
            {
                await ShowInfoDialogAsync("Выберите номер для редактирования");
                return;
            }

            await ShowRoomDialogAsync(selectedRoom);
        }

        private async void DeleteRoom_Click(object sender, RoutedEventArgs e)
        {
            var selectedRoom = RoomsListView.SelectedItem as Room;
            if (selectedRoom == null)
            {
                await ShowInfoDialogAsync("Выберите номер для удаления");
                return;
            }

            ContentDialog deleteDialog = new ContentDialog
            {
                Title = "Удаление номера",
                Content = $"Вы действительно хотите удалить номер {selectedRoom.RoomNumber}?",
                PrimaryButtonText = "Удалить",
                CloseButtonText = "Отмена",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            var result = await deleteDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                try
                {
                    using var db = new AppDbContext();
                    db.Rooms.Remove(selectedRoom);
                    await db.SaveChangesAsync();
                    Rooms.Remove(selectedRoom);
                }
                catch (Exception ex)
                {
                    await ShowErrorDialogAsync($"Ошибка при удалении номера: {ex.Message}");
                }
            }
        }

private async Task ShowRoomDialogAsync(Room roomToEdit)
{
    // Получаем все категории комнат для выбора
    ObservableCollection<RoomCategory> categories = new();
    try
    {
        using var db = new AppDbContext();
        var dbCategories = await db.RoomCategories.ToListAsync();
        foreach (var category in dbCategories)
        {
            categories.Add(category);
        }
    }
    catch (Exception ex)
    {
        await ShowErrorDialogAsync($"Ошибка загрузки категорий: {ex.Message}");
        return;
    }

    if (categories.Count == 0)
    {
        await ShowInfoDialogAsync("Сначала добавьте категории номеров");
        return;
    }

    // Создаем элементы диалога
    var roomNumberBox = new TextBox
    {
        Header = "Номер комнаты",
        PlaceholderText = "Введите номер комнаты",
        Text = roomToEdit?.RoomNumber ?? string.Empty
    };

    var floorNumberBox = new NumberBox
    {
        Header = "Этаж",
        Value = roomToEdit?.Floor ?? 1,
        Minimum = 1,
        SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact
    };

    var categoryComboBox = new ComboBox
    {
        Header = "Категория",
        ItemsSource = categories,
        DisplayMemberPath = "Name"
    };
    
    // Корректно устанавливаем выбранную категорию
    if (roomToEdit?.RoomCategoryId > 0)
    {
        var category = categories.FirstOrDefault(c => c.RoomCategoryId == roomToEdit.RoomCategoryId);
        categoryComboBox.SelectedItem = category;
    }
    else
    {
        categoryComboBox.SelectedIndex = 0;
    }

    var statusComboBox = new ComboBox
    {
        Header = "Статус",
        ItemsSource = Enum.GetValues(typeof(RoomStatus))
    };
    statusComboBox.SelectedItem = roomToEdit?.Status ?? RoomStatus.Свободно;

    // Создаем панель для размещения элементов ввода
    var panel = new StackPanel { Spacing = 10 };
    panel.Children.Add(roomNumberBox);
    panel.Children.Add(floorNumberBox);
    panel.Children.Add(categoryComboBox);
    panel.Children.Add(statusComboBox);

    // Создаем и настраиваем диалог
    var dialog = new ContentDialog
    {
        Title = roomToEdit == null ? "Добавление нового номера" : "Редактирование номера",
        Content = panel,
        PrimaryButtonText = roomToEdit == null ? "Добавить" : "Сохранить",
        CloseButtonText = "Отмена",
        DefaultButton = ContentDialogButton.Primary,
        XamlRoot = this.XamlRoot
    };

    // Показываем диалог и обрабатываем результат
    var result = await dialog.ShowAsync();

    if (result == ContentDialogResult.Primary)
    {
        try
        {
            using var db = new AppDbContext();

            Room room;
            var selectedCategory = categoryComboBox.SelectedItem as RoomCategory;
            if (selectedCategory == null)
            {
                await ShowErrorDialogAsync("Не выбрана категория номера");
                return;
            }

            // Для нового номера создаем новый объект
            if (roomToEdit == null)
            {
                room = new Room
                {
                    RoomNumber = roomNumberBox.Text,
                    Floor = (int)floorNumberBox.Value,
                    RoomCategoryId = selectedCategory.RoomCategoryId,
                    Status = (RoomStatus)statusComboBox.SelectedItem
                };
                
                db.Rooms.Add(room);
                await db.SaveChangesAsync();
                
                // После сохранения загружаем номер с категорией для отображения
                var addedRoom = await db.Rooms
                    .Include(r => r.RoomCategory)
                    .FirstOrDefaultAsync(r => r.RoomId == room.RoomId);
                
                if (addedRoom != null)
                {
                    Rooms.Add(addedRoom);
                }
            }
            else
            {
                // Для существующего номера сначала получаем его из БД
                room = await db.Rooms.FindAsync(roomToEdit.RoomId);
                if (room == null)
                {
                    await ShowErrorDialogAsync("Номер не найден в базе данных");
                    return;
                }

                // Обновляем свойства
                room.RoomNumber = roomNumberBox.Text;
                room.Floor = (int)floorNumberBox.Value;
                room.RoomCategoryId = selectedCategory.RoomCategoryId;
                room.Status = (RoomStatus)statusComboBox.SelectedItem;

                db.Rooms.Update(room);
                await db.SaveChangesAsync();

                // После обновления загружаем с категорией
                var updatedRoom = await db.Rooms
                    .Include(r => r.RoomCategory)
                    .FirstAsync(r => r.RoomId == room.RoomId);

                // Обновляем элемент в коллекции
                var index = Rooms.IndexOf(roomToEdit);
                if (index != -1)
                {
                    Rooms[index] = updatedRoom;
                }
            }
            
            // Обновляем отображение
            Bindings.Update();
        }
        catch (Exception ex)
        {
            await ShowErrorDialogAsync($"Ошибка при сохранении номера: {ex.Message}");
        }
    }
}

        private async Task ShowInfoDialogAsync(string message)
        {
            ContentDialog infoDialog = new ContentDialog
            {
                Title = "Информация",
                Content = message,
                CloseButtonText = "ОК",
                XamlRoot = this.XamlRoot
            };
            await infoDialog.ShowAsync();
        }

        private async Task ShowErrorDialogAsync(string message)
        {
            ContentDialog errorDialog = new ContentDialog
            {
                Title = "Ошибка",
                Content = message,
                CloseButtonText = "ОК",
                XamlRoot = this.XamlRoot
            };
            await errorDialog.ShowAsync();
        }
    }
}
    
