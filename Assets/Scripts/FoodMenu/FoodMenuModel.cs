using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CookingSystem
{
    /// <summary>
    /// Food ingredients as defined in the recipe reference.
    /// </summary>
    public enum IngredientType
    {
        Rice,           // ข้าว
        BrownRice,      // ข้าวกล้อง
        Chicken,        // ไก่
        Egg,            // ไข่
        Carrot,         // แครอต
        Fish,           // ปลา
        Broccoli,       // บรอกโคลี
        Vegetables,     // ผัก
        Tofu,           // เต้าหู้
        ChineseCabbage  // ผักกาดขาว
    }

    /// <summary>
    /// Recipe definition representing a menu item and its required ingredients.
    /// </summary>
    [Serializable]
    public class FoodRecipe
    {
        public string id;
        public string menuNameThai;
        public string menuNameEnglish;
        public List<IngredientType> requiredIngredients = new List<IngredientType>();
        public float cookingTime = 5.0f; // seconds
        [TextArea(2, 4)]
        public string description;

        public FoodRecipe(string id, string thai, string english, IngredientType[] ingredients, string desc = "")
        {
            this.id = id;
            this.menuNameThai = thai;
            this.menuNameEnglish = english;
            this.requiredIngredients = new List<IngredientType>(ingredients);
            this.description = desc;
        }

        /// <summary>
        /// Check if the provided list of ingredients matches this recipe.
        /// </summary>
        public bool Matches(IEnumerable<IngredientType> ingredients)
        {
            if (ingredients == null) return false;
            var inputList = ingredients.ToList();
            if (inputList.Count != requiredIngredients.Count) return false;

            var reqSorted = requiredIngredients.OrderBy(x => x).ToList();
            var inputSorted = inputList.OrderBy(x => x).ToList();

            return reqSorted.SequenceEqual(inputSorted);
        }
    }

    /// <summary>
    /// Database holding all food menu recipes according to the reference specifications.
    /// </summary>
    public static class FoodMenuDatabase
    {
        private static readonly List<FoodRecipe> _recipes = new List<FoodRecipe>
        {
            new FoodRecipe(
                "RECIPE_01",
                "ข้าวต้มไก่ใส่ไข่",
                "Chicken Rice Porridge with Egg",
                new [] { IngredientType.Rice, IngredientType.Chicken, IngredientType.Egg, IngredientType.Carrot },
                "Warm soothing rice porridge cooked with tender chicken, egg, and diced carrots."
            ),
            new FoodRecipe(
                "RECIPE_02",
                "ปลานึ่งกับข้าวกล้อง",
                "Steamed Fish with Brown Rice",
                new [] { IngredientType.Fish, IngredientType.BrownRice, IngredientType.Broccoli, IngredientType.Carrot },
                "Nutritious steamed fish fillet served alongside healthy brown rice, broccoli, and carrots."
            ),
            new FoodRecipe(
                "RECIPE_03",
                "ข้าวผัดไก่ใส่ไข่",
                "Chicken Fried Rice with Egg",
                new [] { IngredientType.Rice, IngredientType.Chicken, IngredientType.Egg, IngredientType.Vegetables },
                "Savory stir-fried jasmine rice with sliced chicken, golden egg, and mixed fresh vegetables."
            ),
            new FoodRecipe(
                "RECIPE_04",
                "ข้าวไก่ย่างกับไข่ต้ม",
                "Grilled Chicken Rice with Boiled Egg",
                new [] { IngredientType.Rice, IngredientType.Chicken, IngredientType.Egg, IngredientType.Vegetables },
                "Aromatic grilled marinated chicken over steamed rice with a boiled egg and garden greens."
            ),
            new FoodRecipe(
                "RECIPE_05",
                "ข้าวไก่ย่างกับแกงจืดเต้าหู้",
                "Grilled Chicken Rice with Tofu Soup",
                new [] { IngredientType.Rice, IngredientType.Chicken, IngredientType.Tofu, IngredientType.ChineseCabbage },
                "Grilled chicken served with steamed rice and a light clear soup with silken tofu and Chinese cabbage."
            )
        };

        public static IReadOnlyList<FoodRecipe> AllRecipes => _recipes;

        /// <summary>
        /// Finds matching recipe for a set of ingredients.
        /// </summary>
        public static FoodRecipe FindMatchingRecipe(IEnumerable<IngredientType> ingredients)
        {
            return _recipes.FirstOrDefault(r => r.Matches(ingredients));
        }

        /// <summary>
        /// Find recipe by Thai or English name.
        /// </summary>
        public static FoodRecipe GetByName(string name)
        {
            return _recipes.FirstOrDefault(r => 
                string.Equals(r.menuNameThai, name, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(r.menuNameEnglish, name, StringComparison.OrdinalIgnoreCase)
            );
        }
    }

    /// <summary>
    /// Component placed on the 3D FoodMenuStand model.
    /// </summary>
    [SelectionBase]
    public class FoodMenuStandInteractable : MonoBehaviour
    {
        [Header("Menu Information")]
        public string menuTitle = "ระบบเมนูอาหารและวิธีทำ";

        [SerializeField]
        private List<FoodRecipe> availableRecipes = new List<FoodRecipe>();

        private void Awake()
        {
            availableRecipes = FoodMenuDatabase.AllRecipes.ToList();
        }

        public void DisplayMenuInConsole()
        {
            Debug.Log($"=== {menuTitle} ===");
            foreach (var r in availableRecipes)
            {
                string ings = string.Join(", ", r.requiredIngredients);
                Debug.Log($"[เมนู] {r.menuNameThai} ({r.menuNameEnglish}) -> วัตถุดิบ: {ings}");
            }
        }
    }
}
