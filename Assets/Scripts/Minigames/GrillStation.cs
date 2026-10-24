using UnityEngine;
using UnityEngine.InputSystem;
using NutritionKitchen.Crafting;

namespace NutritionKitchen.Minigames
{
    /// <summary>
    /// Grill & sear: flip (LMB) when the side down has good grill marks — watch the colour and listen to the
    /// sizzle. Hold RMB to push the food thermometer in and read the core temperature before plating:
    /// chicken must reach 74°C, fish 63°C.
    /// </summary>
    public class GrillStation : HeatStation
    {
        [SerializeField] private float searPerSecond = 0.09f;   // at full heat
        [SerializeField] private float corePerSecond = 4f;      // °C per second at full heat
        [SerializeField] private float probeSettleSeconds = 0.8f;

        private const float StartCoreC = 4f; // straight from the fridge
        private float searDown, searUp, coreC, probeTimer, lastProbeTime = -999f, flipAnim;
        private bool flipped;
        private Transform food, probe;
        private GameObject halfDown, halfUp;
        private GameObject[] marksDown, marksUp;
        private AudioSource sizzle;

        public override string StationName => "Grill Station";
        public override StepKind[] Kinds { get; } = { StepKind.Grill };
        protected override string Controls => "Mouse wheel / Up-Down   grill heat\nLMB   flip\nHold RMB   thermometer\nSpace   plate it";

        protected override bool Accepts(IngredientData i) => i.isRawMeat;

        protected override void BuildProps()
        {
            BuildBurner(Vector3.zero, 0.3f);
            for (int i = 0; i < 6; i++) // grate bars
                PropFactory.Prim(PrimitiveType.Cube, "Grate", transform, new Vector3(-0.125f + i * 0.05f, 0.025f, 0f), new Vector3(0.012f, 0.012f, 0.32f), new Color(0.2f, 0.2f, 0.2f));

            probe = new GameObject("Thermometer").transform;
            probe.SetParent(transform, false);
            PropFactory.Prim(PrimitiveType.Cylinder, "Probe", probe, new Vector3(0f, 0.08f, 0f), new Vector3(0.006f, 0.08f, 0.006f), new Color(0.85f, 0.85f, 0.9f));
            PropFactory.Prim(PrimitiveType.Cube, "Dial", probe, new Vector3(0f, 0.17f, 0f), new Vector3(0.04f, 0.03f, 0.015f), new Color(0.95f, 0.95f, 0.95f));
            probe.localPosition = new Vector3(0.22f, 0f, 0f);

            sizzle = gameObject.AddComponent<AudioSource>();
            sizzle.clip = MakeSizzle();
            sizzle.loop = true;
            sizzle.spatialBlend = 0f;
            sizzle.volume = 0f;
        }

        // Crackly noise: mostly soft hiss with random pops.
        private static AudioClip MakeSizzle()
        {
            const int rate = 22050;
            var data = new float[rate * 2];
            var rng = new System.Random(7);
            for (int i = 0; i < data.Length; i++)
                data[i] = ((float)rng.NextDouble() * 2f - 1f) * (rng.NextDouble() < 0.01 ? 0.9f : 0.15f);
            AudioClip clip = AudioClip.Create("Sizzle", data.Length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        protected override void ClearFood()
        {
            if (food) Destroy(food.gameObject);
        }

        protected override void OnBegin()
        {
            ResetHeat();
            searDown = searUp = probeTimer = flipAnim = 0f;
            coreC = StartCoreC;
            lastProbeTime = -999f;
            flipped = false;
            if (food) Destroy(food.gameObject);

            food = new GameObject("Food").transform;
            food.SetParent(transform, false);
            food.localPosition = new Vector3(0f, 0.06f, 0f);
            halfDown = PropFactory.Prim(PrimitiveType.Cube, "SideA", food, new Vector3(0f, -0.0125f, 0f), new Vector3(0.18f, 0.025f, 0.12f), ingredient.themeColor);
            halfUp = PropFactory.Prim(PrimitiveType.Cube, "SideB", food, new Vector3(0f, 0.0125f, 0f), new Vector3(0.18f, 0.025f, 0.12f), ingredient.themeColor);
            marksDown = Marks(-0.026f);
            marksUp = Marks(0.026f);
            sizzle.Play();
        }

        private GameObject[] Marks(float y)
        {
            var m = new GameObject[4];
            for (int i = 0; i < 4; i++)
            {
                m[i] = PropFactory.Prim(PrimitiveType.Cube, "GrillMark", food, new Vector3(-0.06f + i * 0.04f, y, 0f), new Vector3(0.008f, 0.002f, 0.11f), new Color(0.12f, 0.07f, 0.04f), new Vector3(0f, 25f, 0f));
                m[i].SetActive(false);
            }
            return m;
        }

        protected override void Tick(float dt, Keyboard kb, Mouse mouse)
        {
            UpdateHeat(dt, kb, mouse, Mathf.Max(searDown, searUp) > 1.1f);

            // Side A starts down; after each flip the other side is on the grate.
            if (!flipped) searDown += searPerSecond * heat * dt; else searUp += searPerSecond * heat * dt;
            coreC = Mathf.Min(coreC + corePerSecond * heat * dt, 98f);

            if (mouse.leftButton.wasPressedThisFrame && flipAnim <= 0f) { flipped = !flipped; flipAnim = 1f; }
            flipAnim = Mathf.MoveTowards(flipAnim, 0f, dt * 3.5f);
            food.localRotation = Quaternion.Euler((flipped ? 180f : 0f) - flipAnim * 180f * (flipped ? 1f : -1f), 0f, 0f);
            food.localPosition = new Vector3(0f, 0.06f + Mathf.Sin(flipAnim * Mathf.PI) * 0.08f, 0f);

            // Thermometer: needs a moment in the meat before the reading settles.
            bool probing = mouse.rightButton.isPressed;
            probeTimer = probing ? probeTimer + dt : 0f;
            probe.localPosition = probing ? new Vector3(0f, 0.02f, 0f) : new Vector3(0.22f, 0f, 0f);
            if (probeTimer >= probeSettleSeconds) lastProbeTime = Time.time;

            float sear = flipped ? searUp : searDown;
            sizzle.volume = heat * 0.5f;
            sizzle.pitch = 0.8f + Mathf.Clamp01(sear) * 0.5f; // sizzle sharpens as the side colours

            Shade(halfDown, marksDown, searDown);
            Shade(halfUp, marksUp, searUp);

            if (Mathf.Max(searDown, searUp) >= 1.5f) Finish(); // charcoal
            else if (kb.spaceKey.wasPressedThisFrame) Finish();
        }

        private void Shade(GameObject half, GameObject[] marks, float sear)
        {
            Color c = sear < 1f
                ? Color.Lerp(ingredient.themeColor, new Color(0.72f, 0.48f, 0.25f), sear)
                : Color.Lerp(new Color(0.72f, 0.48f, 0.25f), new Color(0.08f, 0.06f, 0.05f), (sear - 1f) / 0.5f);
            if (Time.frameCount % 6 == 0) PropFactory.SetColor(half, c);
            foreach (GameObject m in marks) m.SetActive(sear > 0.45f);
        }

        private static float SideScore(float sear) => sear < 0.6f ? sear / 0.6f : sear <= 1f ? 1f : Mathf.Clamp01(1f - (sear - 1f) / 0.5f);

        private void Finish()
        {
            var r = new MinigameResult();
            float target = ingredient.safeCoreTempC;
            bool verified = Time.time - lastProbeTime < 10f;

            r.accuracy = (SideScore(searDown) + SideScore(searUp)) * 0.5f * (verified ? 1f : 0.8f);
            r.nutrientRetention = 1f - ingredient.heatSensitivity * Mathf.Clamp01((coreC - target - 10f) / 20f);

            if (Mathf.Max(searDown, searUp) >= 1.5f)
            {
                r.wastePct = 1f;
                r.notes.Add("Burnt to charcoal.");
            }
            else if (coreC < target)
            {
                r.contamination = Contamination.Undercooked;
                r.notes.Add($"Core only {coreC:0}°C, needs {target:0}°C: pathogen risk!");
            }
            else r.notes.Add($"Core {coreC:0}°C (safe ≥ {target:0}°C).");

            if (coreC > target + 15f) r.notes.Add("Overcooked: dry and lost nutrients.");
            if (!verified) r.notes.Add("Not checked with the thermometer: always probe before plating.");
            if (searDown < 0.6f || searUp < 0.6f) r.notes.Add("One side is pale: flip at the right time.");
            if (SideScore(searDown) > 0.9f && SideScore(searUp) > 0.9f) r.notes.Add("Perfect grill marks on both sides!");
            Submit(r);
        }

        protected override void OnStopped()
        {
            base.OnStopped();
            if (sizzle) sizzle.Stop();
            if (probe) probe.localPosition = new Vector3(0.22f, 0f, 0f);
        }

        protected override void DrawHud()
        {
            DrawDial(0);
            bool reading = probeTimer >= probeSettleSeconds;
            MinigameUI.Meter(1, "Thermometer", reading ? coreC / 100f : 0f, reading && coreC >= ingredient.safeCoreTempC ? MinigameUI.Good : MinigameUI.Warn,
                ingredient.safeCoreTempC / 100f, 1f, reading ? $"{coreC:0}°C  (target {ingredient.safeCoreTempC:0}°C)" : "hold RMB to read");
            // No sear bars on purpose: read the colour and listen to the sizzle, like a real grill cook.
            MinigameUI.Status(flipped ? "Side B down" : "Side A down", Color.white, 2);
        }
    }
}
