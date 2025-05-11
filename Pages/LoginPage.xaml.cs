using App1.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

namespace App1.Pages
{
    public sealed partial class LoginPage : Page
    {
        public event EventHandler LoginSucceeded;
        private readonly DatabaseService _databaseService;

        public LoginPage()
        {
            this.InitializeComponent();
            _databaseService = new DatabaseService();
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorMessageTextBlock.Text = ""; 
            var username = UsernameTextBox.Text;
            var password = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ErrorMessageTextBlock.Text = "Имя и пароль не могут быть пустыми";
                return;
            }

            try
            {
                var user = await _databaseService.VerifyUserPasswordAsync(username, password);

                if (user != null)
                {
                    AuthService.Login(user);
                    LoginSucceeded?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    ErrorMessageTextBlock.Text = "Неверные данные.";
                }
            }
            catch (Exception ex)
            {
                // Log the exception ex
                ErrorMessageTextBlock.Text = "Ошибка. попробуйте ещё раз.";
                System.Diagnostics.Debug.WriteLine($"Login error: {ex}");

            }
        }
    }
}