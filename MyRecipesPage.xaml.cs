using RecipeApp.Models;
using RecipeApp.Services;
using System.Diagnostics;

namespace RecipeApp.Pages;

public partial class MyRecipesPage : ContentPage
{
    private readonly AuthService _authService;
    private readonly FirestoreService _firestoreService = new();

    private List<Recipe> _allMyRecipes = new();

    public MyRecipesPage(AuthService authService)
    {
        InitializeComponent();
        _authService = authService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var allRecipes = await _firestoreService.GetRecipesAsync(_authService.GetIdToken());
        _allMyRecipes = allRecipes.Where(r => r.UserId == _authService.GetUserId()).ToList();

        Debug.WriteLine($"[DEBUG] Loaded {_allMyRecipes.Count} recipes for user {_authService.GetUserId()}");

        ApplyFiltersAndSort();
    }

    private void OnCategoryChanged(object sender, EventArgs e)
    {
        Debug.WriteLine("[DEBUG] Category Picker Changed");
        ApplyFiltersAndSort();
    }

    private void OnSortChanged(object sender, EventArgs e)
    {
        Debug.WriteLine("[DEBUG] Sort Picker Changed");
        ApplyFiltersAndSort();
    }

    private void ApplyFiltersAndSort()
    {
        var selectedCategory = CategoryFilterPicker.SelectedItem?.ToString();
        var sortBy = SortPicker.SelectedItem?.ToString();

        Debug.WriteLine($"[DEBUG] Selected Category: '{selectedCategory}'");
        Debug.WriteLine($"[DEBUG] Sort By: '{sortBy}'");

        IEnumerable<Recipe> filtered = _allMyRecipes;

        Debug.WriteLine($"[DEBUG] Total Recipes Before Filtering: {_allMyRecipes.Count}");

        foreach (var recipe in _allMyRecipes)
        {
            Debug.WriteLine($"[DEBUG] Recipe: '{recipe.Title}', Category: '{recipe.Category}'");
        }

        if (!string.IsNullOrWhiteSpace(selectedCategory) && selectedCategory != "All")
        {
            var categoryFilter = selectedCategory.Trim().ToLowerInvariant();

            filtered = filtered.Where(r =>
                !string.IsNullOrWhiteSpace(r.Category) &&
                r.Category.Trim().ToLowerInvariant() == categoryFilter);

            Debug.WriteLine($"[DEBUG] Recipes After Filtering by Category '{selectedCategory}': {filtered.Count()}");
        }

        filtered = sortBy switch
        {
            "Oldest First" => filtered.OrderBy(r => r.CreatedAt),
            _ => filtered.OrderByDescending(r => r.CreatedAt)
        };

        var finalList = filtered.ToList();
        MyRecipesListView.ItemsSource = finalList;

        Debug.WriteLine($"[DEBUG] Final Recipes Shown: {finalList.Count}");
    }

    private async void OnDeleteClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is Recipe recipe)
        {
            bool confirm = await DisplayAlert("Delete Recipe", $"Delete \"{recipe.Title}\"?", "Yes", "No");
            if (!confirm) return;

            bool success = await _firestoreService.DeleteRecipeAsync(recipe.Id, _authService.GetIdToken());
            if (success)
            {
                _allMyRecipes.Remove(recipe);
                ApplyFiltersAndSort();
                await DisplayAlert("Deleted", "Recipe deleted.", "OK");
            }
            else
            {
                await DisplayAlert("Error", "Could not delete the recipe.", "OK");
            }
        }
    }

    private async void OnEditClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is Recipe recipe)
        {
            await Navigation.PushAsync(new EditRecipePage(recipe, _authService));
        }
    }

    private async void OnRecipeTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is Recipe recipe)
        {
            await Navigation.PushAsync(new RecipeDetailPage(recipe, _authService));
        }
    }
}
