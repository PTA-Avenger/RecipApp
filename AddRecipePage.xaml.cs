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

        await DisplayAlert("Success", "Recipe added!", "OK");
        await Navigation.PopAsync();
    }
}