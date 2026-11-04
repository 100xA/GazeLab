// Team: Jan Biernacki, Kürsat, Paul, Malte | Matriculation numbers: TO BE PROVIDED
using UnityEngine;

namespace GazeLab
{
    /// <summary>A shootable object that drifts from right to left while the round is running.</summary>
    public abstract class MovingTarget : MonoBehaviour
    {
        const float DespawnX = -16f;
        GameController game;

        /// <summary>World units per second towards the ship.</summary>
        protected abstract float Speed { get; }
        /// <summary>Rotation in degrees per second.</summary>
        protected abstract float Spin { get; }
        public abstract bool DamagesShip { get; }
        public abstract int Points(GameSettings settings);

        public void Init(GameController owner) { game = owner; }

        void Update()
        {
            if (game == null || !game.IsRunning) return;
            transform.Rotate(0f, 0f, Spin * Time.deltaTime, Space.Self);
            transform.Translate(-Speed * Time.deltaTime, 0f, 0f, Space.World);
            if (transform.position.x < DespawnX) Destroy(gameObject);
        }

        /// <summary>Removes the target at once (colliders off) so it cannot be hit a second time.</summary>
        public void Remove()
        {
            foreach (Collider2D part in GetComponentsInChildren<Collider2D>()) part.enabled = false;
            Destroy(gameObject);
        }
    }
}
