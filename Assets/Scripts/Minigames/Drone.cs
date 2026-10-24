using System.Collections.Generic;
using UnityEngine;
using NutritionKitchen.Crafting;

namespace NutritionKitchen.Minigames
{
    /// <summary>
    /// A quadcopter that flies in from the sky, hovers down over a drop point, lets go of its DeliveryBox and flies away.
    /// </summary>
    public class Drone : MonoBehaviour
    {
        private enum Phase { FlyIn, Descend, Drop, Leave }

        private const float CruiseSpeed = 7f, DescendSpeed = 2f, DropHeight = 2.2f;

        public IngredientData Ingredient { get; private set; }

        private Phase phase;
        private Vector3 hoverPoint, dropPoint, exitPoint;
        private float dropTimer;
        private DeliveryBox box;
        private readonly List<Transform> rotors = new List<Transform>();

        public static Drone Launch(IngredientData ingredient, Vector3 dropAt, Vector3 from, Vector3 to)
        {
            var go = new GameObject("DeliveryDrone_" + ingredient.ingredientId);
            go.transform.position = from;
            var d = go.AddComponent<Drone>();
            d.Ingredient = ingredient;
            d.dropPoint = dropAt + Vector3.up * DropHeight;
            d.hoverPoint = dropAt + Vector3.up * 8f;
            d.exitPoint = to;
            d.Build();
            return d;
        }

        private void Build()
        {
            Color shell = new Color(0.92f, 0.93f, 0.95f), dark = new Color(0.12f, 0.13f, 0.15f), teal = new Color(0.13f, 0.62f, 0.60f);
            PropFactory.Prim(PrimitiveType.Cube, "Body", transform, Vector3.zero, new Vector3(0.45f, 0.14f, 0.45f), shell);
            PropFactory.Prim(PrimitiveType.Cube, "Stripe", transform, new Vector3(0f, 0.071f, 0f), new Vector3(0.46f, 0.01f, 0.08f), teal);
            for (int i = 0; i < 4; i++)
            {
                float a = 45f + i * 90f;
                Vector3 dir = Quaternion.Euler(0f, a, 0f) * Vector3.forward;
                PropFactory.Prim(PrimitiveType.Cube, "Arm", transform, dir * 0.35f, new Vector3(0.05f, 0.04f, 0.5f), dark, new Vector3(0f, a, 0f));
                PropFactory.Prim(PrimitiveType.Cylinder, "Motor", transform, dir * 0.58f + Vector3.up * 0.04f, new Vector3(0.08f, 0.04f, 0.08f), dark);
                Transform rotor = PropFactory.Prim(PrimitiveType.Cube, "Rotor", transform, dir * 0.58f + Vector3.up * 0.09f, new Vector3(0.42f, 0.008f, 0.04f), new Color(0.2f, 0.2f, 0.22f)).transform;
                rotors.Add(rotor);
            }
            box = DeliveryBox.Create(Ingredient, transform);
            box.transform.localPosition = new Vector3(0f, -0.3f, 0f);
            phase = Phase.FlyIn;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < rotors.Count; i++) rotors[i].Rotate(0f, (i % 2 == 0 ? 1f : -1f) * 1800f * dt, 0f, Space.Self);

            switch (phase)
            {
                case Phase.FlyIn:
                    if (MoveTo(hoverPoint, CruiseSpeed, dt)) phase = Phase.Descend;
                    break;
                case Phase.Descend:
                    if (MoveTo(dropPoint, DescendSpeed, dt)) { phase = Phase.Drop; dropTimer = 0.6f; }
                    break;
                case Phase.Drop:
                    dropTimer -= dt; // a short hover so the drop reads clearly
                    if (dropTimer <= 0f)
                    {
                        box.Release();
                        box = null;
                        phase = Phase.Leave;
                        MinigameUI.ShowToast($"Drone delivered {Ingredient.displayNameEng} to the drop pad.", 3f);
                    }
                    break;
                case Phase.Leave:
                    if (MoveTo(exitPoint, CruiseSpeed, dt)) Destroy(gameObject);
                    break;
            }
        }

        /// <summary>Moves toward target, tilting into the direction of travel. True once arrived.</summary>
        private bool MoveTo(Vector3 target, float speed, float dt)
        {
            Vector3 to = target - transform.position;
            Vector3 flat = Vector3.ProjectOnPlane(to, Vector3.up);
            Quaternion lean = flat.sqrMagnitude > 0.01f ? Quaternion.AngleAxis(12f, Vector3.Cross(Vector3.up, flat.normalized)) : Quaternion.identity;
            transform.rotation = Quaternion.Slerp(transform.rotation, lean, 3f * dt);
            transform.position = Vector3.MoveTowards(transform.position, target, speed * dt);
            return to.sqrMagnitude < 0.01f;
        }

        public bool Carrying(IngredientData i) => box != null && Ingredient == i;
    }
}
