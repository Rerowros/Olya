using App1.Data;
using App1.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace App1.Pages
{
    public sealed partial class ServicesPage : Page
    {
        public ObservableCollection<Service> Services { get; private set; } = new();

        public ServicesPage() => InitializeComponent();

        public void LoadData(ObservableCollection<Service> services)
        {
            Services = services;
            // Убедимся, что ListView использует обновленную коллекцию
            if (ServicesListView != null)
            {
                ServicesListView.ItemsSource = Services;
            }
            Bindings.Update(); // Обновляем привязки, если они используются где-то еще
        }

        private async void AddService_Click(object sender, RoutedEventArgs e)
        {
            await ShowServiceDialogAsync(null);
        }

        private async void EditService_Click(object sender, RoutedEventArgs e)
        {
            var selectedService = ServicesListView.SelectedItem as Service;
            if (selectedService == null)
            {
                await ShowInfoDialogAsync("Выберите услугу для редактирования");
                return;
            }
            await ShowServiceDialogAsync(selectedService);
        }

        private async void DeleteService_Click(object sender, RoutedEventArgs e)
        {
            var selectedService = ServicesListView.SelectedItem as Service;
            if (selectedService == null)
            {
                await ShowInfoDialogAsync("Выберите услугу для удаления");
                return;
            }

            ContentDialog deleteDialog = new ContentDialog
            {
                Title = "Удаление услуги",
                Content = $"Вы действительно хотите удалить услугу \"{selectedService.Name}\"?",
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
                    // Находим сущность в контексте или прикрепляем ее
                    var serviceToDelete = await db.Services.FindAsync(selectedService.ServiceId);
                    if (serviceToDelete != null)
                    {
                        db.Services.Remove(serviceToDelete);
                        await db.SaveChangesAsync();
                        Services.Remove(selectedService); // Удаляем из ObservableCollection
                    }
                    else
                    {
                         await ShowErrorDialogAsync("Услуга не найдена в базе данных для удаления.");
                    }
                }
                catch (Exception ex)
                {
                    // Обработка возможных ошибок при удалении (например, связанных с внешними ключами)
                    await ShowErrorDialogAsync($"Ошибка при удалении услуги: {ex.Message}");
                }
            }
        }

        private async Task ShowServiceDialogAsync(Service serviceToEdit)
        {
            // Создаем элементы диалога
            var nameBox = new TextBox
            {
                Header = "Название услуги",
                PlaceholderText = "Введите название",
                Text = serviceToEdit?.Name ?? string.Empty,
                AcceptsReturn = false,
            };

            var priceBox = new NumberBox
            {
                Header = "Цена",
                Value = (double)(serviceToEdit?.Price ?? 0m), // Используем double для NumberBox
                Minimum = 0,
                SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact,
                NumberFormatter = new Windows.Globalization.NumberFormatting.DecimalFormatter { FractionDigits = 2 } // Форматирование до 2 знаков после запятой
            };

            // Создаем панель для размещения элементов ввода
            var panel = new StackPanel { Spacing = 10 };
            panel.Children.Add(nameBox);
            panel.Children.Add(priceBox);

            // Создаем и настраиваем диалог
            var dialog = new ContentDialog
            {
                Title = serviceToEdit == null ? "Добавление новой услуги" : "Редактирование услуги",
                Content = panel,
                PrimaryButtonText = serviceToEdit == null ? "Добавить" : "Сохранить",
                CloseButtonText = "Отмена",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            // Показываем диалог и обрабатываем результат
            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                // Валидация ввода
                if (string.IsNullOrWhiteSpace(nameBox.Text))
                {
                    await ShowErrorDialogAsync("Название услуги не может быть пустым.");
                    // Повторно открываем диалог с введенными данными
                    await ShowServiceDialogAsync(new Service { Name = nameBox.Text, Price = (decimal)priceBox.Value });
                    return;
                }
                if (priceBox.Value < 0)
                {
                     await ShowErrorDialogAsync("Цена не может быть отрицательной.");
                     // Повторно открываем диалог с введенными данными
                     await ShowServiceDialogAsync(new Service { Name = nameBox.Text, Price = (decimal)priceBox.Value });
                     return;
                }


                try
                {
                    using var db = new AppDbContext();
                    Service service;

                    if (serviceToEdit == null) // Добавление новой услуги
                    {
                        service = new Service
                        {
                            Name = nameBox.Text,
                            Price = (decimal)priceBox.Value // Преобразуем double обратно в decimal
                        };
                        db.Services.Add(service);
                        await db.SaveChangesAsync();
                        Services.Add(service); // Добавляем в ObservableCollection
                    }
                    else // Редактирование существующей услуги
                    {
                        // Находим существующую услугу в контексте БД
                        service = await db.Services.FindAsync(serviceToEdit.ServiceId);
                        if (service != null)
                        {
                            // Обновляем свойства
                            service.Name = nameBox.Text;
                            service.Price = (decimal)priceBox.Value; // Преобразуем double обратно в decimal

                            db.Services.Update(service);
                            await db.SaveChangesAsync();

                            // Обновляем объект в ObservableCollection
                            // Необходимо найти и обновить объект в коллекции,
                            // чтобы UI корректно отобразил изменения.
                            var serviceInCollection = Services.FirstOrDefault(s => s.ServiceId == serviceToEdit.ServiceId);
                            if (serviceInCollection != null)
                            {
                                serviceInCollection.Name = service.Name;
                                serviceInCollection.Price = service.Price;
                                // Для обновления UI может потребоваться дополнительная логика,
                                // если Service не реализует INotifyPropertyChanged.
                                // В данном случае ObservableCollection сама уведомит ListView.
                                // Перезагрузка данных не требуется, но может быть альтернативой.
                                var index = Services.IndexOf(serviceInCollection);
                                Services[index] = serviceInCollection; // Форсируем обновление элемента
                            }
                        }
                         else
                        {
                            await ShowErrorDialogAsync("Услуга не найдена в базе данных для обновления.");
                            return; // Выход, если услуга не найдена
                        }
                    }
                    // Обновление привязок может быть излишним, если ObservableCollection используется правильно
                    // Bindings.Update();
                }
                catch (Exception ex)
                {
                    await ShowErrorDialogAsync($"Ошибка при сохранении услуги: {ex.Message}");
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