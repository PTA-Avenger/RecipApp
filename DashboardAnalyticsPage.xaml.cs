using RecipeApp.Models;
using RecipeApp.Services;
using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;

namespace RecipeApp.Pages
{
    public partial class DashboardAnalyticsPage : ContentPage
    {
        private readonly AuthService _authService;
        private readonly FirestoreService _firestoreService;
        private CookingAnalytics _analytics;
        private List<Recipe> _userRecipes;
        private DateTime _currentPeriodStart;
        private DateTime _currentPeriodEnd;
        private ObservableCollection<RecipePerformance> _topRecipes;
        private ObservableCollection<CategoryBreakdown> _categoryBreakdown;
        private ObservableCollection<AchievementProgress> _achievementProgress;
        private ObservableCollection<Insight> _insights;

        public DashboardAnalyticsPage(AuthService authService)
        {
            InitializeComponent();
            _authService = authService;
            _firestoreService = new FirestoreService();
            
            // Initialize collections
            _topRecipes = new ObservableCollection<RecipePerformance>();
            _categoryBreakdown = new ObservableCollection<CategoryBreakdown>();
            _achievementProgress = new ObservableCollection<AchievementProgress>();
            _insights = new ObservableCollection<Insight>();
            
            // Bind collections to UI
            TopRecipesCollection.ItemsSource = _topRecipes;
            CategoryBreakdownCollection.ItemsSource = _categoryBreakdown;
            AchievementProgressCollection.ItemsSource = _achievementProgress;
            InsightsCollection.ItemsSource = _insights;
            
            // Set default period to last 30 days
            _currentPeriodEnd = DateTime.Now;
            _currentPeriodStart = _currentPeriodEnd.AddDays(-30);
            
            UpdatePeriodLabel();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadAnalyticsData();
        }

        private void UpdatePeriodLabel()
        {
            var days = (_currentPeriodEnd - _currentPeriodStart).Days;
            PeriodLabel.Text = days switch
            {
                7 => "Last 7 Days",
                30 => "Last 30 Days",
                90 => "Last 3 Months",
                365 => "Last Year",
                _ => $"{_currentPeriodStart:MMM dd} - {_currentPeriodEnd:MMM dd}"
            };
        }

        private async void OnRefreshing(object sender, EventArgs e)
        {
            await LoadAnalyticsData();
            RefreshView.IsRefreshing = false;
        }

        private async Task LoadAnalyticsData()
        {
            try
            {
                ShowLoadingStates();
                
                // Load data in parallel
                var tasks = new List<Task>
                {
                    LoadUserRecipesAsync(),
                    LoadAnalyticsAsync(),
                    LoadStreakDataAsync(),
                    LoadAchievementDataAsync()
                };
                
                await Task.WhenAll(tasks);
                
                // Generate insights and visualizations
                await GenerateInsights();
                GenerateCategoryBreakdown();
                GenerateTopRecipes();
                CreateWeeklyChart();
                
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load analytics: {ex.Message}", "OK");
            }
        }

        private void ShowLoadingStates()
        {
            TotalViewsLabel.Text = "...";
            TotalCooksLabel.Text = "...";
            ActiveDaysLabel.Text = "...";
            AvgRatingLabel.Text = "...";
            CurrentStreakLabel.Text = "...";
            LongestStreakLabel.Text = "...";
        }

        private async Task LoadUserRecipesAsync()
        {
            try
            {
                var allRecipes = await _firestoreService.GetRecipesAsync(_authService.GetIdToken());
                _userRecipes = allRecipes.Where(r => r.UserId == _authService.GetUserId()).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load user recipes: {ex.Message}");
                _userRecipes = new List<Recipe>();
            }
        }

        private async Task LoadAnalyticsAsync()
        {
            try
            {
                _analytics = await _firestoreService.GetCookingAnalyticsAsync(
                    _authService.GetUserId(), 
                    _authService.GetIdToken(),
                    _currentPeriodStart,
                    _currentPeriodEnd);
                
                // Update UI with analytics data
                TotalViewsLabel.Text = _analytics.TotalViews.ToString();
                TotalCooksLabel.Text = _analytics.TotalCooks.ToString();
                ActiveDaysLabel.Text = _analytics.UniqueDays.ToString();
                
                // Calculate average rating (placeholder for now)
                AvgRatingLabel.Text = "4.5";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load analytics: {ex.Message}");
                _analytics = new CookingAnalytics();
            }
        }

        private async Task LoadStreakDataAsync()
        {
            try
            {
                var currentStreak = await _firestoreService.GetStreakAsync(_authService.GetUserId(), _authService.GetIdToken());
                CurrentStreakLabel.Text = currentStreak.ToString();
                
                // For demo purposes, set longest streak to current + some variation
                var longestStreak = Math.Max(currentStreak, currentStreak + new Random().Next(0, 10));
                LongestStreakLabel.Text = longestStreak.ToString();
                
                // Calculate streak goal progress (assume goal of 30 days)
                var streakGoal = 30;
                var goalProgress = Math.Min((double)currentStreak / streakGoal, 1.0);
                StreakProgressBar.Progress = goalProgress;
                StreakGoalLabel.Text = $"{(goalProgress * 100):F0}%";
                
                // Update motivation message
                StreakMotivationLabel.Text = currentStreak switch
                {
                    0 => "Start your cooking journey today! 🚀",
                    < 7 => "Great start! Keep building your streak! 💪",
                    < 30 => "You're on fire! Amazing consistency! 🔥",
                    _ => "Incredible dedication! You're a cooking master! 👑"
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load streak data: {ex.Message}");
                CurrentStreakLabel.Text = "0";
                LongestStreakLabel.Text = "0";
            }
        }

        private async Task LoadAchievementDataAsync()
        {
            try
            {
                var achievements = await _firestoreService.GetUserAchievementsAsync(_authService.GetUserId(), _authService.GetIdToken());
                var unlockedCount = achievements.Count(a => a.IsUnlocked);
                var totalAchievements = GetTotalAvailableAchievements();
                
                AchievementSummaryLabel.Text = $"{unlockedCount}/{totalAchievements} achievements unlocked";
                
                // Create achievement progress items
                _achievementProgress.Clear();
                var nextAchievements = GetNextAchievements(_userRecipes.Count);
                
                foreach (var achievement in nextAchievements.Take(3))
                {
                    var progress = CalculateAchievementProgress(_userRecipes.Count, achievement.Target);
                    _achievementProgress.Add(new AchievementProgress
                    {
                        Title = achievement.Title,
                        Description = achievement.Description,
                        Icon = GetAchievementIcon(achievement.Id),
                        ProgressPercentage = progress,
                        ProgressText = $"{_userRecipes.Count}/{achievement.Target} completed"
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load achievement data: {ex.Message}");
            }
        }

        private void GenerateCategoryBreakdown()
        {
            try
            {
                _categoryBreakdown.Clear();
                
                var categoryGroups = _userRecipes
                    .GroupBy(r => r.Category ?? "General")
                    .OrderByDescending(g => g.Count())
                    .ToList();
                
                var totalRecipes = _userRecipes.Count;
                var colors = new[] { "#FF6B6B", "#4ECDC4", "#45B7D1", "#96CEB4", "#FFEAA7", "#DDA0DD", "#98D8C8" };
                
                for (int i = 0; i < categoryGroups.Count && i < colors.Length; i++)
                {
                    var group = categoryGroups[i];
                    var percentage = totalRecipes > 0 ? (double)group.Count() / totalRecipes * 100 : 0;
                    
                    _categoryBreakdown.Add(new CategoryBreakdown
                    {
                        Category = group.Key,
                        Count = group.Count(),
                        Percentage = percentage,
                        Color = colors[i]
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to generate category breakdown: {ex.Message}");
            }
        }

        private void GenerateTopRecipes()
        {
            try
            {
                _topRecipes.Clear();
                
                // For demo purposes, create sample performance data
                // In a real app, this would come from actual view/interaction data
                var random = new Random();
                var topRecipes = _userRecipes
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(5)
                    .Select(r => new RecipePerformance
                    {
                        Title = r.Title,
                        Category = r.Category ?? "General",
                        ViewCount = random.Next(10, 100)
                    })
                    .OrderByDescending(r => r.ViewCount)
                    .ToList();
                
                foreach (var recipe in topRecipes)
                {
                    _topRecipes.Add(recipe);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to generate top recipes: {ex.Message}");
            }
        }

        private void CreateWeeklyChart()
        {
            try
            {
                WeeklyChartGrid.Children.Clear();
                
                var random = new Random();
                var days = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
                
                for (int i = 0; i < 7; i++)
                {
                    // Create sample data for demonstration
                    var views = random.Next(0, 10);
                    var cooks = random.Next(0, 5);
                    var favorites = random.Next(0, 3);
                    
                    var maxValue = Math.Max(Math.Max(views, cooks), favorites);
                    var barHeight = maxValue > 0 ? (double)maxValue / 10 * 100 : 5;
                    
                    // Create a simple bar representation
                    var barContainer = new VerticalStackLayout
                    {
                        VerticalOptions = LayoutOptions.End,
                        Spacing = 2
                    };
                    
                    // Add bars for each metric
                    if (views > 0)
                    {
                        barContainer.Add(new BoxView
                        {
                            Color = Colors.Blue,
                            HeightRequest = views * 8,
                            WidthRequest = 20,
                            HorizontalOptions = LayoutOptions.Center
                        });
                    }
                    
                    if (cooks > 0)
                    {
                        barContainer.Add(new BoxView
                        {
                            Color = Colors.Orange,
                            HeightRequest = cooks * 8,
                            WidthRequest = 20,
                            HorizontalOptions = LayoutOptions.Center
                        });
                    }
                    
                    if (favorites > 0)
                    {
                        barContainer.Add(new BoxView
                        {
                            Color = Colors.Red,
                            HeightRequest = favorites * 8,
                            WidthRequest = 20,
                            HorizontalOptions = LayoutOptions.Center
                        });
                    }
                    
                    Grid.SetColumn(barContainer, i);
                    Grid.SetRow(barContainer, 0);
                    WeeklyChartGrid.Add(barContainer);
                    
                    // Add day label
                    var dayLabel = new Label
                    {
                        Text = days[i],
                        FontSize = 10,
                        HorizontalOptions = LayoutOptions.Center,
                        TextColor = Colors.Gray
                    };
                    
                    Grid.SetColumn(dayLabel, i);
                    Grid.SetRow(dayLabel, 1);
                    WeeklyChartGrid.Add(dayLabel);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to create weekly chart: {ex.Message}");
            }
        }

        private async Task GenerateInsights()
        {
            try
            {
                _insights.Clear();
                
                var insights = new List<Insight>();
                
                // Generate insights based on user data
                if (_userRecipes.Count == 0)
                {
                    insights.Add(new Insight
                    {
                        Icon = "📝",
                        Title = "Get Started",
                        Description = "You haven't added any recipes yet.",
                        Recommendation = "Add your first recipe to start tracking your cooking journey!"
                    });
                }
                else if (_userRecipes.Count < 5)
                {
                    insights.Add(new Insight
                    {
                        Icon = "🚀",
                        Title = "Building Your Collection",
                        Description = $"You have {_userRecipes.Count} recipes in your collection.",
                        Recommendation = "Try adding more recipes to unlock achievements and improve recommendations."
                    });
                }
                else
                {
                    insights.Add(new Insight
                    {
                        Icon = "🎉",
                        Title = "Great Progress!",
                        Description = $"You have {_userRecipes.Count} recipes in your collection.",
                        Recommendation = "Consider organizing them into meal plans for better cooking workflow."
                    });
                }
                
                // Streak-based insight
                var currentStreak = await _firestoreService.GetStreakAsync(_authService.GetUserId(), _authService.GetIdToken());
                if (currentStreak == 0)
                {
                    insights.Add(new Insight
                    {
                        Icon = "🔥",
                        Title = "Start Your Streak",
                        Description = "You haven't started a cooking streak yet.",
                        Recommendation = "Cook a recipe today to begin building your cooking consistency!"
                    });
                }
                else if (currentStreak < 7)
                {
                    insights.Add(new Insight
                    {
                        Icon = "💪",
                        Title = "Building Momentum",
                        Description = $"You're on a {currentStreak}-day cooking streak.",
                        Recommendation = "Keep it up! Consistency is key to developing cooking skills."
                    });
                }
                
                // Category diversity insight
                var categories = _userRecipes.Select(r => r.Category ?? "General").Distinct().Count();
                if (categories < 3)
                {
                    insights.Add(new Insight
                    {
                        Icon = "🌈",
                        Title = "Explore New Categories",
                        Description = $"You've explored {categories} recipe categories.",
                        Recommendation = "Try recipes from different cuisines to expand your culinary skills!"
                    });
                }
                
                foreach (var insight in insights)
                {
                    _insights.Add(insight);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to generate insights: {ex.Message}");
            }
        }

        private List<Achievement> GetNextAchievements(int recipeCount)
        {
            return new List<Achievement>
            {
                new Achievement { Id = "first_recipe", Title = "First Recipe", Description = "Post your first recipe", Target = 1 },
                new Achievement { Id = "five_recipes", Title = "Recipe Explorer", Description = "Post 5 recipes", Target = 5 },
                new Achievement { Id = "ten_recipes", Title = "Cook Star", Description = "Post 10 recipes", Target = 10 },
                new Achievement { Id = "twenty_recipes", Title = "Master Chef", Description = "Post 20 recipes", Target = 20 },
                new Achievement { Id = "fifty_recipes", Title = "Culinary Expert", Description = "Post 50 recipes", Target = 50 }
            }.Where(a => a.Target > recipeCount).ToList();
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

        private int GetTotalAvailableAchievements()
        {
            return 15; // Placeholder for total available achievements
        }

        // Event Handlers
        private async void OnPeriodSelectorClicked(object sender, EventArgs e)
        {
            var action = await DisplayActionSheet("Select Time Period", "Cancel", null,
                "Last 7 Days", "Last 30 Days", "Last 3 Months", "Last Year", "Custom Range");
            
            switch (action)
            {
                case "Last 7 Days":
                    _currentPeriodStart = DateTime.Now.AddDays(-7);
                    _currentPeriodEnd = DateTime.Now;
                    break;
                case "Last 30 Days":
                    _currentPeriodStart = DateTime.Now.AddDays(-30);
                    _currentPeriodEnd = DateTime.Now;
                    break;
                case "Last 3 Months":
                    _currentPeriodStart = DateTime.Now.AddMonths(-3);
                    _currentPeriodEnd = DateTime.Now;
                    break;
                case "Last Year":
                    _currentPeriodStart = DateTime.Now.AddYears(-1);
                    _currentPeriodEnd = DateTime.Now;
                    break;
                case "Custom Range":
                    // For now, just use default range
                    break;
            }
            
            if (action != "Cancel" && action != "Custom Range")
            {
                UpdatePeriodLabel();
                await LoadAnalyticsData();
            }
        }

        private async void OnViewAllAchievementsClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new AchievementsPage(_authService));
        }

        private async void OnExportAnalyticsClicked(object sender, EventArgs e)
        {
            try
            {
                var report = GenerateAnalyticsReport();
                var fileName = $"Analytics_Report_{DateTime.Now:yyyyMMdd}.txt";
                var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);
                
                await File.WriteAllTextAsync(filePath, report);
                
                await Share.RequestAsync(new ShareFileRequest
                {
                    Title = "Analytics Report",
                    File = new ShareFile(filePath)
                });
            }
            catch (Exception ex)
            {
                await DisplayAlert("Export Error", $"Failed to export analytics: {ex.Message}", "OK");
            }
        }

        private async void OnShareProgressClicked(object sender, EventArgs e)
        {
            var shareText = $"🍳 My Cooking Progress:\n" +
                           $"📝 {_userRecipes.Count} recipes created\n" +
                           $"🔥 {CurrentStreakLabel.Text}-day cooking streak\n" +
                           $"👁️ {TotalViewsLabel.Text} recipe views\n" +
                           $"🍳 {TotalCooksLabel.Text} times cooked\n\n" +
                           $"Join me on my culinary journey! #RecipeApp #Cooking";
            
            await Share.RequestAsync(new ShareTextRequest
            {
                Text = shareText,
                Title = "My Cooking Progress"
            });
        }

        private async void OnSetGoalsClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Coming Soon", "Goal setting feature will be available in a future update!", "OK");
        }

        private string GenerateAnalyticsReport()
        {
            return $"""
                📊 Recipe App Analytics Report
                Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}
                Period: {PeriodLabel.Text}
                
                📈 Key Metrics:
                • Total Recipe Views: {TotalViewsLabel.Text}
                • Times Cooked: {TotalCooksLabel.Text}
                • Active Days: {ActiveDaysLabel.Text}
                • Average Rating: {AvgRatingLabel.Text}
                
                🔥 Streak Information:
                • Current Streak: {CurrentStreakLabel.Text} days
                • Longest Streak: {LongestStreakLabel.Text} days
                • Goal Progress: {StreakGoalLabel.Text}
                
                📝 Recipe Collection:
                • Total Recipes: {_userRecipes.Count}
                • Categories: {_userRecipes.Select(r => r.Category ?? "General").Distinct().Count()}
                
                🏆 Achievements:
                • {AchievementSummaryLabel.Text}
                
                💡 Top Insights:
                {string.Join("\n", _insights.Select(i => $"• {i.Title}: {i.Description}"))}
                
                Generated by Recipe App Analytics Dashboard
                """;
        }
    }

    // Supporting models for analytics
    public class RecipePerformance
    {
        public string Title { get; set; }
        public string Category { get; set; }
        public int ViewCount { get; set; }
    }

    public class CategoryBreakdown
    {
        public string Category { get; set; }
        public int Count { get; set; }
        public double Percentage { get; set; }
        public string Color { get; set; }
    }

    public class AchievementProgress
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }
        public double ProgressPercentage { get; set; }
        public string ProgressText { get; set; }
    }

    public class Insight
    {
        public string Icon { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Recommendation { get; set; }
    }
}