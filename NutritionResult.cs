using System.Text.Json.Serialization;

namespace RecipeApp.Models
{
    public class NutritionResult
    {
        public float Calories { get; set; }
        public float Protein { get; set; }
        public float Carbs { get; set; }
        public float Fat { get; set; }

        public static NutritionResult FromResponse(NutritionResponse response)
        {
            var totals = new NutritionResult();

            foreach (var item in response.foods)
            {
                totals.Calories += item.Calories;
                totals.Protein += item.Protein;
                totals.Carbs += item.Carbs;
                totals.Fat += item.Fat;
            }

            return totals;
        }
    }

    public class NutritionResponse
    {
        [JsonPropertyName("foods")]
        public List<NutritionData> foods { get; set; }
    }

    public class NutritionData
    {
        [JsonPropertyName("nf_calories")]
        public float Calories { get; set; }

        [JsonPropertyName("nf_total_fat")]
        public float Fat { get; set; }

        [JsonPropertyName("nf_total_carbohydrate")]
        public float Carbs { get; set; }

        [JsonPropertyName("nf_protein")]
        public float Protein { get; set; }
    }
}
