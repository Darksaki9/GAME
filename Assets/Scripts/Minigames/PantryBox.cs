using UnityEngine;
using UnityEngine.InputSystem;
using NutritionKitchen.Crafting;

namespace NutritionKitchen.Minigames
{
    /// <summary>
    /// One ingredient's box on the pantry shelf: icon, name and stock on the front. Aim at it and press E to take one,
    /// or E while holding an unused raw one of the same ingredient to put it back. Built facing local -Z.
    /// </summary>
    public class PantryBox : KitchenInteractable
    {
        public const float W = 0.5f, H = 0.34f, D = 0.4f;

        private static readonly Color Cardboard = new Color(0.62f, 0.45f, 0.28f);
        private static readonly Color Empty = new Color(0.36f, 0.3f, 0.24f);

        private Pantry pantry;
        private IngredientData ingredient;
        private GameObject body;
        private TextMesh label;
        private int shownCount = -1;

        public void Init(Pantry owner, IngredientData ing)
        {
            pantry = owner;
            ingredient = ing;
            body = PropFactory.Prim(PrimitiveType.Cube, "Cardboard", transform, new Vector3(0f, H / 2, 0f), new Vector3(W, H, D), Cardboard);
            PropFactory.Prim(PrimitiveType.Cube, "Tape", transform, new Vector3(0f, H + 0.002f, 0f), new Vector3(0.08f, 0.004f, D + 0.002f), new Color(0.85f, 0.78f, 0.6f));

            // Front label card: icon on top, name + count underneath.
            PropFactory.Prim(PrimitiveType.Cube, "LabelCard", transform, new Vector3(0f, H / 2, -D / 2 - 0.002f), new Vector3(0.3f, 0.28f, 0.004f), new Color(0.97f, 0.96f, 0.92f));
            GameObject icon = GameObject.CreatePrimitive(PrimitiveType.Quad);
            icon.name = "Icon";
            Destroy(icon.GetComponent<Collider>());
            icon.transform.SetParent(transform, false);
            icon.transform.localPosition = new Vector3(0f, H / 2 + 0.035f, -D / 2 - 0.005f);
            icon.transform.localScale = Vector3.one * 0.19f;
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard")); // the Unlit one drew black here
            mat.mainTexture = IngredientIcons.Get(ing);
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", mat.mainTexture);
            icon.GetComponent<Renderer>().sharedMaterial = mat;
            label = PropFactory.Text("", transform, new Vector3(0f, H / 2 - 0.09f, -D / 2 - 0.006f), 0.022f, new Color(0.15f, 0.1f, 0.06f));

            AddTrigger(new Vector3(0f, H / 2, 0f), new Vector3(W, H, D));
        }

        private void Update()
        {
            int n = pantry.Count(ingredient);
            if (n == shownCount) return;
            shownCount = n;
            label.text = $"{ingredient.displayNameEng}\n{(n > 0 ? "x" + n : "OUT OF STOCK")}";
            PropFactory.SetColor(body, n > 0 ? Cardboard : Empty);
        }

        public override string Prompt(PlayerHands hands)
        {
            int n = pantry.Count(ingredient);
            if (hands.Held != null)
                return pantry.CanPutBack(hands.Held, ingredient) ? $"[E]  Put {ingredient.displayNameEng} back" : $"{ingredient.displayNameEng} x{n}\nHands full";
            return n > 0 ? $"[E]  Take {ingredient.displayNameEng}  ({n} left)" : $"{ingredient.displayNameEng}: out of stock\nOrder more at the freezer screen";
        }

        public override void HandleInput(PlayerHands hands, Keyboard kb)
        {
            if (!kb.eKey.wasPressedThisFrame) return;
            if (hands.Held == null) pantry.TakeOne(ingredient, hands);
            else if (pantry.CanPutBack(hands.Held, ingredient)) pantry.PutBack(hands);
        }
    }
}
