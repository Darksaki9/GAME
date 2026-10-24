using System.Collections.Generic;
using UnityEngine;
using NutritionKitchen.Crafting;

namespace NutritionKitchen.Minigames
{
    /// <summary>Simple low-poly station props built from primitives, same approach as the character builders.</summary>
    public static class PropFactory
    {
        private static readonly Dictionary<Color, Material> cache = new Dictionary<Color, Material>();

        // shortcut: Shader.Find works in the editor; a build needs the URP Lit shader referenced (it is, via the project's materials).
        public static Material Mat(Color c, float smoothness = 0.4f)
        {
            if (cache.TryGetValue(c, out Material m) && m != null) return m;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            m = new Material(shader) { color = c };
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            cache[c] = m;
            return m;
        }

        public static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 localScale,
                                      Color color, Vector3 localEuler = default, bool keepCollider = false)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(localEuler);
            go.transform.localScale = localScale;
            if (!keepCollider) Object.Destroy(go.GetComponent<Collider>());
            go.GetComponent<MeshRenderer>().sharedMaterial = Mat(color);
            return go;
        }

        private static Font font;
        private static Material textMaterial;

        /// <summary>World-space text, readable from the parent's -Z side. height = cap height in metres.</summary>
        public static TextMesh Text(string text, Transform parent, Vector3 localPos, float height, Color color, TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (textMaterial == null)
            {
                // The font's own material draws on top of everything; this shader depth-tests so walls and boards hide text behind them.
                Shader text3D = Shader.Find("NutritionKitchen/Text3D");
                textMaterial = text3D != null ? new Material(text3D) { mainTexture = font.material.mainTexture } : font.material;
            }
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var tm = go.AddComponent<TextMesh>();
            tm.font = font;
            tm.fontSize = 96;                     // big glyph texture, scaled down: stays crisp
            tm.characterSize = height * 10f / tm.fontSize;
            tm.anchor = anchor;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            tm.text = text;
            go.GetComponent<MeshRenderer>().sharedMaterial = textMaterial;
            return tm;
        }

        /// <summary>Per-object material so its colour can change at runtime (searing, cleaning...).</summary>
        public static void SetColor(GameObject go, Color c)
        {
            Material m = go.GetComponent<Renderer>().material; // instance
            m.color = c;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        }

        /// <summary>The ingredient's own model, fitted into a cube of 'size' metres; a coloured sphere if it has none.</summary>
        public static GameObject IngredientModel(IngredientData ing, Transform parent, Vector3 localPos, float size)
        {
            if (ing == null || ing.prefab == null)
                return Prim(PrimitiveType.Sphere, "Ingredient", parent, localPos, Vector3.one * size, ing ? ing.themeColor : Color.white);

            GameObject go = Object.Instantiate(ing.prefab, parent, false);
            go.name = "Ingredient";
            foreach (Collider c in go.GetComponentsInChildren<Collider>()) Object.Destroy(c);
            foreach (Rigidbody rb in go.GetComponentsInChildren<Rigidbody>()) Object.Destroy(rb);

            go.transform.localPosition = Vector3.zero;
            go.transform.localScale = Vector3.one;
            Bounds b = new Bounds();
            bool first = true;
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>())
            {
                if (first) { b = r.bounds; first = false; }
                else b.Encapsulate(r.bounds);
            }
            float max = Mathf.Max(b.size.x, b.size.y, b.size.z);
            float k = max > 0.0001f ? size * parent.lossyScale.x / max : 1f;
            Vector3 centreOffset = (b.center - go.transform.position) * k;
            go.transform.localScale = Vector3.one * k;
            go.transform.position = parent.TransformPoint(localPos) - centreOffset; // model's centre lands on localPos
            return go;
        }
    }
}
