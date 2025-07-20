namespace RecipeApp.Models
{
    public class Achievement
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public bool IsUnlocked { get; set; }
        public int Target { get; set; } = 1;
        public DateTime UnlockedDate { get; set; }
        public string Category { get; set; } = "General";
        public int Points { get; set; } = 10;
        public string IconUrl { get; set; }
        public string BadgeColor { get; set; } = "#FFD700";
        public int Progress { get; set; } = 0;
        public bool IsHidden { get; set; } = false;
        public List<string> Requirements { get; set; } = new List<string>();
    }
}
