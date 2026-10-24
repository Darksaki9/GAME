using UnityEngine;
using UnityEngine.InputSystem;

namespace NutritionKitchen.Minigames
{
    /// <summary>What the chef is carrying: one food item, held in front of the camera.</summary>
    public class PlayerHands : MonoBehaviour
    {
        public FoodItem Held { get; private set; }
        private Transform holdPoint;

        private void Awake()
        {
            holdPoint = new GameObject("HoldPoint").transform;
            holdPoint.SetParent(GetComponentInChildren<Camera>().transform, false);
            holdPoint.localPosition = new Vector3(0.22f, -0.2f, 0.45f);
        }

        public void Hold(FoodItem item)
        {
            Held = item;
            item.transform.SetParent(holdPoint, false);
            item.transform.localPosition = Vector3.zero;
            item.transform.localRotation = Quaternion.identity;
            item.gameObject.SetActive(true);
        }

        /// <summary>Takes the item out of the hands; the caller decides where it goes.</summary>
        public FoodItem Take()
        {
            FoodItem item = Held;
            Held = null;
            if (item != null) item.transform.SetParent(null, true);
            return item;
        }

        [SerializeField] private float throwSpeed = 5f;

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb != null && Held != null && !StationInteractor.InStation && kb.qKey.wasPressedThisFrame) Throw();
        }

        /// <summary>Q: throws the held item forward; it becomes a DroppedFood that can be picked up with E.</summary>
        private void Throw()
        {
            FoodItem item = Take();
            item.transform.SetPositionAndRotation(holdPoint.position, holdPoint.rotation);
            item.transform.localScale = Vector3.one;

            var col = item.gameObject.AddComponent<SphereCollider>();
            col.radius = 0.07f;
            Physics.IgnoreCollision(col, GetComponent<CharacterController>()); // don't hit our own capsule on the way out
            var rb = item.gameObject.AddComponent<Rigidbody>();
            rb.mass = 0.3f;
            rb.linearVelocity = holdPoint.forward * throwSpeed + Vector3.up * 1.5f;
            item.gameObject.AddComponent<DroppedFood>().Init(transform.position.y);
        }

        /// <summary>Hide/show the held item while a station's close-up camera is on.</summary>
        public void SetVisible(bool visible)
        {
            if (Held != null) Held.gameObject.SetActive(visible);
        }

        private void OnGUI()
        {
            MinigameUI.Holding(Held != null ? "Holding: " + Held.Describe() + "     [Q] throw" : "Hands empty");
            MinigameUI.DrawToast();
        }
    }
}
