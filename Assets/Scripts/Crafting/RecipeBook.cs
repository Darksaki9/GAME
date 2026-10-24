using UnityEngine;

namespace NutritionKitchen.Crafting
{
    /// <summary>The recipes customers can order. Lives in Resources so runtime code can load it by name.</summary>
    [CreateAssetMenu(fileName = "RecipeBook", menuName = "Nutrition Kitchen/Recipe Book")]
    public class RecipeBook : ScriptableObject
    {
        public RecipeSO[] recipes;

        public static RecipeBook Load() => Resources.Load<RecipeBook>("RecipeBook");
    }
}
