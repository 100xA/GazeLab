// Team: Jan Biernacki, Kürsat, Paul, Malte | Matriculation numbers: TO BE PROVIDED
using UnityEngine;

namespace GazeLab
{
    /// <summary>
    /// Finds the target under the gaze (with a tolerance for tracker inaccuracy), checks the line of
    /// fire and keeps the last aimed target locked for a short time, so a blink, nod or key press
    /// can still confirm it after the eyes moved.
    /// </summary>
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

        /// <summary>Target currently under the gaze with a clear line of fire, or null.</summary>
        public Collider2D AimTarget { get; private set; }
        /// <summary>How long <see cref="AimTarget"/> has been looked at without interruption.</summary>
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
                // Brief jitter off the target keeps the progress frozen instead of resetting it.
                bool jitter = AimTarget != null && IsTarget(AimTarget) && now - lockedTime <= settings.aimLossToleranceSeconds;
                if (!jitter) ResetAim();
                return;
            }

            // Gaze inside the target aims at that spot, a near miss aims at the target centre.
            Vector2 aim = candidate.OverlapPoint(world) ? world : (Vector2)candidate.bounds.center;
            Vector2 hitPoint;
            if (!cannon.HasClearShot(candidate, aim, out hitPoint)) { ResetAim(); return; }

            if (AimTarget != candidate) { AimTarget = candidate; AimSeconds = 0f; }
            else AimSeconds += deltaTime;
            locked = candidate;
            lockedOffset = aim - (Vector2)candidate.transform.position;
            lockedTime = now;
        }

        /// <summary>The target aimed at within the last <paramref name="graceSeconds"/>, or null.</summary>
        public Collider2D LockedTarget(float now, float graceSeconds)
        {
            return locked != null && IsTarget(locked) && now - lockedTime <= graceSeconds ? locked : null;
        }

        /// <summary>Same spot on the locked target as when it was aimed at (targets keep moving).</summary>
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
            // Eye trackers are accurate to about one degree: accept the nearest target close by.
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

        // The models are drawn by depth (see GazeLab/Toon), so the target nearer to the camera is the
        // one seen in front; selecting it keeps visible overlaps and gaze acquisition in agreement.
        bool IsInFront(Collider2D candidate, Collider2D previous)
        {
            float forward = Vector3.Dot(candidate.transform.position - previous.transform.position, cam.transform.forward);
            return forward < 0f;
        }
    }
}
