using System.Net.Http.Json;
using Microsoft.Maui.Storage;
using RecipeApp.Models;

namespace RecipeApp.Services
{
    public class AuthService
    {
        private readonly string _apiKey = "AIzaSyD84ugws_R3P_o3ksw7FECvnoT1B8C1Xdo";
        private readonly HttpClient _httpClient = new();

        public FirebaseAuthResponse CurrentUser { get; private set; }

        public async Task<bool> RegisterAsync(string email, string password)
        {
            var request = new
            {
                email,
                password,
                returnSecureToken = true
            };

            var url = $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={_apiKey}";
            var response = await _httpClient.PostAsJsonAsync(url, request);

            if (!response.IsSuccessStatusCode) return false;

            CurrentUser = await response.Content.ReadFromJsonAsync<FirebaseAuthResponse>();
            SaveSession();
            return true;
        }

        public async Task<bool> LoginAsync(string email, string password)
        {
            var request = new
            {
                email,
                password,
                returnSecureToken = true
            };

            var url = $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={_apiKey}";
            var response = await _httpClient.PostAsJsonAsync(url, request);

            if (!response.IsSuccessStatusCode) return false;

            CurrentUser = await response.Content.ReadFromJsonAsync<FirebaseAuthResponse>();
            SaveSession();
            return true;
        }

        public void SaveSession()
        {
            Preferences.Set("idToken", CurrentUser?.IdToken);
            Preferences.Set("userId", CurrentUser?.LocalId);
        }

        public void LoadSession()
        {
            if (IsLoggedIn())
            {
                CurrentUser = new FirebaseAuthResponse
                {
                    IdToken = Preferences.Get("idToken", null),
                    LocalId = Preferences.Get("userId", null)
                };
            }
        }

        public bool IsLoggedIn()
        {
            return Preferences.ContainsKey("idToken") && Preferences.ContainsKey("userId");
        }
        public async Task<bool> SendPasswordResetEmailAsync(string email)
        {
            var url = $"https://identitytoolkit.googleapis.com/v1/accounts:sendOobCode?key={_apiKey}";

            var data = new
            {
                requestType = "PASSWORD_RESET",
                email = email
            };

            var response = await _httpClient.PostAsJsonAsync(url, data);
            return response.IsSuccessStatusCode;
        }

        public void Logout()
        {
            Preferences.Remove("idToken");
            Preferences.Remove("userId");
            CurrentUser = null;
        }

        public string GetUserId() => CurrentUser?.LocalId;
        public string GetIdToken() => CurrentUser?.IdToken;
    }
}
