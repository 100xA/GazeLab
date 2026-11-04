// Team: Jan Biernacki, Kürsat, Paul, Malte | Matriculation numbers: TO BE PROVIDED
using UnityEngine;

namespace GazeLab
{
    /// <summary>
    /// A shootable object that drifts from right to left while the round is running. Its 3D model
    /// is attached on Awake (see <see cref="ToonModels"/>) and animated on top of the drift.
    /// </summary>
    public abstract class MovingTarget : MonoBehaviour
    {
        const float DespawnX = -16f;

        GameController game;
        Transform model;
        float age;

        public abstract TargetKind Kind { get; }
        public abstract bool DamagesShip { get; }
        public abstract int Points(GameSettings settings);

        /// <summary>World units per second towards the ship.</summary>
        protected abstract float Speed { get; }
        /// <summary>Rotation in the screen plane in degrees per second.</summary>
        protected abstract float Spin { get; }

        /// <summary>Picks the model (and any random motion parameters) for this target.</summary>
        protected abstract Mesh CreateModel();
        /// <summary>3D motion of the model on top of the spin in the screen plane.</summary>
        protected abstract void Animate(Transform model, float age);

        public void Init(GameController owner) { game = owner; }

        void Awake()
        {
            model = ToonModels.Attach(transform, CreateModel());
            Animate(model, 0f);
        }

        void Update()
        {
            if (game == null || !game.IsRunning) return;
            age += Time.deltaTime;
            transform.Rotate(0f, 0f, Spin * Time.deltaTime, Space.Self);
            transform.Translate(-Speed * Time.deltaTime, 0f, 0f, Space.World);
            Animate(model, age);
            if (transform.position.x < DespawnX) Destroy(gameObject);
        }

        /// <summary>Destroyed by the laser or a collision: explosion effect, then <see cref="Remove"/>.</summary>
        public void Explode()
        {
            Collider2D body = GetComponent<Collider2D>();
            float radius = body != null ? Mathf.Max(body.bounds.extents.x, body.bounds.extents.y) * 0.6f : 1f;
            ShotEffects.Explode(transform.position, radius, Kind);
            Remove();
        }

        /// <summary>Removes the target at once (colliders off) so it cannot be hit a second time.</summary>
        public void Remove()
        {
            foreach (Collider2D part in GetComponentsInChildren<Collider2D>()) part.enabled = false;
            Destroy(gameObject);
        }
    }
}
