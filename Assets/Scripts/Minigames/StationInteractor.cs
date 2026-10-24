using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NutritionKitchen.Minigames
{
    /// <summary>
    /// On the player next to PlayerController. Look at a station + E: movement stops, the camera glides to the
    /// station's close-up anchor and the mouse is freed for the minigame. Esc glides back.
    /// Pantry, bin and pass (KitchenInteractable) are used straight from the normal view.
    /// </summary>
    [RequireComponent(typeof(PlayerController), typeof(PlayerHands))]
    public class StationInteractor : MonoBehaviour
    {
        public static Camera PlayerCamera { get; private set; }
        public static bool InStation { get; private set; }

        [SerializeField] private float reach = 2.5f;
        [SerializeField] private float cameraGlideSpeed = 8f;

        private PlayerController controller;
        private PlayerHands hands;
        private Transform cam;
        private MinigameStation looked;
        private KitchenInteractable lookedThing;
        private ScreenButton button;
        private MinigameStation current;
        private Vector3 savedLocalPos;
        private Quaternion savedLocalRot;
        private bool returning;

        private void Awake()
        {
            controller = GetComponent<PlayerController>();
            hands = GetComponent<PlayerHands>();
            PlayerCamera = GetComponentInChildren<Camera>();
            cam = PlayerCamera.transform;
        }

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return;
            float t = 1f - Mathf.Exp(-cameraGlideSpeed * Time.deltaTime);

            if (current != null)
            {
                cam.SetPositionAndRotation(Vector3.Lerp(cam.position, current.CameraAnchor.position, t),
                                           Quaternion.Slerp(cam.rotation, current.CameraAnchor.rotation, t));
                if (kb.escapeKey.wasPressedThisFrame) Leave();
                return;
            }

            if (returning)
            {
                cam.localPosition = Vector3.Lerp(cam.localPosition, savedLocalPos, t);
                cam.localRotation = Quaternion.Slerp(cam.localRotation, savedLocalRot, t);
                if ((cam.localPosition - savedLocalPos).sqrMagnitude < 0.0001f)
                {
                    cam.localPosition = savedLocalPos;
                    cam.localRotation = savedLocalRot;
                    returning = false;
                    InStation = false;
                    controller.enabled = true; // re-locks the cursor
                }
                return;
            }

            looked = null;
            lookedThing = null;
            button = null;
            // Nearest hit wins, except dropped food: it often lands inside a station's look-zone and must stay grabbable.
            RaycastHit[] hits = Physics.RaycastAll(cam.position, cam.forward, reach, ~0, QueryTriggerInteraction.Collide);
            DroppedFood dropped = hits.Select(h => h.collider.GetComponentInParent<DroppedFood>()).FirstOrDefault(d => d != null);
            if (dropped != null) lookedThing = dropped;
            else if (hits.Length > 0)
            {
                Collider nearest = hits.OrderBy(h => h.distance).First().collider;
                looked = nearest.GetComponentInParent<MinigameStation>();
                lookedThing = nearest.GetComponentInParent<KitchenInteractable>();
                button = nearest.GetComponent<ScreenButton>();
            }
            if (button != null)
            {
                button.Hover();
                if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) button.Press();
            }
#if UNITY_EDITOR
            DevJump(kb);
#endif
            if (looked != null && kb.eKey.wasPressedThisFrame)
            {
                if (looked.ReadyFood != null && hands.Held == null) looked.TakeFood(hands);
                else if (looked.CanEnter(hands)) Use(looked);
            }
            else if (lookedThing != null) lookedThing.HandleInput(hands, kb);
        }

#if UNITY_EDITOR
        private MonoBehaviour devTarget;

        // Dev shortcut, editor only: F1-F9 jumps in front of a station/pantry/bin/pass (alphabetical) and targets it
        // until you walk away, so a playtest doesn't need walking and aiming. F10 = freezer order screen, F11 = drone drop pad.
        private void DevJump(Keyboard kb)
        {
            for (int i = 0; i < 11; i++)
            {
                if (!kb[Key.F1 + i].wasPressedThisFrame) continue;
                MonoBehaviour[] all = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                    .Where(m => m is MinigameStation || m is Pantry || (m is KitchenInteractable && !(m is DroppedFood) && !(m is DeliveryBox) && !(m is PantryBox))).OrderBy(m => m.name)
                    .Concat(FindObjectsByType<FreezerScreen>(FindObjectsSortMode.None))
                    .Concat(FindObjectsByType<DroneDelivery>(FindObjectsSortMode.None)).ToArray();
                if (i >= all.Length) return;
                devTarget = all[i];
                Transform t = devTarget.transform;
                var cc = GetComponent<CharacterController>();
                cc.enabled = false; // CharacterController ignores teleports while enabled
                transform.SetPositionAndRotation(t.position - t.forward * 0.9f - Vector3.up * 0.8f, Quaternion.LookRotation(t.forward));
                cc.enabled = true;
                MinigameUI.ShowToast($"Dev jump: {devTarget.name}", 2f);
            }
            if (kb.wKey.isPressed || kb.aKey.isPressed || kb.sKey.isPressed || kb.dKey.isPressed) devTarget = null;
            if (devTarget is MinigameStation s) { looked = s; lookedThing = null; }
            else if (devTarget is KitchenInteractable k) { lookedThing = k; looked = null; }
        }
#endif

        private void Use(MinigameStation station)
        {
            current = station;
            InStation = true;
            savedLocalPos = cam.localPosition;
            savedLocalRot = cam.localRotation;
            controller.enabled = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            hands.SetVisible(false);
            station.Enter(PlayerCamera, hands);
        }

        private void Leave()
        {
            current.Exit();
            current = null;
            hands.SetVisible(true);
            returning = true;
        }

        private string StationPrompt(MinigameStation s)
        {
            if (s.ReadyFood != null)
                return hands.Held == null ? $"[E]  Take {s.ReadyFood.Describe()}" : $"{s.StationName}: {s.ReadyFood.Describe()} is ready\nHands full";
            return hands.Held != null ? $"[E]  Use {s.StationName}" : $"{s.StationName}\nBring an ingredient from the Pantry";
        }

        private void OnGUI()
        {
            if (current != null || returning) return;
            MinigameUI.Crosshair(button != null);
            if (button != null) MinigameUI.Prompt($"[Click]  {button.hint}");
            else if (looked != null) MinigameUI.Prompt(StationPrompt(looked));
            else if (lookedThing != null) MinigameUI.Prompt(lookedThing.Prompt(hands));
        }
    }
}
