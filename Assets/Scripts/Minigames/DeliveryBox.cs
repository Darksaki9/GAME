using UnityEngine;
using UnityEngine.InputSystem;
using NutritionKitchen.Crafting;

namespace NutritionKitchen.Minigames
{
    /// <summary>A cardboard box of one ingredient, dropped by a delivery drone. E with empty hands takes the ingredient.</summary>
    public class DeliveryBox : KitchenInteractable
    {
        public IngredientData Ingredient { get; private set; }

        private static readonly Color Cardboard = new Color(0.62f, 0.45f, 0.28f);
        private const float Size = 0.4f;

        public static DeliveryBox Create(IngredientData ingredient, Transform parent)
        {
            var go = new GameObject("DeliveryBox_" + ingredient.ingredientId);
            go.transform.SetParent(parent, false);
            var box = go.AddComponent<DeliveryBox>();
            box.Ingredient = ingredient;

            PropFactory.Prim(PrimitiveType.Cube, "Cardboard", go.transform, Vector3.zero, Vector3.one * Size, Cardboard);
            PropFactory.Prim(PrimitiveType.Cube, "Tape", go.transform, new Vector3(0f, Size / 2, 0f), new Vector3(0.08f, 0.004f, Size + 0.002f), new Color(0.85f, 0.78f, 0.6f));
            // Name label on all four sides so it reads from any angle.
            for (int side = 0; side < 4; side++)
            {
                var holder = new GameObject("Label").transform;
                holder.SetParent(go.transform, false);
                holder.localRotation = Quaternion.Euler(0f, side * 90f, 0f);
                PropFactory.Text(ingredient.displayNameEng, holder, new Vector3(0f, 0.05f, -Size / 2 - 0.003f), 0.045f, new Color(0.15f, 0.1f, 0.06f));
            }
            return box;
        }

        /// <summary>Called when the drone lets go: from here on it's a physics object on the drop pad.</summary>
        public void Release()
        {
            transform.SetParent(null, true);
            gameObject.AddComponent<BoxCollider>().size = Vector3.one * Size;
            gameObject.AddComponent<Rigidbody>().mass = 3f;
        }

        public override string Prompt(PlayerHands hands) =>
            hands.Held == null ? $"[E]  Take {Ingredient.displayNameEng} from the box" : $"Box: {Ingredient.displayNameEng}\nHands full";

        public override void HandleInput(PlayerHands hands, Keyboard kb)
        {
            if (hands.Held != null || !kb.eKey.wasPressedThisFrame) return;
            hands.Hold(FoodItem.Create(Ingredient));
            Destroy(gameObject);
        }
    }
}
