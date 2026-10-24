using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using NutritionKitchen.Crafting;

namespace NutritionKitchen.Minigames
{
    public enum MinigameState { Idle, Active, PrecisionCheck, Completed, Failed }
    public enum ApplianceState { Idle, Preheating, Cooking, Burning, Broken }
    public enum Contamination { None, SoilBacteria, Salmonella, Undercooked }

    /// <summary>Outcome of one minigame round. Scoring / waste tracking listen to MinigameStation.StepFinished.</summary>
    public class MinigameResult
    {
        public string station;
        public IngredientData ingredient;
        public float accuracy;                 // 0..1, feeds the Cooking Precision Bonus
        public float nutrientRetention = 1f;   // 0..1
        public float wastePct;                 // 0..1 of the ingredient thrown away
        public Contamination contamination;
        public readonly List<string> notes = new List<string>();

        public bool Failed => contamination != Contamination.None || wastePct >= 1f;

        public override string ToString() =>
            $"[{station}] {(ingredient ? ingredient.displayNameEng : "?")}: accuracy {accuracy:P0}, nutrients kept {nutrientRetention:P0}, " +
            $"waste {wastePct:P0}, contamination {contamination}. {string.Join(" ", notes)}";
    }

    /// <summary>
    /// A Cooking Mama style station. The player brings an ingredient (from the Pantry) and presses E: the food goes
    /// onto the station, the camera moves to CameraAnchor and 1-9 picks the step (Chop/Marinate etc.).
    /// The result is recorded on the food, which then stays on the station until the player takes it (E, empty hands).
    /// Active (subclass Tick) -> Submit -> PrecisionCheck -> Completed/Failed.
    /// </summary>
    public abstract class MinigameStation : MonoBehaviour
    {
        public static event Action<MinigameResult> StepFinished;

        public MinigameState State { get; private set; } = MinigameState.Idle;
        public bool InUse { get; private set; }
        public Transform CameraAnchor { get; private set; }
        public abstract string StationName { get; }
        /// <summary>The recipe steps this station performs; the first is the default.</summary>
        public abstract StepKind[] Kinds { get; }
        /// <summary>Finished food waiting on the station to be picked up.</summary>
        public FoodItem ReadyFood => InUse ? null : stationFood;
        /// <summary>The player's camera while in use, for clicking things in the scene.</summary>
        protected Camera ViewCamera { get; private set; }

        protected IngredientData ingredient;
        /// <summary>Which of Kinds is being done this round (e.g. Chop or Marinate).</summary>
        protected StepKind Mode { get; private set; }

        private FoodItem stationFood;      // the food on this station (being cooked, or done and waiting)
        private MinigameResult lastResult;
        private float checkTimer;
        private PlayerHands hands;
        private bool itemDone;      // the food already got its step this visit

        /// <summary>Which ingredients this station works with.</summary>
        protected abstract bool Accepts(IngredientData i);
        /// <summary>Builds the station's props as children (station forward = toward the back wall, player at -Z).</summary>
        protected abstract void BuildProps();
        protected abstract void OnBegin();
        protected abstract void Tick(float dt, Keyboard kb, Mouse mouse);
        /// <summary>Draws the HUD while Active.</summary>
        protected abstract void DrawHud();
        /// <summary>One line per control, shown while Active.</summary>
        protected abstract string Controls { get; }
        /// <summary>Removes the food visuals left on the station once the player takes the food.</summary>
        protected abstract void ClearFood();
        /// <summary>Called when a round stops (submitted or abandoned): stop loops, sounds, etc.</summary>
        protected virtual void OnStopped() { }

        protected virtual Vector3 CameraOffset => new Vector3(0f, 0.75f, -0.55f);
        protected virtual float CameraPitch => 52f;

        protected virtual void Awake()
        {
            CameraAnchor = new GameObject("CameraAnchor").transform;
            CameraAnchor.SetParent(transform, false);
            CameraAnchor.localPosition = CameraOffset;
            CameraAnchor.localRotation = Quaternion.Euler(CameraPitch, 0f, 0f);

            BuildProps();

            // Trigger box so the player's look-ray finds the station without blocking movement.
            BoxCollider box = gameObject.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.center = new Vector3(0f, 0.25f, 0f);
            box.size = new Vector3(0.85f, 0.5f, 0.7f);
        }

        /// <summary>Only with food in hand, and only while nothing finished is still waiting here.</summary>
        public bool CanEnter(PlayerHands h) => h.Held != null && stationFood == null;

        /// <summary>The player arrives with food in hand; it goes onto the station.</summary>
        public void Enter(Camera viewCamera, PlayerHands playerHands)
        {
            ViewCamera = viewCamera;
            hands = playerHands;
            stationFood = hands.Take();
            stationFood.transform.SetParent(transform, false);
            stationFood.gameObject.SetActive(false); // the station draws its own version of the food
            itemDone = false;
            lastResult = null;
            State = MinigameState.Idle;
            InUse = true;
        }

        public void Exit()
        {
            if (State == MinigameState.Active || State == MinigameState.PrecisionCheck)
            {
                var r = lastResult ?? new MinigameResult();
                if (State == MinigameState.Active)
                {
                    r = new MinigameResult { wastePct = 1f };
                    r.notes.Add("Abandoned mid-step, ingredient wasted.");
                }
                Publish(r);
            }
            OnStopped();
            State = MinigameState.Idle;
            InUse = false;
            if (!itemDone) GiveBack(); // nothing was done to it: straight back into the hands
        }

        /// <summary>E with empty hands: take the finished food off the station.</summary>
        public void TakeFood(PlayerHands h)
        {
            hands = h;
            GiveBack();
            ClearFood();
        }

        private void GiveBack()
        {
            if (stationFood == null) return;
            hands.Hold(stationFood);
            stationFood = null;
        }

        private void Begin(StepKind kind)
        {
            ingredient = stationFood.Ingredient;
            Mode = kind;
            lastResult = null;
            State = MinigameState.Active;
            OnBegin();
        }

        /// <summary>Subclasses call this when the player finishes; shows "checking" briefly, then the result.</summary>
        protected void Submit(MinigameResult r)
        {
            OnStopped();
            lastResult = r;
            State = MinigameState.PrecisionCheck;
            checkTimer = 0.8f;
        }

        private void Publish(MinigameResult r)
        {
            r.station = StationName;
            r.ingredient = ingredient;
            r.accuracy = Mathf.Clamp01(r.accuracy);
            r.nutrientRetention = Mathf.Clamp01(r.nutrientRetention);
            r.wastePct = Mathf.Clamp01(r.wastePct);
            lastResult = r;
            State = r.Failed ? MinigameState.Failed : MinigameState.Completed;
            if (stationFood != null && !itemDone)
            {
                stationFood.Record(Mode, r);
                itemDone = true;
            }
            Debug.Log(r);
            StepFinished?.Invoke(r);
        }

        protected virtual void Update()
        {
            if (!InUse) return;
            Keyboard kb = Keyboard.current;
            Mouse mouse = Mouse.current;
            if (kb == null || mouse == null) return;

            switch (State)
            {
                case MinigameState.Active:
                    Tick(Time.deltaTime, kb, mouse);
                    break;
                case MinigameState.PrecisionCheck:
                    checkTimer -= Time.deltaTime;
                    if (checkTimer <= 0f) Publish(lastResult);
                    break;
                default: // Idle / Completed / Failed: start a step on the food
                    if (stationFood == null || itemDone || stationFood.Ruined || !Accepts(stationFood.Ingredient)) break;
                    for (int i = 0; i < Kinds.Length; i++)
                        if (kb[Key.Digit1 + i].wasPressedThisFrame) { Begin(Kinds[i]); break; }
                    break;
            }
        }

        private string MenuText()
        {
            if (stationFood == null) return "Esc  leave";
            var lines = new System.Text.StringBuilder(stationFood.Describe()).Append('\n');
            if (itemDone) lines.Append("Done!  Esc, then E to take it");
            else if (stationFood.Ruined) lines.Append("Ruined: take it to the bin.\nEsc  leave");
            else if (!Accepts(stationFood.Ingredient)) lines.Append($"Can't use {stationFood.Ingredient.displayNameEng} here.\nEsc  leave");
            else
            {
                for (int i = 0; i < Kinds.Length; i++) lines.Append($"  {i + 1}   {Kinds[i]}\n");
                lines.Append("Esc  leave");
            }
            return lines.ToString();
        }

        private void OnGUI()
        {
            if (!InUse) return;
            MinigameUI.Title($"{StationName}   ·   {State}" + (ingredient && State != MinigameState.Idle ? $"   ·   {Mode} {ingredient.displayNameEng}" : ""));

            switch (State)
            {
                case MinigameState.Active:
                    DrawHud();
                    MinigameUI.Help(Controls + "\nEsc  leave (wastes the ingredient)");
                    break;
                case MinigameState.PrecisionCheck:
                    MinigameUI.Center("Checking...", 30);
                    break;
                default:
                    MinigameUI.Help(MenuText());
                    if (lastResult != null) MinigameUI.Result(lastResult, State == MinigameState.Failed);
                    break;
            }
        }
    }
}
