// Team: Jan Biernacki, Kürsat, Paul, Malte | Matriculation numbers: TO BE PROVIDED
using UnityEngine;

namespace GazeLab
{
    /// <summary>The stationary ship: reports collisions, can be reset and crashes after game over.</summary>
    public class PlayerController : MonoBehaviour
    {
        GameController game;
        Vector3 startPosition;
        Quaternion startRotation;
        Rigidbody2D body;
        Collider2D[] parts;

        public void Init(GameController owner)
        {
            game = owner;
            startPosition = transform.position;
            startRotation = transform.rotation;
            body = GetComponent<Rigidbody2D>();
            parts = GetComponentsInChildren<Collider2D>();
            ResetShip();
        }

        public void ResetShip()
        {
            transform.position = startPosition;
            transform.rotation = startRotation;
            if (body != null)
            {
                body.velocity = Vector2.zero;
                body.angularVelocity = 0f;
                body.gravityScale = 0f;
                body.constraints = RigidbodyConstraints2D.FreezeAll;
            }
            SetCollidersEnabled(true);
        }

        /// <summary>Game over: the ship stops colliding and falls out of the screen.</summary>
        public void Crash()
        {
            SetCollidersEnabled(false);
            if (body == null) return;
            body.constraints = RigidbodyConstraints2D.None;
            body.gravityScale = 1f;
        }

        void SetCollidersEnabled(bool enabled)
        {
            if (parts == null) return;
            foreach (Collider2D part in parts) part.enabled = enabled;
        }

        void OnCollisionEnter2D(Collision2D other)
        {
            if (game == null) return;
            game.OnShipCollision(other.gameObject.GetComponent<MovingTarget>());
        }
    }
}
