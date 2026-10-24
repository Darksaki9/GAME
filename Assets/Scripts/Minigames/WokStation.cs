using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using NutritionKitchen.Crafting;

namespace NutritionKitchen.Minigames
{
    /// <summary>
    /// Stir-fry: keep the heat on the food in the golden zone. The burner dial is slow; tilting the wok
    /// (mouse left/right) pulls food off the hottest spot for fine control. Toss (LMB) on the rhythm beat to
    /// keep the bottom from scorching. Too hot = burnt outside, raw inside; too cool = soggy, soaks up oil.
    /// </summary>
    public class WokStation : HeatStation
    {
        [SerializeField] private float zoneMin = 0.55f, zoneMax = 0.8f;
        [SerializeField] private float cookPerSecond = 0.11f;
        [SerializeField] private float beatSeconds = 1.1f;
        [SerializeField] private float beatWindow = 0.18f;

        private float doneness, burn, oil, retention, effective;
        private float beatTimer, zoneTime, activeTime;
        private int tossHits, tossMisses;
        private bool tossedThisBeat;
        private Transform wok;
        private readonly List<Transform> pieces = new List<Transform>();
        private readonly List<Vector3> pieceHome = new List<Vector3>();
        private float tossAnim;

        public override string StationName => "Wok Station";
        public override StepKind[] Kinds { get; } = { StepKind.StirFry };
        protected override string Controls => "Mouse wheel / Up-Down   burner\nMouse left-right   tilt wok\nLMB on the beat   toss\nSpace   plate it";

        protected override bool Accepts(IngredientData i) =>
            i.category == NutrientCategory.Carbohydrate || i.category == NutrientCategory.Protein || i.category == NutrientCategory.Vegetable;

        protected override void BuildProps()
        {
            BuildBurner(Vector3.zero, 0.22f);
            wok = new GameObject("Wok").transform;
            wok.SetParent(transform, false);
            wok.localPosition = new Vector3(0f, 0.07f, 0f);
            PropFactory.Prim(PrimitiveType.Sphere, "Bowl", wok, Vector3.zero, new Vector3(0.38f, 0.1f, 0.38f), new Color(0.12f, 0.12f, 0.13f));
            PropFactory.Prim(PrimitiveType.Cube, "Handle", wok, new Vector3(0f, 0.03f, -0.28f), new Vector3(0.03f, 0.03f, 0.2f), new Color(0.25f, 0.18f, 0.12f));
        }

        protected override void ClearFood()
        {
            foreach (Transform p in pieces) Destroy(p.gameObject);
            pieces.Clear();
            pieceHome.Clear();
        }

        protected override void OnBegin()
        {
            ResetHeat();
            doneness = burn = oil = zoneTime = activeTime = tossAnim = 0f;
            retention = 1f;
            beatTimer = beatSeconds;
            tossHits = tossMisses = 0;
            tossedThisBeat = false;
            foreach (Transform p in pieces) Destroy(p.gameObject);
            pieces.Clear();
            pieceHome.Clear();
            for (int i = 0; i < 12; i++)
            {
                Vector2 c = Random.insideUnitCircle * 0.1f;
                Vector3 home = new Vector3(c.x, 0.05f, c.y);
                pieces.Add(PropFactory.Prim(PrimitiveType.Cube, "Food", wok, home, Vector3.one * 0.03f, ingredient.themeColor, Random.insideUnitSphere * 90f).transform);
                pieceHome.Add(home);
            }
        }

        protected override void Tick(float dt, Keyboard kb, Mouse mouse)
        {
            UpdateHeat(dt, kb, mouse, burn > 0.5f);

            // Tilt from the mouse's horizontal screen position: tilted = food climbs the cooler side.
            float tilt = Mathf.Clamp((mouse.position.ReadValue().x / Screen.width - 0.5f) * 2f, -1f, 1f);
            wok.localRotation = Quaternion.Euler(0f, 0f, -tilt * 25f);
            effective = heat * (1f - Mathf.Abs(tilt) * 0.6f);

            activeTime += dt;
            bool inZone = effective >= zoneMin && effective <= zoneMax;
            if (inZone) zoneTime += dt;

            if (effective < zoneMin)
            {
                doneness += cookPerSecond * 0.5f * effective / zoneMin * dt;
                if (effective > 0.1f) oil += 0.05f * (1f - effective / zoneMin) * dt; // sitting in warm oil
            }
            else
            {
                doneness += cookPerSecond * dt;
                if (effective > zoneMax) burn += 0.35f * (effective - zoneMax) / (1f - zoneMax) * dt;
            }
            if (doneness > 1f) // overcooking: heat-sensitive vitamins go first, then it scorches
            {
                retention -= ingredient.heatSensitivity * 0.08f * effective * dt;
                burn += 0.05f * effective * dt;
            }
            if (effective > zoneMax) retention -= ingredient.heatSensitivity * 0.04f * dt;

            // Toss rhythm: one toss per beat, inside the window at the end of the beat.
            beatTimer -= dt;
            if (mouse.leftButton.wasPressedThisFrame)
            {
                tossAnim = 1f;
                if (beatTimer <= beatWindow && !tossedThisBeat) { tossHits++; tossedThisBeat = true; }
                else oil += 0.01f; // off-beat toss splashes
            }
            if (beatTimer <= -beatWindow)
            {
                if (!tossedThisBeat && effective > 0.3f) { tossMisses++; burn += 0.05f; } // bottom layer scorches
                beatTimer += beatSeconds;
                tossedThisBeat = false;
            }

            AnimatePieces(dt);

            if (burn >= 1f) Finish(); // burnt beyond saving
            else if (kb.spaceKey.wasPressedThisFrame) Finish();
        }

        private void AnimatePieces(float dt)
        {
            tossAnim = Mathf.MoveTowards(tossAnim, 0f, dt * 2.5f);
            float hop = Mathf.Sin(tossAnim * Mathf.PI) * 0.12f;
            Color c = Color.Lerp(Color.Lerp(ingredient.themeColor, new Color(0.75f, 0.5f, 0.25f), Mathf.Clamp01(doneness) * 0.6f), new Color(0.1f, 0.08f, 0.06f), burn);
            for (int i = 0; i < pieces.Count; i++)
            {
                pieces[i].localPosition = pieceHome[i] + Vector3.up * hop * (0.7f + 0.3f * Mathf.Sin(i));
                if (tossAnim > 0f) pieces[i].Rotate(dt * 400f, dt * 300f * (i % 2 == 0 ? 1 : -1), 0f, Space.Self);
            }
            if (Time.frameCount % 10 == 0) foreach (Transform p in pieces) PropFactory.SetColor(p.gameObject, c);
        }

        private void Finish()
        {
            var r = new MinigameResult();
            float zoneShare = activeTime > 0f ? zoneTime / activeTime : 0f;
            int beats = tossHits + tossMisses;
            float rhythm = beats > 0 ? (float)tossHits / beats : 0f;

            r.accuracy = (0.5f * zoneShare + 0.3f * rhythm + 0.2f * (1f - Mathf.Clamp01(oil))) * Mathf.Clamp01(doneness) * (1f - burn * 0.5f);
            r.nutrientRetention = retention;
            if (burn >= 1f)
            {
                r.wastePct = 1f;
                r.notes.Add("Burnt! The whole batch goes in the bin.");
            }
            else if (doneness < 0.85f)
            {
                if (ingredient.isRawMeat || ingredient.safeCoreTempC > 0f) r.contamination = Contamination.Undercooked;
                r.notes.Add($"Only {doneness:P0} cooked.");
            }
            if (oil > 0.3f) r.notes.Add("Heat too low for too long: food soaked up extra oil.");
            if (burn > 0.3f && burn < 1f) r.notes.Add("Scorched in places: keep heat in the zone and toss on the beat.");
            if (retention < 0.85f) r.notes.Add("Overcooked: heat-sensitive vitamins lost.");
            r.notes.Add($"Time in golden zone {zoneShare:P0}, tosses on beat {tossHits}/{beats}.");
            Submit(r);
        }

        protected override void DrawHud()
        {
            DrawDial(0);
            MinigameUI.Meter(1, "Heat on food", effective, effective > zoneMax ? MinigameUI.Bad : effective >= zoneMin ? MinigameUI.Good : MinigameUI.Info, zoneMin, zoneMax);
            MinigameUI.Meter(2, "Cooked", doneness, MinigameUI.Good, 0.85f, 1f);
            MinigameUI.Meter(3, "Burn", burn, MinigameUI.Bad);
            // Beat ring as a meter: fills up to the toss window.
            float beat01 = 1f - Mathf.Clamp01(beatTimer / beatSeconds);
            MinigameUI.Meter(4, "Toss rhythm", beat01, beatTimer <= beatWindow ? MinigameUI.Good : new Color(1f, 1f, 1f, 0.4f),
                1f - beatWindow / beatSeconds, 1f, beatTimer <= beatWindow ? "TOSS!" : "");
        }
    }
}
