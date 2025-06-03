using RecipeApp.Models;
using RecipeApp.Services;

namespace RecipeApp.Pages;

public partial class EditRecipePage : ContentPage
{
    private readonly Recipe _recipe;
    private readonly AuthService _authService;
    private readonly FirestoreService _firestoreService = new();

    public EditRecipePage(Recipe recipe, AuthService authService)
    {
        InitializeComponent();
        _recipe = recipe;
        _authService = authService;

        TitleEntry.Text = recipe.Title;
        DescriptionEditor.Text = recipe.Description;
        IngredientsEditor.Text = recipe.Ingredients;
        StepsEditor.Text = recipe.Steps;
        CategoryPicker.SelectedItem = recipe.Category;
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TitleEntry.Text) ||
            string.IsNullOrWhiteSpace(DescriptionEditor.Text) ||
            string.IsNullOrWhiteSpace(IngredientsEditor.Text) ||
            string.IsNullOrWhiteSpace(StepsEditor.Text) ||
            CategoryPicker.SelectedItem == null)
        {
            await DisplayAlert("Missing Info", "Please complete all fields.", "OK");
            return;
        }

        _recipe.Title = TitleEntry.Text.Trim();
        _recipe.Description = DescriptionEditor.Text.Trim();
        _recipe.Ingredients = IngredientsEditor.Text.Trim();
        _recipe.Steps = StepsEditor.Text.Trim();
        _recipe.Category = CategoryPicker.SelectedItem.ToString();

        bool updated = await _firestoreService.UpdateRecipeAsync(_recipe, _authService.GetIdToken());

        if (updated)
        {
            await DisplayAlert("Updated", "Recipe updated successfully.", "OK");
            await Navigation.PopAsync();
        }
        else
        {
            await DisplayAlert("Error", "Update failed.", "OK");
        }
    }
}
