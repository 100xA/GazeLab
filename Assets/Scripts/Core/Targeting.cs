using UnityEngine;

namespace GazeLab
{
    public sealed class Targeting
    {
        readonly Camera cam;
        readonly LaserCannon cannon;
        readonly GameSettings settings;
        Collider2D locked;
        Vector2 lockedOffset;
        float lockedTime = float.NegativeInfinity;

        public Targeting(Camera cam, LaserCannon cannon, GameSettings settings)
        {
            this.cam = cam;
            this.cannon = cannon;
            this.settings = settings;
        }

        public Collider2D AimTarget { get; private set; }
        public float AimSeconds { get; private set; }

        public void ResetAim()
        {
            AimTarget = null;
            AimSeconds = 0f;
        }

        public void Clear()
        {
            ResetAim();
            locked = null;
            lockedTime = float.NegativeInfinity;
        }

        public void UpdateAim(bool gazeUsable, Vector2 screenPoint, float now, float deltaTime)
        {
            if (!gazeUsable || cam == null || !cam.pixelRect.Contains(screenPoint)) { ResetAim(); return; }
            Vector2 world = cam.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, -cam.transform.position.z));
            Physics2D.SyncTransforms();

            Collider2D candidate = FindTarget(world);
            if (candidate == null)
            {
                bool jitter = AimTarget != null && IsTarget(AimTarget) && now - lockedTime <= settings.aimLossToleranceSeconds;
                if (!jitter) ResetAim();
                return;
            }

            Vector2 aim = candidate.OverlapPoint(world) ? world : (Vector2)candidate.bounds.center;
            Vector2 hitPoint;
            if (!cannon.HasClearShot(candidate, aim, out hitPoint)) { ResetAim(); return; }

            if (AimTarget != candidate) { AimTarget = candidate; AimSeconds = 0f; }
            else AimSeconds += deltaTime;
            locked = candidate;
            lockedOffset = aim - (Vector2)candidate.transform.position;
            lockedTime = now;
        }

        public Collider2D LockedTarget(float now, float graceSeconds)
        {
            return locked != null && IsTarget(locked) && now - lockedTime <= graceSeconds ? locked : null;
        }

        public Vector2 LockedAimPoint
        {
            get { return locked == null ? Vector2.zero : (Vector2)locked.transform.position + lockedOffset; }
        }

        public static bool IsTarget(Collider2D candidate)
        {
            return candidate != null && candidate.enabled && candidate.gameObject.activeInHierarchy
                && candidate.GetComponent<MovingTarget>() != null;
        }

        Collider2D FindTarget(Vector2 world)
        {
            Collider2D exact = FrontmostTargetAt(world);
            if (exact != null || settings.gazeToleranceWorld <= 0f) return exact;
            Collider2D nearest = null;
            float nearestDistance = float.PositiveInfinity;
            foreach (Collider2D candidate in Physics2D.OverlapCircleAll(world, settings.gazeToleranceWorld))
            {
                if (!IsVisibleTarget(candidate)) continue;
                float distance = Vector2.Distance(world, candidate.bounds.ClosestPoint(world));
                if (distance < nearestDistance) { nearestDistance = distance; nearest = candidate; }
            }
            return nearest;
        }

        Collider2D FrontmostTargetAt(Vector2 world)
        {
            Collider2D best = null;
            foreach (Collider2D candidate in Physics2D.OverlapPointAll(world))
            {
                if (!IsVisibleTarget(candidate)) continue;
                if (best == null || IsInFront(candidate, best)) best = candidate;
            }
            return best;
        }

        static bool IsVisibleTarget(Collider2D candidate)
        {
            if (!IsTarget(candidate)) return false;
            Renderer model = candidate.GetComponentInChildren<Renderer>();
            return model != null && model.enabled;
        }

        bool IsInFront(Collider2D candidate, Collider2D previous)
        {
            float forward = Vector3.Dot(candidate.transform.position - previous.transform.position, cam.transform.forward);
            return forward < 0f;
        }
    }
}
