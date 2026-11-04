// Team: Jan Biernacki, Kürsat, Paul, Malte | Matriculation numbers: TO BE PROVIDED
using UnityEngine;

public class GameController : MonoBehaviour
{
    /// <summary>How an aimed target is confirmed (Midas-touch solutions).</summary>
    public enum SelectionMode { Dwell, Blink, Nod, Button }

    // Existing serialized scene references are preserved.
    public GameObject laserbeam, lasercannon, spacerock, satellite, spaceship;
    public int SpawnIntervalSeconds = 4;
    public float DwellSeconds = 0.6f;
    [Tooltip("Shortest eye closure that counts as a deliberate blink (natural blinks are shorter).")]
    public float BlinkMinSeconds = 0.15f;
    [Tooltip("Longest eye closure that still counts as a blink; longer closures pause the game.")]
    public float BlinkMaxSeconds = 1.5f;
    [Tooltip("How long a target stays locked after the gaze left it (blink, nod or button).")]
    public float LockGraceSeconds = 0.35f;
    [Tooltip("Gaze that misses every target by less than this (world units, about 65 px at 1080p) still selects the nearest target.")]
    public float GazeToleranceWorld = 0.6f;
    [Tooltip("Dwell progress survives gaze jitter that leaves the target for at most this long.")]
    public float DwellLossToleranceSeconds = 0.15f;
    public int points, damage;
    public SelectionMode Mode = SelectionMode.Dwell;
    public bool InMenu { get; private set; }
    public float DwellProgress { get; private set; }
    /// <summary>The target currently under gaze with a clear line of fire.</summary>
    public Collider2D DwellTarget { get; private set; }
    public bool GameOver { get; private set; }
    public bool IsLineOfFireClear { get; private set; }
    public float LastShotTime { get; private set; }
    /// <summary>Feedback about the last confirmation attempt (shown in the HUD).</summary>
    public string LastConfirmText { get; private set; }
    public float LastConfirmTime { get; private set; }
    public bool IsPaused
    {
        get { return !GameOver && !InMenu && (input == null || (!input.HasValidGaze && !InBlinkGrace)); }
    }
    /// <summary>Blink mode: closing the eyes briefly must not pause the game.</summary>
    public bool InBlinkGrace
    {
        get { return Mode == SelectionMode.Blink && input != null && input.EyesClosedCandidate && input.EyesClosedSeconds <= BlinkMaxSeconds; }
    }

    private Camera cam;
    private GazeInput input;
    private LineRenderer beam;
    private float spawnElapsed, dwellElapsed;
    private int spawnSequence;
    private Vector3 shipStartPosition;
    private Quaternion shipStartRotation;
    private Rigidbody2D shipBody;
    private Collider2D[] shipColliders;
    // Last aimed target, kept briefly so a blink/nod/button press can confirm it.
    private Collider2D lockedTarget;
    private Vector2 lockedOffset;
    private float lockedTime = float.NegativeInfinity;

    private void Awake()
    {
        cam = Camera.main;
        input = GetComponent<GazeInput>();
        if (input == null) input = gameObject.AddComponent<GazeInput>();
        LastConfirmText = "";
        LastConfirmTime = float.NegativeInfinity;
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

    private void Start() { ShowMenu(); }

    private void Update()
    {
        if (beam != null && beam.enabled && Time.time - LastShotTime >= 0.15f) beam.enabled = false;
        if (input != null) input.trackHeadPose = !InMenu && !GameOver && Mode == SelectionMode.Nod;
        if (GameOver || InMenu) return;
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
        if (GameOver || InMenu) { ResetDwell(); ClearLock(); return; }

        // 1) Explicit confirmation (blink / nod / button) acts on the target locked in earlier frames.
        string confirmation;
        if (Mode != SelectionMode.Dwell && ConfirmationRequested(out confirmation)) FireLockedTarget(confirmation);

        // 2) Update what the player is aiming at with the current gaze sample.
        if (IsPaused || input == null || !input.HasValidGaze || input.IsOverControls || cam == null || lasercannon == null)
        { ResetDwell(); return; }
        Vector2 screen = input.ScreenPoint;
        if (!cam.pixelRect.Contains(screen)) { ResetDwell(); return; }
        Vector3 world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -cam.transform.position.z));
        Physics2D.SyncTransforms();
        Collider2D candidate = FindFrontmostTarget(world);
        if (candidate == null)
        {
            // Brief jitter off the target (tracker noise) keeps the dwell progress frozen.
            if (DwellTarget != null && IsTarget(DwellTarget) && Time.unscaledTime - lockedTime <= DwellLossToleranceSeconds) return;
            ResetDwell();
            return;
        }
        // Gaze inside the target aims at that spot, a near miss aims at the target centre.
        Vector2 aim = candidate.OverlapPoint(world) ? (Vector2)world : (Vector2)candidate.bounds.center;
        RaycastHit2D hit;
        if (!FirstHitIsTarget(aim, candidate, out hit)) { ResetDwell(); return; }
        IsLineOfFireClear = true;
        if (DwellTarget != candidate) { DwellTarget = candidate; dwellElapsed = 0f; }
        else dwellElapsed += Time.deltaTime;
        lockedTarget = candidate;
        lockedOffset = aim - (Vector2)candidate.transform.position;
        lockedTime = Time.unscaledTime;

        if (Mode != SelectionMode.Dwell) { DwellProgress = 0f; return; }
        DwellProgress = Mathf.Clamp01(dwellElapsed / Mathf.Max(0.1f, DwellSeconds));
        if (DwellProgress >= 1f)
        {
            // Recheck the first actual cannon hit immediately before firing.
            if (FirstHitIsTarget(aim, candidate, out hit)) FirePhaserBeam(candidate, hit.point);
            ResetDwell();
        }
    }

    private bool ConfirmationRequested(out string description)
    {
        description = null;
        switch (Mode)
        {
            case SelectionMode.Blink:
                if (!input.BlinkEnded) return false;
                float closed = input.LastBlinkSeconds;
                if (closed < BlinkMinSeconds)
                {
                    // Natural blinks are ignored: this is the Midas-touch protection of this mode.
                    Feedback("Blink " + closed.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + " s - too short, ignored");
                    return false;
                }
                if (closed > BlinkMaxSeconds)
                {
                    Feedback("Eyes closed " + closed.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " s - too long, ignored");
                    return false;
                }
                description = "Blink " + closed.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + " s";
                return true;
            case SelectionMode.Nod:
                if (!input.NodDetected) return false;
                description = "Nod";
                return true;
            case SelectionMode.Button:
                if (!Application.isFocused) return false;
                if (Input.GetKeyDown(KeyCode.Space)) { description = "Space"; return true; }
                if (Input.GetMouseButtonDown(0) && !GazeHud.ControlsContain(Input.mousePosition))
                { description = "Click"; return true; }
                return false;
            default:
                return false;
        }
    }

    private void FireLockedTarget(string confirmation)
    {
        float grace = LockGraceSeconds + (Mode == SelectionMode.Blink ? input.LastBlinkSeconds : 0f);
        Collider2D target = lockedTarget != null && Time.unscaledTime - lockedTime <= grace ? lockedTarget : null;
        if (target == null || !IsTarget(target))
        {
            Feedback(confirmation + " - no target locked");
            return;
        }
        // Targets keep moving during a blink or nod, so aim at the same spot on the target.
        Vector2 aim = (Vector2)target.transform.position + lockedOffset;
        Physics2D.SyncTransforms();
        RaycastHit2D hit;
        if (FirstHitIsTarget(aim, target, out hit))
        {
            FirePhaserBeam(target, hit.point);
            Feedback(confirmation + " - fired");
        }
        else Feedback(confirmation + " - line of fire blocked");
        ResetDwell();
        ClearLock();
    }

    private void Feedback(string text)
    {
        LastConfirmText = text;
        LastConfirmTime = Time.unscaledTime;
    }

    private Collider2D FindFrontmostTarget(Vector2 world)
    {
        Collider2D exact = FindFrontmostTargetAt(world);
        if (exact != null || GazeToleranceWorld <= 0f) return exact;
        // Eye trackers are accurate to roughly 1 degree: accept the nearest target close to the gaze point.
        Collider2D nearest = null;
        float nearestDistance = float.PositiveInfinity;
        foreach (Collider2D candidate in Physics2D.OverlapCircleAll(world, GazeToleranceWorld))
        {
            if (!IsTarget(candidate)) continue;
            SpriteRenderer renderer = candidate.GetComponent<SpriteRenderer>();
            if (renderer == null || !renderer.enabled || renderer.sprite == null) continue;
            float distance = Vector2.Distance(world, candidate.bounds.ClosestPoint(world));
            if (distance < nearestDistance) { nearestDistance = distance; nearest = candidate; }
        }
        return nearest;
    }

    private Collider2D FindFrontmostTargetAt(Vector2 world)
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

    public void CancelSelection() { ResetDwell(); ClearLock(); }

    private void ResetDwell()
    {
        DwellTarget = null;
        dwellElapsed = 0f;
        DwellProgress = 0f;
        IsLineOfFireClear = false;
    }

    private void ClearLock()
    {
        lockedTarget = null;
        lockedTime = float.NegativeInfinity;
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
        ClearLock();
        if (beam != null) beam.enabled = false;
        if (shipColliders != null) foreach (Collider2D collider in shipColliders) collider.enabled = false;
        if (shipBody != null) { shipBody.constraints = RigidbodyConstraints2D.None; shipBody.gravityScale = 1f; }
    }

    private void ResetShip()
    {
        if (spaceship != null) { spaceship.transform.position = shipStartPosition; spaceship.transform.rotation = shipStartRotation; }
        if (shipBody != null)
        {
            shipBody.velocity = Vector2.zero;
            shipBody.angularVelocity = 0f;
            shipBody.gravityScale = 0f;
            shipBody.constraints = RigidbodyConstraints2D.FreezeAll;
        }
        if (shipColliders != null) foreach (Collider2D collider in shipColliders) collider.enabled = true;
    }

    private void BeginRound()
    {
        points = 0;
        damage = 0;
        spawnElapsed = 0f;
        spawnSequence = 0;
        LastShotTime = float.NegativeInfinity;
        LastConfirmText = "";
        GameOver = false;
        InMenu = false;
        ResetDwell();
        ClearLock();
        ResetShip();
        if (beam != null) beam.enabled = false;
        SpawnRock(2.4f);
        SpawnSatellite(-2.4f);
    }

    private static void RemoveTargets()
    {
        foreach (RockController rock in FindObjectsOfType<RockController>())
        { rock.gameObject.SetActive(false); Destroy(rock.gameObject); }
        foreach (SatelliteController satelliteObject in FindObjectsOfType<SatelliteController>())
        { satelliteObject.gameObject.SetActive(false); Destroy(satelliteObject.gameObject); }
    }

    /// <summary>Starts a fresh round with the chosen shooting mode (selected by gaze in the menu).</summary>
    public void StartMode(SelectionMode mode)
    {
        Mode = mode;
        RestartGame();
    }

    /// <summary>Restarts with the current shooting mode.</summary>
    public void RestartGame()
    {
        RemoveTargets();
        BeginRound();
    }

    /// <summary>Shows the gaze-operated mode menu; no targets exist and nothing can be fired.</summary>
    public void ShowMenu()
    {
        RemoveTargets();
        InMenu = true;
        GameOver = false;
        damage = 0;
        ResetDwell();
        ClearLock();
        ResetShip();
        if (beam != null) beam.enabled = false;
    }

    public static string ModeName(SelectionMode mode)
    {
        switch (mode)
        {
            case SelectionMode.Blink: return "Aim + Blink";
            case SelectionMode.Nod: return "Aim + Nod";
            case SelectionMode.Button: return "Aim + Space / Click";
            default: return "Aim + Hold (Dwell)";
        }
    }
}
