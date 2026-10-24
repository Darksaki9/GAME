using UnityEngine;
using UnityEngine.InputSystem;

namespace NutritionKitchen.Minigames
{
    /// <summary>
    /// A thrown food item lying in the world: look at it and press E to pick it up.
    /// Touching the floor marks it contaminated (counters and plates are fine).
    /// </summary>
    [RequireComponent(typeof(FoodItem))]
    public class DroppedFood : KitchenInteractable
    {
        private const float FloorTolerance = 0.15f;

        private FoodItem item;
        private float floorY;

        public void Init(float floorHeight)
        {
            item = GetComponent<FoodItem>();
            floorY = floorHeight;
        }

        private void OnCollisionEnter(Collision c)
        {
            if (item.droppedOnFloor) return;
            foreach (ContactPoint p in c.contacts)
                if (p.point.y < floorY + FloorTolerance)
                {
                    item.droppedOnFloor = true;
                    MinigameUI.ShowToast($"{item.Ingredient.displayNameEng} hit the floor: it's contaminated now.", 3f);
                    return;
                }
        }

        public override string Prompt(PlayerHands hands) =>
            hands.Held == null ? $"[E]  Pick up {item.Describe()}" : $"{item.Describe()}\nHands full";

        public override void HandleInput(PlayerHands hands, Keyboard kb)
        {
            if (hands.Held != null || !kb.eKey.wasPressedThisFrame) return;
            Destroy(GetComponent<Rigidbody>());
            Destroy(GetComponent<Collider>());
            Destroy(this);
            hands.Hold(item);
        }
    }
}
