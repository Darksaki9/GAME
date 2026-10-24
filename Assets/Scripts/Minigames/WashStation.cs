using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using NutritionKitchen.Crafting;

namespace NutritionKitchen.Minigames
{
    /// <summary>
    /// Washing & peeling: hold the left mouse button and scrub (move the mouse back and forth) under the running tap.
    /// Under-washing leaves soil/bacteria; scrubbing past clean peels/rinses away nutrients (fibre, B vitamins).
    /// </summary>
    public class WashStation : MinigameStation
    {
        [SerializeField] private float scrubPerPixel = 0.00015f;   // cleanliness gained per pixel of mouse travel
        [SerializeField] private float safeClean = 0.85f;         // below this the item still carries soil/bacteria
        [SerializeField] private float peelPerPixel = 0.00012f;   // overwash once fully clean

        private float clean, overwash;
        private GameObject washModel, water;
        private Vector3 washModelScale;
        private readonly List<GameObject> dirt = new List<GameObject>();

        public override string StationName => "Wash Station";
        public override StepKind[] Kinds { get; } = { StepKind.Wash };
        protected override string Controls => "Hold LMB + move mouse   scrub\nSpace   done";

        protected override bool Accepts(IngredientData i) =>
            i.category == NutrientCategory.Vegetable || i.ingredientId == "ingredient_rice" || i.ingredientId == "ingredient_fish";

        private string OverwashLoss => ingredient.ingredientId switch
        {
            "ingredient_rice" => "Over-rinsed rice: water-soluble B vitamins washed away.",
            "ingredient_fish" => "Over-scrubbed fish: skin torn, flesh bruised.",
            _ => "Over-peeled: nutrient-rich skin and fibre lost."
        };

        protected override void BuildProps()
        {
            Color steel = new Color(0.7f, 0.72f, 0.75f);
            PropFactory.Prim(PrimitiveType.Cylinder, "Faucet", transform, new Vector3(0f, 0.3f, 0.22f), new Vector3(0.03f, 0.15f, 0.03f), steel);
            PropFactory.Prim(PrimitiveType.Cylinder, "FaucetSpout", transform, new Vector3(0f, 0.44f, 0.15f), new Vector3(0.025f, 0.08f, 0.025f), steel, new Vector3(90f, 0f, 0f));
            water = PropFactory.Prim(PrimitiveType.Cylinder, "Water", transform, new Vector3(0f, 0.22f, 0.08f), new Vector3(0.018f, 0.2f, 0.018f), new Color(0.55f, 0.75f, 1f));
            water.SetActive(false);
            PropFactory.Prim(PrimitiveType.Cube, "Colander", transform, new Vector3(0f, 0.03f, 0.05f), new Vector3(0.3f, 0.05f, 0.25f), new Color(0.85f, 0.85f, 0.88f));
        }

        protected override void ClearFood()
        {
            if (washModel) Destroy(washModel);
            dirt.Clear();
        }

        protected override void OnBegin()
        {
            clean = 0f;
            overwash = 0f;
            if (washModel) Destroy(washModel);
            dirt.Clear();
            washModel = PropFactory.IngredientModel(ingredient, transform, new Vector3(0f, 0.13f, 0.05f), 0.18f);
            washModelScale = washModel.transform.localScale;

            // Soil specks scattered over the item; they vanish as it gets cleaner.
            Random.State old = Random.state;
            Random.InitState(ingredient.ingredientId.GetHashCode());
            for (int i = 0; i < 14; i++)
            {
                Vector3 p = new Vector3(0f, 0.13f, 0.05f) + Random.onUnitSphere * 0.08f;
                dirt.Add(PropFactory.Prim(PrimitiveType.Sphere, "Dirt", transform, p, Vector3.one * Random.Range(0.02f, 0.035f), new Color(0.35f, 0.25f, 0.15f)));
            }
            Random.state = old;
            foreach (GameObject d in dirt) d.transform.SetParent(washModel.transform, true);
        }

        protected override void Tick(float dt, Keyboard kb, Mouse mouse)
        {
            bool scrubbing = mouse.leftButton.isPressed;
            water.SetActive(scrubbing);
            if (scrubbing)
            {
                float travel = mouse.delta.ReadValue().magnitude;
                float gain = travel * scrubPerPixel;
                if (clean < 1f) clean = Mathf.Min(1f, clean + gain);
                else overwash = Mathf.Min(1f, overwash + travel * peelPerPixel);
                washModel.transform.localRotation *= Quaternion.Euler(0f, travel * 0.3f, 0f); // turning it under the tap
            }

            int show = Mathf.CeilToInt(dirt.Count * (1f - clean));
            for (int i = 0; i < dirt.Count; i++) dirt[i].SetActive(i < show);
            washModel.transform.localScale = washModelScale * (1f - overwash * 0.3f); // visibly shrinks when over-peeled

            if (kb.spaceKey.wasPressedThisFrame) Finish();
        }

        private void Finish()
        {
            var r = new MinigameResult();
            // Fibre/vitamins lost by overwashing scale with how much the item has to lose.
            r.nutrientRetention = 1f - overwash * 0.5f;
            r.wastePct = overwash * 0.25f;
            r.accuracy = Mathf.InverseLerp(0.5f, 1f, clean) * (1f - overwash);
            if (clean < safeClean)
            {
                r.contamination = Contamination.SoilBacteria;
                r.notes.Add($"Only {clean:P0} clean: soil and bacteria remain. Wash to at least {safeClean:P0}.");
            }
            if (overwash > 0.15f) r.notes.Add(OverwashLoss);
            if (r.contamination == Contamination.None && overwash <= 0.15f) r.notes.Add("Clean, skin kept intact.");
            Submit(r);
        }

        protected override void OnStopped()
        {
            if (water) water.SetActive(false);
        }

        protected override void DrawHud()
        {
            MinigameUI.Meter(0, "Overwash / peel loss", overwash, MinigameUI.Bad);
            MinigameUI.Meter(1, "Cleanliness", clean, clean < safeClean ? MinigameUI.Warn : MinigameUI.Good, safeClean, 1f);
            if (clean >= 1f) MinigameUI.Status("Clean! Stop scrubbing (Space)", MinigameUI.Good, 2);
        }
    }
}
