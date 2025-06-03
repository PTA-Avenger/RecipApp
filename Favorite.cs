namespace RecipeApp.Models
{
    public class Favorite
    {
        public string Id { get; set; } // Firestore document ID
        public string UserId { get; set; }
        public string RecipeId { get; set; }
    }
}
