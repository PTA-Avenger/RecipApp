using System.Net.Http.Headers;
using System.Text.Json;
using RecipeApp.Models;
using System.Net.Http.Json;


public class NutritionService
{
    private readonly string _appId = "48882d48";
    private readonly string _apiKey = "0188f6918495279a4d9fda1b7af6beff";
    private readonly HttpClient _httpClient = new();

    public async Task<NutritionResult> AnalyzeIngredientsAsync(string ingredientsText)
    {
        var ingredients = ingredientsText.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                                         .Select(i => new { quantity = 1, measure = "item", food = i });

        var requestBody = new { ingredients = ingredients.ToList() };

        var request = new HttpRequestMessage(HttpMethod.Post, "https://trackapi.nutritionix.com/v2/natural/nutrients")
        {
            Content = JsonContent.Create(new { query = ingredientsText })
        };

        request.Headers.Add("x-app-id", _appId);
        request.Headers.Add("x-app-key", _apiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await _httpClient.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            return null;

        var data = JsonSerializer.Deserialize<RecipeApp.Models.NutritionResponse>(json);
        return NutritionResult.FromResponse(data);
    }
}
