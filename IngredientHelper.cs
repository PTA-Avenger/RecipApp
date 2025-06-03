public static class IngredientHelper
{
    public static readonly Dictionary<string, string> Substitutions = new()
    {
        { "milk", "almond milk or oat milk" },
        { "egg", "flaxseed meal + water (vegan egg)" },
        { "butter", "coconut oil or olive oil" },
        { "wheat flour", "gluten-free flour" },
        { "peanut", "sunflower seed butter" }
    };

    public static readonly List<string> CommonAllergens = new()
    {
        "milk", "egg", "peanut", "soy", "wheat", "tree nut", "shellfish", "fish"
    };

    public static List<string> FindAllergens(string ingredients)
    {
        return CommonAllergens
            .Where(allergen => ingredients.ToLower().Contains(allergen.ToLower()))
            .ToList();
    }

    public static Dictionary<string, string> GetSubstitutions(string ingredients)
    {
        return Substitutions
            .Where(pair => ingredients.ToLower().Contains(pair.Key.ToLower()))
            .ToDictionary(pair => pair.Key, pair => pair.Value);
    }
}
