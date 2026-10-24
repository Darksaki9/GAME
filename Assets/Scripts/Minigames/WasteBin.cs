using UnityEngine;
using UnityEngine.InputSystem;

namespace NutritionKitchen.Minigames
{
    /// <summary>Throw away what you're holding. Every item binned counts as food waste.</summary>
    public class WasteBin : KitchenInteractable
    {
        private void Awake() => AddTrigger(new Vector3(0f, -0.3f, 0f), new Vector3(0.7f, 1f, 0.7f));

        public override string Prompt(PlayerHands hands) =>
            hands.Held != null ? $"[E]  Throw away {hands.Held.Ingredient.displayNameEng}" : "Waste bin";

        public override void HandleInput(PlayerHands hands, Keyboard kb)
        {
            if (hands.Held == null || !kb.eKey.wasPressedThisFrame) return;
            FoodItem item = hands.Take();
            OrderManager.Instance?.RecordBinned(item);
            Destroy(item.gameObject);
        }
    }
}
