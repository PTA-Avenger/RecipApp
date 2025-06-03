using System.Text;
using RecipeApp.Models;
using RecipeApp.Services;

namespace RecipeApp.Pages;

public partial class MealPlannerPage : ContentPage
{
    private readonly AuthService _authService;
    private readonly FirestoreService _firestoreService = new();
    private List<Recipe> _mealPlanRecipes = new();

    public MealPlannerPage(AuthService authService)
    {
        InitializeComponent();
        _authService = authService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Example: Load recipes from Firestore (or adjust if you store meal plan elsewhere)
        var allRecipes = await _firestoreService.GetRecipesAsync(_authService.GetIdToken());
        _mealPlanRecipes = allRecipes
            .Where(r => r.IsInMealPlan) // Flag your meal plan recipes however appropriate
            .ToList();

        MealPlanListView.ItemsSource = _mealPlanRecipes;
    }

    private async void OnExportShoppingListClicked(object sender, EventArgs e)
    {
        await ExportShoppingListAsync();
    }

    private async Task ExportShoppingListAsync()
    {
        if (_mealPlanRecipes == null || !_mealPlanRecipes.Any())
        {
            await DisplayAlert("No Recipes", "You have no recipes in your meal plan.", "OK");
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("🛒 Shopping List");

        foreach (var recipe in _mealPlanRecipes)
        {
            sb.AppendLine($"\n🔹 {recipe.Title}");
            sb.AppendLine(recipe.Ingredients);
        }

        var fileName = $"MealPlan_ShoppingList_{DateTime.Now:yyyyMMdd}.txt";
        var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);
        File.WriteAllText(filePath, sb.ToString());

        await Share.RequestAsync(new ShareFileRequest
        {
            Title = "Share Shopping List",
            File = new ShareFile(filePath)
        });
    }
}
