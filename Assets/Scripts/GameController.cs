// Author: Jan Biernacki. Matriculation number: TO BE PROVIDED.
using UnityEngine;

public class GameController : MonoBehaviour
{
    // Existing serialized scene references are preserved.
    public GameObject laserbeam, lasercannon, spacerock, satellite, spaceship;
    public int SpawnIntervalSeconds = 4;
    public float DwellSeconds = 0.6f;
    public int points, damage;
    public float DwellProgress { get; private set; }
    public Collider2D DwellTarget { get; private set; }
    public bool GameOver { get; private set; }
    public bool IsLineOfFireClear { get; private set; }
    public float LastShotTime { get; private set; }
    public bool IsPaused { get { return !GameOver && (input == null || !input.HasValidGaze); } }

    private Camera cam;
    private GazeInput input;
    private LineRenderer beam;
    private float spawnElapsed, dwellElapsed;
    private int spawnSequence;
    private Vector3 shipStartPosition;
    private Quaternion shipStartRotation;
    private Rigidbody2D shipBody;
    private Collider2D[] shipColliders;

    private void Awake()
    {
        cam = Camera.main;
        input = GetComponent<GazeInput>();
        if (input == null) input = gameObject.AddComponent<GazeInput>();
        if (laserbeam != null)
        {
            beam = laserbeam.GetComponent<LineRenderer>();
            if (beam != null)
            {
                beam.positionCount = 2;
                beam.useWorldSpace = true;
                beam.sortingOrder = 10000;
                beam.enabled = false;
            }
        }
        if (spaceship != null)
        {
            shipStartPosition = spaceship.transform.position;
            shipStartRotation = spaceship.transform.rotation;
            shipBody = spaceship.GetComponent<Rigidbody2D>();
            shipColliders = spaceship.GetComponentsInChildren<Collider2D>();
            if (shipBody != null) shipBody.constraints = RigidbodyConstraints2D.FreezeAll;
        }
    }

    private void Start() { BeginRound(); }

    private void Update()
    {
        if (beam != null && beam.enabled && Time.time - LastShotTime >= 0.15f) beam.enabled = false;
        if (GameOver) return;
        if (damage >= 4) { EndGame(); return; }
        if (IsPaused) return;
        spawnElapsed += Time.deltaTime;
        if (spawnElapsed >= Mathf.Max(1, SpawnIntervalSeconds))
        {
            spawnElapsed = 0f;
            if (Random.Range(0, 2) == 0) SpawnRock(Random.Range(-3.35f, 3.35f));
            else SpawnSatellite(Random.Range(-3.7f, 3.7f));
        }
    }

    // Consume the fresh Update gaze sample after target movement.
    private void LateUpdate()
    {
        if (GameOver || IsPaused || input.IsOverControls || cam == null || lasercannon == null)
        { ResetDwell(); return; }
        Vector2 screen = input.ScreenPoint;
        if (!cam.pixelRect.Contains(screen)) { ResetDwell(); return; }
        Vector3 world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -cam.transform.position.z));
        Physics2D.SyncTransforms();
        Collider2D candidate = FindFrontmostTarget(world);
        RaycastHit2D hit;
        if (candidate == null || !FirstHitIsTarget(world, candidate, out hit))
        { ResetDwell(); return; }
        IsLineOfFireClear = true;
        if (DwellTarget != candidate) { DwellTarget = candidate; dwellElapsed = 0f; }
        else dwellElapsed += Time.deltaTime;
        DwellProgress = Mathf.Clamp01(dwellElapsed / Mathf.Max(0.1f, DwellSeconds));
        if (DwellProgress >= 1f)
        {
            // Recheck the first actual cannon hit immediately before firing.
            if (FirstHitIsTarget(world, candidate, out hit)) FirePhaserBeam(candidate, hit.point);
            ResetDwell();
        }
    }

    private Collider2D FindFrontmostTarget(Vector2 world)
    {
        Collider2D best = null;
        SpriteRenderer bestRenderer = null;
        foreach (Collider2D candidate in Physics2D.OverlapPointAll(world))
        {
            if (!IsTarget(candidate)) continue;
            SpriteRenderer renderer = candidate.GetComponent<SpriteRenderer>();
            if (renderer == null || !renderer.enabled || renderer.sprite == null) continue;
            if (best == null || IsInFront(renderer, bestRenderer)) { best = candidate; bestRenderer = renderer; }
        }
        return best;
    }

    private bool IsInFront(SpriteRenderer candidate, SpriteRenderer previous)
    {
        int layer = SortingLayer.GetLayerValueFromID(candidate.sortingLayerID);
        int previousLayer = SortingLayer.GetLayerValueFromID(previous.sortingLayerID);
        if (layer != previousLayer) return layer > previousLayer;
        if (candidate.sortingOrder != previous.sortingOrder) return candidate.sortingOrder > previous.sortingOrder;
        return Vector3.Distance(cam.transform.position, candidate.transform.position)
            < Vector3.Distance(cam.transform.position, previous.transform.position);
    }

    private static bool IsTarget(Collider2D candidate)
    {
        return candidate != null && candidate.enabled && candidate.gameObject.activeInHierarchy
            && (candidate.GetComponent<RockController>() != null || candidate.GetComponent<SatelliteController>() != null);
    }

    private bool IsPlayer(Collider2D candidate)
    {
        return candidate.CompareTag("Player") || (spaceship != null
            && (candidate.gameObject == spaceship || candidate.transform.IsChildOf(spaceship.transform)));
    }

    private bool FirstHitIsTarget(Vector2 aimPoint, Collider2D target, out RaycastHit2D firstHit)
    {
        firstHit = new RaycastHit2D();
        float nearest = float.PositiveInfinity;
        foreach (RaycastHit2D hit in Physics2D.LinecastAll(lasercannon.transform.position, aimPoint))
        {
            if (hit.collider == null || !hit.collider.enabled || IsPlayer(hit.collider)) continue;
            if (hit.distance < nearest) { nearest = hit.distance; firstHit = hit; }
        }
        return IsTarget(target) && firstHit.collider == target;
    }

    private void FirePhaserBeam(Collider2D target, Vector2 hitPoint)
    {
        if (GameOver || !IsTarget(target)) return;
        LastShotTime = Time.time;
        if (beam != null)
        {
            beam.SetPosition(0, lasercannon.transform.position);
            beam.SetPosition(1, new Vector3(hitPoint.x, hitPoint.y, 0f));
            beam.enabled = true;
        }
        // Satellites remain selectable: the player must avoid their penalty.
        points += target.GetComponent<SatelliteController>() != null ? -100 : 20;
        foreach (Collider2D collider in target.GetComponentsInChildren<Collider2D>()) collider.enabled = false;
        Destroy(target.gameObject);
    }

    public void CancelSelection() { ResetDwell(); }

    private void ResetDwell()
    {
        DwellTarget = null;
        dwellElapsed = 0f;
        DwellProgress = 0f;
        IsLineOfFireClear = false;
    }

    private void SpawnRock(float y)
    {
        if (spacerock != null) PrepareSpawn(Instantiate(spacerock), y);
    }

    private void SpawnSatellite(float y)
    {
        if (satellite == null) return;
        GameObject spawned = Instantiate(satellite);
        Sprite[] sprites = Resources.LoadAll<Sprite>("Sprites");
        SpriteRenderer renderer = spawned.GetComponent<SpriteRenderer>();
        if (renderer != null && sprites.Length > 0) renderer.sprite = sprites[Random.Range(0, sprites.Length)];
        PrepareSpawn(spawned, y);
    }

    private void PrepareSpawn(GameObject spawned, float y)
    {
        spawned.transform.position = new Vector3(13f, y, 0f);
        // Script-controlled targets must not coast under physics during gaze loss.
        Rigidbody2D body = spawned.GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.bodyType = RigidbodyType2D.Kinematic;
            body.velocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.gravityScale = 0f;
        }
        SpriteRenderer renderer = spawned.GetComponent<SpriteRenderer>();
        // Explicit sorting makes visible overlaps agree with gaze acquisition.
        if (renderer != null) renderer.sortingOrder = ++spawnSequence;
    }

    private void EndGame()
    {
        GameOver = true;
        damage = 4;
        ResetDwell();
        if (beam != null) beam.enabled = false;
        if (shipColliders != null) foreach (Collider2D collider in shipColliders) collider.enabled = false;
        if (shipBody != null) { shipBody.constraints = RigidbodyConstraints2D.None; shipBody.gravityScale = 1f; }
    }

    private void BeginRound()
    {
        points = 0;
        damage = 0;
        spawnElapsed = 0f;
        spawnSequence = 0;
        LastShotTime = float.NegativeInfinity;
        GameOver = false;
        ResetDwell();
        if (spaceship != null) { spaceship.transform.position = shipStartPosition; spaceship.transform.rotation = shipStartRotation; }
        if (shipBody != null)
        {
            shipBody.velocity = Vector2.zero;
            shipBody.angularVelocity = 0f;
            shipBody.gravityScale = 0f;
            shipBody.constraints = RigidbodyConstraints2D.FreezeAll;
        }
        if (shipColliders != null) foreach (Collider2D collider in shipColliders) collider.enabled = true;
        if (beam != null) beam.enabled = false;
        SpawnRock(2.4f);
        SpawnSatellite(-2.4f);
    }

    public void RestartGame()
    {
        foreach (RockController rock in FindObjectsOfType<RockController>())
        { rock.gameObject.SetActive(false); Destroy(rock.gameObject); }
        foreach (SatelliteController satelliteObject in FindObjectsOfType<SatelliteController>())
        { satelliteObject.gameObject.SetActive(false); Destroy(satelliteObject.gameObject); }
        BeginRound();
    }
}
