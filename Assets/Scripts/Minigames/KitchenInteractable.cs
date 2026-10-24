using UnityEngine;
using UnityEngine.InputSystem;

namespace NutritionKitchen.Minigames
{
    /// <summary>Something the chef uses by looking at it, without a close-up minigame (pantry, bin, pass).</summary>
    public abstract class KitchenInteractable : MonoBehaviour
    {
        /// <summary>Text shown while the player looks at it.</summary>
        public abstract string Prompt(PlayerHands hands);
        /// <summary>Called every frame the player looks at it.</summary>
        public abstract void HandleInput(PlayerHands hands, Keyboard kb);

        protected void AddTrigger(Vector3 center, Vector3 size)
        {
            BoxCollider box = gameObject.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.center = center;
            box.size = size;
        }
    }
}
