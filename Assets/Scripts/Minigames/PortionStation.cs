using UnityEngine;
using UnityEngine.InputSystem;
using NutritionKitchen.Crafting;

namespace NutritionKitchen.Minigames
{
    /// <summary>
    /// Precision portioning: hold LMB to scoop onto the scale (the scoop speeds up the longer you hold),
    /// RMB takes a little back. Stop on the gram target for the waiting customer's energy needs.
    /// </summary>
    public class PortionStation : MinigameStation
    {
        [SerializeField] private float defaultTargetGrams = 150f;
        [SerializeField] private float scoopStart = 30f, scoopMax = 140f; // g/s

        private float grams, target, holdTime;
        private GameObject mound;
        private Color moundColor;

        public override string StationName => "Portion Scale";
        public override StepKind[] Kinds { get; } = { StepKind.Portion };
        protected override string Controls => "Hold LMB   scoop (speeds up)\nRMB   take 10 g back\nSpace   done";

        protected override bool Accepts(IngredientData i) => i.category == NutrientCategory.Carbohydrate;

        protected override void BuildProps()
        {
            PropFactory.Prim(PrimitiveType.Cube, "ScaleBase", transform, new Vector3(0f, 0.02f, 0.02f), new Vector3(0.26f, 0.04f, 0.26f), new Color(0.92f, 0.92f, 0.95f));
            PropFactory.Prim(PrimitiveType.Cube, "ScaleDisplay", transform, new Vector3(0f, 0.025f, -0.115f), new Vector3(0.12f, 0.03f, 0.01f), new Color(0.1f, 0.15f, 0.12f));
            PropFactory.Prim(PrimitiveType.Cylinder, "Plate", transform, new Vector3(0f, 0.045f, 0.02f), new Vector3(0.22f, 0.005f, 0.22f), Color.white);
        }

        protected override void ClearFood()
        {
            if (mound) Destroy(mound);
        }

        protected override void OnBegin()
        {
            grams = holdTime = 0f;
            target = OrderManager.Instance != null ? OrderManager.Instance.PortionTarget(ingredient) : 0f;
            if (target <= 0f) target = defaultTargetGrams;
            if (mound) Destroy(mound);
            moundColor = Color.Lerp(ingredient.themeColor, Color.white, 0.5f);
            mound = PropFactory.Prim(PrimitiveType.Sphere, "Portion", transform, new Vector3(0f, 0.05f, 0.02f), Vector3.zero, moundColor);
        }

        protected override void Tick(float dt, Keyboard kb, Mouse mouse)
        {
            if (mouse.leftButton.isPressed)
            {
                holdTime += dt;
                grams += Mathf.Lerp(scoopStart, scoopMax, holdTime / 2f) * dt;
            }
            else holdTime = 0f;
            if (mouse.rightButton.wasPressedThisFrame) grams = Mathf.Max(0f, grams - 10f);

            float size = 0.04f * Mathf.Pow(grams / 50f, 1f / 3f); // volume grows with grams
            mound.transform.localScale = new Vector3(size * 2f, size, size * 2f);
            mound.transform.localPosition = new Vector3(0f, 0.05f + size * 0.25f, 0.02f);

            if (kb.spaceKey.wasPressedThisFrame) Finish();
        }

        private void Finish()
        {
            var r = new MinigameResult();
            float error = (grams - target) / target; // + over, - under
            r.accuracy = Mathf.Abs(error) <= 0.05f ? 1f : Mathf.Clamp01(1f - (Mathf.Abs(error) - 0.05f) / 0.35f);
            if (error > 0.05f)
            {
                r.wastePct = Mathf.Clamp01(error * 0.5f); // the extra mostly comes back as leftovers
                r.notes.Add($"{grams:0} g, {grams - target:0} g over: extra calories and leftovers.");
            }
            else if (error < -0.05f) r.notes.Add($"{grams:0} g, {target - grams:0} g short: not enough energy for this customer.");
            else r.notes.Add($"{grams:0} g, right on the {target:0} g target!");
            Submit(r);
        }

        protected override void DrawHud()
        {
            float max = target * 2f;
            MinigameUI.Meter(0, "Scale", grams / max, Mathf.Abs(grams - target) <= target * 0.05f ? MinigameUI.Good : grams > target ? MinigameUI.Bad : MinigameUI.Info,
                target * 0.95f / max, target * 1.05f / max, $"{grams:0} g  (target {target:0} g)");
        }
    }
}
