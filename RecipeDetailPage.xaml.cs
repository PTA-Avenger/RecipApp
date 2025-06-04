using RecipeApp.Models;
using RecipeApp.Services;
using RecipeApp.Helpers;
using Microsoft.Maui.Controls;
using System.Text;
using System.Text.Json;
using Microcharts;
using SkiaSharp;
using System.Text.Json.Serialization;

namespace RecipeApp.Pages;

public partial class RecipeDetailPage : ContentPage
{
    private readonly Recipe _recipe;
    private readonly AuthService _authService;
    private readonly FirestoreService _firestoreService = new();
    private bool _isFavorite;
    private readonly NutritionService _nutritionService = new();
    public RecipeDetailPage(Recipe recipe, AuthService authService)
    {
        InitializeComponent();
        _recipe = recipe;
        _authService = authService;
        BindingContext = _recipe;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Enable delete if owner
        DeleteButton.IsVisible = _recipe.UserId == _authService.GetUserId();

        // Check favorite
        _isFavorite = await _firestoreService.IsRecipeFavorited(_authService.GetUserId(), _recipe.Id, _authService.GetIdToken());
        FavoriteButton.Source = _isFavorite ? "heart_filled.png" : "heart_empty.png";
    }

    private async void OnFavoriteClicked(object sender, EventArgs e)
    {
        var updated = await _firestoreService.ToggleFavoriteAsync(_authService.GetUserId(), _recipe.Id, _authService.GetIdToken());
        if (updated)
        {
            _isFavorite = !_isFavorite;
            FavoriteButton.Source = _isFavorite ? "heart_filled.png" : "heart_empty.png";
        }
        else
        {
            await DisplayAlert("Error", "Could not update favorite", "OK");
        }
    }

    private async void OnDeleteClicked(object sender, EventArgs e)
    {
        var confirm = await DisplayAlert("Delete", $"Are you sure you want to delete '{_recipe.Title}'?", "Yes", "No");
        if (!confirm) return;

        var deleted = await _firestoreService.DeleteRecipeAsync(_recipe.Id, _authService.GetIdToken());
        if (deleted)
        {
            await DisplayAlert("Deleted", "Recipe deleted", "OK");
            await Navigation.PopAsync();
        }
        else
        {
            await DisplayAlert("Error", "Could not delete recipe", "OK");
        }
    }

    private async void OnVoiceCookingClicked(object sender, EventArgs e)
    {
        var fullSteps = $"Let's start cooking {_recipe.Title}. {Environment.NewLine}{_recipe.Steps}";
        await TextToSpeech.Default.SpeakAsync(fullSteps);
    }

    private async void OnShareClicked(object sender, EventArgs e)
    {
        try
        {
            var pdfBytes = PdfExporter.ExportRecipeToPdf(_recipe);
            var fileName = $"{_recipe.Title.Replace(" ", "_")}.pdf";
            var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);
            File.WriteAllBytes(filePath, pdfBytes);

            await Share.RequestAsync(new ShareFileRequest
            {
                Title = $"Share {_recipe.Title}",
                File = new ShareFile(filePath)
            });
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to share: {ex.Message}", "OK");
        }
    }

    private async void OnShareToWhatsAppClicked(object sender, EventArgs e)
    {
        // WhatsApp's max message size is ~65,000 chars, but UX is better with shorter text
        var rawMessage = $"Check out this recipe: {_recipe.Title}\n\n{_recipe.Description}\n\nCategory: {_recipe.Category}";
        var message = rawMessage.Length > 1000 ? rawMessage.Substring(0, 1000) + "..." : rawMessage;

#if ANDROID
        var uri = $"whatsapp://send?text={Uri.EscapeDataString(message)}";
        try
        {
            await Launcher.Default.OpenAsync(uri);
        }
        catch
        {
            await DisplayAlert("Error", "WhatsApp is not installed.", "OK");
        }
#elif IOS
    var url = $"whatsapp://send?text={Uri.EscapeDataString(message)}";
    if (await Launcher.Default.CanOpenAsync(url))
        await Launcher.Default.OpenAsync(url);
    else
        await DisplayAlert("Error", "WhatsApp is not installed.", "OK");
#else
    await DisplayAlert("Unsupported", "WhatsApp sharing is not available on this platform.", "OK");
#endif
    }

    private async void OnShareToiMessageClicked(object sender, EventArgs e)
    {
#if IOS
    var message = $"Check out this recipe: {_recipe.Title}\n\n{_recipe.Description}";
    var smsUri = $"sms:&body={Uri.EscapeDataString(message)}";

    if (await Launcher.Default.CanOpenAsync(smsUri))
        await Launcher.Default.OpenAsync(smsUri);
    else
        await DisplayAlert("Error", "iMessage is not available.", "OK");
#else
        await DisplayAlert("Unsupported", "iMessage is only available on iOS.", "OK");
#endif
    }

    private async void OnViewNutritionInfoClicked(object sender, EventArgs e)
    {
        var result = await _nutritionService.AnalyzeIngredientsAsync(_recipe.Ingredients);
        if (result == null)
        {
            await DisplayAlert("Error", "Failed to fetch nutrition info.", "OK");
            return;
        }

        await DisplayAlert("Nutrition Info",
            $"Calories: {result.Calories} kcal\nProtein: {result.Protein} g\nCarbs: {result.Carbs} g\nFat: {result.Fat} g", "OK");
    }
    private async void OnNutritionClicked(object sender, EventArgs e)
    {
        try
        {
            var nutrition = await _nutritionService.AnalyzeIngredientsAsync(_recipe.Ingredients);

            var entries = new List<ChartEntry>
        {
            new ChartEntry(nutrition.Calories) { Label = "Calories", ValueLabel = nutrition.Calories.ToString(), Color = SKColor.Parse("#FF7043") },
            new ChartEntry(nutrition.Protein) { Label = "Protein", ValueLabel = nutrition.Protein + "g", Color = SKColor.Parse("#42A5F5") },
            new ChartEntry(nutrition.Fat) { Label = "Fat", ValueLabel = nutrition.Fat + "g", Color = SKColor.Parse("#AB47BC") },
            new ChartEntry(nutrition.Carbs) { Label = "Carbs", ValueLabel = nutrition.Carbs + "g", Color = SKColor.Parse("#66BB6A") }
        };

            NutritionChartView.Chart = new PieChart { Entries = entries, LabelTextSize = 30 };
            NutritionChartView.IsVisible = true;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Could not fetch nutrition data: {ex.Message}", "OK");
        }
    }

    private async void OnCheckAllergensClicked(object sender, EventArgs e)
    {
        var allergens = IngredientHelper.FindAllergens(_recipe.Ingredients);
        var subs = IngredientHelper.GetSubstitutions(_recipe.Ingredients);

        string message = "";

        if (allergens.Count > 0)
        {
            message += $"⚠️ Allergens detected:\n- {string.Join("\n- ", allergens)}\n\n";
        }
        else
        {
            message += "✅ No common allergens found.\n\n";
        }

        if (subs.Count > 0)
        {
            message += "🔄 Suggested substitutions:\n";
            foreach (var pair in subs)
            {
                message += $"- {pair.Key} → {pair.Value}\n";
            }
        }
        else
        {
            message += "No substitutions suggested.";
        }

        await DisplayAlert("Allergen & Substitution Info", message, "OK");
    }

    private async void OnMarkAsCookedClicked(object sender, EventArgs e)
    {
        var userId = _authService.GetUserId();
        var idToken = _authService.GetIdToken();

        // Get current streak data
        var streakData = await _firestoreService.GetUserCookingStreakAsync(userId, idToken);

        int currentStreak = streakData.StreakCount;
        DateTime lastCookedDate = streakData.LastCookedDate;

        var today = DateTime.UtcNow.Date;
        var daysSinceLastCooked = (today - lastCookedDate.Date).Days;

        if (daysSinceLastCooked == 1)
        {
            currentStreak++;
        }
        else if (daysSinceLastCooked > 1)
        {
            currentStreak = 1;
        }
        else if (daysSinceLastCooked == 0)
        {
            // Already cooked today, keep streak the same
        }
        else
        {
            currentStreak = 1;
        }

        bool streakUpdated = await _firestoreService.UpdateUserCookingStreakAsync(userId, today, currentStreak, idToken);

        if (streakUpdated)
            await DisplayAlert("Streak Updated", $"Great job! Your cooking streak is now {currentStreak} days.", "OK");
        else
            await DisplayAlert("Warning", "Failed to update cooking streak.", "OK");
    }
}
