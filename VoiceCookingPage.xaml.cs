using Microsoft.Maui.ApplicationModel.Communication;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.PlatformConfiguration.TizenSpecific;
using Microsoft.Maui.Media;
using RecipeApp.Models;

namespace RecipeApp.Pages;

public partial class VoiceCookingPage : ContentPage
{
    private Recipe _recipe;
    private List<string> _steps;

    public VoiceCookingPage(Recipe recipe)
    {
        InitializeComponent();
        _recipe = recipe;
        RecipeTitleLabel.Text = _recipe.Title;
        _steps = _recipe.Steps.Split(new[] { '\n', '.' }, StringSplitOptions.RemoveEmptyEntries)
                              .Select(s => s.Trim())
                              .Where(s => !string.IsNullOrWhiteSpace(s))
                              .ToList();
        StepsView.ItemsSource = _steps;
    }

    private async void OnStepSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is string step)
        {
            await TextToSpeech.SpeakAsync(step);
            ((CollectionView)sender).SelectedItem = null;
        }
    }

    private async void OnReadAllStepsClicked(object sender, EventArgs e)
    {
        foreach (var step in _steps)
        {
            await TextToSpeech.SpeakAsync(step);
            await Task.Delay(1500); // Small delay between steps
        }
    }

    private async void OnShareClicked(object sender, EventArgs e)
    {
        var fullSteps = string.Join("\n", _steps);
        await Share.RequestAsync(new ShareTextRequest
        {
            Title = $"Steps for {_recipe.Title}",
            Text = $"Recipe: {_recipe.Title}\n\nSteps:\n{fullSteps}"
        });
    }
}
