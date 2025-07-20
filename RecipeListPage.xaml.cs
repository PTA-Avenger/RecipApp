using RecipeApp.Models;
using RecipeApp.Services;
using RecipeApp.Pages;
using RecipeApp.Helpers;

namespace RecipeApp.Pages;

public partial class RecipeListPage : ContentPage
{
    private readonly AuthService _authService;
    private readonly FirestoreService _firestoreService = new();
    private List<Recipe> _allRecipes = new();
    private string _initialSearchTerm;

    public RecipeListPage(AuthService authService, string searchTerm = null)
    {
        InitializeComponent();
        _authService = authService;
        _initialSearchTerm = searchTerm;
        
        // Set the search term if provided
        if (!string.IsNullOrWhiteSpace(_initialSearchTerm))
        {
            SearchBar.Text = _initialSearchTerm;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        _allRecipes = await _firestoreService.GetRecipesAsync(_authService.GetIdToken());
        
        // Apply initial search filter if provided
        if (!string.IsNullOrWhiteSpace(_initialSearchTerm))
        {
            FilterRecipes(_initialSearchTerm);
        }
        else
        {
            RecipeListView.ItemsSource = _allRecipes;
        }
    }

    private void FilterRecipes(string query)
    {
        var filtered = _allRecipes
            .Where(r =>
                (!string.IsNullOrWhiteSpace(r.Title) &&
                 r.Title.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(r.Description) &&
                 r.Description.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(r.Ingredients) &&
                 r.Ingredients.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(r.Category) &&
                 r.Category.Contains(query, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        
        RecipeListView.ItemsSource = filtered;
    }

    private async void OnRecipeTapped(object sender, TappedEventArgs e)
    {
        if (sender is Frame frame)
        {
            await frame.ScaleTo(0.97, 80, Easing.CubicInOut);
            await frame.ScaleTo(1.0, 80, Easing.CubicOut);
        }

        if (e.Parameter is Recipe recipe)
        {
            // Track recipe view for analytics
            try
            {
                await _firestoreService.TrackRecipeViewAsync(_authService.GetUserId(), recipe.Id, _authService.GetIdToken());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to track recipe view: {ex.Message}");
            }
            
            await Navigation.PushAsync(new RecipeDetailPage(recipe, _authService));
        }
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        var query = e.NewTextValue ?? "";
        FilterRecipes(query);
    }

    private void OnCategoryFilterChanged(object sender, EventArgs e)
    {
        var selectedCategory = CategoryFilterPicker.SelectedItem?.ToString();

        if (selectedCategory == "All" || string.IsNullOrWhiteSpace(selectedCategory))
        {
            RecipeListView.ItemsSource = _allRecipes;
        }
        else
        {
            RecipeListView.ItemsSource = _allRecipes
                .Where(r =>
                    !string.IsNullOrEmpty(r.Category) &&
                    r.Category.Equals(selectedCategory, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }

    private async void OnFavoriteClicked(object sender, EventArgs e)
    {
        if (sender is ImageButton button && button.CommandParameter is Recipe recipe)
        {
            bool success = await _firestoreService.ToggleFavoriteAsync(
                _authService.GetUserId(),
                recipe.Id,
                _authService.GetIdToken()
            );

            if (success)
            {
                await DisplayAlert("Favorite", "Favorite updated!", "OK");
            }
            else
            {
                await DisplayAlert("Error", "Failed to update favorite.", "OK");
            }
        }
    }

    private async void OnShareRecipeClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is Recipe recipe)
        {
            try
            {
                // Generate PDF from recipe
                var pdfBytes = PdfExporter.ExportRecipeToPdf(recipe); // Ensure this method exists
                var fileName = $"{recipe.Title.Replace(" ", "_")}.pdf";
                var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);

                // Save the file
                File.WriteAllBytes(filePath, pdfBytes);

#if WINDOWS
            // Open the containing folder on Windows
            var processStartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{filePath}\"",
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(processStartInfo);
#else
                // Share the file on mobile platforms
                await Share.RequestAsync(new ShareFileRequest
                {
                    Title = "Share Recipe",
                    File = new ShareFile(filePath)
                });
#endif
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to share recipe: {ex.Message}", "OK");
            }
        }
    }

}
