using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using NutritionKitchen.Crafting;

namespace NutritionKitchen.Minigames
{
    /// <summary>
    /// Boil & simmer: hold the pot at a simmer (85-98°C). A rolling boil on high heat foams up and boils over,
    /// and destroys heat-sensitive vitamins (vitamin C in veg). Click the foam/scum to skim it off.
    /// Meat and egg must also reach their safe core temperature.
    /// </summary>
    public class BoilStation : HeatStation
    {
        [SerializeField] private float simmerMin = 85f, simmerMax = 98f;
        [SerializeField] private float cookPerSecond = 0.07f;
        [SerializeField] private int maxFoam = 12;

        private const float StartWaterC = 20f;
        private float waterC, coreC, doneness, boilOver, retention, foamTimer, zoneTime, activeTime;
        private int spills, skimmed;
        private Transform pot, waterSurface;
        private readonly List<GameObject> foam = new List<GameObject>();
        private readonly List<Transform> bubbles = new List<Transform>();
        private readonly List<GameObject> chunks = new List<GameObject>();

        public override string StationName => "Boiling Station";
        public override StepKind[] Kinds { get; } = { StepKind.Boil, StepKind.Steam };
        protected override string Controls => Steaming
            ? "Mouse wheel / Up-Down   burner\nKeep a steady boil under the steamer\nSpace   done"
            : "Mouse wheel / Up-Down   burner\nLMB on foam   skim it\nSpace   done";

        // Steam = same pot with a basket and lid: needs a full boil, no scum, no boil-over, gentler on vitamins.
        private bool Steaming => Mode == StepKind.Steam;
        private const float SteamMinC = 98f;
        private GameObject steamer;
        private float ZoneMin => Steaming ? SteamMinC : simmerMin;
        private float ZoneMax => Steaming ? 100f : simmerMax;

        protected override bool Accepts(IngredientData i) => i.category != NutrientCategory.CookingFat && i.category != NutrientCategory.Seasoning;

        // The pot is a solid cylinder (top at y 0.09), so the water and everything in it sit just on that top.
        private const float WaterY = 0.092f;

        // Proteins and starch throw more scum than vegetables.
        private float FoamPerSecond => ingredient.category == NutrientCategory.Vegetable ? 0.15f : 0.45f;

        protected override void BuildProps()
        {
            BuildBurner(Vector3.zero, 0.22f);
            pot = new GameObject("Pot").transform;
            pot.SetParent(transform, false);
            pot.localPosition = new Vector3(0f, 0.11f, 0f);
            Color steel = new Color(0.7f, 0.72f, 0.75f);
            PropFactory.Prim(PrimitiveType.Cylinder, "PotBody", pot, Vector3.zero, new Vector3(0.28f, 0.09f, 0.28f), steel);
            PropFactory.Prim(PrimitiveType.Cube, "HandleL", pot, new Vector3(-0.17f, 0.06f, 0f), new Vector3(0.06f, 0.015f, 0.03f), steel);
            PropFactory.Prim(PrimitiveType.Cube, "HandleR", pot, new Vector3(0.17f, 0.06f, 0f), new Vector3(0.06f, 0.015f, 0.03f), steel);
            waterSurface = PropFactory.Prim(PrimitiveType.Cylinder, "Water", pot, new Vector3(0f, WaterY, 0f), new Vector3(0.26f, 0.002f, 0.26f), new Color(0.6f, 0.75f, 0.85f)).transform;
            for (int i = 0; i < 8; i++)
            {
                Transform b = PropFactory.Prim(PrimitiveType.Sphere, "Bubble", pot, Vector3.zero, Vector3.one * 0.015f, new Color(0.9f, 0.95f, 1f)).transform;
                b.gameObject.SetActive(false);
                bubbles.Add(b);
            }

            steamer = new GameObject("Steamer");
            steamer.transform.SetParent(pot, false);
            Color bamboo = new Color(0.82f, 0.68f, 0.45f);
            PropFactory.Prim(PrimitiveType.Cylinder, "Basket", steamer.transform, new Vector3(0f, 0.12f, 0f), new Vector3(0.27f, 0.03f, 0.27f), bamboo);
            PropFactory.Prim(PrimitiveType.Sphere, "Lid", steamer.transform, new Vector3(0f, 0.15f, 0f), new Vector3(0.27f, 0.06f, 0.27f), bamboo * 0.9f);
            steamer.SetActive(false);
        }

        protected override void ClearFood()
        {
            foreach (GameObject f in foam) Destroy(f);
            foam.Clear();
            foreach (GameObject c in chunks) Destroy(c);
            chunks.Clear();
        }

        protected override void OnBegin()
        {
            ResetHeat();
            waterC = coreC = StartWaterC;
            doneness = boilOver = foamTimer = zoneTime = activeTime = 0f;
            retention = 1f;
            spills = skimmed = 0;
            foreach (GameObject f in foam) Destroy(f);
            foam.Clear();
            foreach (GameObject c in chunks) Destroy(c);
            chunks.Clear();
            for (int i = 0; i < 6; i++)
            {
                Vector2 p = Random.insideUnitCircle * 0.08f;
                chunks.Add(PropFactory.Prim(PrimitiveType.Cube, "Chunk", pot, new Vector3(p.x, WaterY, p.y), Vector3.one * 0.03f, ingredient.themeColor, Random.insideUnitSphere * 90f));
            }
            PropFactory.SetColor(waterSurface.gameObject, new Color(0.6f, 0.75f, 0.85f));
            steamer.SetActive(Steaming);
        }

        protected override void Tick(float dt, Keyboard kb, Mouse mouse)
        {
            bool rolling = waterC >= 99.5f && heat > 0.85f && !Steaming;
            UpdateHeat(dt, kb, mouse, rolling);

            // Water heads toward what the burner can hold it at (100°C max).
            float targetC = Mathf.Min(100f, StartWaterC + heat * 90f);
            waterC += (targetC - waterC) * 0.25f * dt;
            coreC += (waterC - coreC) * 0.06f * dt;

            activeTime += dt;
            if (waterC >= ZoneMin && waterC <= ZoneMax) zoneTime += dt;
            if (Steaming)
            {
                if (waterC >= SteamMinC) doneness += cookPerSecond * dt;
                if (doneness > 1f) retention -= ingredient.heatSensitivity * 0.015f * dt;
                AnimateWater();
                if (kb.spaceKey.wasPressedThisFrame) Finish();
                return;
            }
            if (waterC >= simmerMin) doneness += cookPerSecond * Mathf.InverseLerp(80f, 100f, waterC) * dt;

            // Vitamins: hard boiling and cooking past done both cost heat-sensitive nutrients.
            if (waterC >= 99f) retention -= ingredient.heatSensitivity * 0.04f * dt;
            if (doneness > 1f && waterC >= simmerMin) retention -= ingredient.heatSensitivity * 0.02f * dt;

            boilOver = Mathf.Clamp01(boilOver + (rolling ? 0.12f : -0.1f) * dt);
            if (boilOver >= 1f)
            {
                spills++;
                boilOver = 0.3f;
                waterC -= 8f; // cold splash as it spills
            }

            if (waterC >= simmerMin)
            {
                foamTimer += FoamPerSecond * (rolling ? 2f : 1f) * dt;
                while (foamTimer >= 1f) { foamTimer -= 1f; SpawnFoam(); }
            }
            if (mouse.leftButton.wasPressedThisFrame) TrySkim(mouse.position.ReadValue());

            AnimateWater();
            if (kb.spaceKey.wasPressedThisFrame) Finish();
        }

        private void SpawnFoam()
        {
            if (foam.Count >= maxFoam) return;
            Vector2 p = Random.insideUnitCircle * 0.1f;
            foam.Add(PropFactory.Prim(PrimitiveType.Sphere, "Foam", pot, new Vector3(p.x, WaterY + 0.004f, p.y), new Vector3(0.05f, 0.012f, 0.05f),
                new Color(0.95f, 0.93f, 0.85f)));
        }

        // Generous on purpose: the nearest patch within ~5% of the screen height of the click counts.
        private void TrySkim(Vector2 screenPos)
        {
            if (ViewCamera == null) return;
            int best = -1;
            float bestDist = Screen.height * 0.05f;
            for (int i = 0; i < foam.Count; i++)
            {
                float d = Vector2.Distance(screenPos, ViewCamera.WorldToScreenPoint(foam[i].transform.position));
                if (d < bestDist) { bestDist = d; best = i; }
            }
            if (best < 0) return;
            Destroy(foam[best]);
            foam.RemoveAt(best);
            skimmed++;
        }

        private void AnimateWater()
        {
            float boil01 = Mathf.InverseLerp(85f, 100f, waterC);
            for (int i = 0; i < bubbles.Count; i++)
            {
                bool on = i < Mathf.CeilToInt(boil01 * bubbles.Count) && waterC > 80f;
                bubbles[i].gameObject.SetActive(on);
                if (on && Random.value < 0.2f)
                {
                    Vector2 p = Random.insideUnitCircle * 0.11f;
                    bubbles[i].localPosition = new Vector3(p.x, WaterY + 0.004f + boil01 * 0.006f, p.y);
                }
            }
            // Water rises toward the rim as it nears boiling over.
            waterSurface.localPosition = new Vector3(0f, WaterY + boilOver * 0.008f, 0f);
        }

        private void Finish()
        {
            var r = new MinigameResult();
            float zoneShare = activeTime > 0f ? zoneTime / activeTime : 0f;
            float foamScore = skimmed + foam.Count > 0 ? (float)skimmed / (skimmed + foam.Count) : 1f;

            r.accuracy = (0.5f * zoneShare + 0.3f * foamScore + 0.2f * Mathf.Clamp01(1f - spills * 0.34f)) * Mathf.Clamp01(doneness);
            r.nutrientRetention = retention;
            r.wastePct = spills * 0.1f;

            if (ingredient.safeCoreTempC > 0f && coreC < ingredient.safeCoreTempC)
            {
                r.contamination = Contamination.Undercooked;
                r.notes.Add($"Core only {coreC:0}°C, needs {ingredient.safeCoreTempC:0}°C.");
            }
            else if (doneness < 0.9f) r.notes.Add($"Only {doneness:P0} cooked.");
            if (foam.Count > 0) r.notes.Add($"{foam.Count} patches of scum left: cloudy broth (skimmed {skimmed}).");
            else if (skimmed > 0) r.notes.Add($"All {skimmed} patches of scum skimmed: clear broth.");
            if (spills > 0) r.notes.Add($"Boiled over {spills}x: mess on the stove (hygiene).");
            if (retention < 0.85f) r.notes.Add(Steaming ? "Steamed too long: some vitamins lost." : "Boiled too hard/long: vitamin C and other heat-sensitive vitamins lost.");
            r.notes.Add(Steaming ? $"Time at a steady boil {zoneShare:P0}." : $"Time at a simmer {zoneShare:P0}.");
            Submit(r);
        }

        protected override void DrawHud()
        {
            DrawDial(0);
            MinigameUI.Meter(1, "Water", waterC / 100f, waterC > ZoneMax ? MinigameUI.Bad : waterC >= ZoneMin ? MinigameUI.Good : MinigameUI.Info,
                ZoneMin / 100f, ZoneMax / 100f, $"{waterC:0}°C");
            MinigameUI.Meter(2, "Cooked", doneness, MinigameUI.Good, 0.9f, 1f);
            if (!Steaming) MinigameUI.Meter(3, "Boil-over", boilOver, MinigameUI.Bad);
            if (foam.Count > 3) MinigameUI.Status("Skim the foam!", MinigameUI.Warn, 4);
        }
    }
}
