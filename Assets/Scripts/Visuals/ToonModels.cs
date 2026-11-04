// Team: Jan Biernacki, Kürsat, Paul, Malte | Matriculation numbers: TO BE PROVIDED
using UnityEngine;
using UnityEngine.Rendering;

namespace GazeLab
{
    /// <summary>Colours of the explosion puffs.</summary>
    public enum PuffColor { White, Yellow, Orange, Red }

    /// <summary>
    /// Entry point for the 3D models: builds each mesh once (see the classes in Visuals/Models),
    /// shares it between all objects and attaches it with the cartoon material
    /// (Resources/Shaders/GazeLabToon.shader).
    /// Model space: the ship points along +Y, the camera looks along +Z (front faces are at -Z).
    /// </summary>
    public static class ToonModels
    {
        public const int RockVariants = 4;
        public const int SatelliteVariants = 2;
        public const int DebrisVariants = 4;

        static Material material;
        static Mesh ship, flames;
        static readonly Mesh[] rocks = new Mesh[RockVariants];
        static readonly Mesh[] satellites = new Mesh[SatelliteVariants];
        static readonly Mesh[] puffs = new Mesh[4];
        static readonly Mesh[] rockDebris = new Mesh[DebrisVariants];
        static readonly Mesh[] satelliteDebris = new Mesh[DebrisVariants];

        public static Material Material
        {
            get
            {
                if (material != null) return material;
                Shader shader = Resources.Load<Shader>("Shaders/GazeLabToon");
                if (shader == null || !shader.isSupported)
                {
                    Debug.LogWarning("GazeLab/Toon shader missing or unsupported - models drawn unlit.");
                    shader = Shader.Find("Sprites/Default");
                }
                material = new Material(shader) { name = "GazeLab Toon" };
                return material;
            }
        }

        public static Mesh Ship { get { return ship != null ? ship : ship = ShipModel.Build(); } }

        /// <summary>Engine flames; placed at <see cref="FlamesPivot"/> so they can flicker in length.</summary>
        public static Mesh Flames { get { return flames != null ? flames : flames = ShipModel.BuildFlames(); } }

        public static Vector3 FlamesPivot { get { return ShipModel.FlamesPivot; } }

        public static Mesh Rock(int variant) { return Cached(rocks, variant, RockModel.Build); }

        public static Mesh Satellite(int variant) { return Cached(satellites, variant, SatelliteModel.Build); }

        public static Mesh Puff(PuffColor color) { return Cached(puffs, (int)color, c => EffectModels.Puff((PuffColor)c)); }

        public static Mesh Debris(TargetKind kind, int variant)
        {
            return kind == TargetKind.Rock
                ? Cached(rockDebris, variant, EffectModels.RockChunk)
                : Cached(satelliteDebris, variant, EffectModels.SatellitePart);
        }

        /// <summary>Adds a child that draws <paramref name="mesh"/>; returns its transform (for animation).</summary>
        public static Transform Attach(Transform parent, Mesh mesh, string name = "Model")
        {
            var model = new GameObject(name);
            model.transform.SetParent(parent, false);
            model.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = model.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = Material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return model.transform;
        }

        static Mesh Cached(Mesh[] cache, int variant, System.Func<int, Mesh> build)
        {
            variant = Mathf.Abs(variant) % cache.Length;
            return cache[variant] != null ? cache[variant] : cache[variant] = build(variant);
        }
    }
}
