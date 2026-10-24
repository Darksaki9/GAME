using UnityEngine;

namespace NutritionKitchen.Crafting
{
    /// <summary>All ingredients the stations can use. Lives in Resources so runtime code can load it by name.</summary>
    [CreateAssetMenu(fileName = "IngredientLibrary", menuName = "Nutrition Kitchen/Ingredient Library")]
    public class IngredientLibrary : ScriptableObject
    {
        public IngredientData[] ingredients;

        public static IngredientLibrary Load() => Resources.Load<IngredientLibrary>("IngredientLibrary");
    }
}
