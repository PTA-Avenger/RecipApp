using RecipeApp.Models;
using RecipeApp.Services;

namespace RecipeApp.Pages;

public partial class ShoppingListPage : ContentPage
{
    private readonly AuthService _authService;
    private readonly FirestoreService _firestoreService = new();

    public ShoppingListPage(AuthService authService)
    {
        InitializeComponent();
        _authService = authService;
    }

    private async void OnGenerateClicked(object sender, EventArgs e)
    {
        var allRecipes = await _firestoreService.GetRecipesAsync(_authService.GetIdToken());
        var userRecipes = allRecipes.Where(r => r.UserId == _authService.GetUserId()).ToList();

        var ingredients = new List<string>();

        foreach (var recipe in userRecipes)
        {
            if (!string.IsNullOrWhiteSpace(recipe.Ingredients))
            {
                // Split on all common line endings to support both Windows and Unix text files
                var lines = recipe.Ingredients
                    .Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                ingredients.AddRange(lines);
            }
        }

        var distinctIngredients = ingredients
            .Select(i => i.Trim())
            .Where(i => !string.IsNullOrWhiteSpace(i))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(i => i)
            .ToList();

        ShoppingListView.ItemsSource = distinctIngredients;

        if (distinctIngredients.Count == 0)
        {
            await DisplayAlert("No Ingredients", "No ingredients found in your recipes.", "OK");
        }
    }
}