using System;
using UnityEngine;

namespace NutritionKitchen.Crafting
{
    /// <summary>One thing done to an ingredient at a station. Steam = Boiling Station with the lid; Marinate = Chopping Station.</summary>
    public enum StepKind { Wash, Chop, Marinate, StirFry, Grill, Boil, Steam, Portion }

    public enum NpcArchetype { Malnourished, WeightControl, Child, HeavyLaborer, UnbalancedDiet }

    /// <summary>One ingredient of a dish and the exact steps it must go through, in order.</summary>
    [Serializable]
    public class RecipeComponent
    {
        public IngredientData ingredient;
        public StepKind[] steps;
        [Tooltip("Target grams on the Portion Scale (only used when steps contain Portion)")]
        public float portionGrams = 150f;
    }

    [CreateAssetMenu(fileName = "NewRecipe", menuName = "Nutrition Kitchen/Recipe")]
    public class RecipeSO : ScriptableObject
    {
        [Header("Dish")]
        public string displayNameThai;
        public string displayNameEng;

        [Header("Customer")]
        public NpcArchetype customer;
        [TextArea(2, 3)] public string customerNeed;
        public GameObject[] customerPrefabs;
        public float patienceSeconds = 300f;

        [Header("Components (each plated separately)")]
        public RecipeComponent[] components;
    }
}
