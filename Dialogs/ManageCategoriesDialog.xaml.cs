using App1.Data;
using App1.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Globalization.NumberFormatting;
using App1.Services;
using Microsoft.EntityFrameworkCore;

namespace App1.Dialogs
{
    public sealed partial class ManageCategoriesDialog : ContentDialog
    {
        // Список категорий для отображения
        public ObservableCollection<RoomCategory> Categories { get; } = new();
        // Сервис для работы с базой данных
        private readonly DatabaseService _databaseService;

        public ManageCategoriesDialog()
        {
            this.InitializeComponent();
            _databaseService = new DatabaseService(); // Создаём сервис
            this.Loaded += ManageCategoriesDialog_Loaded;
        }

        // Загружаем категории при открытии окна
        private async void ManageCategoriesDialog_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadCategoriesAsync();
        }

        // Загружаем категории из базы данных
        private async Task LoadCategoriesAsync()
        {
            Categories.Clear();
            try
            {
                var categoriesFromDb = await _databaseService.GetAllRoomCategoriesAsync();
                foreach (var category in categoriesFromDb.OrderBy(c => c.Name))
                {
                    Categories.Add(category);
                }
            }
            catch (Exception ex)
            {
                await ShowErrorDialogAsync($"Ошибка загрузки категорий: {ex.Message}");
            }
        }

        // Кнопка "Добавить категорию"
        private async void AddCategory_Click(object sender, RoutedEventArgs e)
        {
            await ShowEditCategoryDialogAsync(null); // null — добавление новой
        }

        // Кнопка "Редактировать категорию"
        private async void EditCategory_Click(object sender, RoutedEventArgs e)
        {
            var listView = this.FindName("CategoriesListView") as ListView;
            var selectedCategory = listView?.SelectedItem as RoomCategory;
            if (selectedCategory == null)
            {
                await ShowInfoDialogAsync("Выберите категорию для редактирования.");
                return;
            }
            await ShowEditCategoryDialogAsync(selectedCategory);
        }

        // Кнопка "Удалить категорию"
        private async void DeleteCategory_Click(object sender, RoutedEventArgs e)
        {
            var listView = this.FindName("CategoriesListView") as ListView;
            var selectedCategory = listView?.SelectedItem as RoomCategory;
            if (selectedCategory == null)
            {
                await ShowInfoDialogAsync("Выберите категорию для удаления.");
                return;
            }

            // Проверяем, используется ли категория в номерах
            bool isUsed = false;
            try
            {
                using var dbCheck = new AppDbContext();
                isUsed = await dbCheck.Rooms.AnyAsync(r => r.RoomCategoryId == selectedCategory.RoomCategoryId);
            }
            catch (Exception ex)
            {
                 await ShowErrorDialogAsync($"Ошибка проверки использования категории: {ex.Message}");
                 return;
            }

            if (isUsed)
            {
                await ShowInfoDialogAsync($"Категорию '{selectedCategory.Name}' нельзя удалить, так как она используется.");
                return;
            }

            // Подтверждение удаления
            var deleteDialog = new ContentDialog
            {
                Title = "Удаление категории",
                Content = $"Вы действительно хотите удалить категорию '{selectedCategory.Name}'?",
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
                    db.RoomCategories.Remove(selectedCategory);
                    await db.SaveChangesAsync();
                    Categories.Remove(selectedCategory); // Удаляем из списка
                }
                catch (Exception ex)
                {
                    await ShowErrorDialogAsync($"Ошибка при удалении категории: {ex.Message}");
                }
            }
        }

        // Окно для добавления или редактирования категории
        private async Task ShowEditCategoryDialogAsync(RoomCategory categoryToEdit)
        {
            bool isNew = categoryToEdit == null;
            var category = isNew ? new RoomCategory() : categoryToEdit;

            // Поля для ввода
            var nameBox = new TextBox
            {
                Header = "Название категории",
                Text = category.Name ?? string.Empty,
                PlaceholderText = "Например, Стандарт, Люкс"
            };
            var capacityBox = new NumberBox
            {
                Header = "Вместимость (чел.)",
                Value = category.Capacity > 0 ? category.Capacity : 1,
                Minimum = 1,
                SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact
            };
            var priceBox = new NumberBox
            {
                Header = "Базовая цена за ночь (₽)",
                Value = (double)(category.BasePricePerNight > 0 ? category.BasePricePerNight : 1000m),
                Minimum = 0,
                SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact,
                NumberFormatter = new DecimalFormatter { FractionDigits = 2, IsGrouped = true }
            };

            var panel = new StackPanel { Spacing = 10 };
            panel.Children.Add(nameBox);
            panel.Children.Add(capacityBox);
            panel.Children.Add(priceBox);

            var editDialog = new ContentDialog
            {
                Title = isNew ? "Добавить категорию" : "Редактировать категорию",
                Content = panel,
                PrimaryButtonText = isNew ? "Добавить" : "Сохранить",
                CloseButtonText = "Отмена",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            var result = await editDialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                // Проверяем, что имя не пустое
                if (string.IsNullOrWhiteSpace(nameBox.Text))
                {
                    await ShowErrorDialogAsync("Название категории не может быть пустым.");
                    return;
                }

                // Сохраняем данные
                category.Name = nameBox.Text.Trim();
                category.Capacity = (int)capacityBox.Value;
                category.BasePricePerNight = (decimal)priceBox.Value;

                try
                {
                    await LoadCategoriesAsync();
                }
                catch (DbUpdateException dbEx)
                {
                    var innerExceptionMessage = dbEx.InnerException?.Message ?? dbEx.Message;
                    await ShowErrorDialogAsync($"Ошибка базы данных при сохранении категории: {innerExceptionMessage}");
                }
                catch (Exception ex)
                {
                    await ShowErrorDialogAsync($"Ошибка сохранения категории: {ex.Message}");
                }
            }
        }

        // Показываем информационное сообщение
        private async Task ShowInfoDialogAsync(string message)
        {
            if (this.XamlRoot == null) return;
            ContentDialog infoDialog = new ContentDialog
            {
                Title = "Информация",
                Content = message,
                CloseButtonText = "ОК",
                XamlRoot = this.XamlRoot
            };
            try
            {
                await infoDialog.ShowAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to show info dialog: {ex.Message}");
            }
        }

        // Показываем сообщение об ошибке
        private async Task ShowErrorDialogAsync(string message)
        {
            if (this.XamlRoot == null) return;
            ContentDialog errorDialog = new ContentDialog
            {
                Title = "Ошибка",
                Content = message,
                CloseButtonText = "ОК",
                XamlRoot = this.XamlRoot
            };
            try
            {
                await errorDialog.ShowAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to show error dialog: {ex.Message}");
            }
        }
    }
}