using System;
using System.Collections.ObjectModel;
using App1.Data;
using App1.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Linq;

namespace App1.Pages
{
    public sealed partial class GuestsPage : Page
    {
        public ObservableCollection<Guest> Guests { get; private set; } = new();

        public GuestsPage() => InitializeComponent();

        public void LoadData(ObservableCollection<Guest> guests)
        {
            Guests = guests;
            Bindings.Update();
        }

        private async void FindGuest_Click(object sender, RoutedEventArgs e)
        {
            var term = GuestSearchBox.Text.Trim();
            using var db = new AppDbContext();
            var filtered = await db.Guests
                                   .Where(g => g.FirstName.Contains(term) || g.LastName.Contains(term))
                                   .ToListAsync();
            Guests.Clear();
            foreach (var g in filtered)
                Guests.Add(g);
        }

        private async void AddGuest_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                Title = "Добавить гостя",
                PrimaryButtonText = "Добавить",
                CloseButtonText = "Отмена",
                XamlRoot = this.XamlRoot
            };
            var panel = new StackPanel();
            var fn = new TextBox { PlaceholderText = "Имя" };
            var ln = new TextBox { PlaceholderText = "Фамилия" };
            var phone = new TextBox { PlaceholderText = "Телефон" };
            var email = new TextBox { PlaceholderText = "Email" };
            panel.Children.Add(fn);
            panel.Children.Add(ln);
            panel.Children.Add(phone);
            panel.Children.Add(email);
            dialog.Content = panel;

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                var newGuest = new Guest
                {
                    FirstName = fn.Text,
                    LastName = ln.Text,
                    PhoneNumber = phone.Text,
                    Email = email.Text
                };
                using var db = new AppDbContext();
                db.Guests.Add(newGuest);
                await db.SaveChangesAsync();
                Guests.Add(newGuest);
            }
        }

        private async void EditGuest_Click(object sender, RoutedEventArgs e)
        {
            if (GuestsListView.SelectedItem is not Guest sel) return;

            var dialog = new ContentDialog
            {
                Title = "Редактировать гостя",
                PrimaryButtonText = "Сохранить",
                CloseButtonText = "Отмена",
                XamlRoot = this.XamlRoot
            };
            var panel = new StackPanel();
            var fn = new TextBox { Text = sel.FirstName };
            var ln = new TextBox { Text = sel.LastName };
            var phone = new TextBox { Text = sel.PhoneNumber };
            var email = new TextBox { Text = sel.Email };
            panel.Children.Add(fn);
            panel.Children.Add(ln);
            panel.Children.Add(phone);
            panel.Children.Add(email);
            dialog.Content = panel;

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                using var db = new AppDbContext();
                var guest = await db.Guests.FindAsync(sel.GuestId);
                if (guest != null)
                {
                    guest.FirstName = fn.Text;
                    guest.LastName = ln.Text;
                    guest.PhoneNumber = phone.Text;
                    guest.Email = email.Text;
                    await db.SaveChangesAsync();

                    sel.FirstName = fn.Text;
                    sel.LastName = ln.Text;
                    sel.PhoneNumber = phone.Text;
                    sel.Email = email.Text;
                    Bindings.Update();
                }
            }
        }

        private async void DeleteGuest_Click(object sender, RoutedEventArgs e)
        {
            if (GuestsListView.SelectedItem is not Guest sel) return;

            var confirm = new ContentDialog
            {
                Title = "Удалить гостя?",
                Content = $"{sel.FirstName} {sel.LastName}",
                PrimaryButtonText = "Да",
                CloseButtonText = "Нет",
                XamlRoot = this.XamlRoot
            };
            if (await confirm.ShowAsync() == ContentDialogResult.Primary)
            {
                using var db = new AppDbContext();
                var guest = await db.Guests.FindAsync(sel.GuestId);
                if (guest != null)
                {
                    db.Guests.Remove(guest);
                    await db.SaveChangesAsync();
                    Guests.Remove(sel);
                }
            }
        }
    }
}