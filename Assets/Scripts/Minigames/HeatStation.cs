using UnityEngine;
using UnityEngine.InputSystem;

namespace NutritionKitchen.Minigames
{
    /// <summary>
    /// A station on a burner: heat dial (mouse wheel or Up/Down), heat that lags behind the dial (preheating),
    /// a flame that shows it, and the ApplianceState machine. Break() is the hook for appliance-failure chaos events.
    /// </summary>
    public abstract class HeatStation : MinigameStation
    {
        [SerializeField] private float heatFollowPerSecond = 0.35f;

        public ApplianceState Appliance { get; private set; } = ApplianceState.Idle;
        protected float dial;   // what the player set, 0..1
        protected float heat;   // what the burner actually gives, 0..1
        private GameObject flame;

        public void Break() => Appliance = ApplianceState.Broken;
        public void Repair() => Appliance = ApplianceState.Idle;

        protected void BuildBurner(Vector3 localPos, float diameter)
        {
            PropFactory.Prim(PrimitiveType.Cylinder, "Burner", transform, localPos, new Vector3(diameter, 0.008f, diameter), new Color(0.15f, 0.15f, 0.16f));
            flame = PropFactory.Prim(PrimitiveType.Cylinder, "Flame", transform, localPos + Vector3.up * 0.012f, new Vector3(diameter * 0.8f, 0.01f, diameter * 0.8f), new Color(1f, 0.45f, 0.1f));
            flame.SetActive(false);
        }

        protected void ResetHeat()
        {
            dial = 0f;
            heat = 0f;
            if (Appliance != ApplianceState.Broken) Appliance = ApplianceState.Idle;
        }

        /// <summary>Reads the dial and moves heat toward it. burning = the food is scorching right now.</summary>
        protected void UpdateHeat(float dt, Keyboard kb, Mouse mouse, bool burning)
        {
            float wheel = mouse.scroll.ReadValue().y;
            if (wheel != 0f) dial += Mathf.Sign(wheel) * 0.05f;
            if (kb.upArrowKey.isPressed) dial += dt * 0.6f;
            if (kb.downArrowKey.isPressed) dial -= dt * 0.6f;
            dial = Mathf.Clamp01(dial);

            float target = Appliance == ApplianceState.Broken ? 0f : dial;
            heat = Mathf.MoveTowards(heat, target, heatFollowPerSecond * dt);

            if (Appliance != ApplianceState.Broken)
                Appliance = burning ? ApplianceState.Burning
                          : heat < 0.05f ? ApplianceState.Idle
                          : heat < dial - 0.05f ? ApplianceState.Preheating
                          : ApplianceState.Cooking;

            flame.SetActive(heat > 0.02f);
            float d = flame.transform.localScale.x;
            flame.transform.localScale = new Vector3(d, 0.005f + heat * 0.03f, d);
        }

        protected override void OnStopped()
        {
            if (flame) flame.SetActive(false);
            ResetHeat();
        }

        protected void DrawDial(int row)
        {
            MinigameUI.Meter(row, $"Burner ({Appliance})", dial, new Color(1f, 0.5f, 0.15f, 0.8f), valueText: $"dial {dial:P0}  heat {heat:P0}");
            MinigameUI.Marker(row, heat, Color.white);
            if (Appliance == ApplianceState.Broken) MinigameUI.Status("APPLIANCE BROKEN: no heat!", MinigameUI.Bad, row + 2);
        }
    }
}
