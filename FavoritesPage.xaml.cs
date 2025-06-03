using RecipeApp.Models;
using RecipeApp.Services;

namespace RecipeApp.Pages;

public partial class FavoritesPage : ContentPage
{
    private readonly AuthService _authService;
    private readonly FirestoreService _firestoreService = new();

    public FavoritesPage(AuthService authService)
    {
        InitializeComponent();
        _authService = authService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Get all recipes and filter to the user's favorites
        var allRecipes = await _firestoreService.GetRecipesAsync(_authService.GetIdToken());
        var favoriteIds = await _firestoreService.GetFavoriteRecipeIdsAsync(_authService.GetUserId(), _authService.GetIdToken());

        var favorites = allRecipes.Where(r => favoriteIds.Contains(r.Id)).ToList();
        FavoritesListView.ItemsSource = favorites;
    }

    private async void OnRecipeTapped(object sender, EventArgs e)
    {
        if (sender is Frame frame && frame.BindingContext is Recipe recipe)
        {
            await Navigation.PushAsync(new RecipeDetailPage(recipe, _authService));
        }
    }
}
