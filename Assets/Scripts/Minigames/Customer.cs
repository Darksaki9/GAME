using UnityEngine;
using UnityEngine.AI;

namespace NutritionKitchen.Minigames
{
    /// <summary>
    /// An NPC customer: walks from the entrance to a counter spot, waits with an order bubble, then leaves.
    /// Uses the runtime-baked NavMesh when there is one, otherwise walks in a straight line.
    /// </summary>
    public class Customer : MonoBehaviour
    {
        private enum Phase { WalkingIn, Waiting, Leaving }

        [SerializeField] private float walkSpeed = 1.6f;

        public Order Order { get; set; }
        public bool Arrived => phase == Phase.Waiting;

        private Phase phase;
        private Vector3 target, exit, face;
        private NavMeshAgent agent;
        private Transform model;
        private string mood;
        private float moodUntil;

        public void Setup(GameObject prefab, Vector3 counterSpot, Vector3 faceDirection, Vector3 exitPoint)
        {
            model = Instantiate(prefab, transform, false).transform;
            model.localPosition = Vector3.zero;
            foreach (Collider c in model.GetComponentsInChildren<Collider>()) Destroy(c);
            target = counterSpot;
            face = faceDirection;
            exit = exitPoint;

            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                agent = gameObject.AddComponent<NavMeshAgent>();
                agent.Warp(hit.position);
                agent.speed = walkSpeed;
                agent.radius = 0.3f;
                agent.angularSpeed = 360f;
                agent.SetDestination(target);
            }
            phase = Phase.WalkingIn;
        }

        public void Leave(string reaction)
        {
            mood = reaction;
            moodUntil = Time.time + 4f;
            phase = Phase.Leaving;
            if (agent != null)
            {
                agent.isStopped = false;
                agent.SetDestination(exit);
            }
        }

        private void Update()
        {
            Vector3 goal = phase == Phase.Leaving ? exit : target;
            bool moving = phase != Phase.Waiting;

            if (agent != null)
            {
                if (moving && !agent.pathPending && agent.remainingDistance <= 0.35f) Reached();
            }
            else if (moving)
            {
                transform.position = Vector3.MoveTowards(transform.position, goal, walkSpeed * Time.deltaTime);
                Vector3 d = goal - transform.position;
                d.y = 0f;
                if (d.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(d);
                if (d.sqrMagnitude < 0.04f) Reached();
            }

            if (phase == Phase.Waiting)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(face), 5f * Time.deltaTime);

            // No walk animation on these models yet: a small bob reads as walking.
            float bob = moving ? Mathf.Abs(Mathf.Sin(Time.time * 9f)) * 0.04f : 0f;
            model.localPosition = new Vector3(0f, bob, 0f);
        }

        private void Reached()
        {
            if (phase == Phase.Leaving) { Destroy(gameObject); return; }
            phase = Phase.Waiting;
            if (agent != null) agent.isStopped = true;
        }

        private void OnGUI()
        {
            Camera cam = StationInteractor.PlayerCamera;
            Vector3 head = transform.position + Vector3.up * 2.1f;
            if (phase == Phase.Leaving && Time.time < moodUntil) MinigameUI.Bubble(cam, head, mood, 0f);
            else if (phase == Phase.Waiting && Order != null && !StationInteractor.InStation)
                MinigameUI.Bubble(cam, head, $"{Order.recipe.displayNameEng}\n({Order.recipe.customer})", 1f - Order.waited / Order.recipe.patienceSeconds);
        }
    }
}
