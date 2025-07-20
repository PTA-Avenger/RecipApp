using RecipeApp.Models;
using RecipeApp.Pages;
using RecipeApp.Services;
using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;

namespace RecipeApp.Pages;

public partial class HomePage : ContentPage
{
    private readonly AuthService _authService;
    private Recipe _recipeOfTheDay;
    private readonly FirestoreService _firestoreService;
    private ObservableCollection<Recipe> _recentRecipes;
    private List<Recipe> _allRecipes;
    private Challenge _currentChallenge;
    private int _challengeProgress;
    private Timer _refreshTimer;
    private DateTime _lastRefresh;
    
    // Analytics data
    private int _totalRecipes;
    private int _favoritesCount;
    private int _currentStreak;
    private int _weeklyRecipesAdded;
    private int _weeklyCookingCount;
    private int _weeklyFavoritesAdded;

    public HomePage(AuthService authService, FirestoreService firestoreService)
    {
        InitializeComponent();
        _authService = authService;
        _firestoreService = firestoreService;
        _recentRecipes = new ObservableCollection<Recipe>();
        _allRecipes = new List<Recipe>();
        
        RecentRecipesCollection.ItemsSource = _recentRecipes;
        
        // Set up automatic refresh every 5 minutes
        _refreshTimer = new Timer(async _ => await RefreshDashboardData(), null, TimeSpan.Zero, TimeSpan.FromMinutes(5));
        
        SetupWelcomeMessage();
    }

    private void SetupWelcomeMessage()
    {
        var currentHour = DateTime.Now.Hour;
        var greeting = currentHour switch
        {
            < 12 => "Good morning",
            < 17 => "Good afternoon",
            _ => "Good evening"
        };
        
        WelcomeLabel.Text = $"{greeting}, Chef!";
        DateLabel.Text = DateTime.Now.ToString("MMMM dd, yyyy");
    }

    private async Task AnimateButtonAsync(View view)
    {
        await view.ScaleTo(0.95, 75, Easing.CubicIn);
        await view.ScaleTo(1.0, 75, Easing.CubicOut);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await RefreshDashboardData();
    }

    private async void OnRefreshing(object sender, EventArgs e)
    {
        await RefreshDashboardData();
        RefreshView.IsRefreshing = false;
    }

    private async Task RefreshDashboardData()
    {
        try
        {
            // Show loading states
            ShowLoadingStates();
            
            // Load all data in parallel for better performance
            var tasks = new List<Task>
            {
                LoadRecipeOfTheDayAsync(),
                LoadStreakAsync(),
                LoadRecipeStatsAsync(),
                LoadDailyChallengeAsync(),
                LoadRecentRecipesAsync(),
                LoadAchievementProgressAsync(),
                LoadWeeklyAnalyticsAsync()
            };

            await Task.WhenAll(tasks);
            _lastRefresh = DateTime.Now;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to refresh dashboard: {ex.Message}", "OK");
        }
    }

    private void ShowLoadingStates()
    {
        TotalRecipesLabel.Text = "...";
        StreakNumberLabel.Text = "...";
        FavoritesCountLabel.Text = "...";
        ChallengeTitle.Text = "Loading challenge...";
        DailyTitle.Text = "Loading...";
    }

    private async Task LoadRecipeOfTheDayAsync()
    {
        try
        {
            _recipeOfTheDay = await _firestoreService.GetRandomRecipeAsync(_authService.GetIdToken());
            if (_recipeOfTheDay != null)
            {
                DailyTitle.Text = _recipeOfTheDay.Title;
                DailyDescription.Text = _recipeOfTheDay.Description;
                DailyCategory.Text = $"Category: {_recipeOfTheDay.Category ?? "General"}";
            }
        }
        catch (Exception ex)
        {
            DailyTitle.Text = "Failed to load recipe";
            DailyDescription.Text = ex.Message;
        }
    }

    private async Task LoadStreakAsync()
    {
        try
        {
            _currentStreak = await _firestoreService.GetStreakAsync(_authService.GetUserId(), _authService.GetIdToken());
            StreakNumberLabel.Text = _currentStreak.ToString();
        }
        catch (Exception ex)
        {
            StreakNumberLabel.Text = "0";
            Console.WriteLine($"Failed to load streak: {ex.Message}");
        }
    }

    private async Task LoadRecipeStatsAsync()
    {
        try
        {
            _allRecipes = await _firestoreService.GetRecipesAsync(_authService.GetIdToken());
            var userRecipes = _allRecipes.Where(r => r.UserId == _authService.GetUserId()).ToList();
            
            _totalRecipes = userRecipes.Count;
            TotalRecipesLabel.Text = _totalRecipes.ToString();
            
            // Load favorites count
            var favorites = await _firestoreService.GetFavoritesAsync(_authService.GetUserId(), _authService.GetIdToken());
            _favoritesCount = favorites?.Count ?? 0;
            FavoritesCountLabel.Text = _favoritesCount.ToString();
        }
        catch (Exception ex)
        {
            TotalRecipesLabel.Text = "0";
            FavoritesCountLabel.Text = "0";
            Console.WriteLine($"Failed to load recipe stats: {ex.Message}");
        }
    }

    private async Task LoadDailyChallengeAsync()
    {
        try
        {
            var (challenge, progress) = await _firestoreService.GetDailyChallengeAsync(_authService.GetUserId(), _authService.GetIdToken());
            _currentChallenge = challenge;
            _challengeProgress = progress;

            if (_currentChallenge != null)
            {
                ChallengeTitle.Text = "Daily Challenge";
                ChallengeDescription.Text = _currentChallenge.Description;
                
                double progressPercentage = _currentChallenge.Target > 0 ? (double)_challengeProgress / _currentChallenge.Target : 0;
                ChallengeProgress.Progress = progressPercentage;
                ChallengeProgressLabel.Text = $"{(progressPercentage * 100):F0}% Complete ({_challengeProgress}/{_currentChallenge.Target})";
            }
            else
            {
                ChallengeTitle.Text = "No Challenge Today";
                ChallengeDescription.Text = "Check back tomorrow for a new challenge!";
                ChallengeProgress.Progress = 0;
                ChallengeProgressLabel.Text = "0% Complete";
            }
        }
        catch (Exception ex)
        {
            ChallengeTitle.Text = "Challenge Unavailable";
            ChallengeDescription.Text = ex.Message;
        }
    }

    private async Task LoadRecentRecipesAsync()
    {
        try
        {
            // Load recently viewed recipes from preferences or database
            var recentRecipeIds = await GetRecentRecipeIds();
            var recentRecipes = new List<Recipe>();

            foreach (var recipeId in recentRecipeIds.Take(5))
            {
                var recipe = _allRecipes.FirstOrDefault(r => r.Id == recipeId);
                if (recipe != null)
                {
                    recentRecipes.Add(recipe);
                }
            }

            _recentRecipes.Clear();
            foreach (var recipe in recentRecipes)
            {
                _recentRecipes.Add(recipe);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load recent recipes: {ex.Message}");
        }
    }

    private async Task LoadAchievementProgressAsync()
    {
        try
        {
            // Calculate achievement progress based on user stats
            var nextAchievement = GetNextAchievement(_totalRecipes);
            
            if (nextAchievement != null)
            {
                NextAchievementTitle.Text = nextAchievement.Title;
                NextAchievementDescription.Text = nextAchievement.Description;
                
                double progress = CalculateAchievementProgress(_totalRecipes, nextAchievement.Target);
                AchievementProgress.Progress = progress;
                AchievementProgressLabel.Text = $"{_totalRecipes}/{nextAchievement.Target} recipes posted";
                
                AchievementIcon.Text = GetAchievementIcon(nextAchievement.Id);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load achievement progress: {ex.Message}");
        }
    }

    private async Task LoadWeeklyAnalyticsAsync()
    {
        try
        {
            var weekStart = DateTime.Now.StartOfWeek(DayOfWeek.Monday);
            var userRecipes = _allRecipes.Where(r => r.UserId == _authService.GetUserId()).ToList();
            
            _weeklyRecipesAdded = userRecipes.Count(r => r.CreatedAt >= weekStart);
            WeeklyRecipesLabel.Text = _weeklyRecipesAdded.ToString();
            
            // For now, set cooking count and favorites to sample data
            // In a real implementation, you'd track these metrics
            _weeklyCookingCount = Math.Min(_weeklyRecipesAdded * 2, 10);
            _weeklyFavoritesAdded = Math.Min(_weeklyRecipesAdded / 2, 5);
            
            WeeklyCookingLabel.Text = _weeklyCookingCount.ToString();
            WeeklyFavoritesLabel.Text = _weeklyFavoritesAdded.ToString();
        }
        catch (Exception ex)
        {
            WeeklyRecipesLabel.Text = "0";
            WeeklyCookingLabel.Text = "0";
            WeeklyFavoritesLabel.Text = "0";
            Console.WriteLine($"Failed to load weekly analytics: {ex.Message}");
        }
    }

    private async Task<List<string>> GetRecentRecipeIds()
    {
        try
        {
            var recentIds = Preferences.Get("RecentRecipeIds", "");
            return string.IsNullOrEmpty(recentIds) ? new List<string>() : recentIds.Split(',').ToList();
        }
        catch
        {
            return new List<string>();
        }
    }

    private async Task AddToRecentRecipes(string recipeId)
    {
        try
        {
            var recentIds = await GetRecentRecipeIds();
            recentIds.Remove(recipeId); // Remove if already exists
            recentIds.Insert(0, recipeId); // Add to front
            
            // Keep only last 10
            if (recentIds.Count > 10)
                recentIds = recentIds.Take(10).ToList();
            
            Preferences.Set("RecentRecipeIds", string.Join(",", recentIds));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to add to recent recipes: {ex.Message}");
        }
    }

    private Achievement GetNextAchievement(int recipeCount)
    {
        var achievements = new List<Achievement>
        {
            new Achievement { Id = "first_recipe", Title = "First Recipe", Description = "Post your first recipe", Target = 1 },
            new Achievement { Id = "five_recipes", Title = "Recipe Explorer", Description = "Post 5 recipes", Target = 5 },
            new Achievement { Id = "ten_recipes", Title = "Cook Star", Description = "Post 10 recipes", Target = 10 },
            new Achievement { Id = "twenty_recipes", Title = "Master Chef", Description = "Post 20 recipes", Target = 20 },
            new Achievement { Id = "fifty_recipes", Title = "Culinary Expert", Description = "Post 50 recipes", Target = 50 }
        };

        return achievements.FirstOrDefault(a => a.Target > recipeCount);
    }

    private double CalculateAchievementProgress(int current, int target)
    {
        return target > 0 ? Math.Min((double)current / target, 1.0) : 0;
    }

    private string GetAchievementIcon(string achievementId)
    {
        return achievementId switch
        {
            "first_recipe" => "🏆",
            "five_recipes" => "⭐",
            "ten_recipes" => "🌟",
            "twenty_recipes" => "👑",
            "fifty_recipes" => "🎖️",
            _ => "🏅"
        };
    }

    // Event Handlers
    private async void OnSearchClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new RecipeListPage(_authService));
    }

    private async void OnQuickSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        // Implement real-time search suggestions if needed
        if (string.IsNullOrWhiteSpace(e.NewTextValue))
            return;
        
        // Could show search suggestions here
    }

    private async void OnQuickSearchPressed(object sender, EventArgs e)
    {
        var searchTerm = QuickSearchBar.Text;
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            // Navigate to search results
            await Navigation.PushAsync(new RecipeListPage(_authService, searchTerm));
        }
    }

    private async void OnChallengeFrameTapped(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new ChallengesPage(_authService));
    }

    private async void OnRecipeOfTheDayTapped(object sender, EventArgs e)
    {
        if (_recipeOfTheDay != null)
        {
            await AddToRecentRecipes(_recipeOfTheDay.Id);
            await Navigation.PushAsync(new RecipeDetailPage(_recipeOfTheDay, _authService));
        }
    }

    private async void OnRecentRecipeTapped(object sender, EventArgs e)
    {
        if (sender is Element element && element.BindingContext is Recipe recipe)
        {
            await AddToRecentRecipes(recipe.Id);
            await Navigation.PushAsync(new RecipeDetailPage(recipe, _authService));
        }
    }

    private async void OnFloatingActionButtonClicked(object sender, EventArgs e)
    {
        await AnimateButtonAsync((View)sender);
        
        var action = await DisplayActionSheet("Quick Action", "Cancel", null, 
            "Add New Recipe", "Search Recipes", "View Favorites", "Meal Planner");
        
        switch (action)
        {
            case "Add New Recipe":
                await OnAddRecipeClicked(sender, e);
                break;
            case "Search Recipes":
                await OnViewRecipesClicked(sender, e);
                break;
            case "View Favorites":
                await OnFavoritesClicked(sender, e);
                break;
            case "Meal Planner":
                await OnMealPlannerClicked(sender, e);
                break;
        }
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

    private async void OnMyRecipesClicked(object sender, EventArgs e)
    {
        await AnimateButtonAsync((View)sender);
        await this.FadeTo(0.6, 100);
        await Navigation.PushAsync(new MyRecipesPage(_authService));
        await this.FadeTo(1.0, 100);
    }

    private async void OnMealPlannerClicked(object sender, EventArgs e)
    {
        await AnimateButtonAsync((View)sender);
        await Navigation.PushAsync(new MealPlannerPage(_authService));
    }

    private async void OnIngredientSuggestClicked(object sender, EventArgs e)
    {
        await AnimateButtonAsync((View)sender);
        await Navigation.PushAsync(new IngredientSuggestPage());
    }

    private async void OnShoppingListClicked(object sender, EventArgs e)
    {
        await AnimateButtonAsync((View)sender);
        await Navigation.PushAsync(new ShoppingListPage(_authService));
    }

    private async void OnExportCookbookClicked(object sender, EventArgs e)
    {
        await AnimateButtonAsync((View)sender);
        
        var recipes = await _firestoreService.GetRecipesAsync(_authService.GetIdToken());
        if (recipes == null || recipes.Count == 0)
        {
            await DisplayAlert("No Recipes", "You have no recipes to export.", "OK");
            return;
        }

        try
        {
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
        catch (Exception ex)
        {
            await DisplayAlert("Export Error", $"Failed to export cookbook: {ex.Message}", "OK");
        }
    }

    private async void OnAchievementsClicked(object sender, EventArgs e)
    {
        await AnimateButtonAsync((View)sender);
        await Navigation.PushAsync(new AchievementsPage(_authService));
    }

    private async void OnLeaderboardClicked(object sender, EventArgs e)
    {
        await AnimateButtonAsync((View)sender);
        await Navigation.PushAsync(new LeaderboardPage(_authService));
    }

    private async void OnProfileClicked(object sender, EventArgs e)
    {
        await AnimateButtonAsync((View)sender);
        await Navigation.PushAsync(new ProfilePage(_authService));
    }

    private async void OnVoiceCookingClicked(object sender, EventArgs e)
    {
        await AnimateButtonAsync((View)sender);
        
        if (_recipeOfTheDay != null)
        {
            await Navigation.PushAsync(new VoiceCookingPage(_recipeOfTheDay));
        }
        else
        {
            await DisplayAlert("No Recipe", "Please select a recipe first.", "OK");
        }
    }

    private async void OnAnalyticsDashboardClicked(object sender, EventArgs e)
    {
        await AnimateButtonAsync((View)sender);
        await Navigation.PushAsync(new DashboardAnalyticsPage(_authService));
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        await AnimateButtonAsync((View)sender);
        
        var result = await DisplayAlert("Logout", "Are you sure you want to logout?", "Yes", "No");
        if (result)
        {
            _authService.Logout();
            await Navigation.PushAsync(new LoginPage());
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _refreshTimer?.Dispose();
    }
}

// Extension method for DateTime
public static class DateTimeExtensions
{
    public static DateTime StartOfWeek(this DateTime dt, DayOfWeek startOfWeek)
    {
        int diff = (7 + (dt.DayOfWeek - startOfWeek)) % 7;
        return dt.AddDays(-1 * diff).Date;
    }
}
