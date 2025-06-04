using RecipeApp.Services;

namespace RecipeApp.Pages;

public partial class ProfilePage : ContentPage
{
    private readonly AuthService _authService;

    public ProfilePage(AuthService authService)
    {
        InitializeComponent();
        _authService = authService;
        LoadUserData();
    }

    private async void LoadUserData()
    {
        // Load current username for display
        var currentUser = await _authService.GetCurrentUserAsync();
        UsernameEntry.Text = currentUser?.Username ?? string.Empty;
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        var newUsername = UsernameEntry.Text?.Trim();
        var newPassword = PasswordEntry.Text?.Trim();

        if (string.IsNullOrWhiteSpace(newUsername))
        {
            await DisplayAlert("Missing Username", "Please enter a username.", "OK");
            return;
        }

        // Update username if changed
        bool usernameUpdated = await _authService.UpdateUsernameAsync(newUsername);

        // Update password if entered
        bool passwordUpdated = true;
        if (!string.IsNullOrWhiteSpace(newPassword))
            passwordUpdated = await _authService.UpdatePasswordAsync(newPassword);

        if (usernameUpdated && passwordUpdated)
        {
            await DisplayAlert("Success", "Profile updated successfully!", "OK");
            PasswordEntry.Text = string.Empty;
        }
        else if (!usernameUpdated)
        {
            await DisplayAlert("Error", "Failed to update username.", "OK");
        }
        else if (!passwordUpdated)
        {
            await DisplayAlert("Error", "Failed to update password.", "OK");
        }
    }
}