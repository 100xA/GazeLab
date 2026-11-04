// Team: Jan Biernacki, Kürsat, Paul, Malte | Matriculation numbers: TO BE PROVIDED
using UnityEngine;

namespace GazeLab
{
    /// <summary>The ship's laser: fires only if the target is the first object on the line of fire.</summary>
    public sealed class LaserCannon
    {
        readonly Transform muzzle;
        readonly LineRenderer beam;
        readonly Transform ship;

        public LaserCannon(Transform muzzle, LineRenderer beam, Transform ship)
        {
            this.muzzle = muzzle;
            this.beam = beam;
            this.ship = ship;
            if (beam != null)
            {
                beam.positionCount = 2;
                beam.useWorldSpace = true;
                beam.sortingOrder = 10000;
                beam.enabled = false;
            }
        }

        public float LastShotTime { get; private set; } = float.NegativeInfinity;
        public bool IsBeamVisible { get { return beam != null && beam.enabled; } }
        public Vector2 MuzzlePosition { get { return muzzle != null ? (Vector2)muzzle.position : Vector2.zero; } }

        /// <summary>True if <paramref name="target"/> (any of its colliders) is the first object between the cannon and the aim point.</summary>
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
            if (beam == null) return;
            beam.SetPosition(0, muzzle.position);
            beam.SetPosition(1, new Vector3(hitPoint.x, hitPoint.y, 0f));
            beam.enabled = true;
        }

        public void UpdateBeam(float now, float visibleSeconds)
        {
            if (IsBeamVisible && now - LastShotTime >= visibleSeconds) beam.enabled = false;
        }

        public void Hide()
        {
            LastShotTime = float.NegativeInfinity;
            if (beam != null) beam.enabled = false;
        }

        bool IsShip(Collider2D candidate)
        {
            return candidate.CompareTag("Player") ||
                   (ship != null && (candidate.transform == ship || candidate.transform.IsChildOf(ship)));
        }
    }
}
