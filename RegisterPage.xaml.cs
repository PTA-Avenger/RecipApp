using RecipeApp.Services;
using System.Net.Mail;
using System.Text.Json;
using System.Net.Http.Json;


namespace RecipeApp.Pages;

public partial class RegisterPage : ContentPage
{
    private readonly AuthService _authService = new();

    public RegisterPage()
    {
        InitializeComponent();
    }

    private async Task AnimateButtonAsync(View view)
    {
        await view.ScaleTo(0.95, 75, Easing.CubicIn);
        await view.ScaleTo(1.0, 75, Easing.CubicOut);
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        await AnimateButtonAsync((View)sender);
        string email = EmailEntry.Text?.Trim().ToLower();
        string password = PasswordEntry.Text;
        string confirmPassword = ConfirmPasswordEntry.Text;

        // ✅ Check for empty fields
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(confirmPassword))
        {
            await DisplayAlert("Missing Fields", "Please fill in all fields.", "OK");
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

        // ✅ Check if passwords match
        if (password != confirmPassword)
        {
            await DisplayAlert("Password Mismatch", "Passwords do not match.", "OK");
            return;
        }

        // ✅ Password strength check
        if (password.Length < 6 ||
            !password.Any(char.IsDigit) ||
            !password.Any(char.IsUpper))
        {
            await DisplayAlert("Weak Password",
                "Password must be at least 6 characters long, include an uppercase letter and a number.", "OK");
            return;
        }

        // 🔐 Register using Firebase REST API to capture errors
        var request = new
        {
            email,
            password,
            returnSecureToken = true
        };

        var url = $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key=YOUR_API_KEY"; // Replace with your API key
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
                    "EMAIL_EXISTS" => "This email is already registered.",
                    "INVALID_EMAIL" => "The email address is badly formatted.",
                    "OPERATION_NOT_ALLOWED" => "Registration is currently disabled.",
                    "TOO_MANY_ATTEMPTS_TRY_LATER" => "Too many attempts. Try again later.",
                    _ => "Registration failed. Please try again."
                };

                await DisplayAlert("Registration Error", message, "OK");
            }
            catch
            {
                await DisplayAlert("Registration Error", "An unknown error occurred.", "OK");
            }

            return;
        }

        // ✅ Register in AuthService & save session
        bool success = await _authService.RegisterAsync(email, password);
        if (success)
        {
            await DisplayAlert("Success", "Account created. Please log in.", "OK");
            await Navigation.PopAsync(); // Go back to login
        }
        else
        {
            await DisplayAlert("Error", "Account was created but could not save session. Try logging in.", "OK");
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await AnimateButtonAsync((View)sender);
        await Navigation.PopAsync(); // Return to login
    }
}
