using RecipeApp.Services;

namespace RecipeApp.Pages;

public partial class ChallengesPage : ContentPage
{
    private readonly AuthService _authService;
    private readonly FirestoreService _firestoreService = new();

    private int _target;
    private int _progress;
    private string _challengeId;

    public ChallengesPage(AuthService authService)
    {
        InitializeComponent();
        _authService = authService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var (challenge, progress) = await _firestoreService.GetDailyChallengeAsync(_authService.GetUserId(), _authService.GetIdToken());

        if (challenge != null)
        {
            _challengeId = challenge.Id;
            _target = challenge.Target;
            _progress = progress;

            ChallengeDescription.Text = challenge.Description;
            ChallengeProgressBar.Progress = (double)_progress / _target;
            ProgressLabel.Text = $"{_progress} / {_target} complete";
        }
        else
        {
            ChallengeDescription.Text = "No challenge today.";
            ChallengeProgressBar.Progress = 0;
            ProgressLabel.Text = "";
        }
    }

    private async void OnMarkProgressClicked(object sender, EventArgs e)
    {
        _progress++;
        await _firestoreService.UpdateChallengeProgressAsync(_authService.GetUserId(), _challengeId, _progress, _authService.GetIdToken());

        ChallengeProgressBar.Progress = (double)_progress / _target;
        ProgressLabel.Text = $"{_progress} / {_target} complete";

        if (_progress >= _target)
            await DisplayAlert("Well done!", "You completed today's challenge! 🎉", "OK");
    }
}
