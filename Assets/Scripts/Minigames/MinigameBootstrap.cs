using Unity.AI.Navigation;
using UnityEngine;

namespace NutritionKitchen.Minigames
{
    /// <summary>
    /// When a scene with the chef starts and has no stations placed yet, puts the minigame stations, pantry, bin and
    /// pass on the restaurant's kitchen line (found by the object names gen_restaurant.py writes), gives the chef
    /// hands + a StationInteractor, bakes a NavMesh for customers and starts the OrderManager.
    /// shortcut: everything is spawned at runtime; once the spots feel right, place them in the scene and this stays out of the way.
    /// </summary>
    public static class MinigameBootstrap
    {
        private const float StoveHalf = 0.45f; // wok on one half of the 4-burner stove, pot on the other
        // 'side' runs along the station's right, which is OBJ -X because Unity mirrors OBJ X on import.
        private const float PortionSide = -0.725f; // free end of the prep counter, past the cutting board

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            PlayerController player = Object.FindFirstObjectByType<PlayerController>();
            if (player == null) return; // not a gameplay scene
            if (player.GetComponent<StationInteractor>() == null) player.gameObject.AddComponent<StationInteractor>();
            if (Object.FindFirstObjectByType<MinigameStation>() != null) return; // already placed by hand

            Transform root = new GameObject("Minigame_Stations").transform;
            Transform p = player.transform;
            int slot = 0;
            PlaceOn<WashStation>(root, "Prep_Sink", b => b.min.y + 0.76f, p, slot++); // basin floor; a ray from above hits the faucet
            Place<ChopStation>(root, "Cutting_Board", 0f, p, slot++);
            Place<PortionStation>(root, "Prep_Counter", PortionSide, p, slot++);
            Place<WokStation>(root, "Stove", -StoveHalf, p, slot++);
            Place<BoilStation>(root, "Stove", StoveHalf, p, slot++);
            Place<GrillStation>(root, "Grill_Counter", 0f, p, slot++);

            PlaceOn<Pantry>(root, "Dry_Storage_Shelf_1", b => b.min.y + 1.08f, p, slot++);  // the middle shelf
            PlaceOn<WasteBin>(root, "Kitchen_Trash_1", b => b.max.y, p, slot++);
            ServingPass pass = PlaceOn<ServingPass>(root, "Pass_Table", b => b.max.y, p, slot++);

            PlaceFreezerScreen(root);
            SetUpDropPad();
            StartOrders(pass, p);
        }

        /// <summary>The "Drop ingredient" pad (built in the scene) is where delivery drones drop ordered boxes.</summary>
        private static void SetUpDropPad()
        {
            GameObject pad = GameObject.Find("Drop ingredient");
            if (pad == null) { Debug.LogWarning("MinigameBootstrap: no 'Drop ingredient' pad, orders go straight to the pantry"); return; }
            if (pad.GetComponent<DroneDelivery>() == null) pad.AddComponent<DroneDelivery>();
        }

        /// <summary>On the inside of the walk-in freezer's front wall (the wall between its two doors), facing in.</summary>
        private static void PlaceFreezerScreen(Transform root)
        {
            GameObject freezer = GameObject.Find("Walk_In_Freezer");
            if (freezer == null) { Debug.LogWarning("MinigameBootstrap: no Walk_In_Freezer, order screen skipped"); return; }
            Bounds b = Bounds(freezer);
            Vector3 fwd = Flat(freezer.transform.forward); // toward the back wall, so the front wall is at -fwd
            float depth = Mathf.Abs(b.extents.x * fwd.x) + Mathf.Abs(b.extents.z * fwd.z);
            Vector3 pos = b.center - fwd * (depth - 0.14f); // 0.12 m wall + a hair
            pos.y = b.min.y + 1.55f;
            Spawn<FreezerScreen>(root, pos, Quaternion.LookRotation(-fwd)); // built facing local -Z = into the freezer
        }

        private static void StartOrders(ServingPass pass, Transform player)
        {
            GameObject counter = GameObject.Find("Front_Counter");
            GameObject door = GameObject.Find("Entrance_Door_Frame");
            Vector3 fwd, right, entrance, centre;
            if (counter != null && door != null)
            {
                // Bake a walkable mesh for customers from the restaurant's own colliders.
                Transform building = counter.transform.parent != null ? counter.transform.parent : counter.transform;
                var surface = building.gameObject.AddComponent<NavMeshSurface>();
                surface.collectObjects = CollectObjects.Children;
                surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
                surface.BuildNavMesh();

                fwd = Flat(counter.transform.forward);           // toward the kitchen
                right = Flat(counter.transform.right);
                centre = Bounds(counter).center;
                entrance = Bounds(door).center + fwd * 1.2f;      // just inside the door
                entrance.y = centre.y - Bounds(counter).extents.y;
            }
            else
            {
                Debug.LogWarning("MinigameBootstrap: no Front_Counter/Entrance_Door_Frame, customers queue in front of the pass");
                fwd = Flat(pass.transform.forward);
                right = Flat(pass.transform.right);
                centre = pass.transform.position - fwd * 1.5f;
                entrance = centre - fwd * 6f;
                entrance.y = player.position.y;
            }

            var spots = new Vector3[ServingPass.SlotCount];
            for (int i = 0; i < spots.Length; i++)
            {
                spots[i] = centre - fwd * 0.95f + right * ((i - (spots.Length - 1) / 2f) * 1.1f);
                spots[i].y = entrance.y;
            }
            var manager = new GameObject("OrderManager").AddComponent<OrderManager>();
            manager.Init(pass, entrance, spots, fwd);
        }

        private static Vector3 Flat(Vector3 v) => Vector3.ProjectOnPlane(v, Vector3.up).normalized;

        private static Bounds Bounds(GameObject go) => go.GetComponentInChildren<Renderer>().bounds;

        private static T Place<T>(Transform root, string anchorName, float side, Transform player, int slot) where T : MonoBehaviour
        {
            GameObject anchor = GameObject.Find(anchorName);
            Renderer r = anchor != null ? anchor.GetComponentInChildren<Renderer>() : null;
            if (r == null) return Fallback<T>(root, anchorName, player, slot);

            // The restaurant's back wall is local +Z, so stations face the player at local -Z.
            Quaternion rot = Quaternion.LookRotation(Flat(anchor.transform.forward));
            Bounds b = r.bounds;
            Vector3 top = b.center + rot * Vector3.right * side;
            top.y = b.max.y + 0.5f;
            Vector3 pos = Physics.Raycast(top, Vector3.down, out RaycastHit hit, b.size.y + 1f, ~0, QueryTriggerInteraction.Ignore)
                ? hit.point
                : new Vector3(top.x, b.max.y, top.z);
            return Spawn<T>(root, pos, rot);
        }

        /// <summary>For things whose surface height is known from the anchor's bounds (shelf level, bin lid, pass table).</summary>
        private static T PlaceOn<T>(Transform root, string anchorName, System.Func<Bounds, float> surfaceY, Transform player, int slot) where T : MonoBehaviour
        {
            GameObject anchor = GameObject.Find(anchorName);
            Renderer r = anchor != null ? anchor.GetComponentInChildren<Renderer>() : null;
            if (r == null) return Fallback<T>(root, anchorName, player, slot);
            Bounds b = r.bounds;
            return Spawn<T>(root, new Vector3(b.center.x, surfaceY(b), b.center.z), Quaternion.LookRotation(Flat(anchor.transform.forward)));
        }

        private static T Fallback<T>(Transform root, string anchorName, Transform player, int slot) where T : MonoBehaviour
        {
            // No restaurant in this scene: a row of plain counters in front of the player.
            Debug.LogWarning($"MinigameBootstrap: '{anchorName}' not found, placing {typeof(T).Name} in front of the player");
            Quaternion rot = Quaternion.LookRotation(Flat(player.forward));
            Vector3 floor = player.position + rot * new Vector3((slot - 4) * 1.2f, 0f, 2.2f);
            PropFactory.Prim(PrimitiveType.Cube, "Counter_" + typeof(T).Name, root, floor + Vector3.up * 0.45f,
                new Vector3(1f, 0.9f, 0.8f), new Color(0.75f, 0.77f, 0.8f), rot.eulerAngles, keepCollider: true);
            return Spawn<T>(root, floor + Vector3.up * 0.9f, rot);
        }

        private static T Spawn<T>(Transform root, Vector3 pos, Quaternion rot) where T : MonoBehaviour
        {
            GameObject go = new GameObject(typeof(T).Name);
            go.transform.SetParent(root, false);
            go.transform.SetPositionAndRotation(pos, rot);
            return go.AddComponent<T>();
        }
    }
}
