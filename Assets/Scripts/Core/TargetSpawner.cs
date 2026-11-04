// Team: Jan Biernacki, Kürsat, Paul, Malte | Matriculation numbers: TO BE PROVIDED
using UnityEngine;

namespace GazeLab
{
    /// <summary>Creates rocks and satellites at the right edge and removes them between rounds.</summary>
    public sealed class TargetSpawner
    {
        // Newer targets spawn closer to the camera, so they are drawn (and selected) in front.
        // The z layers repeat; targets live far shorter than one cycle of spawns.
        const int DepthLayers = 20;
        const float DepthStep = 0.1f;

        readonly GameController game;
        readonly GameObject rockPrefab, satellitePrefab;
        readonly GameSettings settings;
        int spawnCount;

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

        public void SpawnRock(float y) { Spawn(rockPrefab, y); }

        public void SpawnSatellite(float y) { Spawn(satellitePrefab, y); }

        public void ClearAll()
        {
            foreach (MovingTarget target in Object.FindObjectsOfType<MovingTarget>())
            {
                target.gameObject.SetActive(false);
                Object.Destroy(target.gameObject);
            }
            spawnCount = 0;
        }

        void Spawn(GameObject prefab, float y)
        {
            if (prefab == null) return;
            GameObject spawned = Object.Instantiate(prefab);
            float z = -(++spawnCount % DepthLayers) * DepthStep;
            spawned.transform.position = new Vector3(settings.spawnX, y, z);

            // Targets are moved by script; physics must not let them coast while the game is paused.
            Rigidbody2D body = spawned.GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.bodyType = RigidbodyType2D.Kinematic;
                body.velocity = Vector2.zero;
                body.angularVelocity = 0f;
                body.gravityScale = 0f;
            }
            MovingTarget target = spawned.GetComponent<MovingTarget>();
            if (target != null) target.Init(game);
        }
    }
}
