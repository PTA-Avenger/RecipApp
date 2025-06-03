using RecipeApp.Models;
using RecipeApp.Services;

namespace RecipeApp.Pages;

public partial class AchievementsPage : ContentPage
{
    private readonly AuthService _authService;
    private readonly FirestoreService _firestoreService = new();

    public AchievementsPage(AuthService authService)
    {
        InitializeComponent();
        _authService = authService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        var achievements = await _firestoreService.GetAchievementsAsync(_authService.GetUserId(), _authService.GetIdToken());
        AchievementsListView.ItemsSource = achievements;
    }
}
