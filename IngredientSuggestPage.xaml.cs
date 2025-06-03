using RecipeApp.Services;

namespace RecipeApp.Pages;

public partial class IngredientSuggestPage : ContentPage
{
    private readonly GeminiService _geminiService = new();

    public IngredientSuggestPage()
    {
        InitializeComponent();
    }

    private async void OnFindRecipesClicked(object sender, EventArgs e)
    {
        RecipesLabel.Text = "Searching for recipes...";
        var result = await _geminiService.GetRecipesByIngredientsAsync(IngredientEditor.Text);
        RecipesLabel.Text = result;
    }
}
