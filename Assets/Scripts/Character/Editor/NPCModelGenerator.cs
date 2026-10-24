using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NutritionKitchen.Character.EditorTools
{
    public enum NPCCategory
    {
        Child,
        Adolescent,
        ThinAdult,
        OverweightAdult,
        AverageFitAdult,
        Elderly
    }

    public enum NPCGender
    {
        Male,
        Female
    }

    /// <summary>
    /// สร้างโมเดล NPC สไตล์การ์ตูน 3D (อ้างอิงภาพ TurboSquid) แล้วรวมทุกชิ้นส่วนเป็น Mesh เดียว
    /// ต่อ 1 ตัวละคร = 1 GameObject + 1 MeshFilter/MeshRenderer จึงย้าย/เลือกได้ง่ายเป็นก้อนเดียว
    /// </summary>
    public static class NPCModelGenerator
    {
        private struct Part
        {
            public Mesh mesh;
            public Matrix4x4 matrix;
            public Color color;
        }

        private struct Spec
        {
            public float H, head, shoulder, torsoW, torsoD, armR, legR, belly, hipMul, shoulderF, hipF;
            public Color shirt, pants, hair;
        }

        private static readonly List<Part> parts = new List<Part>();
        private static Mesh sphereMesh, capsuleMesh, cylinderMesh, cubeMesh, torusMesh;

        private static readonly Color Skin = new Color(0.93f, 0.74f, 0.64f);
        private static readonly Color Shoe = new Color(0.78f, 0.56f, 0.35f);
        private static readonly Color Frame = new Color(0.12f, 0.12f, 0.14f);

        // ---------------------------------------------------------------- primitives

        private static Mesh GetPrimitiveMesh(PrimitiveType type)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            Mesh m = go.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(go);
            return m;
        }

        /// <summary>Revolves a (radius, y) profile around the Y axis with analytic smooth normals.</summary>
        private static Mesh Revolve(List<Vector2> prof, bool closed, int seg)
        {
            int n = prof.Count;
            List<Vector3> v = new List<Vector3>();
            List<Vector3> nr = new List<Vector3>();
            List<int> tri = new List<int>();
            for (int i = 0; i < n; i++)
            {
                Vector2 p0 = prof[closed ? (i + n - 1) % n : Mathf.Max(i - 1, 0)];
                Vector2 p1 = prof[closed ? (i + 1) % n : Mathf.Min(i + 1, n - 1)];
                Vector2 t = p1 - p0;
                Vector2 nn = new Vector2(t.y, -t.x).normalized;
                for (int j = 0; j <= seg; j++)
                {
                    float phi = j / (float)seg * Mathf.PI * 2f;
                    float c = Mathf.Cos(phi), sn = Mathf.Sin(phi);
                    v.Add(new Vector3(prof[i].x * c, prof[i].y, prof[i].x * sn));
                    nr.Add(new Vector3(nn.x * c, nn.y, nn.x * sn));
                }
            }
            int rows = closed ? n : n - 1;
            for (int i = 0; i < rows; i++)
            {
                int i2 = (i + 1) % n;
                for (int j = 0; j < seg; j++)
                {
                    int a = i * (seg + 1) + j, b = a + 1, c = i2 * (seg + 1) + j, d = c + 1;
                    tri.Add(a); tri.Add(c); tri.Add(b);
                    tri.Add(b); tri.Add(c); tri.Add(d);
                }
            }
            Mesh m = new Mesh();
            m.SetVertices(v); m.SetNormals(nr); m.SetTriangles(tri, 0);
            m.RecalculateBounds();
            return m;
        }

        private static Mesh MakeSphere()
        {
            List<Vector2> p = new List<Vector2>();
            const int st = 24;
            for (int i = 0; i <= st; i++)
            {
                float a = -Mathf.PI * 0.5f + Mathf.PI * i / st;
                p.Add(new Vector2(Mathf.Cos(a) * 0.5f, Mathf.Sin(a) * 0.5f));
            }
            return Revolve(p, false, 40);
        }

        /// <summary>Same dimensions as the Unity capsule primitive (radius 0.5, height 2).</summary>
        private static Mesh MakeCapsule()
        {
            List<Vector2> p = new List<Vector2>();
            const int st = 12;
            for (int i = 0; i <= st; i++)
            {
                float a = -Mathf.PI * 0.5f + Mathf.PI * 0.5f * i / st;
                p.Add(new Vector2(Mathf.Cos(a) * 0.5f, Mathf.Sin(a) * 0.5f - 0.5f));
            }
            for (int i = 0; i <= st; i++)
            {
                float a = Mathf.PI * 0.5f * i / st;
                p.Add(new Vector2(Mathf.Cos(a) * 0.5f, Mathf.Sin(a) * 0.5f + 0.5f));
            }
            return Revolve(p, false, 32);
        }

        private static Mesh MakeTorus()
        {
            List<Vector2> p = new List<Vector2>();
            const int st = 12;
            for (int i = 0; i < st; i++)
            {
                float a = Mathf.PI * 2f * i / st;
                p.Add(new Vector2(0.45f + Mathf.Cos(a) * 0.05f, Mathf.Sin(a) * 0.05f));
            }
            return Revolve(p, true, 32);
        }

        private static Mesh MakeSmoothTorsoMesh(float height, float torsoW, float shoulderW, float torsoD, float belly, float hipMul)
        {
            const int vRings = 24;
            const int hSegs = 32;

            List<Vector3> verts = new List<Vector3>();
            List<int> tris = new List<int>();

            float botRx = torsoW * 0.95f * hipMul;
            float botRz = torsoD * 0.92f;
            Vector3 botPole = new Vector3(0, -botRz * 0.35f, 0);
            verts.Add(botPole);

            for (int i = 0; i <= vRings; i++)
            {
                float u = i / (float)vRings;
                float y = u * height;

                float rx = Mathf.Lerp(botRx, shoulderW * 1.08f, u);
                float rz = Mathf.Lerp(botRz, torsoD * 1.05f, u);
                float zFwd = 0f;

                if (belly > 0f)
                {
                    float bWeight = Mathf.Sin(Mathf.PI * Mathf.Pow(u, 0.72f));
                    rx += belly * 1.10f * bWeight;
                    rz += belly * 2.10f * bWeight;
                    zFwd = belly * 1.75f * bWeight;
                }
                else
                {
                    float chestW = Mathf.Sin(Mathf.PI * u) * 0.08f;
                    rx += torsoW * chestW;
                    rz += torsoD * chestW * 1.5f;
                    zFwd = torsoD * chestW * 0.8f;
                }

                if (u > 0.88f)
                {
                    float domeT = (u - 0.88f) / 0.12f;
                    rx *= Mathf.Lerp(1f, 0.65f, domeT);
                    rz *= Mathf.Lerp(1f, 0.70f, domeT);
                }

                for (int j = 0; j < hSegs; j++)
                {
                    float phi = j / (float)hSegs * Mathf.PI * 2f;
                    float cosP = Mathf.Cos(phi);
                    float sinP = Mathf.Sin(phi);

                    Vector3 pos = new Vector3(rx * cosP, y, zFwd + rz * sinP);
                    verts.Add(pos);
                }
            }

            float topY = height + torsoD * 0.22f;
            Vector3 topPole = new Vector3(0, topY, 0);
            verts.Add(topPole);
            int topPoleIdx = verts.Count - 1;

            for (int j = 0; j < hSegs; j++)
            {
                int nextJ = (j + 1) % hSegs;
                int v0 = 1 + j;
                int v1 = 1 + nextJ;
                tris.Add(0);
                tris.Add(v0);
                tris.Add(v1);
            }

            for (int i = 0; i < vRings; i++)
            {
                int r0 = 1 + i * hSegs;
                int r1 = 1 + (i + 1) * hSegs;
                for (int j = 0; j < hSegs; j++)
                {
                    int nextJ = (j + 1) % hSegs;
                    int a = r0 + j;
                    int b = r0 + nextJ;
                    int c = r1 + j;
                    int d = r1 + nextJ;

                    tris.Add(a); tris.Add(c); tris.Add(b);
                    tris.Add(b); tris.Add(c); tris.Add(d);
                }
            }

            int lastRing = 1 + vRings * hSegs;
            for (int j = 0; j < hSegs; j++)
            {
                int nextJ = (j + 1) % hSegs;
                int v0 = lastRing + j;
                int v1 = lastRing + nextJ;
                tris.Add(v0);
                tris.Add(topPoleIdx);
                tris.Add(v1);
            }

            Mesh m = new Mesh { name = "ProceduralSmoothTorso" };
            m.SetVertices(verts);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        private static Mesh MakeSkirtMesh(float length, float waistRx, float waistRz, float flare)
        {
            const int vRings = 16;
            const int hSegs = 32;

            List<Vector3> verts = new List<Vector3>();
            List<int> tris = new List<int>();

            verts.Add(Vector3.zero);

            for (int i = 0; i <= vRings; i++)
            {
                float t = i / (float)vRings;
                float y = -t * length;

                float f = 1f + (flare - 1f) * Mathf.Pow(t, 1.25f);
                float rx = waistRx * f;
                float rz = waistRz * f;

                for (int j = 0; j < hSegs; j++)
                {
                    float phi = j / (float)hSegs * Mathf.PI * 2f;
                    float c = Mathf.Cos(phi), sn = Mathf.Sin(phi);
                    verts.Add(new Vector3(rx * c, y, rz * sn));
                }
            }

            int botCenterIdx = verts.Count;
            verts.Add(new Vector3(0, -length, 0));

            // Top cap triangles (facing +Y)
            for (int j = 0; j < hSegs; j++)
            {
                int nextJ = (j + 1) % hSegs;
                tris.Add(0);
                tris.Add(1 + nextJ);
                tris.Add(1 + j);
            }

            // Outer quads (facing outward)
            for (int i = 0; i < vRings; i++)
            {
                int r0 = 1 + i * hSegs;
                int r1 = 1 + (i + 1) * hSegs;
                for (int j = 0; j < hSegs; j++)
                {
                    int nextJ = (j + 1) % hSegs;
                    int a = r0 + j;
                    int b = r0 + nextJ;
                    int c = r1 + j;
                    int d = r1 + nextJ;

                    tris.Add(a); tris.Add(b); tris.Add(c);
                    tris.Add(b); tris.Add(d); tris.Add(c);
                }
            }

            // Bottom cap triangles (facing -Y)
            int lastRing = 1 + vRings * hSegs;
            for (int j = 0; j < hSegs; j++)
            {
                int nextJ = (j + 1) % hSegs;
                tris.Add(botCenterIdx);
                tris.Add(lastRing + j);
                tris.Add(lastRing + nextJ);
            }

            Mesh m = new Mesh { name = "ProceduralSkirt" };
            m.SetVertices(verts);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        private static void EnsureMeshes()
        {
            if (sphereMesh == null) sphereMesh = MakeSphere();
            if (capsuleMesh == null) capsuleMesh = MakeCapsule();
            if (torusMesh == null) torusMesh = MakeTorus();
            if (cylinderMesh == null) cylinderMesh = GetPrimitiveMesh(PrimitiveType.Cylinder);
            if (cubeMesh == null) cubeMesh = GetPrimitiveMesh(PrimitiveType.Cube);
        }

        /// <summary>Ellipsoid; size = diameters on each axis.</summary>
        private static void Ellipsoid(Vector3 center, Vector3 size, Color color)
        {
            parts.Add(new Part { mesh = sphereMesh, matrix = Matrix4x4.TRS(center, Quaternion.identity, size), color = color });
        }

        /// <summary>Capsule running from A to B (A/B are the extreme ends).</summary>
        private static void Capsule(Vector3 a, Vector3 b, float radius, Color color)
        {
            Vector3 dir = b - a;
            float len = dir.magnitude;
            if (len < 0.0001f) return;
            Quaternion rot = Quaternion.FromToRotation(Vector3.up, dir);
            Vector3 scale = new Vector3(radius * 2f, len * 0.5f, radius * 2f);
            parts.Add(new Part { mesh = capsuleMesh, matrix = Matrix4x4.TRS((a + b) * 0.5f, rot, scale), color = color });
        }

        /// <summary>Thin ring facing +Z (used for glasses frames).</summary>
        private static void Disc(Vector3 center, float diameter, float thickness, Color color)
        {
            parts.Add(new Part
            {
                mesh = torusMesh,
                matrix = Matrix4x4.TRS(center, Quaternion.Euler(90f, 0f, 0f), new Vector3(diameter * 1.1f, diameter * 0.7f, diameter * 1.1f)),
                color = color
            });
        }

        private static void Box(Vector3 center, Vector3 size, Color color)
        {
            parts.Add(new Part { mesh = cubeMesh, matrix = Matrix4x4.TRS(center, Quaternion.identity, size), color = color });
        }

        // ---------------------------------------------------------------- specs

        private static Spec GetSpec(NPCCategory cat, NPCGender gen)
        {
            bool f = gen == NPCGender.Female;
            Spec s = new Spec { hipMul = 1f, shoulderF = 0.82f, hipF = 0.47f };
            s.hair = (cat == NPCCategory.Elderly) ? new Color(0.82f, 0.82f, 0.84f)
                   : f ? new Color(0.50f, 0.28f, 0.30f) : new Color(0.45f, 0.26f, 0.18f);
            s.pants = new Color(0.28f, 0.22f, 0.27f);

            switch (cat)
            {
                case NPCCategory.Child:
                    s.H = 1.10f; s.head = 0.15f; s.shoulder = 0.12f; s.torsoW = 0.11f; s.torsoD = 0.09f;
                    s.armR = 0.03f; s.legR = 0.04f; s.belly = 0.015f; s.shoulderF = 0.70f; s.hipF = 0.40f;
                    s.shirt = f ? new Color(0.96f, 0.72f, 0.78f) : new Color(0.93f, 0.76f, 0.35f);
                    s.pants = f ? new Color(0.98f, 0.40f, 0.52f) : new Color(0.30f, 0.42f, 0.65f); // 1. Strawberry coral pink
                    break;
                case NPCCategory.Adolescent:
                    s.H = 1.52f; s.head = 0.12f; s.shoulder = 0.17f; s.torsoW = 0.14f; s.torsoD = 0.10f;
                    s.armR = 0.032f; s.legR = 0.048f; s.shoulderF = 0.80f;
                    s.shirt = f ? new Color(0.92f, 0.94f, 0.98f) : new Color(0.45f, 0.62f, 0.68f);
                    s.pants = f ? new Color(0.58f, 0.42f, 0.86f) : new Color(0.28f, 0.22f, 0.27f); // 2. Lavender violet
                    break;
                case NPCCategory.ThinAdult:
                    s.H = 1.78f; s.head = 0.115f; s.shoulder = 0.20f; s.torsoW = 0.15f; s.torsoD = 0.10f;
                    s.armR = 0.032f; s.legR = 0.05f;
                    s.shirt = f ? new Color(0.98f, 0.92f, 0.82f) : new Color(0.60f, 0.62f, 0.65f);
                    s.pants = f ? new Color(0.20f, 0.62f, 0.56f) : new Color(0.28f, 0.22f, 0.27f); // 3. Emerald teal
                    break;
                case NPCCategory.OverweightAdult:
                    s.H = 1.72f; s.head = 0.125f; s.shoulder = 0.25f; s.torsoW = 0.24f; s.torsoD = 0.20f;
                    s.armR = 0.05f; s.legR = 0.07f; s.belly = 0.085f; s.shoulderF = 0.80f;
                    s.shirt = f ? new Color(0.72f, 0.64f, 0.82f) : new Color(0.55f, 0.60f, 0.68f);
                    s.pants = f ? new Color(0.88f, 0.58f, 0.22f) : new Color(0.28f, 0.22f, 0.27f); // 4. Amber marigold
                    break;
                case NPCCategory.AverageFitAdult:
                    s.H = 1.78f; s.head = 0.115f; s.shoulder = 0.22f; s.torsoW = 0.18f; s.torsoD = 0.11f;
                    s.armR = 0.04f; s.legR = 0.055f;
                    s.shirt = f ? new Color(0.95f, 0.62f, 0.66f) : new Color(0.58f, 0.60f, 0.62f);
                    s.pants = f ? new Color(0.26f, 0.46f, 0.84f) : new Color(0.28f, 0.22f, 0.27f); // 5. Royal sapphire blue
                    break;
                default: // Elderly
                    s.H = 1.64f; s.head = 0.12f; s.shoulder = 0.20f; s.torsoW = 0.19f; s.torsoD = 0.14f;
                    s.armR = 0.038f; s.legR = 0.055f; s.belly = 0.055f; s.shoulderF = 0.80f;
                    s.shirt = f ? new Color(0.76f, 0.68f, 0.76f) : new Color(0.68f, 0.64f, 0.52f);
                    s.pants = f ? new Color(0.56f, 0.22f, 0.32f) : new Color(0.38f, 0.35f, 0.35f); // 6. Burgundy wine
                    break;
            }

            // Cute chibi proportions: big round head, soft chubby limbs
            s.head *= (cat == NPCCategory.Child) ? 1.9f : 2.25f;
            s.legR *= 1.5f;
            s.hipF *= 0.88f;
            s.torsoW *= 1.15f;
            s.armR *= 1.3f;
            s.shoulderF -= 0.02f;

            if (f && cat != NPCCategory.Child)
            {
                s.H *= 0.94f;
                s.shoulder *= 0.88f;
                s.torsoW *= 0.92f;
                s.armR *= 0.92f;
                s.legR *= 0.95f;
                s.hipMul = 1.12f;
            }
            return s;
        }

        // ---------------------------------------------------------------- build

        private static void BuildParts(Spec s, NPCCategory cat, NPCGender gen, List<Mesh> tempMeshes)
        {
            parts.Clear();
            float H = s.H;
            float k = H / 1.78f;
            float hipY = H * s.hipF;
            float shY = H * s.shoulderF;

            // Legs + shoes
            bool isFemale = gen == NPCGender.Female;
            Color legCol = isFemale ? Skin : s.pants;
            float legX = s.torsoW * 0.55f * s.hipMul;
            for (int i = -1; i <= 1; i += 2)
            {
                Capsule(new Vector3(i * legX, hipY, 0), new Vector3(i * legX * 1.03f, 0.07f * k, 0), s.legR, legCol);
                // Cute white socks for child girl
                if (isFemale && cat == NPCCategory.Child)
                {
                    Ellipsoid(new Vector3(i * legX * 1.03f, 0.08f * k, 0), new Vector3(s.legR * 2.1f, 0.05f * k, s.legR * 2.1f), Color.white);
                }
                Ellipsoid(new Vector3(i * legX * 1.03f, 0.045f * k, s.legR * 0.9f),
                    new Vector3(s.legR * 2.2f, 0.09f * k, s.legR * 4.4f + 0.05f * k), Shoe);
            }

            // Smooth unified procedural torso (no segmented rolls/creases, natural pear/potbelly curve like reference)
            float torsoH = shY - hipY;
            Mesh torsoMesh = MakeSmoothTorsoMesh(torsoH, s.torsoW, s.shoulder, s.torsoD, s.belly, s.hipMul);
            tempMeshes.Add(torsoMesh);
            parts.Add(new Part { mesh = torsoMesh, matrix = Matrix4x4.TRS(new Vector3(0, hipY, 0), Quaternion.identity, Vector3.one), color = s.shirt });

            // Reference style brown belt & golden buckle below belly
            float beltY = hipY + 0.012f * H;
            Color BeltCol = new Color(0.38f, 0.22f, 0.12f);
            Color BuckleCol = new Color(0.88f, 0.68f, 0.22f);
            float beltW = s.torsoW * 1.84f * s.hipMul;
            float beltD = s.torsoD * 1.80f;
            float beltFwd = (s.belly > 0f) ? s.belly * 0.40f : 0f;

            Ellipsoid(new Vector3(0, beltY, beltFwd), new Vector3(beltW, 0.045f * H, beltD), BeltCol);
            Box(new Vector3(0, beltY, beltFwd + beltD * 0.50f + 0.005f), new Vector3(0.045f * H, 0.038f * H, 0.014f * H), BuckleCol);
            Box(new Vector3(0, beltY, beltFwd + beltD * 0.50f + 0.010f), new Vector3(0.022f * H, 0.018f * H, 0.010f * H), BeltCol);

            if (isFemale)
            {
                // Female: Flared A-line skirt with individual color per category
                float skirtLen = (cat == NPCCategory.Elderly) ? (0.28f * H) : ((cat == NPCCategory.Child) ? (0.20f * H) : (0.23f * H));
                float waistRx = s.torsoW * 0.98f * s.hipMul;
                float waistRz = s.torsoD * 0.98f;
                float flare = (cat == NPCCategory.Child) ? 1.65f : ((cat == NPCCategory.Elderly) ? 1.35f : 1.50f);

                Mesh skirtMesh = MakeSkirtMesh(skirtLen, waistRx, waistRz, flare);
                tempMeshes.Add(skirtMesh);
                parts.Add(new Part { mesh = skirtMesh, matrix = Matrix4x4.TRS(new Vector3(0, hipY + 0.015f * H, 0), Quaternion.identity, Vector3.one), color = s.pants });

                // Modesty under-skirt pelvis
                Ellipsoid(new Vector3(0, hipY - 0.015f * H, 0),
                    new Vector3(s.torsoW * 1.60f * s.hipMul, 0.10f * H, s.torsoD * 1.45f), s.pants);
            }
            else
            {
                // Male: Pants pelvis neatly tucked inside the belt and smoothly bridging to legs
                Ellipsoid(new Vector3(0, hipY - 0.025f * H, 0),
                    new Vector3(s.torsoW * 1.70f * s.hipMul, 0.14f * H, s.torsoD * 1.55f), s.pants);
            }

            // Reference style polo collar & button placket
            Color collarCol = (cat == NPCCategory.Elderly) 
                ? new Color(0.48f, 0.58f, 0.45f) // classic sage-green polo collar from reference!
                : Color.Lerp(s.shirt, Color.white, 0.18f);

            for (int i = -1; i <= 1; i += 2)
            {
                Capsule(new Vector3(i * s.head * 0.16f, shY + 0.005f * H, s.torsoD * 0.90f),
                        new Vector3(i * s.head * 0.40f, shY - 0.045f * H, s.torsoD * 1.02f), s.head * 0.065f, collarCol);
            }
            Box(new Vector3(0, shY - 0.045f * H, s.torsoD * 1.04f), new Vector3(s.head * 0.14f, 0.075f * H, 0.014f), collarCol);
            Ellipsoid(new Vector3(0, shY - 0.035f * H, s.torsoD * 1.05f + 0.008f), Vector3.one * (s.head * 0.04f), Color.white);

            // Neck + big round head
            float rx = s.head * 0.98f, ry = s.head * 0.92f, rz = s.head * 0.95f;
            Capsule(new Vector3(0, shY - 0.02f * H, 0), new Vector3(0, shY + 0.03f * H, 0), s.head * 0.3f, Skin);
            Vector3 C = new Vector3(0, shY + 0.01f * H + ry, 0);
            Ellipsoid(C, new Vector3(rx * 2f, ry * 2f, rz * 2f), Skin);

            // Tiny nose, ears, glossy eyes, rosy cheeks and a little smile
            Ellipsoid(C + new Vector3(0, -ry * 0.12f, rz * 0.98f), Vector3.one * (s.head * 0.12f), Skin);
            for (int i = -1; i <= 1; i += 2)
                Ellipsoid(C + new Vector3(i * rx * 0.97f, -ry * 0.05f, 0), new Vector3(s.head * 0.10f, s.head * 0.22f, s.head * 0.18f), Skin);

            Color eye = new Color(0.10f, 0.08f, 0.10f);
            Color pink = new Color(0.98f, 0.62f, 0.64f);
            Color browCol = (cat == NPCCategory.Elderly) ? new Color(0.78f, 0.78f, 0.80f) : s.hair;
            for (int i = -1; i <= 1; i += 2)
            {
                Vector3 e = C + new Vector3(i * rx * 0.40f, -ry * 0.08f, rz * 0.88f);
                Ellipsoid(e, new Vector3(s.head * 0.20f, s.head * 0.27f, s.head * 0.14f), eye);
                Ellipsoid(e + new Vector3(i * -s.head * 0.03f, s.head * 0.06f, s.head * 0.05f), Vector3.one * (s.head * 0.07f), Color.white);
                Ellipsoid(C + new Vector3(i * rx * 0.62f, -ry * 0.32f, rz * 0.76f), new Vector3(s.head * 0.22f, s.head * 0.13f, s.head * 0.10f), pink);
                // Cute expressive eyebrows
                Ellipsoid(C + new Vector3(i * rx * 0.40f, ry * 0.16f, rz * 0.90f), new Vector3(s.head * 0.20f, s.head * 0.07f, s.head * 0.08f), browCol);
            }
            Ellipsoid(C + new Vector3(0, -ry * 0.42f, rz * 0.88f), new Vector3(s.head * 0.16f, s.head * 0.06f, s.head * 0.08f), new Color(0.75f, 0.30f, 0.32f));

            BuildHair(s, cat, gen, C, rx, ry, rz);

            // Glasses
            bool glasses = cat == NPCCategory.Elderly || (cat == NPCCategory.ThinAdult && gen == NPCGender.Male);
            if (glasses)
            {
                for (int i = -1; i <= 1; i += 2)
                    Disc(C + new Vector3(i * rx * 0.40f, -ry * 0.08f, rz * 0.92f), s.head * 0.62f, 0.02f, Frame);
                Box(C + new Vector3(0, -ry * 0.04f, rz * 0.97f), new Vector3(rx * 0.2f, 0.012f, 0.012f), Frame);
            }

            BuildFacialHair(s, cat, gen, C, rx, ry, rz);

            // Soft chubby arms in a relaxed A-pose (single smooth limb + round mitten hand)
            float a1 = 24f * Mathf.Deg2Rad;
            for (int i = -1; i <= 1; i += 2)
            {
                Vector3 S = new Vector3(i * s.shoulder, shY - 0.035f * H, 0);
                Vector3 d1 = new Vector3(i * Mathf.Sin(a1), -Mathf.Cos(a1), 0);
                float len = 0.27f * H;
                Vector3 W = S + d1 * len;

                Capsule(S + d1 * len * 0.3f, W, s.armR, Skin);                    // arm
                Capsule(S - d1 * s.armR * 0.5f, S + d1 * len * 0.42f, s.armR * 1.25f, s.shirt); // short sleeve
                Ellipsoid(W + d1 * s.armR * 0.8f, new Vector3(s.armR * 2.5f, s.armR * 2.6f, s.armR * 2.3f), Skin); // hand
            }
        }

        private static void BuildFemaleHair(Spec s, NPCCategory cat, Vector3 C, float rx, float ry, float rz)
        {
            Color h = s.hair;

            // 1. Full voluminous hair crown & back (covers top and back completely)
            Ellipsoid(C + new Vector3(0, ry * 0.40f, -rz * 0.08f), new Vector3(rx * 2.22f, ry * 1.55f, rz * 2.18f), h);
            Ellipsoid(C + new Vector3(0, -ry * 0.12f, -rz * 0.32f), new Vector3(rx * 2.15f, ry * 1.60f, rz * 1.90f), h);

            // 2. Beautiful feminine bangs across forehead (eliminates bald forehead)
            Ellipsoid(C + new Vector3(0, ry * 0.52f, rz * 0.72f), new Vector3(rx * 1.70f, ry * 0.48f, rz * 0.60f), h);

            Capsule(C + new Vector3(-rx * 0.45f, ry * 0.65f, rz * 0.70f),
                    C + new Vector3(-rx * 0.18f, ry * 0.32f, rz * 0.94f), rx * 0.20f, h);
            Capsule(C + new Vector3(-rx * 0.12f, ry * 0.68f, rz * 0.72f),
                    C + new Vector3(rx * 0.22f, ry * 0.30f, rz * 0.94f), rx * 0.22f, h);
            Capsule(C + new Vector3(rx * 0.18f, ry * 0.65f, rz * 0.70f),
                    C + new Vector3(rx * 0.52f, ry * 0.35f, rz * 0.90f), rx * 0.20f, h);

            // 3. Side locks framing both cheeks
            for (int i = -1; i <= 1; i += 2)
            {
                Vector3 sTop = C + new Vector3(i * rx * 0.88f, ry * 0.45f, rz * 0.30f);
                Vector3 sMid = C + new Vector3(i * rx * 0.96f, -ry * 0.15f, rz * 0.50f);
                Vector3 sBot = C + new Vector3(i * rx * 0.85f, -ry * 0.60f, rz * 0.48f);

                Capsule(sTop, sMid, rx * 0.22f, h);
                Capsule(sMid, sBot, rx * 0.18f, h);
                Ellipsoid(sBot, Vector3.one * (rx * 0.24f), h);
            }

            // 4. Category-specific feminine styling
            if (cat == NPCCategory.Child)
            {
                Color tieCol = new Color(0.98f, 0.45f, 0.55f);
                for (int i = -1; i <= 1; i += 2)
                {
                    Vector3 tie = C + new Vector3(i * rx * 1.02f, ry * 0.38f, -rz * 0.05f);
                    Ellipsoid(tie, Vector3.one * (rx * 0.38f), tieCol);

                    Vector3 tailEnd = tie + new Vector3(i * rx * 0.35f, -ry * 0.55f, 0);
                    Capsule(tie, tailEnd, rx * 0.30f, h);
                    Ellipsoid(tailEnd, Vector3.one * (rx * 0.36f), h);
                }
            }
            else if (cat == NPCCategory.Adolescent)
            {
                Vector3 ponyBase = C + new Vector3(0, ry * 0.68f, -rz * 0.85f);
                Color scrunchieCol = new Color(0.95f, 0.50f, 0.60f);
                Ellipsoid(ponyBase, Vector3.one * (rx * 0.48f), scrunchieCol);

                Vector3 ponyMid = ponyBase + new Vector3(rx * 0.40f, -ry * 0.35f, -rz * 0.10f);
                Vector3 ponyTip = ponyMid + new Vector3(rx * 0.25f, -ry * 0.65f, rz * 0.40f);
                Capsule(ponyBase, ponyMid, rx * 0.32f, h);
                Capsule(ponyMid, ponyTip, rx * 0.24f, h);
                Ellipsoid(ponyTip, Vector3.one * (rx * 0.30f), h);
            }
            else if (cat == NPCCategory.Elderly)
            {
                Vector3 bunPos = C + new Vector3(0, ry * 0.50f, -rz * 0.95f);
                Ellipsoid(bunPos, Vector3.one * (rx * 0.75f), h);
                Color pinCol = new Color(0.88f, 0.72f, 0.32f);
                Capsule(bunPos + new Vector3(-rx * 0.42f, 0, 0), bunPos + new Vector3(rx * 0.42f, 0, 0), rx * 0.06f, pinCol);
            }
            else
            {
                // Adult Females: Elegant Bob + side hair clip
                for (int i = -1; i <= 1; i += 2)
                {
                    Ellipsoid(C + new Vector3(i * rx * 0.88f, -ry * 0.72f, rz * 0.20f),
                        new Vector3(rx * 0.60f, ry * 0.50f, rz * 0.55f), h);
                }
                Color clipCol = (cat == NPCCategory.OverweightAdult) ? new Color(0.96f, 0.48f, 0.60f) : new Color(0.48f, 0.78f, 0.96f);
                Box(C + new Vector3(-rx * 0.72f, ry * 0.48f, rz * 0.72f), new Vector3(rx * 0.24f, ry * 0.12f, 0.02f), clipCol);
                Ellipsoid(C + new Vector3(-rx * 0.72f, ry * 0.48f, rz * 0.74f), Vector3.one * (rx * 0.12f), Color.white);
            }
        }

        private static void BuildMaleHair(Spec s, NPCCategory cat, Vector3 C, float rx, float ry, float rz)
        {
            Color h = s.hair;

            if (cat == NPCCategory.Elderly)
            {
                // Balding: hair ring at sides/back only
                Ellipsoid(C + new Vector3(0, ry * 0.0f, -rz * 0.2f), new Vector3(rx * 2.08f, ry * 0.7f, rz * 2.0f), h);
                return;
            }

            // Short cap
            Ellipsoid(C + new Vector3(0, ry * 0.48f, -rz * 0.16f), new Vector3(rx * 2.12f, ry * 1.3f, rz * 2.16f), h);
            // Swept fringe
            Ellipsoid(C + new Vector3(rx * 0.15f, ry * 0.62f, rz * 0.38f), new Vector3(rx * 1.3f, ry * 0.5f, rz * 0.7f), h);
        }

        private static void BuildHair(Spec s, NPCCategory cat, NPCGender gen, Vector3 C, float rx, float ry, float rz)
        {
            if (gen == NPCGender.Female)
                BuildFemaleHair(s, cat, C, rx, ry, rz);
            else
                BuildMaleHair(s, cat, C, rx, ry, rz);
        }

        private static void BuildFacialHair(Spec s, NPCCategory cat, NPCGender gen, Vector3 C, float rx, float ry, float rz)
        {
            if (gen != NPCGender.Male) return;
            if (cat != NPCCategory.OverweightAdult && cat != NPCCategory.Elderly) return;

            if (cat == NPCCategory.Elderly)
            {
                // Reference style: fluffy white mustache right under nose, clean smiling chin
                Color whiteMustache = new Color(0.96f, 0.96f, 0.98f);
                float my = -ry * 0.20f;
                float mz = rz * 0.96f;
                float mR = s.head * 0.082f;

                for (int i = -1; i <= 1; i += 2)
                {
                    Vector3 mStart = C + new Vector3(i * rx * 0.035f, my, mz);
                    Vector3 mEnd = C + new Vector3(i * rx * 0.32f, my - ry * 0.08f, mz * 0.88f);
                    Capsule(mStart, mEnd, mR, whiteMustache);
                }
            }
            else if (cat == NPCCategory.OverweightAdult)
            {
                Color bc = new Color(0.35f, 0.20f, 0.14f);
                float my = -ry * 0.25f;
                float mz = rz * 0.95f;
                float mR = s.head * 0.052f;

                for (int i = -1; i <= 1; i += 2)
                {
                    Vector3 mStart = C + new Vector3(i * rx * 0.035f, my, mz);
                    Vector3 mMid = C + new Vector3(i * rx * 0.22f, my - ry * 0.04f, mz * 0.94f);
                    Vector3 mEnd = C + new Vector3(i * rx * 0.36f, my - ry * 0.11f, mz * 0.86f);

                    Capsule(mStart, mMid, mR, bc);
                    Capsule(mMid, mEnd, mR * 0.72f, bc);
                }

                // Neat chin beard
                Ellipsoid(C + new Vector3(0, -ry * 0.76f, rz * 0.60f), new Vector3(rx * 0.42f, ry * 0.22f, rz * 0.36f), bc);
            }
        }

        // ---------------------------------------------------------------- asset output

        private static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + name))
                AssetDatabase.CreateFolder(parent, name);
        }

        private static GameObject CreateNPC(NPCCategory cat, NPCGender gen, Transform parent, Vector3 localPos)
        {
            EnsureMeshes();
            List<Mesh> tempMeshes = new List<Mesh>();
            Spec spec = GetSpec(cat, gen);
            BuildParts(spec, cat, gen, tempMeshes);

            string npcName = "NPC_" + cat + "_" + gen;

            // Group parts by color -> one submesh / material per color
            List<Color32> keys = new List<Color32>();
            Dictionary<Color32, List<CombineInstance>> groups = new Dictionary<Color32, List<CombineInstance>>();
            foreach (Part p in parts)
            {
                Color32 key = p.color;
                if (!groups.ContainsKey(key))
                {
                    groups[key] = new List<CombineInstance>();
                    keys.Add(key);
                }
                groups[key].Add(new CombineInstance { mesh = p.mesh, transform = p.matrix });
            }

            List<Mesh> groupMeshes = new List<Mesh>();
            List<CombineInstance> finalInstances = new List<CombineInstance>();
            foreach (Color32 key in keys)
            {
                Mesh gm = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                gm.CombineMeshes(groups[key].ToArray(), true, true);
                groupMeshes.Add(gm);
                finalInstances.Add(new CombineInstance { mesh = gm, transform = Matrix4x4.identity });
            }

            Mesh final = new Mesh { name = npcName + "_Mesh", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            final.CombineMeshes(finalInstances.ToArray(), false, false);
            final.RecalculateBounds();
            foreach (Mesh gm in groupMeshes) Object.DestroyImmediate(gm);
            foreach (Mesh tm in tempMeshes) Object.DestroyImmediate(tm);

            EnsureFolder("Assets", "Meshes");
            EnsureFolder("Assets/Meshes", "NPC");
            EnsureFolder("Assets", "Materials");
            EnsureFolder("Assets/Materials", "NPC");
            EnsureFolder("Assets", "Prefabs");
            EnsureFolder("Assets/Prefabs", "NPCs");

            string meshPath = "Assets/Meshes/NPC/" + npcName + ".asset";
            AssetDatabase.DeleteAsset(meshPath);
            AssetDatabase.CreateAsset(final, meshPath);

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            Material[] mats = new Material[keys.Count];
            for (int i = 0; i < keys.Count; i++)
            {
                Material m = new Material(shader) { name = npcName + "_Mat" + i, color = (Color)keys[i] };
                if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.4f);
                if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
                string matPath = "Assets/Materials/NPC/" + npcName + "_Mat" + i + ".mat";
                AssetDatabase.DeleteAsset(matPath);
                AssetDatabase.CreateAsset(m, matPath);
                mats[i] = m;
            }

            // One single GameObject = one block
            GameObject root = new GameObject(npcName);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPos;
            root.AddComponent<MeshFilter>().sharedMesh = final;
            root.AddComponent<MeshRenderer>().sharedMaterials = mats;
            BoxCollider box = root.AddComponent<BoxCollider>();
            box.center = final.bounds.center;
            box.size = final.bounds.size;

            string prefabPath = "Assets/Prefabs/NPCs/" + npcName + ".prefab";
            AssetDatabase.DeleteAsset(prefabPath);
            PrefabUtility.SaveAsPrefabAssetAndConnect(root, prefabPath, InteractionMode.AutomatedAction);
            return root;
        }

        [MenuItem("Tools/Character/Generate All 12 NPCs", priority = 20)]
        public static void GenerateAllNPCs()
        {
            GameObject old;
            while ((old = GameObject.Find("NPC_Collection")) != null)
                Object.DestroyImmediate(old);

            GameObject group = new GameObject("NPC_Collection");
            Undo.RegisterCreatedObjectUndo(group, "Create NPC Collection");

            NPCCategory[] cats = (NPCCategory[])System.Enum.GetValues(typeof(NPCCategory));
            NPCGender[] gens = (NPCGender[])System.Enum.GetValues(typeof(NPCGender));

            for (int c = 0; c < cats.Length; c++)
            {
                for (int g = 0; g < gens.Length; g++)
                {
                    Vector3 pos = new Vector3((c - 2.5f) * 1.2f, 0f, g * 1.2f);
                    CreateNPC(cats[c], gens[g], group.transform, pos);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeGameObject = group;
            Debug.Log("[NPCModelGenerator] Generated 12 single-mesh NPC prefabs in Assets/Prefabs/NPCs");
        }
    }
}
