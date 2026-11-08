// Jan Biernacki, 3275061; Malte Lührs, 3138900; Kürsat Yurdakul, 3282791; Paul Prigge, 3097391
using UnityEngine;

namespace GazeLab
{
    public class PlayerController : MonoBehaviour
    {
        const float RecoilSeconds = 0.25f;

        GameController game;
        Vector3 startPosition;
        Quaternion startRotation;
        Rigidbody2D body;
        Collider2D[] parts;
        Transform model, flames;

        public void Init(GameController owner)
        {
            game = owner;
            startPosition = transform.position;
            startRotation = transform.rotation;
            body = GetComponent<Rigidbody2D>();
            parts = GetComponentsInChildren<Collider2D>();
            if (model == null)
            {
                model = ToonModels.Attach(transform, ToonModels.Ship);
                flames = ToonModels.Attach(model, ToonModels.Flames, "Flames");
                flames.localPosition = ToonModels.FlamesPivot;
            }
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

        public void Crash()
        {
            SetCollidersEnabled(false);
            if (body == null) return;
            body.constraints = RigidbodyConstraints2D.None;
            body.gravityScale = 1f;
        }

        void Update()
        {
            if (model == null) return;
            float t = Time.time;
            float sinceShot = game != null ? Time.unscaledTime - game.Cannon.LastShotTime : float.PositiveInfinity;
            float kick = sinceShot >= 0f && sinceShot < RecoilSeconds ? 1f - sinceShot / RecoilSeconds : 0f;
            model.localRotation = Quaternion.Euler(0f, Mathf.Sin(t * 1.1f) * 16f, 0f);
            model.localPosition = new Vector3(Mathf.Sin(t * 1.7f) * 0.06f, -0.22f * kick * kick, 0f);
            float flicker = Mathf.PerlinNoise(t * 9f, 0.3f);
            flames.localScale = new Vector3(0.92f + 0.12f * flicker, 0.75f + 0.5f * flicker, 1f);
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
