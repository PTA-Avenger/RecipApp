using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;

namespace RecipeApp.Services
{
    public class GeminiService
    {
        private readonly string _apiKey = "AIzaSyCwSmPsOMk4rQRURb2HC7ZAj7OR_IR1oVk";
        private readonly HttpClient _httpClient = new();

        public async Task<string> GetMealPlanAsync(string dietaryPreference)
        {
            var prompt = $"Generate a weekly meal plan based on the following dietary preference: {dietaryPreference}. Include 3 meals per day.";

            return await QueryGeminiAsync(prompt);
        }

        public async Task<string> GetRecipesByIngredientsAsync(string ingredients)
        {
            var prompt = $"Suggest recipes using the following ingredients only: {ingredients}. List recipe names and brief steps.";

            return await QueryGeminiAsync(prompt);
        }

        private async Task<string> QueryGeminiAsync(string prompt)
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={_apiKey}";

            var body = new
            {
                contents = new[]
                {
            new
            {
                parts = new[]
                {
                    new { text = prompt }
                }
            }
        }
            };

            var request = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, request);
            var json = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Check for error response
            if (root.TryGetProperty("error", out JsonElement error))
            {
                var message = error.GetProperty("message").GetString();
                Console.WriteLine($"Gemini API Error: {message}");
                return $"Error from Gemini API: {message}";
            }

            // Check if 'candidates' exists
            if (!root.TryGetProperty("candidates", out JsonElement candidates) || candidates.GetArrayLength() == 0)
            {
                return "No response candidates from Gemini API.";
            }

            try
            {
                return candidates[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Parsing error: {ex.Message}");
                return "Failed to parse Gemini API response.";
            }
        }
    }
}
