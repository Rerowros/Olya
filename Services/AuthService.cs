using App1.Models;

namespace App1.Services
{
    public static class AuthService
    {
        public static User? CurrentUser { get; private set; }
        
        public static bool IsLoggedIn => CurrentUser != null;

        public static void Login(User user)
        {
            CurrentUser = user;
            System.Diagnostics.Debug.WriteLine($"User '{user.Username}' logged in.");
        }

        public static void Logout()
        {
            System.Diagnostics.Debug.WriteLine($"User '{CurrentUser?.Username}' logging out.");
            CurrentUser = null;
        }
    }
}