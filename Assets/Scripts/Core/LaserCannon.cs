// Jan Biernacki, 3275061; Malte Lührs, 3138900; Kürsat Yurdakul, 3282791; Paul Prigge, 3097391
using UnityEngine;

namespace GazeLab
{
    public sealed class LaserCannon
    {
        const float BeamZ = -3f;
        const float ExtendUntil = 0.3f, RetractFrom = 0.45f;

        static readonly Color CoreColor = new Color(1f, 0.98f, 0.9f);
        static readonly Color GlowStart = HudTheme.ShipYellow;
        static readonly Color GlowEnd = new Color(1f, 0.45f, 0.15f);

        readonly Transform muzzle;
        readonly LineRenderer beam, glow;
        readonly Transform ship;
        Vector3 from, to;

        public LaserCannon(Transform muzzle, LineRenderer beam, Transform ship)
        {
            this.muzzle = muzzle;
            this.beam = beam;
            this.ship = ship;
            if (beam == null) return;
            var material = new Material(Shader.Find("Sprites/Default")) { name = "GazeLab Laser" };
            Setup(beam, material);
            glow = new GameObject("Laser glow").AddComponent<LineRenderer>();
            glow.transform.SetParent(beam.transform, false);
            Setup(glow, material);
        }

        public float LastShotTime { get; private set; } = float.NegativeInfinity;
        public bool IsBeamVisible { get { return beam != null && beam.enabled; } }
        public Vector2 MuzzlePosition { get { return muzzle != null ? (Vector2)muzzle.position : Vector2.zero; } }

        public bool HasClearShot(Collider2D target, Vector2 aimPoint, out Vector2 hitPoint)
        {
            hitPoint = aimPoint;
            if (muzzle == null || target == null) return false;
            float nearest = float.PositiveInfinity;
            Collider2D first = null;
            foreach (RaycastHit2D hit in Physics2D.LinecastAll(muzzle.position, aimPoint))
            {
                if (hit.collider == null || !hit.collider.enabled || IsShip(hit.collider)) continue;
                if (hit.distance < nearest) { nearest = hit.distance; first = hit.collider; hitPoint = hit.point; }
            }
            return first != null && (first == target || first.gameObject == target.gameObject);
        }

        public void Fire(Vector2 hitPoint, float now)
        {
            LastShotTime = now;
            ShotEffects.MuzzleFlash(MuzzlePosition);
            if (beam == null) return;
            from = new Vector3(muzzle.position.x, muzzle.position.y, BeamZ);
            to = new Vector3(hitPoint.x, hitPoint.y, BeamZ);
            beam.enabled = glow.enabled = true;
            Animate(0f);
        }

        public void UpdateBeam(float now, float visibleSeconds)
        {
            if (!IsBeamVisible) return;
            float t = (now - LastShotTime) / Mathf.Max(0.01f, visibleSeconds);
            if (t >= 1f) beam.enabled = glow.enabled = false;
            else Animate(t);
        }

        public void Hide()
        {
            LastShotTime = float.NegativeInfinity;
            if (beam != null) beam.enabled = glow.enabled = false;
        }

        void Animate(float t)
        {
            float head = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / ExtendUntil), 3f);
            float tail = Mathf.Pow(Mathf.Clamp01((t - RetractFrom) / (1f - RetractFrom)), 2f);
            Vector3 headPoint = Vector3.Lerp(from, to, head), tailPoint = Vector3.Lerp(from, to, tail);
            float flare = 1f + 0.6f * Mathf.Sin(Mathf.Clamp01(t / 0.5f) * Mathf.PI) + 0.12f * Mathf.Sin(t * 60f);
            float fade = 1f - t * t;

            beam.SetPosition(0, tailPoint);
            beam.SetPosition(1, headPoint);
            beam.widthMultiplier = 0.11f * flare * fade;
            beam.startColor = beam.endColor = CoreColor;

            glow.SetPosition(0, tailPoint + Vector3.forward * 0.05f);
            glow.SetPosition(1, headPoint + Vector3.forward * 0.05f);
            glow.widthMultiplier = 0.3f * flare * fade;
            Color start = Color.Lerp(GlowStart, GlowEnd, t), end = GlowEnd;
            start.a = end.a = Mathf.Clamp01(1.4f * fade);
            glow.startColor = start;
            glow.endColor = end;
        }

        static void Setup(LineRenderer line, Material material)
        {
            line.positionCount = 2;
            line.useWorldSpace = true;
            line.sharedMaterial = material;
            line.numCapVertices = 6;
            line.widthCurve = new AnimationCurve(new Keyframe(0f, 0.6f), new Keyframe(1f, 1f));
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.sortingOrder = 10000;
            line.enabled = false;
        }

        bool IsShip(Collider2D candidate)
        {
            return candidate.CompareTag("Player") ||
                   (ship != null && (candidate.transform == ship || candidate.transform.IsChildOf(ship)));
        }
    }
}
