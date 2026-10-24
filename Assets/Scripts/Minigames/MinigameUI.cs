using UnityEngine;

namespace NutritionKitchen.Minigames
{
    /// <summary>
    /// IMGUI drawing for station HUDs.
    /// shortcut: IMGUI needs no Canvas/prefab setup; move to uGUI/UI Toolkit once the art style is decided.
    /// </summary>
    public static class MinigameUI
    {
        public static readonly Color Good = new Color(0.30f, 0.80f, 0.40f);
        public static readonly Color Warn = new Color(0.95f, 0.70f, 0.20f);
        public static readonly Color Bad = new Color(0.90f, 0.25f, 0.20f);
        public static readonly Color Info = new Color(0.30f, 0.60f, 0.95f);

        private static GUIStyle label;

        private static GUIStyle Style(int size, TextAnchor anchor = TextAnchor.UpperLeft)
        {
            if (label == null) label = new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true };
            label.fontSize = Mathf.RoundToInt(size * Screen.height / 1080f);
            label.alignment = anchor;
            return label;
        }

        private static float S(float v) => v * Screen.height / 1080f; // design at 1080p, scale to screen

        public static void Box(Rect r, Color c)
        {
            Color old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = old;
        }

        public static void Title(string text)
        {
            Box(new Rect(0, 0, Screen.width, S(56)), new Color(0, 0, 0, 0.6f));
            GUI.Label(new Rect(S(20), S(10), Screen.width, S(46)), text, Style(28));
        }

        public static void Help(string text)
        {
            Rect r = new Rect(S(20), S(76), S(430), S(40) + S(30) * (text.Split('\n').Length));
            Box(r, new Color(0, 0, 0, 0.55f));
            GUI.Label(new Rect(r.x + S(14), r.y + S(10), r.width - S(20), r.height), text, Style(22));
        }

        public static void Center(string text, int size, Color? color = null)
        {
            Color old = GUI.contentColor;
            GUI.contentColor = color ?? Color.white;
            GUI.Label(new Rect(0, Screen.height * 0.4f, Screen.width, S(80)), text, Style(size, TextAnchor.MiddleCenter));
            GUI.contentColor = old;
        }

        /// <summary>Horizontal meter in the bottom HUD area. row 0 is the lowest. zone = green "golden" band.</summary>
        public static void Meter(int row, string name, float value01, Color fill, float zoneMin = -1f, float zoneMax = -1f, string valueText = null)
        {
            float w = S(640), h = S(30);
            Rect r = new Rect((Screen.width - w) * 0.5f, Screen.height - S(70) - row * S(62), w, h);
            GUI.Label(new Rect(r.x, r.y - S(28), w, S(28)), $"{name}  <b>{valueText ?? Mathf.RoundToInt(value01 * 100f) + "%"}</b>", Style(20));
            Box(r, new Color(0, 0, 0, 0.6f));
            if (zoneMin >= 0f) Box(new Rect(r.x + w * zoneMin, r.y, w * (zoneMax - zoneMin), h), new Color(0.3f, 0.9f, 0.4f, 0.35f));
            Box(new Rect(r.x + S(3), r.y + S(3), (w - S(6)) * Mathf.Clamp01(value01), h - S(6)), fill);
        }

        /// <summary>A marker on a meter row (e.g. the knife position or a target line).</summary>
        public static void Marker(int row, float pos01, Color c, float widthPx = 4f)
        {
            float w = S(640), h = S(30);
            Rect r = new Rect((Screen.width - w) * 0.5f, Screen.height - S(70) - row * S(62), w, h);
            Box(new Rect(r.x + w * Mathf.Clamp01(pos01) - S(widthPx) * 0.5f, r.y - S(6), S(widthPx), h + S(12)), c);
        }

        /// <summary>Short status text above the meters.</summary>
        public static void Status(string text, Color c, int rowsBelow)
        {
            Color old = GUI.contentColor;
            GUI.contentColor = c;
            GUI.Label(new Rect(0, Screen.height - S(130) - rowsBelow * S(62), Screen.width, S(40)), text, Style(26, TextAnchor.MiddleCenter));
            GUI.contentColor = old;
        }

        public static void Result(MinigameResult r, bool failed)
        {
            float w = S(620);
            Rect box = new Rect(Screen.width - w - S(20), S(76), w, S(150) + S(30) * r.notes.Count);
            Box(box, new Color(0, 0, 0, 0.65f));
            string head = failed ? "<color=#ff6655><b>FAILED</b></color>" : "<color=#66dd77><b>DONE</b></color>";
            string body = $"{head}  {(r.ingredient ? r.ingredient.displayNameEng : "")}\n" +
                          $"Accuracy {r.accuracy:P0}    Nutrients kept {r.nutrientRetention:P0}    Waste {r.wastePct:P0}\n" +
                          $"Contamination: {(r.contamination == Contamination.None ? "none" : "<color=#ff6655>" + r.contamination + "</color>")}\n";
            foreach (string n in r.notes) body += "• " + n + "\n";
            GUI.Label(new Rect(box.x + S(14), box.y + S(10), w - S(28), box.height), body, Style(21));
        }

        /// <summary>Small "[E] Use ..." prompt in the middle of the screen (multi-line is fine).</summary>
        public static void Prompt(string text)
        {
            float h = S(34) * text.Split('\n').Length + S(10);
            Rect r = new Rect(Screen.width * 0.5f - S(450), Screen.height * 0.55f, S(900), h);
            Box(r, new Color(0, 0, 0, 0.45f));
            GUI.Label(r, text, Style(24, TextAnchor.MiddleCenter));
        }

        /// <summary>Small dot in the middle of the screen; grows and turns teal over something clickable.</summary>
        public static void Crosshair(bool overButton)
        {
            float s = S(overButton ? 12 : 6);
            Box(new Rect((Screen.width - s) / 2, (Screen.height - s) / 2, s, s), overButton ? new Color(0.3f, 0.85f, 0.8f) : new Color(1f, 1f, 1f, 0.8f));
        }

        /// <summary>What the chef is carrying, bottom-left.</summary>
        public static void Holding(string text)
        {
            Rect r = new Rect(S(20), Screen.height - S(60), S(760), S(44));
            Box(r, new Color(0, 0, 0, 0.5f));
            GUI.Label(new Rect(r.x + S(12), r.y + S(8), r.width, r.height), text, Style(22));
        }

        private static string toast;
        private static float toastUntil;

        /// <summary>A short message in the upper middle of the screen for a few seconds.</summary>
        public static void ShowToast(string text, float seconds = 4f)
        {
            toast = text;
            toastUntil = Time.time + seconds;
        }

        public static void DrawToast()
        {
            if (toast == null || Time.time > toastUntil) return;
            Rect r = new Rect(Screen.width * 0.5f - S(420), S(130), S(840), S(40) + S(30) * toast.Split('\n').Length);
            Box(r, new Color(0, 0, 0, 0.7f));
            GUI.Label(r, toast, Style(24, TextAnchor.MiddleCenter));
        }

        /// <summary>Order tickets down the right side.</summary>
        public static void Tickets(string text)
        {
            float w = S(470);
            Rect r = new Rect(Screen.width - w - S(20), S(76), w, S(30) + S(27) * text.Split('\n').Length);
            Box(r, new Color(0, 0, 0, 0.55f));
            GUI.Label(new Rect(r.x + S(12), r.y + S(8), w - S(20), r.height), text, Style(19));
        }

        /// <summary>Speech bubble over a world position with a patience bar.</summary>
        public static void Bubble(Camera cam, Vector3 worldPos, string text, float patience01)
        {
            if (cam == null) return;
            Vector3 p = cam.WorldToScreenPoint(worldPos);
            if (p.z <= 0f) return; // behind the camera
            float w = S(260), h = S(58);
            Rect r = new Rect(p.x - w / 2, Screen.height - p.y - h, w, h);
            Box(r, new Color(1f, 1f, 1f, 0.85f));
            Color old = GUI.contentColor;
            GUI.contentColor = Color.black;
            GUI.Label(new Rect(r.x + S(6), r.y + S(2), w - S(12), S(36)), text, Style(16, TextAnchor.UpperCenter));
            GUI.contentColor = old;
            Box(new Rect(r.x + S(8), r.yMax - S(14), (w - S(16)) * Mathf.Clamp01(patience01), S(8)),
                patience01 > 0.5f ? Good : patience01 > 0.2f ? Warn : Bad);
        }
    }
}
