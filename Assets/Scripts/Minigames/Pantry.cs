using System.Collections.Generic;
using UnityEngine;
using NutritionKitchen.Crafting;

namespace NutritionKitchen.Minigames
{
    /// <summary>
    /// Pantry stock and the shift budget. Each carried ingredient has one PantryBox on the dry-storage shelves
    /// (aim + E to take, E with an unused raw one to put it back). More stock is ordered at the freezer screen and a drone
    /// drops it at the "Drop ingredient" pad. Oil and seasoning are used at the stations, not carried.
    /// </summary>
    public class Pantry : MonoBehaviour
    {
        public static Pantry Instance { get; private set; }

        [SerializeField] private int startingStock = 2;
        [SerializeField] private int startingBudget = 500;   // baht

        public readonly List<IngredientData> stock = new List<IngredientData>();
        public int Money { get; private set; }

        private readonly Dictionary<IngredientData, int> counts = new Dictionary<IngredientData, int>();

        // Box spots, filled in order: (shelf object, shelf top height above the shelf's base, slot across the shelf).
        private static readonly (string shelf, float top, float across)[] Slots =
        {
            ("Dry_Storage_Shelf_1", 1.53f, -0.6f), ("Dry_Storage_Shelf_1", 1.53f, 0.6f),
            ("Dry_Storage_Shelf_1", 1.08f, -0.6f), ("Dry_Storage_Shelf_1", 1.08f, 0.6f),
            ("Dry_Storage_Shelf_2", 1.53f, -0.6f), ("Dry_Storage_Shelf_2", 1.53f, 0.6f),
            ("Dry_Storage_Shelf_2", 1.08f, -0.6f), ("Dry_Storage_Shelf_2", 1.08f, 0.6f),
        };

        private void Awake()
        {
            Instance = this;
            Money = startingBudget;
            IngredientLibrary lib = IngredientLibrary.Load();
            if (lib != null)
                foreach (IngredientData i in lib.ingredients)
                    if (i != null && i.category != NutrientCategory.CookingFat && i.category != NutrientCategory.Seasoning)
                    {
                        stock.Add(i);
                        counts[i] = startingStock;
                    }
        }

        private void Start() // after every Awake, so the scene and the rest of the kitchen are set up
        {
            for (int i = 0; i < stock.Count; i++)
            {
                var box = new GameObject("PantryBox_" + stock[i].ingredientId).AddComponent<PantryBox>();
                box.transform.SetParent(transform, true);
                PlaceBox(box.transform, i);
                box.Init(this, stock[i]);
            }
        }

        private void PlaceBox(Transform box, int i)
        {
            GameObject shelf = i < Slots.Length ? GameObject.Find(Slots[i].shelf) : null;
            Renderer r = shelf != null ? shelf.GetComponentInChildren<Renderer>() : null;
            if (r == null)
            {
                // No shelves in this scene: a row in front of the pantry spot.
                box.SetPositionAndRotation(transform.position + transform.right * ((i - (stock.Count - 1) / 2f) * 0.6f), transform.rotation);
                return;
            }
            Vector3 fwd = Vector3.ProjectOnPlane(shelf.transform.forward, Vector3.up).normalized; // toward the back wall
            Quaternion rot = Quaternion.LookRotation(fwd);
            Bounds b = r.bounds;
            Vector3 pos = b.center + rot * Vector3.right * Slots[i].across - fwd * 0.05f;
            pos.y = b.min.y + Slots[i].top;
            box.SetPositionAndRotation(pos, rot);
        }

        public int Count(IngredientData i) => counts.TryGetValue(i, out int n) ? n : 0;

        public int OnTheWay(IngredientData i) => DroneDelivery.Instance != null ? DroneDelivery.Instance.InFlight(i) : 0;

        /// <summary>Pays for one unit and sends a drone with it. False if the budget can't cover it.</summary>
        public bool Order(IngredientData i)
        {
            if (Money < i.costPerUnit) return false;
            Money -= i.costPerUnit;
            if (DroneDelivery.Instance != null) DroneDelivery.Instance.Dispatch(i);
            else counts[i] = Count(i) + 1; // no drop pad in this scene: straight onto the shelf
            return true;
        }

        public void TakeOne(IngredientData i, PlayerHands hands)
        {
            if (Count(i) <= 0)
            {
                MinigameUI.ShowToast($"Out of {i.displayNameEng}: order more at the freezer screen.", 3f);
                return;
            }
            counts[i]--;
            hands.Hold(FoodItem.Create(i));
        }

        /// <summary>Only an untouched, clean raw ingredient goes back, and only into its own box.</summary>
        public bool CanPutBack(FoodItem f, IngredientData box) =>
            f.Ingredient == box && f.steps.Count == 0 && f.Contamination == Contamination.None;

        public void PutBack(PlayerHands hands)
        {
            FoodItem f = hands.Take();
            counts[f.Ingredient] = Count(f.Ingredient) + 1;
            Destroy(f.gameObject);
        }
    }
}
