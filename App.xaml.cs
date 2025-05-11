using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using App1.Data;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls; 

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace App1
{
    public partial class App : Application
    {
        public App()
        {
            this.InitializeComponent();
        }

        // Восстанавливаем метод инициализации
        private async Task InitializeDatabaseAsync()
        {
            using (var db = new AppDbContext())
            {
                await db.Database.EnsureCreatedAsync();
            }
        }

        // Делаем OnLaunched асинхронным и добавляем инициализацию БД
        protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            m_window = new MainWindow();

            // Получаем DispatcherQueue до попытки инициализации, на случай ошибки
            var dispatcherQueue = m_window.DispatcherQueue;

            try
            {
                // Дожидаемся инициализации БД ПЕРЕД активацией окна
                await InitializeDatabaseAsync();

                // Активация окна ТОЛЬКО после успешной инициализации БД
                m_window.Activate();
            }
            catch (Exception ex)
            {
                // Обработка ошибки инициализации БД
                // Используем DispatcherQueue для показа диалога в UI-потоке
                dispatcherQueue.TryEnqueue(async () =>
                {
                    var dialog = new ContentDialog
                    {
                        Title = "Критическая ошибка",
                        Content = $"Не удалось инициализировать базу данных: {ex.Message}\nПриложение будет закрыто.",
                        CloseButtonText = "OK",
                        // XamlRoot нужно установить после активации окна,
                        // но если активации не было, диалог может не показаться.
                        // В идеале нужна система логирования ошибок на раннем этапе.
                        // Попытаемся использовать XamlRoot активного окна, если оно есть
                        XamlRoot = m_window?.Content?.XamlRoot
                    };

                    // Если XamlRoot доступен, показываем диалог
                    if (dialog.XamlRoot != null)
                    {
                         await dialog.ShowAsync();
                    }
                    else
                    {
                        // Альтернативный вывод, если UI еще не готов
                        System.Diagnostics.Debug.WriteLine($"КРИТИЧЕСКАЯ ОШИБКА ИНИЦИАЛИЗАЦИИ БД (UI недоступен): {ex.Message}");
                    }
                    // Закрываем приложение после критической ошибки инициализации
                    Application.Current.Exit();
                });
            }
        }

        private Window? m_window;
    }
}