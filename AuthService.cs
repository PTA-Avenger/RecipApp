using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace RecipeApp.Services
{
    public class AuthService
    {
        private readonly string _firebaseApiKey = "AIzaSyD84ugws_R3P_o3ksw7FECvnoT1B8C1Xdo"; // Replace with your API key
        private readonly string _firebaseProjectId = "recipeapp-2fc64"; // Your project ID
        private readonly HttpClient _httpClient = new();

        private string _currentUserId;
        private string _currentIdToken;

        public string GetUserId() => _currentUserId;
        public string GetIdToken() => _currentIdToken;

        // 1. Register user and store username in Firestore
        public async Task<bool> RegisterAsync(string email, string password, string username)
        {
            try
            {
                // 1. Register with Firebase Auth REST API
                var registerUrl = $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={_firebaseApiKey}";
                var registerPayload = new
                {
                    email,
                    password,
                    returnSecureToken = true
                };

                var regResp = await _httpClient.PostAsJsonAsync(registerUrl, registerPayload);
                if (!regResp.IsSuccessStatusCode) return false;

                var regJson = await regResp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(regJson);

                _currentUserId = doc.RootElement.GetProperty("localId").GetString();
                _currentIdToken = doc.RootElement.GetProperty("idToken").GetString();

                // 2. Save user profile (username and email) in Firestore
                var userProfileUrl = $"https://firestore.googleapis.com/v1/projects/{_firebaseProjectId}/databases/(default)/documents/users/{_currentUserId}";
                var data = new
                {
                    fields = new
                    {
                        email = new { stringValue = email },
                        username = new { stringValue = username }
                    }
                };

                var req = new HttpRequestMessage(HttpMethod.Patch, userProfileUrl)
                {
                    Content = JsonContent.Create(data)
                };
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _currentIdToken);

                var resp = await _httpClient.SendAsync(req);
                return resp.IsSuccessStatusCode;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // 2. Get user profile (username and email) from Firestore
        public async Task<UserProfile> GetCurrentUserAsync()
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrWhiteSpace(userId)) return null;

                var url = $"https://firestore.googleapis.com/v1/projects/{_firebaseProjectId}/databases/(default)/documents/users/{userId}";
                var req = new HttpRequestMessage(HttpMethod.Get, url);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", GetIdToken());

                var resp = await _httpClient.SendAsync(req);
                if (!resp.IsSuccessStatusCode) return null;

                var json = await resp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                var fields = doc.RootElement.GetProperty("fields");
                return new UserProfile
                {
                    UserId = userId,
                    Email = fields.GetProperty("email").GetProperty("stringValue").GetString(),
                    Username = fields.GetProperty("username").GetProperty("stringValue").GetString()
                };
            }
            catch (Exception)
            {
                return null;
            }
        }

        // 3. Update username in Firestore
        public async Task<bool> UpdateUsernameAsync(string newUsername)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(newUsername))
                    return false;

                var url = $"https://firestore.googleapis.com/v1/projects/{_firebaseProjectId}/databases/(default)/documents/users/{userId}?updateMask.fieldPaths=username";
                var data = new
                {
                    fields = new
                    {
                        username = new { stringValue = newUsername }
                    }
                };

                var req = new HttpRequestMessage(HttpMethod.Patch, url)
                {
                    Content = JsonContent.Create(data)
                };
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", GetIdToken());

                var resp = await _httpClient.SendAsync(req);
                return resp.IsSuccessStatusCode;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // 4. Update email via Firebase Auth, and in Firestore
        public async Task<bool> UpdateEmailAsync(string newEmail)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(newEmail))
                    return false;

                // 1. Update email in Firebase Auth
                var updateUrl = $"https://identitytoolkit.googleapis.com/v1/accounts:update?key={_firebaseApiKey}";
                var payload = new
                {
                    idToken = GetIdToken(),
                    email = newEmail,
                    returnSecureToken = true
                };

                var resp = await _httpClient.PostAsJsonAsync(updateUrl, payload);
                if (!resp.IsSuccessStatusCode) return false;

                var json = await resp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                _currentIdToken = doc.RootElement.GetProperty("idToken").GetString();

                // 2. Update email in Firestore
                var firestoreUrl = $"https://firestore.googleapis.com/v1/projects/{_firebaseProjectId}/databases/(default)/documents/users/{userId}?updateMask.fieldPaths=email";
                var data = new
                {
                    fields = new
                    {
                        email = new { stringValue = newEmail }
                    }
                };

                var req = new HttpRequestMessage(HttpMethod.Patch, firestoreUrl)
                {
                    Content = JsonContent.Create(data)
                };
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _currentIdToken);

                var firestoreResp = await _httpClient.SendAsync(req);
                return firestoreResp.IsSuccessStatusCode;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // 5. Update password via Firebase Auth
        public async Task<bool> UpdatePasswordAsync(string newPassword)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(newPassword))
                    return false;

                var updateUrl = $"https://identitytoolkit.googleapis.com/v1/accounts:update?key={_firebaseApiKey}";
                var payload = new
                {
                    idToken = GetIdToken(),
                    password = newPassword,
                    returnSecureToken = true
                };

                var resp = await _httpClient.PostAsJsonAsync(updateUrl, payload);
                if (!resp.IsSuccessStatusCode) return false;

                // Update the local idToken in case Firebase rotates it
                var json = await resp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                _currentIdToken = doc.RootElement.GetProperty("idToken").GetString();

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    // User profile model
    public class UserProfile
    {
        public string UserId { get; set; }
        public string Email { get; set; }
        public string Username { get; set; }
    }
}