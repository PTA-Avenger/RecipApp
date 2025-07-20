using RecipeApp.Models;
using System.Text.Json;

namespace RecipeApp.Services
{
    public class SmartRecommendationService
    {
        private readonly FirestoreService _firestoreService;
        private readonly GeminiService _geminiService;
        private readonly AuthService _authService;

        public SmartRecommendationService(FirestoreService firestoreService, GeminiService geminiService, AuthService authService)
        {
            _firestoreService = firestoreService;
            _geminiService = geminiService;
            _authService = authService;
        }

        public async Task<List<RecipeRecommendation>> GetPersonalizedRecommendationsAsync(string userId, int count = 5)
        {
            try
            {
                var userProfile = await BuildUserProfileAsync(userId);
                var allRecipes = await _firestoreService.GetRecipesAsync(_authService.GetIdToken());
                var userRecipes = await _firestoreService.GetUserRecipesAsync(userId, _authService.GetIdToken());
                var favorites = await _firestoreService.GetFavoritesAsync(userId, _authService.GetIdToken());

                var recommendations = new List<RecipeRecommendation>();

                // 1. Content-based filtering (similar to user's favorites)
                var contentBasedRecs = await GetContentBasedRecommendations(userProfile, allRecipes, userRecipes, favorites);
                recommendations.AddRange(contentBasedRecs.Take(2));

                // 2. Collaborative filtering (what similar users like)
                var collaborativeRecs = await GetCollaborativeRecommendations(userId, allRecipes, favorites);
                recommendations.AddRange(collaborativeRecs.Take(2));

                // 3. Seasonal and trending recommendations
                var seasonalRecs = await GetSeasonalRecommendations(allRecipes, userProfile);
                recommendations.AddRange(seasonalRecs.Take(1));

                // 4. Skill-level appropriate recommendations
                var skillBasedRecs = await GetSkillBasedRecommendations(userProfile, allRecipes);
                recommendations.AddRange(skillBasedRecs.Take(2));

                // 5. Dietary restriction compliant recommendations
                var dietaryRecs = await GetDietaryRecommendations(userProfile, allRecipes);
                recommendations.AddRange(dietaryRecs.Take(1));

                // Remove duplicates and recipes user already has
                var existingRecipeIds = userRecipes.Select(r => r.Id).ToHashSet();
                recommendations = recommendations
                    .Where(r => !existingRecipeIds.Contains(r.Recipe.Id))
                    .GroupBy(r => r.Recipe.Id)
                    .Select(g => g.First())
                    .OrderByDescending(r => r.ConfidenceScore)
                    .Take(count)
                    .ToList();

                return recommendations;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting personalized recommendations: {ex.Message}");
                return new List<RecipeRecommendation>();
            }
        }

        public async Task<List<Recipe>> GetMealPlanSuggestionsAsync(string userId, DateTime startDate, int days = 7)
        {
            try
            {
                var userProfile = await BuildUserProfileAsync(userId);
                var allRecipes = await _firestoreService.GetRecipesAsync(_authService.GetIdToken());
                var suggestions = new List<Recipe>();

                for (int i = 0; i < days; i++)
                {
                    var currentDate = startDate.AddDays(i);
                    var dayOfWeek = currentDate.DayOfWeek;
                    
                    // Consider day of week preferences (e.g., quick meals on weekdays)
                    var isWeekend = dayOfWeek == DayOfWeek.Saturday || dayOfWeek == DayOfWeek.Sunday;
                    var complexity = isWeekend ? "medium" : "easy";
                    
                    var dayRecipes = await GetDaySpecificRecommendations(userProfile, allRecipes, complexity, currentDate);
                    suggestions.AddRange(dayRecipes.Take(3)); // Breakfast, lunch, dinner
                }

                return suggestions.Take(days * 3).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting meal plan suggestions: {ex.Message}");
                return new List<Recipe>();
            }
        }

        public async Task<List<Recipe>> GetIngredientBasedSuggestionsAsync(List<string> availableIngredients, string userId)
        {
            try
            {
                var userProfile = await BuildUserProfileAsync(userId);
                var allRecipes = await _firestoreService.GetRecipesAsync(_authService.GetIdToken());
                
                var suggestions = allRecipes
                    .Where(recipe => HasCommonIngredients(recipe.Ingredients, availableIngredients))
                    .Select(recipe => new
                    {
                        Recipe = recipe,
                        MatchScore = CalculateIngredientMatchScore(recipe.Ingredients, availableIngredients),
                        PreferenceScore = CalculatePreferenceScore(recipe, userProfile)
                    })
                    .Where(x => x.MatchScore > 0.3) // At least 30% ingredient match
                    .OrderByDescending(x => x.MatchScore * 0.7 + x.PreferenceScore * 0.3)
                    .Select(x => x.Recipe)
                    .Take(10)
                    .ToList();

                return suggestions;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting ingredient-based suggestions: {ex.Message}");
                return new List<Recipe>();
            }
        }

        public async Task<CookingInsights> GenerateCookingInsightsAsync(string userId)
        {
            try
            {
                var userProfile = await BuildUserProfileAsync(userId);
                var analytics = await _firestoreService.GetCookingAnalyticsAsync(userId, _authService.GetIdToken());
                
                var insights = new CookingInsights
                {
                    UserId = userId,
                    GeneratedAt = DateTime.UtcNow
                };

                // Analyze cooking patterns
                insights.CookingFrequency = AnalyzeCookingFrequency(analytics);
                insights.PreferredCategories = userProfile.PreferredCategories.Take(3).ToList();
                insights.SkillProgression = AnalyzeSkillProgression(userProfile);
                insights.SeasonalTrends = AnalyzeSeasonalTrends(userProfile);
                
                // Generate AI-powered insights using Gemini
                insights.AIInsights = await GenerateAIInsights(userProfile, analytics);
                
                // Suggest improvements
                insights.ImprovementSuggestions = GenerateImprovementSuggestions(userProfile, analytics);

                return insights;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating cooking insights: {ex.Message}");
                return new CookingInsights { UserId = userId, GeneratedAt = DateTime.UtcNow };
            }
        }

        public async Task<List<Recipe>> GetTrendingRecipesAsync(int count = 10)
        {
            try
            {
                var allRecipes = await _firestoreService.GetRecipesAsync(_authService.GetIdToken());
                var analytics = await _firestoreService.GetCookingAnalyticsAsync("", _authService.GetIdToken());
                
                // For demo purposes, simulate trending based on creation date and random popularity
                var random = new Random();
                var trending = allRecipes
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(50) // Recent recipes
                    .OrderBy(r => random.Next()) // Add some randomness
                    .Take(count)
                    .ToList();

                return trending;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting trending recipes: {ex.Message}");
                return new List<Recipe>();
            }
        }

        private async Task<UserProfile> BuildUserProfileAsync(string userId)
        {
            try
            {
                var userRecipes = await _firestoreService.GetUserRecipesAsync(userId, _authService.GetIdToken());
                var favorites = await _firestoreService.GetFavoritesAsync(userId, _authService.GetIdToken());
                var analytics = await _firestoreService.GetCookingAnalyticsAsync(userId, _authService.GetIdToken());

                var profile = new UserProfile
                {
                    UserId = userId,
                    TotalRecipes = userRecipes.Count,
                    TotalFavorites = favorites.Count,
                    CookingStreak = await _firestoreService.GetStreakAsync(userId, _authService.GetIdToken())
                };

                // Analyze preferred categories
                profile.PreferredCategories = userRecipes
                    .GroupBy(r => r.Category ?? "General")
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key)
                    .ToList();

                // Analyze cooking frequency
                profile.AverageCookingFrequency = analytics.UniqueDays > 0 ? 
                    (double)analytics.TotalCooks / analytics.UniqueDays : 0;

                // Determine skill level based on recipe complexity and variety
                profile.EstimatedSkillLevel = DetermineSkillLevel(userRecipes, analytics);

                // Extract dietary preferences (this would be enhanced with user input)
                profile.DietaryRestrictions = ExtractDietaryRestrictions(userRecipes);

                return profile;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error building user profile: {ex.Message}");
                return new UserProfile { UserId = userId };
            }
        }

        private async Task<List<RecipeRecommendation>> GetContentBasedRecommendations(
            UserProfile userProfile, List<Recipe> allRecipes, List<Recipe> userRecipes, List<Favorite> favorites)
        {
            var recommendations = new List<RecipeRecommendation>();

            try
            {
                var favoriteRecipeIds = favorites.Select(f => f.RecipeId).ToHashSet();
                var favoriteRecipes = allRecipes.Where(r => favoriteRecipeIds.Contains(r.Id)).ToList();

                foreach (var favoriteRecipe in favoriteRecipes.Take(3))
                {
                    var similarRecipes = allRecipes
                        .Where(r => r.Id != favoriteRecipe.Id && !userRecipes.Any(ur => ur.Id == r.Id))
                        .Where(r => r.Category == favoriteRecipe.Category)
                        .Take(2);

                    foreach (var recipe in similarRecipes)
                    {
                        recommendations.Add(new RecipeRecommendation
                        {
                            Recipe = recipe,
                            ReasonType = "Similar to your favorites",
                            Reason = $"Similar to '{favoriteRecipe.Title}' which you favorited",
                            ConfidenceScore = 0.8
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in content-based recommendations: {ex.Message}");
            }

            return recommendations;
        }

        private async Task<List<RecipeRecommendation>> GetCollaborativeRecommendations(
            string userId, List<Recipe> allRecipes, List<Favorite> userFavorites)
        {
            var recommendations = new List<RecipeRecommendation>();

            try
            {
                // Simplified collaborative filtering - in a real implementation,
                // this would analyze similar users' preferences
                var userFavoriteRecipeIds = userFavorites.Select(f => f.RecipeId).ToHashSet();
                
                // Find recipes that are popular among users with similar taste
                var popularRecipes = allRecipes
                    .Where(r => !userFavoriteRecipeIds.Contains(r.Id))
                    .OrderByDescending(r => r.CreatedAt) // Simulate popularity with recency
                    .Take(5);

                foreach (var recipe in popularRecipes)
                {
                    recommendations.Add(new RecipeRecommendation
                    {
                        Recipe = recipe,
                        ReasonType = "Popular choice",
                        Reason = "Other users with similar taste love this recipe",
                        ConfidenceScore = 0.6
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in collaborative recommendations: {ex.Message}");
            }

            return recommendations;
        }

        private async Task<List<RecipeRecommendation>> GetSeasonalRecommendations(List<Recipe> allRecipes, UserProfile userProfile)
        {
            var recommendations = new List<RecipeRecommendation>();

            try
            {
                var currentSeason = GetCurrentSeason();
                var seasonalKeywords = GetSeasonalKeywords(currentSeason);

                var seasonalRecipes = allRecipes
                    .Where(r => seasonalKeywords.Any(keyword => 
                        r.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                        r.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                        r.Ingredients.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                    .Take(3);

                foreach (var recipe in seasonalRecipes)
                {
                    recommendations.Add(new RecipeRecommendation
                    {
                        Recipe = recipe,
                        ReasonType = "Seasonal suggestion",
                        Reason = $"Perfect for {currentSeason.ToLower()} season",
                        ConfidenceScore = 0.7
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in seasonal recommendations: {ex.Message}");
            }

            return recommendations;
        }

        private async Task<List<RecipeRecommendation>> GetSkillBasedRecommendations(UserProfile userProfile, List<Recipe> allRecipes)
        {
            var recommendations = new List<RecipeRecommendation>();

            try
            {
                // Recommend recipes slightly above user's current skill level for growth
                var targetComplexity = userProfile.EstimatedSkillLevel switch
                {
                    "Beginner" => new[] { "Easy", "Medium" },
                    "Intermediate" => new[] { "Medium", "Hard" },
                    "Advanced" => new[] { "Hard", "Expert" },
                    _ => new[] { "Easy" }
                };

                var skillAppropriateRecipes = allRecipes
                    .Where(r => targetComplexity.Any(complexity => 
                        r.Steps.Length < GetComplexityStepThreshold(complexity)))
                    .Take(3);

                foreach (var recipe in skillAppropriateRecipes)
                {
                    recommendations.Add(new RecipeRecommendation
                    {
                        Recipe = recipe,
                        ReasonType = "Skill development",
                        Reason = $"Great for developing your {userProfile.EstimatedSkillLevel.ToLower()} cooking skills",
                        ConfidenceScore = 0.75
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in skill-based recommendations: {ex.Message}");
            }

            return recommendations;
        }

        private async Task<List<RecipeRecommendation>> GetDietaryRecommendations(UserProfile userProfile, List<Recipe> allRecipes)
        {
            var recommendations = new List<RecipeRecommendation>();

            try
            {
                if (userProfile.DietaryRestrictions.Any())
                {
                    var dietaryCompliantRecipes = allRecipes
                        .Where(r => IsDietaryCompliant(r, userProfile.DietaryRestrictions))
                        .Take(2);

                    foreach (var recipe in dietaryCompliantRecipes)
                    {
                        recommendations.Add(new RecipeRecommendation
                        {
                            Recipe = recipe,
                            ReasonType = "Dietary preference",
                            Reason = $"Matches your {string.Join(", ", userProfile.DietaryRestrictions)} preferences",
                            ConfidenceScore = 0.85
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in dietary recommendations: {ex.Message}");
            }

            return recommendations;
        }

        private async Task<List<Recipe>> GetDaySpecificRecommendations(UserProfile userProfile, List<Recipe> allRecipes, 
            string complexity, DateTime date)
        {
            try
            {
                var dayRecipes = allRecipes
                    .Where(r => GetRecipeComplexity(r) == complexity)
                    .Where(r => userProfile.PreferredCategories.Contains(r.Category ?? "General"))
                    .OrderBy(r => Guid.NewGuid()) // Random selection
                    .Take(3)
                    .ToList();

                return dayRecipes;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting day-specific recommendations: {ex.Message}");
                return new List<Recipe>();
            }
        }

        // Helper methods
        private bool HasCommonIngredients(string recipeIngredients, List<string> availableIngredients)
        {
            if (string.IsNullOrEmpty(recipeIngredients)) return false;
            
            var recipeIngredientList = recipeIngredients.ToLower().Split(',', ';', '\n')
                .Select(i => i.Trim())
                .Where(i => !string.IsNullOrEmpty(i));

            return availableIngredients.Any(available => 
                recipeIngredientList.Any(recipe => 
                    recipe.Contains(available.ToLower()) || available.ToLower().Contains(recipe)));
        }

        private double CalculateIngredientMatchScore(string recipeIngredients, List<string> availableIngredients)
        {
            if (string.IsNullOrEmpty(recipeIngredients)) return 0;
            
            var recipeIngredientList = recipeIngredients.ToLower().Split(',', ';', '\n')
                .Select(i => i.Trim())
                .Where(i => !string.IsNullOrEmpty(i))
                .ToList();

            if (!recipeIngredientList.Any()) return 0;

            var matchCount = availableIngredients.Count(available => 
                recipeIngredientList.Any(recipe => 
                    recipe.Contains(available.ToLower()) || available.ToLower().Contains(recipe)));

            return (double)matchCount / recipeIngredientList.Count;
        }

        private double CalculatePreferenceScore(Recipe recipe, UserProfile userProfile)
        {
            double score = 0;
            
            if (userProfile.PreferredCategories.Contains(recipe.Category ?? "General"))
                score += 0.5;
            
            if (GetRecipeComplexity(recipe) == userProfile.EstimatedSkillLevel)
                score += 0.3;
            
            if (IsDietaryCompliant(recipe, userProfile.DietaryRestrictions))
                score += 0.2;

            return Math.Min(score, 1.0);
        }

        private string GetCurrentSeason()
        {
            var month = DateTime.Now.Month;
            return month switch
            {
                12 or 1 or 2 => "Winter",
                3 or 4 or 5 => "Spring",
                6 or 7 or 8 => "Summer",
                9 or 10 or 11 => "Fall",
                _ => "Spring"
            };
        }

        private List<string> GetSeasonalKeywords(string season)
        {
            return season switch
            {
                "Winter" => new List<string> { "soup", "stew", "warm", "comfort", "hearty", "hot" },
                "Spring" => new List<string> { "fresh", "light", "green", "asparagus", "peas", "herbs" },
                "Summer" => new List<string> { "grilled", "salad", "cold", "fresh", "tomato", "berries" },
                "Fall" => new List<string> { "pumpkin", "apple", "squash", "roasted", "cinnamon", "spice" },
                _ => new List<string>()
            };
        }

        private string DetermineSkillLevel(List<Recipe> userRecipes, CookingAnalytics analytics)
        {
            if (userRecipes.Count < 3) return "Beginner";
            if (userRecipes.Count < 10) return "Intermediate";
            return "Advanced";
        }

        private List<string> ExtractDietaryRestrictions(List<Recipe> userRecipes)
        {
            var restrictions = new List<string>();
            
            // Simple keyword analysis - in a real app, this would be more sophisticated
            var allText = string.Join(" ", userRecipes.Select(r => $"{r.Title} {r.Description} {r.Ingredients}")).ToLower();
            
            if (allText.Contains("vegetarian") || allText.Contains("veggie"))
                restrictions.Add("Vegetarian");
            if (allText.Contains("vegan"))
                restrictions.Add("Vegan");
            if (allText.Contains("gluten-free") || allText.Contains("gluten free"))
                restrictions.Add("Gluten-Free");
            if (allText.Contains("keto") || allText.Contains("low-carb"))
                restrictions.Add("Keto");

            return restrictions;
        }

        private string GetRecipeComplexity(Recipe recipe)
        {
            var stepCount = recipe.Steps.Split('.', '!', '?').Length;
            return stepCount switch
            {
                <= 3 => "Easy",
                <= 6 => "Medium",
                <= 10 => "Hard",
                _ => "Expert"
            };
        }

        private int GetComplexityStepThreshold(string complexity)
        {
            return complexity switch
            {
                "Easy" => 100,
                "Medium" => 200,
                "Hard" => 300,
                "Expert" => 500,
                _ => 100
            };
        }

        private bool IsDietaryCompliant(Recipe recipe, List<string> restrictions)
        {
            var recipeText = $"{recipe.Title} {recipe.Description} {recipe.Ingredients}".ToLower();
            
            foreach (var restriction in restrictions)
            {
                switch (restriction.ToLower())
                {
                    case "vegetarian":
                        if (recipeText.Contains("meat") || recipeText.Contains("chicken") || 
                            recipeText.Contains("beef") || recipeText.Contains("pork"))
                            return false;
                        break;
                    case "vegan":
                        if (recipeText.Contains("meat") || recipeText.Contains("dairy") || 
                            recipeText.Contains("egg") || recipeText.Contains("cheese"))
                            return false;
                        break;
                    case "gluten-free":
                        if (recipeText.Contains("wheat") || recipeText.Contains("flour") || 
                            recipeText.Contains("bread"))
                            return false;
                        break;
                }
            }
            
            return true;
        }

        private string AnalyzeCookingFrequency(CookingAnalytics analytics)
        {
            var frequency = analytics.UniqueDays > 0 ? (double)analytics.TotalCooks / analytics.UniqueDays : 0;
            return frequency switch
            {
                >= 2 => "Very Active",
                >= 1 => "Active",
                >= 0.5 => "Moderate",
                > 0 => "Occasional",
                _ => "Inactive"
            };
        }

        private string AnalyzeSkillProgression(UserProfile userProfile)
        {
            return userProfile.TotalRecipes switch
            {
                >= 50 => "Expert level - You're a cooking master!",
                >= 20 => "Advanced level - Great cooking skills!",
                >= 10 => "Intermediate level - Keep improving!",
                >= 3 => "Beginner level - Good start!",
                _ => "New to cooking - Welcome!"
            };
        }

        private List<string> AnalyzeSeasonalTrends(UserProfile userProfile)
        {
            // Simplified seasonal analysis
            return new List<string> { "You cook more comfort food in winter", "You prefer fresh ingredients in summer" };
        }

        private async Task<List<string>> GenerateAIInsights(UserProfile userProfile, CookingAnalytics analytics)
        {
            try
            {
                var prompt = $"Analyze this cooking profile and provide 3 personalized insights: " +
                           $"Total recipes: {userProfile.TotalRecipes}, " +
                           $"Skill level: {userProfile.EstimatedSkillLevel}, " +
                           $"Preferred categories: {string.Join(", ", userProfile.PreferredCategories)}, " +
                           $"Cooking frequency: {userProfile.AverageCookingFrequency}";

                var aiResponse = await _geminiService.GetResponseAsync(prompt);
                return new List<string> { aiResponse };
            }
            catch
            {
                return new List<string> { "Keep exploring new recipes to expand your culinary horizons!" };
            }
        }

        private List<string> GenerateImprovementSuggestions(UserProfile userProfile, CookingAnalytics analytics)
        {
            var suggestions = new List<string>();

            if (userProfile.TotalRecipes < 5)
                suggestions.Add("Try adding more recipes to get better recommendations");
            
            if (userProfile.PreferredCategories.Count < 3)
                suggestions.Add("Explore different cuisine categories to diversify your cooking");
            
            if (userProfile.CookingStreak < 7)
                suggestions.Add("Build a cooking streak to develop consistent habits");

            return suggestions;
        }
    }

    // Supporting models
    public class RecipeRecommendation
    {
        public Recipe Recipe { get; set; }
        public string ReasonType { get; set; }
        public string Reason { get; set; }
        public double ConfidenceScore { get; set; }
    }

    public class UserProfile
    {
        public string UserId { get; set; }
        public int TotalRecipes { get; set; }
        public int TotalFavorites { get; set; }
        public int CookingStreak { get; set; }
        public List<string> PreferredCategories { get; set; } = new List<string>();
        public double AverageCookingFrequency { get; set; }
        public string EstimatedSkillLevel { get; set; } = "Beginner";
        public List<string> DietaryRestrictions { get; set; } = new List<string>();
    }

    public class CookingInsights
    {
        public string UserId { get; set; }
        public DateTime GeneratedAt { get; set; }
        public string CookingFrequency { get; set; }
        public List<string> PreferredCategories { get; set; } = new List<string>();
        public string SkillProgression { get; set; }
        public List<string> SeasonalTrends { get; set; } = new List<string>();
        public List<string> AIInsights { get; set; } = new List<string>();
        public List<string> ImprovementSuggestions { get; set; } = new List<string>();
    }
}