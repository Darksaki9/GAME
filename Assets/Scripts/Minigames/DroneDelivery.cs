using System.Collections.Generic;
using UnityEngine;
using NutritionKitchen.Crafting;

namespace NutritionKitchen.Minigames
{
    /// <summary>
    /// Sits on the "Drop ingredient" pad in the scene. Ordered ingredients arrive here by drone, one box per unit,
    /// and the pad's sign (its first child) gets an "ORDER ITEM HERE" label on both faces.
    /// </summary>
    public class DroneDelivery : MonoBehaviour
    {
        public static DroneDelivery Instance { get; private set; }

        [SerializeField] private float launchGap = 1.5f;   // seconds between drones when several are ordered at once
        [SerializeField] private float approachDistance = 35f;

        private readonly Queue<IngredientData> queue = new Queue<IngredientData>();
        private readonly List<Drone> flying = new List<Drone>();
        private float nextLaunch;
        private Bounds pad;

        private void Awake()
        {
            Instance = this;
            pad = GetComponent<Renderer>().bounds;
            if (transform.childCount > 0) LabelSign(transform.GetChild(0));
        }

        public void Dispatch(IngredientData i) => queue.Enqueue(i);

        /// <summary>Ordered but not on the pad yet (queued or still on a drone).</summary>
        public int InFlight(IngredientData i)
        {
            int n = 0;
            foreach (IngredientData q in queue) if (q == i) n++;
            foreach (Drone d in flying) if (d != null && d.Carrying(i)) n++;
            return n;
        }

        private void Update()
        {
            flying.RemoveAll(d => d == null);
            if (queue.Count == 0 || Time.time < nextLaunch) return;
            nextLaunch = Time.time + launchGap;

            // Land somewhere on the pad (kept off the edges) so boxes don't stack on one spot.
            Vector3 drop = new Vector3(Random.Range(pad.min.x + 0.6f, pad.max.x - 0.6f), pad.max.y + 0.2f, Random.Range(pad.min.z + 0.6f, pad.max.z - 0.6f));
            Vector3 dir = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward;
            Vector3 from = drop + dir * approachDistance + Vector3.up * 20f;
            Vector3 to = drop - dir * approachDistance + Vector3.up * 20f;
            flying.Add(Drone.Launch(queue.Dequeue(), drop, from, to));
        }

        /// <summary>Text on both faces of the sign. Left at the scene root: the sign and pad scales are non-uniform and would squash it.</summary>
        private static void LabelSign(Transform sign)
        {
            Vector3 size = sign.lossyScale;   // the sign is a unit cube scaled into a board
            var dark = new Color(0.08f, 0.1f, 0.14f);
            for (int face = 0; face < 2; face++)
            {
                var holder = new GameObject("SignLabel").transform;
                Quaternion rot = sign.rotation * Quaternion.Euler(0f, face * 180f, 0f);
                holder.SetPositionAndRotation(sign.position - rot * Vector3.forward * (size.z / 2 + 0.005f), rot);
                PropFactory.Text("ORDER ITEM\nHERE", holder, Vector3.zero, Mathf.Min(size.y * 0.22f, size.x * 0.12f), dark);
            }
        }
    }
}
