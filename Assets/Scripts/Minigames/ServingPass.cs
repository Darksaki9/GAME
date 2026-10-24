using UnityEngine;
using UnityEngine.InputSystem;

namespace NutritionKitchen.Minigames
{
    /// <summary>
    /// The pass under the heat lamps: one plate per active order. Put finished components on with E;
    /// OrderManager decides which order they belong to and serves the plate when it's complete.
    /// </summary>
    public class ServingPass : KitchenInteractable
    {
        public const int SlotCount = 4;
        private const float SlotSpacing = 0.8f;
        private readonly Transform[] slots = new Transform[SlotCount];   // unscaled roots: plate + food
        private readonly Transform[] food = new Transform[SlotCount];

        private void Awake()
        {
            for (int i = 0; i < SlotCount; i++)
            {
                slots[i] = new GameObject("Slot_" + (i + 1)).transform;
                slots[i].SetParent(transform, false);
                slots[i].localPosition = new Vector3((i - (SlotCount - 1) / 2f) * SlotSpacing, 0.01f, 0f);
                PropFactory.Prim(PrimitiveType.Cylinder, "Plate", slots[i], Vector3.zero, new Vector3(0.3f, 0.008f, 0.3f), Color.white);
                food[i] = new GameObject("Food").transform;
                food[i].SetParent(slots[i], false);
                slots[i].gameObject.SetActive(false);
            }
            AddTrigger(new Vector3(0f, 0.2f, 0f), new Vector3(SlotCount * SlotSpacing + 0.2f, 0.5f, 0.7f));
        }

        public void ShowPlate(int slot, bool on)
        {
            slots[slot].gameObject.SetActive(on);
            if (!on)
                foreach (Transform c in food[slot]) Destroy(c.gameObject);
        }

        /// <summary>Puts an item on a slot's plate, arranged around the rim by component index.</summary>
        public void PlaceOnPlate(int slot, FoodItem item, int componentIndex, int componentCount)
        {
            float a = componentIndex * Mathf.PI * 2f / componentCount;
            item.transform.SetParent(food[slot], false);
            item.transform.localPosition = new Vector3(Mathf.Cos(a) * 0.08f, 0.05f, Mathf.Sin(a) * 0.08f);
            item.transform.localRotation = Quaternion.identity;
            item.transform.localScale = Vector3.one * 0.7f;
        }

        public override string Prompt(PlayerHands hands) =>
            hands.Held != null ? $"[E]  Plate {hands.Held.Ingredient.displayNameEng}" : "Pass: bring finished food here";

        public override void HandleInput(PlayerHands hands, Keyboard kb)
        {
            if (hands.Held != null && kb.eKey.wasPressedThisFrame) OrderManager.Instance?.TryPlate(hands);
        }
    }
}
