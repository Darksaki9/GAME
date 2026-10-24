using System;
using UnityEngine;

namespace NutritionKitchen.Minigames
{
    /// <summary>A button on an in-world screen: aim the crosshair at it and left-click (StationInteractor does the aiming).</summary>
    public class ScreenButton : MonoBehaviour
    {
        public Action onPress;
        public string hint;            // shown under the crosshair while aimed at
        public Color normal, hover;

        private bool hovered;

        /// <summary>Called every frame the crosshair is on this button.</summary>
        public void Hover() => hovered = true;

        public void Press() => onPress?.Invoke();

        private void LateUpdate()
        {
            PropFactory.SetColor(gameObject, hovered ? hover : normal);
            hovered = false; // must be re-hovered next frame
        }
    }
}
