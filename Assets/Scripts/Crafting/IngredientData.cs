using UnityEngine;

namespace NutritionKitchen.Crafting
{
    public enum NutrientCategory
    {
        Carbohydrate,
        Protein,
        Vegetable,
        CookingFat,
        Seasoning,
        Broth
    }

    public enum CookingRole
    {
        StapleBase,       // ข้าว / แป้ง
        MainProtein,      // ไก่, ปลา, ไข่, เต้าหู้
        VegetableAddin,   // ผักรวม
        CookingFat,       // ผัดและทอด
        Seasoning,        // เพิ่มรสชาติและปรับโซเดียม
        SoupStock         // ต้มและแกงจืด
    }

    [CreateAssetMenu(fileName = "NewIngredient", menuName = "Nutrition Kitchen/Ingredient Data")]
    public class IngredientData : ScriptableObject
    {
        [Header("Identity")]
        public string ingredientId;
        public string displayNameThai;
        public string displayNameEng;
        [TextArea(2, 4)]
        public string nutritionRoleThai;

        [Header("Classification")]
        public NutrientCategory category;
        public CookingRole cookingRole;

        [Header("Nutritional Values (per serving)")]
        [Tooltip("Energy in kilocalories (kcal)")]
        public float calories;
        [Tooltip("Carbohydrates in grams (g)")]
        public float carbohydrates;
        [Tooltip("Protein in grams (g)")]
        public float protein;
        [Tooltip("Fat in grams (g)")]
        public float fat;
        [Tooltip("Dietary fiber in grams (g)")]
        public float fiber;
        [Tooltip("Sodium in milligrams (mg)")]
        public float sodium;

        [Header("Purchasing")]
        [Tooltip("Price of one unit when ordered at the freezer screen (baht)")]
        public int costPerUnit = 20;

        [Header("Food Safety & Cooking")]
        [Tooltip("Raw meat/fish: belongs on the red board and contaminates the knife")]
        public bool isRawMeat;
        [Tooltip("Safe internal (core) temperature in °C, 0 = no requirement")]
        public float safeCoreTempC;
        [Tooltip("0..1, how fast heat-sensitive nutrients (e.g. vitamin C) break down when overcooked")]
        [Range(0f, 1f)] public float heatSensitivity;

        [Header("Visual")]
        public GameObject prefab;
        public Color themeColor = Color.white;
    }
}
