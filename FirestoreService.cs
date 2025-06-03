using System.Net.Http.Headers;
using System.Net.Http.Json;
using RecipeApp.Models;
using System.Text.Json;
using System.Text.RegularExpressions;


namespace RecipeApp.Services
{
    public class FirestoreService
    {
        private readonly string _projectId = "recipeapp-2fc64";
        private readonly HttpClient _httpClient = new();

        // Save a new recipe document to Firestore
        public async Task<bool> SaveRecipeAsync(Recipe recipe, string idToken)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", idToken);

                var firestoreUrl = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/recipes";

                var data = new
                {
                    fields = new
                    {
                        title = new { stringValue = recipe.Title },
                        description = new { stringValue = recipe.Description },
                        ingredients = new { stringValue = recipe.Ingredients },
                        steps = new { stringValue = recipe.Steps },
                        userId = new { stringValue = recipe.UserId },
                        createdAt = new { timestampValue = DateTime.UtcNow.ToString("o") }
                    }
                };

                var response = await _httpClient.PostAsJsonAsync(firestoreUrl, data);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        // Toggle favorite status for a recipe
        public async Task<bool> ToggleFavoriteAsync(string userId, string recipeId, string idToken)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", idToken);

                var favoritesUrl = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/favorites";
                var response = await _httpClient.GetAsync($"{favoritesUrl}?filter=fields.userId.stringValue=\"{userId}\" AND fields.recipeId.stringValue=\"{recipeId}\"");

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                if (doc.RootElement.TryGetProperty("documents", out var docs) && docs.GetArrayLength() > 0)
                {
                    // If favorite exists, delete it
                    var favDocName = docs[0].GetProperty("name").GetString();
                    var deleteResp = await _httpClient.DeleteAsync($"https://firestore.googleapis.com/v1/{favDocName}");
                    return deleteResp.IsSuccessStatusCode;
                }
                else
                {
                    // If favorite does not exist, create it
                    var favoriteData = new
                    {
                        fields = new
                        {
                            userId = new { stringValue = userId },
                            recipeId = new { stringValue = recipeId }
                        }
                    };

                    var createResp = await _httpClient.PostAsJsonAsync(favoritesUrl, favoriteData);
                    return createResp.IsSuccessStatusCode;
                }
            }
            catch
            {
                return false;
            }
        }

        // Get all recipe documents
        public async Task<List<Recipe>> GetRecipesAsync(string idToken)
        {
            var recipes = new List<Recipe>();

            try
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", idToken);

                var firestoreUrl = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/recipes";
                var response = await _httpClient.GetAsync(firestoreUrl);
                if (!response.IsSuccessStatusCode) return recipes;

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                if (!doc.RootElement.TryGetProperty("documents", out var docs)) return recipes;

                foreach (var entry in docs.EnumerateArray())
                {
                    var fields = entry.GetProperty("fields");
                    var docId = entry.GetProperty("name").GetString()?.Split('/').Last();

                    recipes.Add(new Recipe
                    {
                        Id = docId,
                        Title = fields.GetProperty("title").GetProperty("stringValue").GetString(),
                        Description = fields.GetProperty("description").GetProperty("stringValue").GetString(),
                        Ingredients = fields.GetProperty("ingredients").GetProperty("stringValue").GetString(),
                        Steps = fields.GetProperty("steps").GetProperty("stringValue").GetString(),
                        UserId = fields.GetProperty("userId").GetProperty("stringValue").GetString(),
                        CreatedAt = DateTime.TryParse(
                            fields.GetProperty("createdAt").GetProperty("timestampValue").GetString(),
                            out var createdAt
                        ) ? createdAt : DateTime.UtcNow
                    });
                }

                return recipes;
            }
            catch
            {
                return recipes;
            }
        }

        public async Task<List<Recipe>> GetTopFavoritedRecipesAsync(string idToken)
        {
            var allRecipes = await GetRecipesAsync(idToken);
            var favoritesUrl = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/favorites";

            var request = new HttpRequestMessage(HttpMethod.Get, favoritesUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", idToken);
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
                return new List<Recipe>();

            var json = await response.Content.ReadAsStringAsync();
            var counts = new Dictionary<string, int>();

            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("documents", out var docs)) return new List<Recipe>();

            foreach (var fav in docs.EnumerateArray())
            {
                var fields = fav.GetProperty("fields");
                var recipeId = fields.GetProperty("recipeId").GetProperty("stringValue").GetString();

                if (!string.IsNullOrEmpty(recipeId))
                {
                    if (!counts.ContainsKey(recipeId))
                        counts[recipeId] = 0;

                    counts[recipeId]++;
                }
            }

            var ranked = allRecipes
                .Where(r => counts.ContainsKey(r.Id))
                .Select(r => new { Recipe = r, Count = counts[r.Id] })
                .OrderByDescending(rc => rc.Count)
                .Take(10)
                .Select(rc => rc.Recipe)
                .ToList();

            return ranked;
        }


        // Delete a recipe by ID
        public async Task<bool> DeleteRecipeAsync(string recipeId, string idToken)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", idToken);

                var deleteUrl = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/recipes/{recipeId}";
                var response = await _httpClient.DeleteAsync(deleteUrl);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        // Update an existing recipe
        public async Task<bool> UpdateRecipeAsync(Recipe recipe, string idToken)
        {
            try
            {
                var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/recipes/{recipe.Id}";
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", idToken);

                var data = new
                {
                    fields = new
                    {
                        title = new { stringValue = recipe.Title },
                        description = new { stringValue = recipe.Description },
                        ingredients = new { stringValue = recipe.Ingredients },
                        steps = new { stringValue = recipe.Steps },
                        category = new { stringValue = recipe.Category },
                        userId = new { stringValue = recipe.UserId },
                        createdAt = new { timestampValue = recipe.CreatedAt.ToString("o") }
                    }
                };

                var response = await _httpClient.PatchAsJsonAsync(url, data);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        // Get a random recipe from Firestore
        public async Task<Recipe> GetRandomRecipeAsync(string idToken)
        {
            try
            {
                var recipes = await GetRecipesAsync(idToken);
                if (recipes == null || recipes.Count == 0) return null;

                var random = new Random();
                return recipes[random.Next(recipes.Count)];
            }
            catch
            {
                return null;
            }
        }

        // Get the list of recipe IDs that a user has marked as favorite
        public async Task<List<string>> GetFavoriteRecipeIdsAsync(string userId, string idToken)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", idToken);
                var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/favorites";

                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode) return new List<string>();

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                var recipeIds = new List<string>();

                if (doc.RootElement.TryGetProperty("documents", out var docs))
                {
                    foreach (var item in docs.EnumerateArray())
                    {
                        var fields = item.GetProperty("fields");
                        if (fields.TryGetProperty("userId", out var userField) &&
                            fields.TryGetProperty("recipeId", out var recipeField) &&
                            userField.GetProperty("stringValue").GetString() == userId)
                        {
                            recipeIds.Add(recipeField.GetProperty("stringValue").GetString());
                        }
                    }
                }

                return recipeIds;
            }
            catch
            {
                return new List<string>();
            }
        }

        // Checking if recipe is favorited.
        public async Task<bool> IsRecipeFavorited(string userId, string recipeId, string idToken)
        {
            try
            {
                var url = $"https://firestore.googleapis.com/v1/projects/YOUR_PROJECT_ID/databases/(default)/documents/users/{userId}/favorites/{recipeId}";
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", idToken);

                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
        public async Task SaveUserAchievementAsync(string userId, Achievement achievement, string idToken)
        {
            var docPath = $"users/{userId}/achievements/{achievement.Id}";
            var data = new
            {
                fields = new
                {
                    title = new { stringValue = achievement.Title },
                    description = new { stringValue = achievement.Description },
                    isUnlocked = new { booleanValue = achievement.IsUnlocked },
                    unlockedAt = new { timestampValue = DateTime.UtcNow.ToString("o") }
                }
            };

            var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/{docPath}";
            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", idToken);

            await client.PatchAsJsonAsync(url, data);
        }

        public async Task<List<Achievement>> GetAchievementsAsync(string userId, string idToken)
        {
            var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/users/{userId}/achievements";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", idToken);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return new List<Achievement>();

            var json = await response.Content.ReadAsStringAsync();
            var parsed = JsonDocument.Parse(json);
            var achievements = new List<Achievement>();

            foreach (var doc in parsed.RootElement.GetProperty("documents").EnumerateArray())
            {
                var fields = doc.GetProperty("fields");
                achievements.Add(new Achievement
                {
                    Title = fields.GetProperty("title").GetProperty("stringValue").GetString(),
                    Description = fields.GetProperty("description").GetProperty("stringValue").GetString(),
                    IsUnlocked = fields.GetProperty("isUnlocked").GetProperty("booleanValue").GetBoolean()
                });
            }

            return achievements;
        }

        public async Task UpdateCookingStreakAsync(string userId, string idToken)
        {
            var now = DateTime.UtcNow.Date;

            var userDoc = await _httpClient.GetAsync($"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/users/{userId}");
            var json = await userDoc.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);

            var lastCooked = doc.RootElement.GetProperty("fields").GetProperty("lastCookedDate").GetProperty("timestampValue").GetDateTime().Date;
            var streak = doc.RootElement.GetProperty("fields").GetProperty("streakCount").GetProperty("integerValue").GetInt32();

            if (now == lastCooked.AddDays(1))
                streak += 1;
            else if (now != lastCooked)
                streak = 1;

            var update = new
            {
                fields = new
                {
                    lastCookedDate = new { timestampValue = now.ToString("yyyy-MM-dd") },
                    streakCount = new { integerValue = streak }
                }
            };

            var request = new HttpRequestMessage(new HttpMethod("PATCH"), $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/users/{userId}")
            {
                Content = JsonContent.Create(update)
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", idToken);
            await _httpClient.SendAsync(request);
        }

        public async Task<int> GetStreakAsync(string userId, string idToken)
        {
            var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/users/{userId}";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", idToken);

            var response = await _httpClient.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);

            //return doc.RootElement.GetProperty("fields").GetProperty("StreakCount").GetProperty("integerValue").GetInt32();
            if (doc.RootElement.TryGetProperty("fields", out var fields) &&
    fields.TryGetProperty("StreakCount", out var streakProp) &&
    streakProp.TryGetProperty("integerValue", out var streakVal))
            {
                int streak = streakVal.GetInt32();
                return streak;
            }
            else
            {
                // Field is missing or improperly formatted
                Console.WriteLine("StreakCount field missing or invalid.");
                return 0;
            }

        }

        public async Task<(Challenge?, int)> GetDailyChallengeAsync(string userId, string idToken)
        {
            var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/users/{userId}/challenge_progress/today";
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", idToken);

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                // Create new daily challenge
                var challenge = await GetRandomChallengeAsync(idToken);
                if (challenge == null) return (null, 0);

                var createData = new
                {
                    fields = new
                    {
                        challengeId = new { stringValue = challenge.Id },
                        progress = new { integerValue = 0 }
                    }
                };

                await _httpClient.PatchAsync(url, JsonContent.Create(createData));

                return (challenge, 0);
            }

            var json = await response.Content.ReadAsStringAsync();
            var parsed = JsonDocument.Parse(json);

            var fields = parsed.RootElement.GetProperty("fields");
            var challengeId = fields.GetProperty("challengeId").GetProperty("stringValue").GetString();
            var progress = fields.GetProperty("progress").GetProperty("integerValue").GetInt32();

            // Fetch challenge
            var challengeResp = await _httpClient.GetAsync(
                $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/challenges/{challengeId}"
            );
            if (!challengeResp.IsSuccessStatusCode) return (null, progress);

            var challengeJson = await challengeResp.Content.ReadAsStringAsync();
            var challengeDoc = JsonDocument.Parse(challengeJson).RootElement.GetProperty("fields");

            var challengeObj = new Challenge
            {
                Id = challengeId,
                Description = challengeDoc.GetProperty("description").GetProperty("stringValue").GetString(),
                Target = challengeDoc.GetProperty("target").GetProperty("integerValue").GetInt32()
            };

            return (challengeObj, progress);
        }


        public async Task<bool> UpdateChallengeProgressAsync(string userId, string challengeId, int progress, string idToken)
        {
            var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/users/{userId}/challenge_progress/today";
            var data = new
            {
                fields = new
                {
                    challengeId = new { stringValue = challengeId },
                    progress = new { integerValue = progress }
                }
            };

            var request = new HttpRequestMessage(HttpMethod.Patch, url)
            {
                Content = JsonContent.Create(data)
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", idToken);

            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }

        public async Task<Challenge?> GetRandomChallengeAsync(string idToken)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", idToken);
            var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/challenges";

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("documents", out var docs) || docs.GetArrayLength() == 0) return null;

            var list = docs.EnumerateArray().ToList();
            var selected = list[new Random().Next(list.Count)];

            var fields = selected.GetProperty("fields");

            return new Challenge
            {
                Id = selected.GetProperty("name").GetString()?.Split('/').Last(),
                Description = fields.GetProperty("description").GetProperty("stringValue").GetString(),
                Target = fields.GetProperty("target").GetProperty("integerValue").GetInt32()
            };
        }

        public async Task<List<Recipe>> GetUserRecipesAsync(string userId, string idToken)
        {
            var allRecipes = await GetRecipesAsync(idToken);
            return allRecipes.Where(r => r.UserId == userId).ToList();
        }

        public async Task<(int StreakCount, DateTime LastCookedDate)> GetUserCookingStreakAsync(string userId, string idToken)
        {
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("Missing user ID");
            if (string.IsNullOrWhiteSpace(idToken)) throw new ArgumentException("Missing ID token");

            string url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/cookingStreaks/{userId}";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", idToken);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Failed to fetch streak: {response.StatusCode}");
                return (0, DateTime.MinValue);
            }

            var content = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            int streak = 0;
            DateTime lastCooked = DateTime.MinValue;

            if (root.TryGetProperty("fields", out JsonElement fields))
            {
                if (fields.TryGetProperty("StreakCount", out JsonElement streakField) &&
                    streakField.TryGetProperty("integerValue", out JsonElement streakValue))
                {
                    int.TryParse(streakValue.GetString(), out streak);
                }

                if (fields.TryGetProperty("LastCookedDate", out JsonElement dateField) &&
                    dateField.TryGetProperty("timestampValue", out JsonElement dateValue))
                {
                    DateTime.TryParse(dateValue.GetString(), out lastCooked);
                }
            }

            return (streak, lastCooked);
        }

        public async Task<bool> UpdateUserCookingStreakAsync(string userId, DateTime lastCookedDate, int streakCount, string idToken)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(idToken))
                throw new ArgumentException("User ID and token are required.");

            string projectId = "your-project-id"; // 🔁 Replace with your actual Firebase project ID
            string url = $"https://firestore.googleapis.com/v1/projects/{projectId}/databases/(default)/documents/cookingStreaks/{userId}?updateMask.fieldPaths=StreakCount&updateMask.fieldPaths=LastCookedDate";

            var payload = new
            {
                fields = new
                {
                    StreakCount = new { integerValue = streakCount.ToString() },
                    LastCookedDate = new { timestampValue = lastCookedDate.ToUniversalTime().ToString("o") }
                }
            };

            var json = JsonSerializer.Serialize(payload);
            var request = new HttpRequestMessage(HttpMethod.Patch, url)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", idToken);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Failed to update streak: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
                return false;
            }

            return true;
        }

    }
}

