using UnityEngine;

namespace GazeLab
{
    public abstract class MovingTarget : MonoBehaviour
    {
        const float DespawnX = -16f;

        GameController game;
        Transform model;
        float age;

        public abstract TargetKind Kind { get; }
        public abstract bool DamagesShip { get; }
        public abstract int Points(GameSettings settings);

        protected abstract float Speed { get; }
        protected abstract float Spin { get; }

        protected abstract Mesh CreateModel();
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

        public void Explode()
        {
            Collider2D body = GetComponent<Collider2D>();
            float radius = body != null ? Mathf.Max(body.bounds.extents.x, body.bounds.extents.y) * 0.6f : 1f;
            ShotEffects.Explode(transform.position, radius, Kind);
            Remove();
        }

        public void Remove()
        {
            foreach (Collider2D part in GetComponentsInChildren<Collider2D>()) part.enabled = false;
            Destroy(gameObject);
        }
    }
}
