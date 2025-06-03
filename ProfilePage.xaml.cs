using RecipeApp.Models;
using RecipeApp.Services;
using RecipeApp.Pages;

namespace RecipeApp.Pages;

public partial class ProfilePage : ContentPage
{
    private readonly AuthService _authService;
    private readonly FirestoreService _firestoreService = new();
    private List<Recipe> _userRecipes = new();

    public ProfilePage(AuthService authService)
    {
        InitializeComponent();
        _authService = authService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Show email
        EmailLabel.Text = $"Email: {_authService.CurrentUser?.Email ?? "Unknown"}";

        // Get user's own recipes
        var all = await _firestoreService.GetRecipesAsync(_authService.GetIdToken());
        _userRecipes = all.Where(r => r.UserId == _authService.GetUserId()).ToList();
        MyRecipeListView.ItemsSource = _userRecipes;

        StatsLabel.Text = $"Recipes: {_userRecipes.Count}";
    }


    private async void OnDeleteRecipeClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is Recipe recipe)
        {
            var confirm = await DisplayAlert("Delete", $"Delete \"{recipe.Title}\"?", "Yes", "No");
            if (!confirm) return;

            var success = await _firestoreService.DeleteRecipeAsync(recipe.Id, _authService.GetIdToken());

            if (success)
            {
                _userRecipes.Remove(recipe);
                MyRecipeListView.ItemsSource = _userRecipes.ToList();
                await DisplayAlert("Deleted", "Recipe deleted.", "OK");
            }
            else
            {
                await DisplayAlert("Error", "Failed to delete recipe.", "OK");
            }
        }
    }

    private async void OnEditRecipeClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is Recipe recipe)
        {
            await Navigation.PushAsync(new EditRecipePage(recipe, _authService));
        }
    }
}
