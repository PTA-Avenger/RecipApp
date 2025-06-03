using RecipeApp.Pages;
using RecipeApp.Services;
using System.Net.Http.Json;
using System.Net.Mail;
using System.Text.Json;

namespace RecipeApp;

public partial class LoginPage : ContentPage
{
    private readonly AuthService _authService = new();
    private readonly FirestoreService _firestoreService = new();

    public LoginPage()
    {
        InitializeComponent();
    }

    private async Task AnimateButtonAsync(View view)
    {
        await view.ScaleTo(0.95, 75, Easing.CubicIn);
        await view.ScaleTo(1.0, 75, Easing.CubicOut);
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        await AnimateButtonAsync((View)sender);
        var email = EmailEntry.Text?.Trim().ToLower();
        var password = PasswordEntry.Text;

        // ✅ Check for empty fields
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            await DisplayAlert("Missing Info", "Please enter both email and password.", "OK");
            return;
        }

        // ✅ Validate email format
        try
        {
            var addr = new MailAddress(email);
            if (addr.Address != email)
                throw new Exception();
        }
        catch
        {
            await DisplayAlert("Invalid Email", "Please enter a valid email address.", "OK");
            return;
        }

        // ✅ Attempt login with error handling
        var request = new
        {
            email,
            password,
            returnSecureToken = true
        };

        var url = $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key=AIzaSyD84ugws_R3P_o3ksw7FECvnoT1B8C1Xdo"; // Replace with your actual key
        var httpClient = new HttpClient();
        var response = await httpClient.PostAsJsonAsync(url, request);

        if (!response.IsSuccessStatusCode)
        {
            var errorJson = await response.Content.ReadAsStringAsync();
            try
            {
                var doc = JsonDocument.Parse(errorJson);
                var code = doc.RootElement.GetProperty("error").GetProperty("message").GetString();

                string message = code switch
                {
                    "EMAIL_NOT_FOUND" => "No account found with that email. Please register first.",
                    "INVALID_PASSWORD" => "Incorrect password. Please try again.",
                    "USER_DISABLED" => "This account has been disabled.",
                    _ => "Login failed. Please try again."
                };

                await DisplayAlert("Login Error", message, "OK");
            }
            catch
            {
                await DisplayAlert("Login Error", "An unknown error occurred.", "OK");
            }

            return;
        }

        // ✅ Login successful
        bool success = await _authService.LoginAsync(email, password);
        if (success)
        {
            await DisplayAlert("Success", "Logged in", "OK");
            await Navigation.PushAsync(new HomePage(_authService,_firestoreService));
        }
        else
        {
            await DisplayAlert("Error", "Login failed. Please try again.", "OK");
        }
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        await AnimateButtonAsync((View)sender);
        await Navigation.PushAsync(new RegisterPage());
    }

    private async void OnForgotPasswordClicked(object sender, EventArgs e)
    {
        await AnimateButtonAsync((View)sender);
        string email = await DisplayPromptAsync("Reset Password", "Enter your email:");
        if (string.IsNullOrWhiteSpace(email))
        {
            await DisplayAlert("Missing Email", "Please enter your email address.", "OK");
            return;
        }

        var success = await _authService.SendPasswordResetEmailAsync(email);
        if (success)
            await DisplayAlert("Email Sent", "Check your inbox for a reset link.", "OK");
        else
            await DisplayAlert("Error", "Failed to send reset email. Please try again.", "OK");
    }
}
