using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using NutritionKitchen.Crafting;

namespace NutritionKitchen.Minigames
{
    /// <summary>
    /// Precision chopping: the knife sweeps across the ingredient, click when it is over each guide line.
    /// Even slices cook evenly; uneven ones cook half-raw/half-burnt and leave scraps.
    /// Colour-coded boards: red = raw meat/fish, green = ready-to-eat veg/tofu (wrong board costs accuracy).
    /// Knife/board cross-contamination is switched off for now: they always count as clean.
    /// </summary>
    public class ChopStation : MinigameStation
    {
        [SerializeField] private int targetPieces = 8;
        [SerializeField] private float sweepSeconds = 4.5f;

        private enum Phase { Setup, Slicing, Marinate }
        private const float SodiumMin = 200f, SodiumMax = 450f; // mg: enough flavour, still low-sodium
        private const float BlockLength = 0.3f;

        private bool usingRed;

        private Phase phase;
        private float knifePos;
        private bool wrongBoard;
        private readonly List<float> cuts = new List<float>();
        private GameObject redBoard, greenBoard, knife, block;
        private float chopAnim;
        private float coverage, sodiumMg;
        private GameObject bowl;

        public override string StationName => "Chopping Station";
        public override StepKind[] Kinds { get; } = { StepKind.Chop, StepKind.Marinate };
        protected override string Controls => phase switch
        {
            Phase.Setup => "R / G   red or green board\nLMB   start slicing",
            Phase.Slicing => "LMB   cut when the knife is on a guide line",
            _ => "Hold LMB + move mouse   rub in the marinade\nMouse wheel / Up   add seasoning\nSpace   done"
        };

        protected override bool Accepts(IngredientData i) =>
            (i.category == NutrientCategory.Protein || i.category == NutrientCategory.Vegetable) && i.ingredientId != "ingredient_egg";

        protected override void BuildProps()
        {
            redBoard = PropFactory.Prim(PrimitiveType.Cube, "Board_Red", transform, Vector3.zero, new Vector3(0.42f, 0.02f, 0.28f), new Color(0.80f, 0.18f, 0.16f));
            greenBoard = PropFactory.Prim(PrimitiveType.Cube, "Board_Green", transform, Vector3.zero, new Vector3(0.42f, 0.02f, 0.28f), new Color(0.22f, 0.62f, 0.30f));
            // Boards are hidden on request; the red/green rule still runs and the HUD names the board in use.
            redBoard.GetComponent<Renderer>().enabled = false;
            greenBoard.GetComponent<Renderer>().enabled = false;

            knife = new GameObject("Knife");
            knife.transform.SetParent(transform, false);
            PropFactory.Prim(PrimitiveType.Cube, "Blade", knife.transform, new Vector3(0f, 0.05f, 0f), new Vector3(0.004f, 0.06f, 0.2f), new Color(0.85f, 0.87f, 0.9f));
            PropFactory.Prim(PrimitiveType.Cube, "Handle", knife.transform, new Vector3(0f, 0.06f, -0.15f), new Vector3(0.02f, 0.03f, 0.1f), new Color(0.15f, 0.12f, 0.1f));
            bowl = PropFactory.Prim(PrimitiveType.Sphere, "MarinadeBowl", transform, new Vector3(0f, 0.03f, 0.02f), new Vector3(0.3f, 0.08f, 0.3f), new Color(0.9f, 0.9f, 0.92f));
            bowl.SetActive(false);
            SetBoard(false);
        }

        private void SetBoard(bool red)
        {
            usingRed = red;
            // Board in use sits in front, the other one waits to the side.
            Vector3 work = new Vector3(0f, 0.01f, 0.02f), aside = new Vector3(0.33f, 0.01f, 0.12f);
            redBoard.transform.localPosition = red ? work : aside;
            greenBoard.transform.localPosition = red ? aside : work;
        }

        protected override void ClearFood()
        {
            if (block) Destroy(block);
        }

        protected override void OnBegin()
        {
            phase = Phase.Setup;
            cuts.Clear();
            if (block) Destroy(block);

            block = new GameObject("Block");
            block.transform.SetParent(transform, false);
            block.transform.localPosition = new Vector3(0f, 0.045f, 0.02f);
            PropFactory.Prim(PrimitiveType.Cube, "Food", block.transform, Vector3.zero, new Vector3(BlockLength, 0.05f, 0.09f), ingredient.themeColor);
            for (int i = 1; i < targetPieces; i++) // guide lines
                PropFactory.Prim(PrimitiveType.Cube, "Guide", block.transform, new Vector3(-BlockLength / 2 + BlockLength * i / targetPieces, 0.026f, 0f),
                    new Vector3(0.003f, 0.002f, 0.09f), Color.white);
            knife.transform.localPosition = new Vector3(-BlockLength / 2, 0.05f, 0.02f);
            SetBoard(ingredient.isRawMeat); // sensible default, the player can still pick wrong

            bool marinate = Mode == StepKind.Marinate;
            bowl.SetActive(marinate);
            knife.SetActive(!marinate);
            if (marinate)
            {
                phase = Phase.Marinate;
                coverage = sodiumMg = 0f;
                Destroy(block);
                block = new GameObject("Block");
                block.transform.SetParent(transform, false);
                PropFactory.IngredientModel(ingredient, block.transform, new Vector3(0f, 0.08f, 0.02f), 0.16f);
            }
        }

        protected override void Tick(float dt, Keyboard kb, Mouse mouse)
        {
            if (phase == Phase.Marinate)
            {
                if (mouse.leftButton.isPressed)
                {
                    float travel = mouse.delta.ReadValue().magnitude;
                    coverage = Mathf.Min(1f, coverage + travel * 0.00015f);
                    block.transform.localRotation *= Quaternion.Euler(0f, travel * 0.2f, 0f);
                }
                if (mouse.scroll.ReadValue().y > 0f || kb.upArrowKey.wasPressedThisFrame) sodiumMg += 50f;
                Color c = Color.Lerp(ingredient.themeColor, new Color(0.65f, 0.38f, 0.18f), coverage * 0.7f);
                if (Time.frameCount % 6 == 0) foreach (Renderer r in block.GetComponentsInChildren<Renderer>()) PropFactory.SetColor(r.gameObject, c);
                if (kb.spaceKey.wasPressedThisFrame) FinishMarinate();
                return;
            }
            if (phase == Phase.Setup)
            {
                if (kb.rKey.wasPressedThisFrame) SetBoard(true);
                if (kb.gKey.wasPressedThisFrame) SetBoard(false);
                if (mouse.leftButton.wasPressedThisFrame) StartSlicing();
                return;
            }

            knifePos += dt / sweepSeconds;
            if (mouse.leftButton.wasPressedThisFrame && knifePos < 1f && (cuts.Count == 0 || knifePos - cuts[cuts.Count - 1] > 0.02f))
            {
                cuts.Add(knifePos);
                chopAnim = 1f;
                PropFactory.Prim(PrimitiveType.Cube, "Cut", block.transform, new Vector3(-BlockLength / 2 + BlockLength * knifePos, 0f, 0f),
                    new Vector3(0.004f, 0.052f, 0.092f), ingredient.themeColor * 0.5f);
            }
            chopAnim = Mathf.MoveTowards(chopAnim, 0f, dt * 6f);
            knife.transform.localPosition = new Vector3(-BlockLength / 2 + BlockLength * Mathf.Clamp01(knifePos), 0.05f - chopAnim * 0.04f, 0.02f);

            if (knifePos >= 1f) Finish();
        }

        private void FinishMarinate()
        {
            var r = new MinigameResult();
            float salt = sodiumMg < SodiumMin ? sodiumMg / SodiumMin : sodiumMg <= SodiumMax ? 1f : Mathf.Clamp01(1f - (sodiumMg - SodiumMax) / SodiumMax);
            r.accuracy = Mathf.InverseLerp(0.3f, 1f, coverage) * salt;
            if (coverage < 0.7f) r.notes.Add("Patchy marinade: rub it in all over.");
            if (sodiumMg < SodiumMin) r.notes.Add($"Only {sodiumMg:0} mg sodium: bland.");
            else if (sodiumMg > SodiumMax) r.notes.Add($"{sodiumMg:0} mg sodium: too salty for a healthy meal (aim {SodiumMin:0}-{SodiumMax:0}).");
            else r.notes.Add($"{sodiumMg:0} mg sodium: tasty and still low-sodium.");
            Submit(r);
        }

        protected override void OnStopped()
        {
            if (knife) knife.SetActive(true);
            if (bowl) bowl.SetActive(false);
        }

        private void StartSlicing()
        {
            phase = Phase.Slicing;
            knifePos = 0f;
            wrongBoard = ingredient.isRawMeat != usingRed;
        }

        private void Finish()
        {
            var r = new MinigameResult();
            var edges = new List<float> { 0f };
            edges.AddRange(cuts);
            edges.Add(1f);

            float target = 1f / targetPieces, mean = 1f / (edges.Count - 1), variance = 0f, scraps = 0f;
            for (int i = 1; i < edges.Count; i++)
            {
                float w = edges[i] - edges[i - 1];
                variance += (w - mean) * (w - mean);
                if (w < target * 0.4f) scraps += w;
            }
            float cv = Mathf.Sqrt(variance / (edges.Count - 1)) / mean; // 0 = perfectly even
            int pieces = edges.Count - 1;

            r.accuracy = (1f - Mathf.Clamp01(cv * 1.5f)) * Mathf.Min(pieces, targetPieces) / Mathf.Max(pieces, targetPieces);
            r.wastePct = scraps;
            if (wrongBoard)
            {
                r.accuracy *= 0.8f;
                r.notes.Add(ingredient.isRawMeat ? "Raw meat on the GREEN board: use red." : "Ready-to-eat food on the RED board: use green.");
            }
            if (pieces != targetPieces) r.notes.Add($"{pieces} pieces instead of {targetPieces}.");
            if (cv > 0.35f) r.notes.Add("Uneven slices: some will be raw while others burn.");
            else if (cv < 0.12f && pieces == targetPieces) r.notes.Add("Perfectly even slices!");
            if (scraps > 0f) r.notes.Add($"{scraps:P0} cut into unusable scraps.");

            SpreadPieces(edges);
            Submit(r);
        }

        private void SpreadPieces(List<float> edges)
        {
            foreach (Transform c in block.transform) Destroy(c.gameObject);
            for (int i = 1; i < edges.Count; i++)
            {
                float a = edges[i - 1], b = edges[i];
                float x = -BlockLength / 2 + BlockLength * (a + b) / 2 + (i - edges.Count / 2f) * 0.012f;
                PropFactory.Prim(PrimitiveType.Cube, "Piece", block.transform, new Vector3(x, 0f, 0f),
                    new Vector3(BlockLength * (b - a), 0.05f, 0.09f), ingredient.themeColor);
            }
        }

        protected override void DrawHud()
        {
            if (phase == Phase.Marinate)
            {
                MinigameUI.Meter(0, "Seasoning (sodium)", sodiumMg / (SodiumMax * 2f), sodiumMg > SodiumMax ? MinigameUI.Bad : MinigameUI.Warn,
                    SodiumMin / (SodiumMax * 2f), 0.5f, $"{sodiumMg:0} mg");
                MinigameUI.Meter(1, "Marinade coverage", coverage, MinigameUI.Good, 0.7f, 1f);
                return;
            }
            if (phase == Phase.Setup)
            {
                string board = usingRed ? "<color=#ff5544>RED</color>" : "<color=#55dd66>GREEN</color>";
                MinigameUI.Status($"Board: {board}", Color.white, 0);
                return;
            }
            MinigameUI.Meter(0, "Knife", knifePos, new Color(1f, 1f, 1f, 0.15f), valueText: $"{cuts.Count} cuts");
            for (int i = 1; i < targetPieces; i++) MinigameUI.Marker(0, (float)i / targetPieces, new Color(1f, 1f, 1f, 0.6f), 3f);
            foreach (float c in cuts) MinigameUI.Marker(0, c, MinigameUI.Bad, 4f);
            MinigameUI.Marker(0, knifePos, MinigameUI.Warn, 6f);
        }
    }
}
