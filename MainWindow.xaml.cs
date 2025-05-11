using App1.Data;
using App1.Models;
using App1.Pages;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using App1.Services;
using Microsoft.UI.Windowing;
using WinRT.Interop;
using Microsoft.UI;
using System.Threading.Tasks;

namespace App1
{
    public sealed partial class MainWindow : Window
    {
        public ObservableCollection<Room> Rooms { get; set; } = new ObservableCollection<Room>();
        public ObservableCollection<Booking> Bookings { get; set; } = new ObservableCollection<Booking>();
        public ObservableCollection<Guest> Guests { get; set; } = new ObservableCollection<Guest>();
        public ObservableCollection<Service> Services { get; set; } = new ObservableCollection<Service>();

        private readonly LoginPage _loginPage;
        private readonly RoomsPage _roomsPage;
        private readonly BookingsPage _bookingsPage;
        private readonly GuestsPage _guestsPage;
        private readonly ServicesPage _servicesPage;

        public MainWindow()
        {
            this.InitializeComponent();

            var hwnd = WindowNative.GetWindowHandle(this);
            var appWindow = AppWindow.GetFromWindowId(Win32Interop.GetWindowIdFromWindow(hwnd));
            if (appWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.PreferredMinimumWidth = 850;
                presenter.PreferredMinimumHeight = 550;
            }

            _loginPage = new LoginPage();
            _roomsPage = new RoomsPage();
            _bookingsPage = new BookingsPage();
            _guestsPage = new GuestsPage();
            _servicesPage = new ServicesPage();

            _loginPage.LoginSucceeded += OnLoginSucceeded;
            this.Activated += MainWindow_Activated;
        }

        private async void MainWindow_Activated(object sender, WindowActivatedEventArgs e)
        {
            this.Activated -= MainWindow_Activated;
            
            if (!AuthService.IsLoggedIn)
            {
                ShowLoginPage();
            }
            else
            {
                await ProceedToMainContentAsync();
            }
        }
        
        private void ShowLoginPage()
        {
            // Show AuthFrame with LoginPage
            AuthFrame.Visibility = Visibility.Visible;
            AuthFrame.Content = _loginPage;

            // Hide MainNavView (which contains ContentFrame)
            MainNavView.Visibility = Visibility.Collapsed;
            if (ContentFrame != null) // Good practice to check
            {
                ContentFrame.Content = null; // Clear any old app page
            }
            MainNavView.SelectedItem = null; 

            Rooms.Clear();
            Bookings.Clear();
            Guests.Clear();
            Services.Clear();
        }

        private async void OnLoginSucceeded(object sender, EventArgs e)
        {
            await ProceedToMainContentAsync();
        }

        private async Task ProceedToMainContentAsync()
        {
            // Hide AuthFrame
            AuthFrame.Visibility = Visibility.Collapsed;
            AuthFrame.Content = null; // Clear LoginPage

            // Show MainNavView
            MainNavView.Visibility = Visibility.Visible; 

            await LoadDataAsync(); 

            // Set default page in MainNavView's ContentFrame
            if (ContentFrame != null) // Good practice to check
            {
                ContentFrame.Content = _roomsPage;
                _roomsPage.LoadData(Rooms);
            }

            var firstItem = MainNavView.MenuItems.OfType<NavigationViewItemBase>().FirstOrDefault();
            if (firstItem != null)
            {
                MainNavView.SelectedItem = firstItem;
            }
            else if (MainNavView.MenuItems.Count > 0) 
            {
                MainNavView.SelectedItem = MainNavView.MenuItems[0];
            }
        }

        private void MainNavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (!AuthService.IsLoggedIn) 
            {
                return;
            }
            
            // Navigation now happens in the MainNavView's ContentFrame
            if (args.SelectedItemContainer != null && args.SelectedItemContainer.Tag != null && ContentFrame != null)
            {
                var tag = args.SelectedItemContainer.Tag.ToString();

                switch (tag)
                {
                    case "rooms":
                        ContentFrame.Content = _roomsPage;
                        _roomsPage.LoadData(Rooms); 
                        break;
                    case "bookings":
                        ContentFrame.Content = _bookingsPage;
                        _bookingsPage.LoadData(Bookings);
                        break;
                    case "guests":
                        ContentFrame.Content = _guestsPage;
                        _guestsPage.LoadData(Guests);
                        break;
                    case "services":
                        ContentFrame.Content = _servicesPage;
                        _servicesPage.LoadData(Services);
                        break;
                }
            }
        }

        private async Task LoadDataAsync()
        {
            try 
            {
                using (var db = new AppDbContext())
                {
                    try 
                    {
                        var roomsData = await db.Rooms
                                                .Include(r => r.RoomCategory)
                                                .ToListAsync();
                        Rooms.Clear();
                        foreach (var room in roomsData) Rooms.Add(room);

                        var bookingsData = await db.Bookings
                                                   .Include(b => b.Guest)
                                                   .Include(b => b.Room)
                                                   .ToListAsync();
                        Bookings.Clear();
                        foreach (var booking in bookingsData) Bookings.Add(booking);

                        var guestsData = await db.Guests.ToListAsync();
                        Guests.Clear();
                        foreach (var guest in guestsData) Guests.Add(guest);

                        var servicesData = await db.Services.ToListAsync();
                        Services.Clear();
                        foreach (var service in servicesData) Services.Add(service);
                    }
                    catch (Exception exDbOperation) 
                    {
                        await ShowErrorDialogAsync($"Не удалось выполнить операцию с базой данных: {exDbOperation.Message}");
                    }
                }
            }
            catch (Exception exContext)
            {
                await ShowErrorDialogAsync($"Ошибка при доступе к контексту данных: {exContext.Message}");
            }
        }

        private async Task ShowErrorDialogAsync(string message)
        {
            ContentDialog errorDialog = new ContentDialog
            {
                Title = "Ошибка",
                Content = message,
                CloseButtonText = "Ок",
                XamlRoot = this.Content?.XamlRoot // Ensure this window is activated or Content is set for XamlRoot
            };

            // Ensure XamlRoot is available before showing, especially during early startup
            if (errorDialog.XamlRoot == null && AuthFrame?.XamlRoot != null) {
                 errorDialog.XamlRoot = AuthFrame.XamlRoot; // Try to use AuthFrame's XamlRoot if main one isn't ready
            }
            if (errorDialog.XamlRoot == null && MainNavView?.XamlRoot != null && MainNavView.Visibility == Visibility.Visible) {
                 errorDialog.XamlRoot = MainNavView.XamlRoot; // Or MainNavView's if it's visible
            }


            if (errorDialog.XamlRoot != null)
            {
                try 
                {
                    await errorDialog.ShowAsync();
                }
                catch (Exception exDialog)
                {
                    System.Diagnostics.Debug.WriteLine($"Не удалось показать диалог ошибки (внутренняя ошибка): {exDialog.Message}");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Не удалось показать диалог ошибки (XamlRoot is null): {message}");
            }
        }
        
        public void Logout()
        {
            AuthService.Logout();
            ShowLoginPage(); // This will now correctly use AuthFrame and hide MainNavView
        }
    }
}