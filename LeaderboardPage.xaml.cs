using RecipeApp.Services;
using RecipeApp.Models;

namespace RecipeApp.Pages;

public partial class LeaderboardPage : ContentPage
{
    private readonly FirestoreService _firestoreService = new();
    private readonly AuthService _authService;

    public LeaderboardPage(AuthService authService)
    {
        InitializeComponent();
        _authService = authService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var topRecipes = await _firestoreService.GetTopFavoritedRecipesAsync(_authService.GetIdToken());
        LeaderboardListView.ItemsSource = topRecipes;
    }
}
