using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using NutritionKitchen.Crafting;

namespace NutritionKitchen.Minigames
{
    /// <summary>An ingredient the player carries around. Remembers every step done to it and how well.</summary>
    public class FoodItem : MonoBehaviour
    {
        public IngredientData Ingredient { get; private set; }
        public readonly List<StepKind> steps = new List<StepKind>();
        public readonly List<MinigameResult> results = new List<MinigameResult>();

        /// <summary>Burnt, abandoned or otherwise wasted: only fit for the bin.</summary>
        public bool Ruined => results.Any(r => r.wastePct >= 1f);
        /// <summary>Landed on the floor after a throw: not safe to serve any more.</summary>
        public bool droppedOnFloor;

        public Contamination Contamination => droppedOnFloor ? Contamination.SoilBacteria
            : results.Select(r => r.contamination).FirstOrDefault(c => c != Contamination.None);

        public static FoodItem Create(IngredientData ingredient)
        {
            var go = new GameObject("Held_" + ingredient.ingredientId);
            var item = go.AddComponent<FoodItem>();
            item.Ingredient = ingredient;
            PropFactory.IngredientModel(ingredient, go.transform, Vector3.zero, 0.14f);
            return item;
        }

        public void Record(StepKind kind, MinigameResult r)
        {
            steps.Add(kind);
            results.Add(r);
            if (Ruined)
                foreach (Renderer rend in GetComponentsInChildren<Renderer>()) PropFactory.SetColor(rend.gameObject, new Color(0.1f, 0.08f, 0.06f));
        }

        public bool Matches(RecipeComponent c) => c.ingredient == Ingredient && c.steps.SequenceEqual(steps);

        public string Describe()
        {
            string s = Ingredient.displayNameEng + (steps.Count == 0 ? " (raw)" : ": " + string.Join(" > ", steps));
            if (Ruined) s += "  <color=#ff6655>RUINED</color>";
            else if (droppedOnFloor) s += "  <color=#ff6655>Dropped on the floor!</color>";
            else if (Contamination != Contamination.None) s += $"  <color=#ff6655>{Contamination}!</color>";
            return s;
        }
    }
}
