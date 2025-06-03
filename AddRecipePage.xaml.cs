using RecipeApp.Models;
using RecipeApp.Services;

namespace RecipeApp.Pages;

public partial class AddRecipePage : ContentPage
{
    private readonly AuthService _authService;
    private readonly FirestoreService _firestoreService = new();

    public AddRecipePage(AuthService authService)
    {
        InitializeComponent();
        _authService = authService;
    }

    private async void OnSaveRecipeClicked(object sender, EventArgs e)
    {
        // Validate inputs
        if (string.IsNullOrWhiteSpace(TitleEntry.Text))
        {
            await DisplayAlert("Missing Field", "Please enter a title.", "OK");
            return;
        }
        if (string.IsNullOrWhiteSpace(DescriptionEditor.Text))
        {
            await DisplayAlert("Missing Field", "Please enter a description.", "OK");
            return;
        }
        if (string.IsNullOrWhiteSpace(IngredientsEditor.Text))
        {
            await DisplayAlert("Missing Field", "Please enter ingredients.", "OK");
            return;
        }
        if (string.IsNullOrWhiteSpace(StepsEditor.Text))
        {
            await DisplayAlert("Missing Field", "Please enter the preparation steps.", "OK");
            return;
        }
        if (CategoryPicker.SelectedItem == null)
        {
            await DisplayAlert("Missing Field", "Please select a category.", "OK");
            return;
        }

        var userId = _authService.GetUserId();
        var idToken = _authService.GetIdToken();

        var recipe = new Recipe
        {
            Title = TitleEntry.Text.Trim(),
            Description = DescriptionEditor.Text?.Trim(),
            Ingredients = IngredientsEditor.Text?.Trim(),
            Steps = StepsEditor.Text?.Trim(),
            UserId = userId,
            Category = CategoryPicker.SelectedItem.ToString()
        };

        var success = await _firestoreService.SaveRecipeAsync(recipe, idToken);

        if (!success)
        {
            await DisplayAlert("Error", "Failed to save recipe.", "OK");
            return;
        }

        // Get current streak data from Firestore
        var streakData = await _firestoreService.GetUserCookingStreakAsync(userId,idToken);

        int currentStreak = 0;
        DateTime lastCookedDate = DateTime.MinValue;

        currentStreak = streakData.StreakCount;
        lastCookedDate = streakData.LastCookedDate;

        // Calculate new streak based on lastCookedDate and today
        var today = DateTime.UtcNow.Date;
        var daysSinceLastCooked = (today - lastCookedDate.Date).Days;

        if (daysSinceLastCooked == 1)
        {
            // Continue streak
            currentStreak++;
        }
        else if (daysSinceLastCooked > 1)
        {
            // Streak broken, reset to 1
            currentStreak = 1;
        }
        else if (daysSinceLastCooked == 0)
        {
            // Already cooked today, keep streak the same
        }
        else
        {
            // If lastCookedDate is in the future or invalid, reset streak
            currentStreak = 1;
        }

        // Update streak in Firestore
        bool streakUpdated = await _firestoreService.UpdateUserCookingStreakAsync(userId, today, currentStreak, idToken);

        if (streakUpdated)
        {
            await DisplayAlert("Success", $"Recipe added! Your cooking streak is now {currentStreak} days.", "OK");
        }
        else
        {
            await DisplayAlert("Warning", "Recipe added but failed to update cooking streak.", "OK");
        }

        await Navigation.PopAsync();
    }
}
