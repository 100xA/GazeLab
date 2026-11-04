// Team: Jan Biernacki, Kürsat, Paul, Malte | Matriculation numbers: TO BE PROVIDED
using UnityEngine;

namespace GazeLab
{
    /// <summary>Creates rocks and satellites at the right edge and removes them between rounds.</summary>
    public sealed class TargetSpawner
    {
        readonly GameController game;
        readonly GameObject rockPrefab, satellitePrefab;
        readonly GameSettings settings;
        Sprite[] satelliteSprites;
        int sortingOrder;

        public TargetSpawner(GameController game, GameObject rockPrefab, GameObject satellitePrefab, GameSettings settings)
        {
            this.game = game;
            this.rockPrefab = rockPrefab;
            this.satellitePrefab = satellitePrefab;
            this.settings = settings;
        }

        public void SpawnRandom()
        {
            if (Random.value < 0.5f) SpawnRock(Random.Range(-settings.rockSpawnHalfHeight, settings.rockSpawnHalfHeight));
            else SpawnSatellite(Random.Range(-settings.satelliteSpawnHalfHeight, settings.satelliteSpawnHalfHeight));
        }

        public void SpawnRock(float y) { Spawn(rockPrefab, y, null); }

        public void SpawnSatellite(float y)
        {
            if (satelliteSprites == null) satelliteSprites = Resources.LoadAll<Sprite>("Sprites");
            Sprite sprite = satelliteSprites.Length > 0 ? satelliteSprites[Random.Range(0, satelliteSprites.Length)] : null;
            Spawn(satellitePrefab, y, sprite);
        }

        public void ClearAll()
        {
            foreach (MovingTarget target in Object.FindObjectsOfType<MovingTarget>())
            {
                target.gameObject.SetActive(false);
                Object.Destroy(target.gameObject);
            }
            sortingOrder = 0;
        }

        void Spawn(GameObject prefab, float y, Sprite sprite)
        {
            if (prefab == null) return;
            GameObject spawned = Object.Instantiate(prefab);
            spawned.transform.position = new Vector3(settings.spawnX, y, 0f);

            // Targets are moved by script; physics must not let them coast while the game is paused.
            Rigidbody2D body = spawned.GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.bodyType = RigidbodyType2D.Kinematic;
                body.velocity = Vector2.zero;
                body.angularVelocity = 0f;
                body.gravityScale = 0f;
            }
            SpriteRenderer renderer = spawned.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                if (sprite != null) renderer.sprite = sprite;
                renderer.sortingOrder = ++sortingOrder; // newer targets are drawn (and selected) in front
            }
            MovingTarget target = spawned.GetComponent<MovingTarget>();
            if (target != null) target.Init(game);
        }
    }
}
