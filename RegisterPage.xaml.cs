using RecipeApp.Services;

namespace RecipeApp.Pages;

public partial class RegisterPage : ContentPage
{
    private readonly AuthService _authService;

    public RegisterPage(AuthService authService)
    {
        InitializeComponent();
        _authService = authService;
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        var username = UsernameEntry.Text?.Trim();
        var email = EmailEntry.Text?.Trim();
        var password = PasswordEntry.Text?.Trim();

        if (string.IsNullOrWhiteSpace(username))
        {
            await DisplayAlert("Missing Field", "Please enter a username.", "OK");
            return;
        }
        if (string.IsNullOrWhiteSpace(email))
        {
            await DisplayAlert("Missing Field", "Please enter your email.", "OK");
            return;
        }
        if (string.IsNullOrWhiteSpace(password))
        {
            await DisplayAlert("Missing Field", "Please enter your password.", "OK");
            return;
        }

        // Register the user (implement registration logic in AuthService)
        var registerSuccess = await _authService.RegisterAsync(email, password, username);
        if (registerSuccess)
        {
            await DisplayAlert("Success", "Registration successful.", "OK");
            await Navigation.PopAsync();
        }
        else
        {
            await DisplayAlert("Error", "Registration failed. Please try again.", "OK");
        }
    }
}