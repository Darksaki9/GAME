using UnityEngine;
using NutritionKitchen.Crafting;

namespace NutritionKitchen.Minigames
{
    /// <summary>
    /// Wall tablet in the walk-in freezer for ordering ingredients: aim at &lt; / &gt; to browse, ORDER to buy one
    /// (spends the Pantry budget; a drone drops the box at the "Drop ingredient" pad).
    /// Built facing local -Z, like the stations.
    /// </summary>
    public class FreezerScreen : MonoBehaviour
    {
        private const float W = 1.6f, H = 0.95f;          // screen size in metres
        private static readonly Color Bezel = new Color(0.10f, 0.11f, 0.13f);
        private static readonly Color Glass = new Color(0.04f, 0.07f, 0.11f);
        private static readonly Color Accent = new Color(0.13f, 0.62f, 0.60f);   // the restaurant's teal
        private static readonly Color AccentHover = new Color(0.30f, 0.85f, 0.80f);
        private static readonly Color Muted = new Color(0.62f, 0.70f, 0.76f);

        private int index;
        private Transform modelHolder;
        private TextMesh nameText, infoText, budgetText;
        private ScreenButton orderButton;

        private void Start() // after the Pantry's Awake, so its stock list exists
        {
            // Body: bezel, dark glass and a thin glowing accent strip along the top.
            PropFactory.Prim(PrimitiveType.Cube, "Bezel", transform, Vector3.zero, new Vector3(W + 0.08f, H + 0.08f, 0.04f), Bezel);
            PropFactory.Prim(PrimitiveType.Cube, "Glass", transform, new Vector3(0f, 0f, -0.021f), new Vector3(W, H, 0.004f), Glass);
            PropFactory.Prim(PrimitiveType.Cube, "TopStrip", transform, new Vector3(0f, H / 2 - 0.012f, -0.024f), new Vector3(W, 0.012f, 0.002f), Accent);

            PropFactory.Text("ORDER INGREDIENTS", transform, new Vector3(-W / 2 + 0.06f, H / 2 - 0.07f, -0.03f), 0.045f, Muted, TextAnchor.MiddleLeft);
            budgetText = PropFactory.Text("", transform, new Vector3(W / 2 - 0.06f, H / 2 - 0.07f, -0.03f), 0.045f, Accent, TextAnchor.MiddleRight);
            nameText = PropFactory.Text("", transform, new Vector3(0f, 0.3f, -0.03f), 0.07f, Color.white);
            infoText = PropFactory.Text("", transform, new Vector3(0f, -0.18f, -0.03f), 0.05f, Muted);

            modelHolder = new GameObject("Model").transform;
            modelHolder.SetParent(transform, false);
            modelHolder.localPosition = new Vector3(0f, 0.05f, -0.13f); // in front of the glass so it doesn't clip into the wall

            Button("Prev", new Vector3(-W / 2 + 0.14f, 0.03f, -0.03f), true, "<", () => Browse(-1), "Previous ingredient");
            Button("Next", new Vector3(W / 2 - 0.14f, 0.03f, -0.03f), true, ">", () => Browse(1), "Next ingredient");
            orderButton = Button("Order", new Vector3(0f, -0.35f, -0.03f), false, "ORDER", OrderCurrent, null);

            Show();
        }

        /// <summary>round = circular arrow button, otherwise the wide ORDER pill.</summary>
        private ScreenButton Button(string name, Vector3 pos, bool round, string label, System.Action onPress, string hint)
        {
            GameObject b = round
                ? PropFactory.Prim(PrimitiveType.Cylinder, name, transform, pos, new Vector3(0.16f, 0.01f, 0.16f), Accent, new Vector3(90f, 0f, 0f))
                : PropFactory.Prim(PrimitiveType.Cube, name, transform, pos, new Vector3(0.5f, 0.12f, 0.02f), Accent);
            b.AddComponent<BoxCollider>();
            var sb = b.AddComponent<ScreenButton>();
            sb.normal = Accent;
            sb.hover = AccentHover;
            sb.onPress = onPress;
            sb.hint = hint;
            // Label on the screen root (not the button) so the button's scale doesn't squash it.
            PropFactory.Text(label, transform, pos + new Vector3(0f, round ? 0.012f : 0f, -0.015f), round ? 0.09f : 0.06f, Color.white);
            return sb;
        }

        private void Browse(int step)
        {
            Pantry p = Pantry.Instance;
            if (p == null || p.stock.Count == 0) return;
            index = (index + step + p.stock.Count) % p.stock.Count;
            Show();
        }

        private void OrderCurrent()
        {
            Pantry p = Pantry.Instance;
            if (p == null || p.stock.Count == 0) return;
            IngredientData i = p.stock[index];
            MinigameUI.ShowToast(p.Order(i)
                ? $"Ordered 1 {i.displayNameEng} for {i.costPerUnit} baht: a drone is bringing it to the drop pad."
                : $"Not enough budget for {i.displayNameEng} ({i.costPerUnit} baht).", 3f);
        }

        /// <summary>Rebuilds the model for the current page.</summary>
        private void Show()
        {
            foreach (Transform c in modelHolder) Destroy(c.gameObject);
            Pantry p = Pantry.Instance;
            if (p == null || p.stock.Count == 0) { nameText.text = "No pantry"; return; }
            PropFactory.IngredientModel(p.stock[index], modelHolder, Vector3.zero, 0.2f);
        }

        private void Update()
        {
            if (modelHolder != null) modelHolder.Rotate(0f, 40f * Time.deltaTime, 0f, Space.Self);

            // Text refreshes every frame: stock and budget change from elsewhere (pantry, deliveries).
            Pantry p = Pantry.Instance;
            if (p == null || p.stock.Count == 0 || nameText == null) return;
            IngredientData i = p.stock[index];
            nameText.text = i.displayNameEng;
            int coming = p.OnTheWay(i);
            infoText.text = $"{i.costPerUnit} baht      In stock: {p.Count(i)}" + (coming > 0 ? $"      On the way: {coming}" : "");
            budgetText.text = $"Budget {p.Money} baht";
            orderButton.hint = $"Order 1 {i.displayNameEng} for {i.costPerUnit} baht";
        }
    }
}
