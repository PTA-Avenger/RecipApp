using RecipeApp.Models;
using RecipeApp.Pages;
using RecipeApp.Services;

namespace RecipeApp.Pages;

public partial class HomePage : ContentPage
{
    private readonly AuthService _authService;
    private Recipe _recipeOfTheDay;
    private readonly FirestoreService _firestoreService;

    public HomePage(AuthService authService,FirestoreService firestoreService)
    {
        InitializeComponent();
        _authService = authService;
        _firestoreService = firestoreService;
    }

    private async Task AnimateButtonAsync(View view)
    {
        await view.ScaleTo(0.95, 75, Easing.CubicIn);
        await view.ScaleTo(1.0, 75, Easing.CubicOut);
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        _recipeOfTheDay = await new FirestoreService().GetRandomRecipeAsync(_authService.GetIdToken());
        if (_recipeOfTheDay != null)
        {
            DailyTitle.Text = _recipeOfTheDay.Title;
            DailyDescription.Text = _recipeOfTheDay.Description;
        }
        await LoadStreakAsync();
    }
    private async Task LoadStreakAsync()
    {
        var streak = await _firestoreService.GetStreakAsync(_authService.GetUserId(), _authService.GetIdToken());
        StreakLabel.Text = $"You're on a {streak}-day cooking streak! 🔥";
    }

    private async void OnAddRecipeClicked(object sender, EventArgs e)
    {
        await AnimateButtonAsync((View)sender);
        await this.FadeTo(0.6, 100);
        await Navigation.PushAsync(new AddRecipePage(_authService));
        await this.FadeTo(1.0, 100);
    }

    private async void OnViewRecipesClicked(object sender, EventArgs e)
    {
        await AnimateButtonAsync((View)sender);
        await this.FadeTo(0.6, 100);
        await Navigation.PushAsync(new RecipeListPage(_authService));
        await this.FadeTo(1.0, 100);
    }

    private async void OnFavoritesClicked(object sender, EventArgs e)
    {
        await AnimateButtonAsync((View)sender);
        await this.FadeTo(0.6, 100);
        await Navigation.PushAsync(new FavoritesPage(_authService));
        await this.FadeTo(1.0, 100);
    }

    private async void OnRecipeOfTheDayTapped(object sender, EventArgs e)
    {
        if (_recipeOfTheDay != null)
        {
            await Navigation.PushAsync(new RecipeDetailPage(_recipeOfTheDay, _authService));
        }
    }
    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        await AnimateButtonAsync((View)sender);
        _authService.Logout();
        await Navigation.PushAsync(new LoginPage());
    }
    private async void OnMyRecipesClicked(object sender, EventArgs e)
    {
        await AnimateButtonAsync((View)sender);
        await this.FadeTo(0.6, 100);
        await Navigation.PushAsync(new MyRecipesPage(_authService));
        await this.FadeTo(1.0, 100);
    }
    private async void OnMealPlannerClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new MealPlannerPage(_authService));
    }

    private async void OnIngredientSuggestClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new IngredientSuggestPage());
    }

    private async void OnShoppingListClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new ShoppingListPage(_authService));
    }

    private async void OnExportCookbookClicked(object sender, EventArgs e)
    {
        var recipes = await _firestoreService.GetRecipesAsync(_authService.GetIdToken());
        if (recipes == null || recipes.Count == 0)
        {
            await DisplayAlert("No Recipes", "You have no recipes to export.", "OK");
            return;
        }

        var pdfBytes = CookbookExporter.ExportCookbook(recipes);

        var fileName = $"MyCookbook_{DateTime.Now:yyyyMMdd}.pdf";
        var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);
        File.WriteAllBytes(filePath, pdfBytes);

        await Share.RequestAsync(new ShareFileRequest
        {
            Title = "My Recipe Cookbook",
            File = new ShareFile(filePath)
        });
    }

    private async void OnAchievementsClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new AchievementsPage(_authService));
    }

    private async void OnLeaderboardClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new LeaderboardPage(_authService));
    }

    private async void OnProfileClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new ProfilePage(_authService));
    }
}
