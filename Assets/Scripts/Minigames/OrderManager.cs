using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using NutritionKitchen.Crafting;

namespace NutritionKitchen.Minigames
{
    /// <summary>One customer's order: a recipe plus a plate slot on the pass that fills up component by component.</summary>
    public class Order
    {
        public RecipeSO recipe;
        public Customer customer;
        public int counterSpot;
        public int slot = -1;      // pass slot once the customer is at the counter
        public float waited;
        public FoodItem[] plated;

        public bool Active => slot >= 0;
        public bool Complete => plated.All(p => p != null);
    }

    /// <summary>
    /// Runs the shift: customers walk in and order a recipe for their health profile, the chef plates each
    /// component on the pass, and a complete plate is served and judged on safety, cooking, nutrients and speed.
    /// </summary>
    public class OrderManager : MonoBehaviour
    {
        public static OrderManager Instance { get; private set; }

        [SerializeField] private float firstCustomerDelay = 5f;
        [SerializeField] private float spawnInterval = 75f;
        [SerializeField] private int maxCustomers = 3;

        private RecipeBook book;
        private ServingPass pass;
        private Vector3 entrance, faceCounter;
        private Vector3[] counterSpots;
        private readonly List<Order> orders = new List<Order>();
        private float spawnTimer;

        // Shift stats (the full score matrix comes later).
        private int served, missed, poisoned, binned;
        private float satisfactionSum, stepWaste;

        public void Init(ServingPass servingPass, Vector3 entrancePoint, Vector3[] spots, Vector3 faceDirection)
        {
            pass = servingPass;
            entrance = entrancePoint;
            counterSpots = spots;
            faceCounter = faceDirection;
        }

        private void Awake()
        {
            Instance = this;
            book = RecipeBook.Load();
            if (book == null || book.recipes.Length == 0) Debug.LogWarning("OrderManager: Resources/RecipeBook missing or empty");
            spawnTimer = firstCustomerDelay;
        }

        private void OnEnable() => MinigameStation.StepFinished += OnStep;
        private void OnDisable() => MinigameStation.StepFinished -= OnStep;
        private void OnStep(MinigameResult r) => stepWaste += r.wastePct;

        private void Update()
        {
            if (book == null || pass == null) return;

            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0f)
            {
                spawnTimer = spawnInterval;
                if (orders.Count < maxCustomers) SpawnCustomer();
            }

            foreach (Order o in orders.ToArray())
            {
                if (!o.Active)
                {
                    if (o.customer.Arrived) Activate(o);
                    continue;
                }
                o.waited += Time.deltaTime;
                if (o.waited >= o.recipe.patienceSeconds)
                {
                    missed++;
                    Close(o, "Too slow! I'm leaving.");
                    MinigameUI.ShowToast($"A {o.recipe.customer} customer gave up waiting for {o.recipe.displayNameEng}.");
                }
            }
        }

        private void SpawnCustomer()
        {
            int spot = FirstFree(counterSpots.Length, i => orders.All(o => o.counterSpot != i));
            if (spot < 0) return;
            RecipeSO recipe = book.recipes[Random.Range(0, book.recipes.Length)];
            GameObject prefab = recipe.customerPrefabs.Length > 0 ? recipe.customerPrefabs[Random.Range(0, recipe.customerPrefabs.Length)] : null;
            if (prefab == null) { Debug.LogWarning($"{recipe.name}: no customer prefab"); return; }

            var go = new GameObject($"Customer_{recipe.customer}");
            go.transform.position = entrance;
            var customer = go.AddComponent<Customer>();
            var order = new Order { recipe = recipe, customer = customer, counterSpot = spot, plated = new FoodItem[recipe.components.Length] };
            customer.Order = order;
            customer.Setup(prefab, counterSpots[spot], faceCounter, entrance);
            orders.Add(order);
        }

        private void Activate(Order o)
        {
            o.slot = FirstFree(ServingPass.SlotCount, i => orders.All(x => x.slot != i));
            if (o.slot < 0) return; // pass full, try again next frame
            pass.ShowPlate(o.slot, true);
            MinigameUI.ShowToast($"New order: {o.recipe.displayNameEng}\nfor a {o.recipe.customer} customer: {o.recipe.customerNeed}", 6f);
        }

        /// <summary>Gram target for the first open order that still needs this ingredient portioned.</summary>
        public float PortionTarget(IngredientData ingredient)
        {
            foreach (Order o in orders.Where(o => o.Active))
                for (int i = 0; i < o.recipe.components.Length; i++)
                {
                    RecipeComponent c = o.recipe.components[i];
                    if (o.plated[i] == null && c.ingredient == ingredient && c.steps.Contains(StepKind.Portion)) return c.portionGrams;
                }
            return 0f;
        }

        public void TryPlate(PlayerHands hands)
        {
            FoodItem item = hands.Held;
            if (item.Ruined) { MinigameUI.ShowToast("That's ruined: put it in the bin."); return; }

            foreach (Order o in orders.Where(o => o.Active))
                for (int i = 0; i < o.recipe.components.Length; i++)
                    if (o.plated[i] == null && item.Matches(o.recipe.components[i]))
                    {
                        hands.Take();
                        o.plated[i] = item;
                        pass.PlaceOnPlate(o.slot, item, i, o.plated.Length);
                        if (o.Complete) Serve(o);
                        return;
                    }

            // Tell the player what the orders actually want for this ingredient.
            var wanted = orders.Where(o => o.Active)
                .SelectMany(o => o.recipe.components.Where((c, i) => o.plated[i] == null && c.ingredient == item.Ingredient))
                .Select(c => string.Join(" > ", c.steps)).Distinct().ToList();
            MinigameUI.ShowToast(wanted.Count == 0
                ? $"No order needs {item.Ingredient.displayNameEng} right now."
                : $"{item.Ingredient.displayNameEng} must be: {string.Join("  or  ", wanted)}\n(yours: {(item.steps.Count == 0 ? "raw" : string.Join(" > ", item.steps))})");
        }

        private void Serve(Order o)
        {
            List<MinigameResult> results = o.plated.SelectMany(p => p.results).ToList();
            Contamination bad = o.plated.Select(p => p.Contamination).FirstOrDefault(c => c != Contamination.None);
            float quality = results.Count > 0 ? results.Average(r => r.accuracy) : 0f;
            float nutrients = results.Count > 0 ? results.Average(r => r.nutrientRetention) : 0f;
            float speed = 1f - o.waited / o.recipe.patienceSeconds;

            served++;
            float satisfaction;
            string reaction;
            if (bad != Contamination.None)
            {
                poisoned++;
                satisfaction = 0f;
                reaction = "I feel sick...";
                MinigameUI.ShowToast($"Served {o.recipe.displayNameEng}, but it was unsafe ({bad})!\nThe {o.recipe.customer} customer got food poisoning.", 6f);
            }
            else
            {
                satisfaction = 0.45f * quality + 0.3f * nutrients + 0.25f * speed;
                reaction = satisfaction > 0.75f ? "Delicious, thank you!" : satisfaction > 0.5f ? "Not bad." : "Hmm...";
                MinigameUI.ShowToast($"Served {o.recipe.displayNameEng}: {satisfaction:P0} satisfied\n" +
                                     $"cooking {quality:P0} · nutrients kept {nutrients:P0} · speed {speed:P0}", 6f);
            }
            satisfactionSum += satisfaction;
            Close(o, reaction);
        }

        private void Close(Order o, string reaction)
        {
            if (o.Active) pass.ShowPlate(o.slot, false); // destroys whatever was plated
            o.customer.Leave(reaction);
            orders.Remove(o);
        }

        private static int FirstFree(int count, System.Func<int, bool> free)
        {
            for (int i = 0; i < count; i++) if (free(i)) return i;
            return -1;
        }

        public void RecordBinned(FoodItem item) => binned++;

        private void OnGUI()
        {
            if (StationInteractor.InStation) return;
            var sb = new StringBuilder($"<b>Orders</b>   served {served} · missed {missed} · sick {poisoned} · " +
                                       $"happy {(served > 0 ? satisfactionSum / served : 0f):P0} · binned {binned} · step waste {stepWaste:0.0}" +
                                       (Pantry.Instance != null ? $" · budget {Pantry.Instance.Money} baht\n" : "\n"));
            foreach (Order o in orders.Where(o => o.Active))
            {
                int left = Mathf.CeilToInt(o.recipe.patienceSeconds - o.waited);
                sb.Append($"\n<b>Plate {o.slot + 1}: {o.recipe.displayNameEng}</b>  ({o.recipe.customer}, {left / 60}:{left % 60:00})\n");
                for (int i = 0; i < o.recipe.components.Length; i++)
                {
                    RecipeComponent c = o.recipe.components[i];
                    string line = $"{c.ingredient.displayNameEng}: {string.Join(" > ", c.steps)}" +
                                  (c.steps.Contains(StepKind.Portion) ? $" {c.portionGrams:0} g" : "");
                    sb.Append(o.plated[i] != null ? $"  <color=#66dd77>[x] {line}</color>\n" : $"  [  ] {line}\n");
                }
            }
            if (orders.Count == 0) sb.Append($"\nNext customer in {Mathf.CeilToInt(spawnTimer)} s");
            MinigameUI.Tickets(sb.ToString().TrimEnd());
        }
    }
}
