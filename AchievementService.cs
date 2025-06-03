using RecipeApp.Services;
public class AchievementService
{
    private readonly FirestoreService _firestoreService = new();

    public async Task CheckAndUnlockAchievementsAsync(string userId, string idToken)
    {
        var recipes = await _firestoreService.GetUserRecipesAsync(userId, idToken);
        var unlocked = new List<Achievement>();

        if (recipes.Count >= 1)
            unlocked.Add(new Achievement { Id = "first_recipe", Title = "First Recipe Posted", Description = "You posted your first recipe!", IsUnlocked = true });

        if (recipes.Count >= 10)
            unlocked.Add(new Achievement { Id = "ten_recipes", Title = "Cook Star", Description = "You've posted 10 recipes!", IsUnlocked = true });

        // TODO: Save to Firestore
        foreach (var achievement in unlocked)
        {
            await _firestoreService.SaveUserAchievementAsync(userId, achievement, idToken);
        }
    }
}
