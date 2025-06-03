namespace RecipeApp.Models
{
    public class Recipe
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Ingredients { get; set; }
        public string Steps { get; set; }
        public string UserId { get; set; }
        public string Category { get; set; }
        public bool IsInMealPlan { get; set; } = false;


        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
