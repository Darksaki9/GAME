using System.Collections.Generic;
using UnityEngine;
using NutritionKitchen.Crafting;

namespace NutritionKitchen.Minigames
{
    /// <summary>
    /// Makes a small picture of each ingredient by photographing its 3D model once, off-screen, with a temporary camera.
    /// shortcut: rendered at runtime; bake them to PNGs if real icon art never arrives.
    /// </summary>
    public static class IngredientIcons
    {
        private const int IconLayer = 29;          // unused layer, so the icon camera sees only the model
        private const int Size = 128;
        private static readonly Color Paper = new Color(0.97f, 0.96f, 0.92f);
        private static readonly Vector3 Stage = new Vector3(0f, -500f, 0f);
        private static readonly Dictionary<IngredientData, Texture2D> cache = new Dictionary<IngredientData, Texture2D>();

        /// <summary>The icon texture. It starts blank and gets the picture a second later.</summary>
        public static Texture Get(IngredientData ing)
        {
            if (cache.TryGetValue(ing, out Texture2D t) && t != null) return t;

            var icon = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { name = "Icon_" + ing.ingredientId };
            var pixels = new Color[Size * Size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Paper;
            icon.SetPixels(pixels);
            icon.Apply();

            var rt = new RenderTexture(Size, Size, 16);
            var stage = new GameObject("IconStage").transform;
            stage.position = Stage + Vector3.right * (10f * cache.Count); // own spot, so cameras don't see each other's models
            PropFactory.IngredientModel(ing, stage, Vector3.zero, 0.7f);
            foreach (Transform c in stage.GetComponentsInChildren<Transform>(true)) c.gameObject.layer = IconLayer;

            var cam = new GameObject("IconCamera").AddComponent<Camera>();
            cam.transform.position = stage.position + new Vector3(0f, 0.7f, -1.6f);
            cam.transform.LookAt(stage.position);
            cam.fieldOfView = 30f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Paper;
            cam.cullingMask = 1 << IconLayer;
            cam.targetTexture = rt;
            cam.depth = -100;
            // URP ignores Camera.Render() and the one-shot render request came out black, and the RenderTexture is
            // emptied once the camera goes: let it draw normal frames for a while, then copy the picture out.
            cam.gameObject.AddComponent<Capture>().Init(rt, icon, stage.gameObject);
            cache[ing] = icon;
            return icon;
        }

        private class Capture : MonoBehaviour
        {
            private RenderTexture rt;
            private Texture2D icon;
            private GameObject stage;
            private int frames = 30; // frames, not seconds: the first frame of Play counts all the loading time

            public void Init(RenderTexture source, Texture2D target, GameObject model)
            {
                rt = source;
                icon = target;
                stage = model;
            }

            private void Update()
            {
                if (--frames > 0) return;
                RenderTexture prev = RenderTexture.active;
                RenderTexture.active = rt;
                icon.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
                icon.Apply();
                RenderTexture.active = prev;
                GetComponent<Camera>().targetTexture = null;
                rt.Release();
                Destroy(rt);
                Destroy(stage);
                Destroy(gameObject);
            }
        }
    }
}
