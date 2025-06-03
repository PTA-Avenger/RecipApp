using RecipeApp.Services;
using RecipeApp.Pages;

namespace RecipeApp;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        var authService = new AuthService();
        var firestoreService = new FirestoreService();
        authService.LoadSession();

        if (authService.IsLoggedIn())
        {
            MainPage = new NavigationPage(new HomePage(authService, firestoreService));
        }
        else
        {
            MainPage = new NavigationPage(new LoginPage()) 
            {
                BarBackgroundColor = Color.FromArgb("#6200EE"),
                BarTextColor = Colors.White
            };
        }
    }
}
