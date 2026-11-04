// Team: Jan Biernacki, Kürsat, Paul, Malte | Matriculation numbers: TO BE PROVIDED
using UnityEngine;

namespace GazeLab
{
    /// <summary>
    /// A satellite (about 2.5 x 0.8): octagonal body with a gold band, a dish and a red warning light.
    /// Variant 0 has two tilted panels per side, variant 1 one large panel per side.
    /// </summary>
    static class SatelliteModel
    {
        public static Mesh Build(int variant)
        {
            var b = new ToonMeshBuilder();
            Body(b);
            foreach (float side in new[] { -1f, 1f })
            {
                if (variant == 0)
                {
                    foreach (float row in new[] { -1f, 1f })
                    {
                        Arm(b, side, row * 0.19f);
                        SolarPanel(b, new Vector3(side * 0.93f, row * 0.19f, 0f), new Vector3(row * -14f, 0f, 0f), 0.64f, 0.3f, 4, 2);
                    }
                }
                else
                {
                    Arm(b, side, 0f);
                    SolarPanel(b, new Vector3(side * 0.93f, 0f, 0f), new Vector3(side * 10f, 0f, 0f), 0.64f, 0.66f, 4, 3);
                }
            }
            return b.Build("GazeLab Satellite " + variant);
        }

        static void Body(ToonMeshBuilder b)
        {
            b.Part(Vector3.zero).Style(ModelPalette.Metal);
            b.Frustum(8, -0.36f, 0.3f, -0.13f, 0.3f, false, false, false);
            b.Frustum(8, 0.13f, 0.3f, 0.36f, 0.3f, false, false, false);
            b.Style(ModelPalette.Gold);
            b.Frustum(8, -0.13f, 0.31f, 0.13f, 0.31f, false, false, false);
            b.Style(ModelPalette.DarkMetal);
            b.Frustum(8, 0.36f, 0.3f, 0.44f, 0.2f, false, false, true);   // bevelled top
            b.Frustum(8, -0.44f, 0.2f, -0.36f, 0.3f, false, true, false); // bevelled bottom
            b.Style(ModelPalette.Metal);
            b.Frustum(6, 0.44f, 0.03f, 0.53f, 0.03f);                     // dish stem
            b.Frustum(12, 0.5f, 0.04f, 0.6f, 0.17f);                      // dish
            b.Part(new Vector3(0f, -0.48f, 0f)).Style(HudTheme.ShipRed, 0.8f);
            b.Sphere(0.06f);                                              // warning light
        }

        static void Arm(ToonMeshBuilder b, float side, float y)
        {
            b.Part(new Vector3(0f, y, 0f), new Vector3(0f, 0f, side * -90f), Vector3.one).Style(ModelPalette.Metal);
            b.Frustum(6, 0.25f, 0.035f, 0.62f, 0.035f);
        }

        /// <summary>Dark backing plate with raised blue cells.</summary>
        static void SolarPanel(ToonMeshBuilder b, Vector3 centre, Vector3 euler, float width, float height, int columns, int rows)
        {
            const float Gap = 0.025f;
            Matrix4x4 panel = Matrix4x4.TRS(centre, Quaternion.Euler(euler), Vector3.one);
            b.Part(panel).Style(ModelPalette.PanelBacking);
            b.Box(new Vector3(width, height, 0.035f));

            float cellW = (width - Gap * (columns + 1)) / columns, cellH = (height - Gap * (rows + 1)) / rows;
            b.Style(HudTheme.SatelliteBlue);
            for (int x = 0; x < columns; x++)
                for (int y = 0; y < rows; y++)
                {
                    var cell = new Vector3(-width / 2 + Gap + cellW / 2 + x * (cellW + Gap), -height / 2 + Gap + cellH / 2 + y * (cellH + Gap), 0f);
                    b.Part(panel * Matrix4x4.Translate(cell));
                    b.Box(new Vector3(cellW, cellH, 0.055f));
                }
        }
    }
}
