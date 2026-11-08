// Jan Biernacki, 3275061; Malte Lührs, 3138900; Kürsat Yurdakul, 3282791; Paul Prigge, 3097391
using UnityEngine;

namespace GazeLab
{
    static class ShipModel
    {
        const float NozzleY = -1.32f;
        const float EngineX = 0.3f;

        public static Vector3 FlamesPivot { get { return new Vector3(0f, NozzleY, 0f); } }

        public static Mesh Build()
        {
            var b = new ToonMeshBuilder();
            Hull(b);
            Cockpit(b);
            Engines(b);
            foreach (float side in new[] { -1f, 1f }) Wing(b, side);
            return b.Build("GazeLab Ship");
        }

        public static Mesh BuildFlames()
        {
            var b = new ToonMeshBuilder();
            foreach (float side in new[] { -1f, 1f })
            {
                b.Part(new Vector3(side * EngineX, 0f, 0f)).Style(ModelPalette.Flame, 1f);
                b.Frustum(10, -0.62f, 0f, 0f, 0.17f);
                b.Part(new Vector3(side * EngineX, 0f, -0.12f)).Style(HudTheme.ShipYellow, 1f);
                b.Frustum(10, -0.4f, 0f, 0f, 0.1f);
            }
            return b.Build("GazeLab Flames");
        }

        static void Hull(ToonMeshBuilder b)
        {
            b.Part(Vector3.zero).Style(HudTheme.ShipRed);
            b.Paint = centre =>
            {
                float angle = Mathf.Abs(Mathf.Atan2(centre.x, -centre.z) * Mathf.Rad2Deg);
                return Mathf.Abs(angle - 27f) < 6f ? HudTheme.ShipYellow : HudTheme.ShipRed;
            };
            b.Frustum(20, -0.86f, 0.6f, 0.44f, 0.6f, true, false, false);
            b.Style(ModelPalette.DarkMetal);
            b.Frustum(20, -1.02f, 0.46f, -0.86f, 0.6f, true, true, false);
            b.Style(HudTheme.ShipYellow);
            b.Frustum(20, 0.4f, 0.645f, 0.5f, 0.645f);
        }

        static void Cockpit(ToonMeshBuilder b)
        {
            b.Part(new Vector3(0f, 0.48f, 0f), Vector3.zero, new Vector3(1f, 1.1f, 1f)).Style(HudTheme.CockpitTeal);
            b.Dome(20, 6, 0.6f);
            b.Part(Vector3.zero).Style(ModelPalette.Metal);
            b.Frustum(6, 1.05f, 0.04f, 1.42f, 0.03f);
            b.Part(new Vector3(0f, 1.46f, 0f)).Style(HudTheme.ShipRed, 0.7f);
            b.Sphere(0.075f);
        }

        static void Engines(ToonMeshBuilder b)
        {
            foreach (float side in new[] { -1f, 1f })
            {
                b.Part(new Vector3(side * EngineX, 0f, 0f)).Style(ModelPalette.DarkMetal);
                b.Frustum(12, NozzleY, 0.22f, -0.92f, 0.16f);
                b.Style(ModelPalette.Flame, 1f);
                b.Frustum(12, NozzleY - 0.02f, 0.16f, NozzleY + 0.01f, 0.16f);
            }
        }

        static void Wing(ToonMeshBuilder b, float side)
        {
            Vector2[] inner = { new Vector2(0f, 0.2f), new Vector2(0.6f, -0.36f), new Vector2(0.6f, -1.36f), new Vector2(0f, -1f) };
            Vector2[] edge = { new Vector2(0.6f, -0.36f), new Vector2(0.78f, -0.53f), new Vector2(0.78f, -1.47f), new Vector2(0.6f, -1.36f) };
            Matrix4x4 wing = Matrix4x4.TRS(new Vector3(side * 0.42f, 0f, 0f), Quaternion.Euler(0f, side * -18f, 0f), new Vector3(side, 1f, 1f));

            b.Part(wing).Style(HudTheme.SatelliteBlue);
            b.Slab(inner, 0.16f);
            b.Style(HudTheme.WingGreen);
            b.Slab(edge, 0.16f);

            b.Style(HudTheme.ShipYellow, 1f);
            for (int i = 0; i < 3; i++)
            {
                b.Part(wing.MultiplyPoint3x4(new Vector3(0.14f + 0.16f * i, -0.6f - 0.22f * i, -0.085f)));
                b.Sphere(0.055f);
            }

            b.Part(wing.MultiplyPoint3x4(new Vector3(0.69f, -0.45f, 0f))).Style(ModelPalette.Metal);
            b.Frustum(6, 0f, 0.035f, 0.5f, 0.03f);
        }
    }
}
