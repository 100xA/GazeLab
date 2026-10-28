using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class GazeLabValidation
{
    const string ActiveKey = "GazeLabValidation.Active";
    const string LiveTalonKey = "GazeLabValidation.LiveTalonNoHardware";
    const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    static double deadline;
    static double playObservedAt = -1;
    static int checks;

    static GazeLabValidation()
    {
        if (SessionState.GetBool(ActiveKey, false)) Attach();
    }

    public static void RunWithTalonNoHardware()
    {
        SessionState.SetBool(LiveTalonKey, true);
        Run();
    }

    public static void Run()
    {
        try
        {
            SessionState.SetBool(ActiveKey, true);
            Attach();
            EditorSceneManager.OpenScene("Assets/Scenes/Main.unity", OpenSceneMode.Single);
            EditorApplication.isPlaying = true;
        }
        catch (Exception exception) { Finish(exception); }
    }

    static void Attach()
    {
        deadline = EditorApplication.timeSinceStartup + 120;
        playObservedAt = -1;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
    }

    static void Tick()
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        try
        {
            if (EditorApplication.timeSinceStartup > deadline)
                throw new TimeoutException("Timed out waiting for Play Mode initialization.");
            if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            if (playObservedAt < 0) playObservedAt = EditorApplication.timeSinceStartup;
            if (EditorApplication.timeSinceStartup - playObservedAt < 1) return;
            EditorApplication.update -= Tick;
            ExecuteChecks();
            Finish(null);
        }
        catch (Exception exception) { Finish(exception); }
    }

    static void ExecuteChecks()
    {
        checks = 0;
        GameController game = Object.FindObjectOfType<GameController>();
        Check(game != null, "scene contains GameController");
        GazeInput input = game.GetComponent<GazeInput>();
        Check(input != null && !input.IsSimulation && !input.simulateMouse,
            "real gaze is the default; no silent simulation fallback");
        if (SessionState.GetBool(LiveTalonKey, false))
        {
            var receiver = game.GetComponent<TalonGazeReceiver>();
            Vector2 unused;
            bool receivedGaze = receiver != null && receiver.TryGetScreenPoint(out unused);
            Check(receiver != null && !receivedGaze && receiver.StatusText == "Talon active - no gaze/tracker",
                "actual Talon no-hardware heartbeat reaches Unity and keeps gaze invalid");
        }
        Vector3 initialShipPosition = game.spaceship.transform.position;
        Quaternion initialShipRotation = game.spaceship.transform.rotation;

        // Prevent uncontrolled frame input/movement while driving real controller methods.
        game.enabled = false;
        input.enabled = false;
        GazeHud hud = game.GetComponent<GazeHud>();
        if (hud != null) hud.enabled = false;
        RemoveTargets();
        Camera camera = Camera.main;
        Check(camera != null && camera.pixelWidth > 0 && camera.pixelHeight > 0,
            "test scene has a usable camera viewport");

        InjectGaze(input, camera, new Vector2(2f, 0f), true);
        Invoke(game, "LateUpdate");
        Check(game.points == 0 && game.DwellTarget == null && !BeamIsVisible(game),
            "empty space never emits a laser");

        Collider2D rock = SpawnFixture(game.spacerock, new Vector2(2f, 0f));
        Invoke(game, "LateUpdate");
        Check(game.DwellTarget == rock && game.DwellProgress == 0 && game.points == 0,
            "initial target acquisition does not fire");
        SetField(game, "dwellElapsed", 0.1f);
        Invoke(game, "LateUpdate");
        Check(game.points == 0 && rock.enabled && game.DwellProgress < 1f,
            "brief dwell does not fire");

        input.SetSimulation(true);
        Check(game.DwellTarget == null && game.DwellProgress == 0,
            "switching input source cancels pending selection");
        input.SetSimulation(false);
        InjectGaze(input, camera, new Vector2(2f, 0f), true);
        Invoke(game, "LateUpdate");

        Collider2D otherRock = SpawnFixture(game.spacerock, new Vector2(2f, -4f));
        SetField(game, "dwellElapsed", game.DwellSeconds * 0.8f);
        InjectGaze(input, camera, new Vector2(2f, -4f), true);
        Invoke(game, "LateUpdate");
        Check(game.DwellTarget == otherRock && game.DwellProgress == 0 && game.points == 0,
            "changing targets discards previous dwell progress");
        SetField(game, "dwellElapsed", game.DwellSeconds * 0.8f);
        SetProperty(input, "HasValidGaze", false);
        Invoke(game, "LateUpdate");
        Check(game.DwellTarget == null && game.DwellProgress == 0 && game.IsPaused,
            "invalid gaze cancels dwell and pauses gameplay");

        Object.DestroyImmediate(otherRock.gameObject);
        InjectGaze(input, camera, new Vector2(2f, 0f), true);
        Invoke(game, "LateUpdate");
        SetField(game, "dwellElapsed", game.DwellSeconds);
        GameObject blocker = new GameObject("Validation cannon-path blocker");
        blocker.transform.position = Vector3.Lerp(game.lasercannon.transform.position, rock.transform.position, 0.5f);
        blocker.AddComponent<CircleCollider2D>().radius = 0.4f;
        Physics2D.SyncTransforms();
        Invoke(game, "LateUpdate");
        Check(game.points == 0 && rock.enabled && !BeamIsVisible(game) && game.DwellTarget == null,
            "first obstacle blocks shooting through it");
        Object.DestroyImmediate(blocker);

        Physics2D.SyncTransforms();
        Invoke(game, "LateUpdate");
        SetField(game, "dwellElapsed", game.DwellSeconds);
        Invoke(game, "LateUpdate");
        Check(game.points == 20 && !rock.enabled && BeamIsVisible(game),
            "confirmed unobstructed rock scores 20 and disables its collider");
        int scoreAfterShot = game.points;
        Invoke(game, "LateUpdate");
        Check(game.points == scoreAfterShot && game.DwellTarget == null,
            "destroyed target cannot receive a repeated queued hit");

        RemoveTargets();
        Collider2D satellite = SpawnFixture(game.satellite, new Vector2(2f, 0f));
        InjectGaze(input, camera, new Vector2(2f, 0f), true);
        Invoke(game, "LateUpdate");
        SetField(game, "dwellElapsed", game.DwellSeconds);
        Invoke(game, "LateUpdate");
        Check(game.points == -80 && !satellite.enabled,
            "satellite remains selectable and applies the original 100-point penalty");

        RemoveTargets();
        rock = SpawnFixture(game.spacerock, new Vector2(2f, 0f));
        Invoke(game, "LateUpdate");
        game.damage = 4;
        Invoke(game, "Update");
        SetField(game, "dwellElapsed", game.DwellSeconds);
        SetField(game, "spawnElapsed", 1000f);
        int targetsAtGameOver = CountTargets();
        Invoke(game, "LateUpdate");
        Invoke(game, "Update");
        Check(game.GameOver && game.points == -80 && rock.enabled && !BeamIsVisible(game)
              && CountTargets() == targetsAtGameOver,
            "game over prevents shooting and spawning");

        game.spaceship.transform.position += Vector3.down * 3;
        game.RestartGame();
        Rigidbody2D shipBody = game.spaceship.GetComponent<Rigidbody2D>();
        bool shipCollidersEnabled = true;
        foreach (Collider2D collider in game.spaceship.GetComponentsInChildren<Collider2D>())
            shipCollidersEnabled &= collider.enabled;
        Check(!game.GameOver && game.points == 0 && game.damage == 0 && game.DwellTarget == null
              && CountTargets() == 2 && shipCollidersEnabled
              && Vector3.Distance(game.spaceship.transform.position, initialShipPosition) < 0.001f
              && Quaternion.Angle(game.spaceship.transform.rotation, initialShipRotation) < 0.001f
              && shipBody.velocity == Vector2.zero && shipBody.gravityScale == 0f,
            "restart restores score, damage, ship and two fresh targets");
    }

    static void InjectGaze(GazeInput input, Camera camera, Vector2 worldPoint, bool valid)
    {
        Vector2 point = camera.WorldToScreenPoint(new Vector3(worldPoint.x, worldPoint.y, 0));
        Check(!GazeHud.ControlsContain(point), "fixture gaze lies outside HUD controls");
        SetProperty(input, "ScreenPoint", point);
        SetProperty(input, "HasValidGaze", valid);
    }

    static Collider2D SpawnFixture(GameObject prefab, Vector2 position)
    {
        GameObject fixture = Object.Instantiate(prefab);
        fixture.name = "Validation " + prefab.name;
        fixture.transform.position = position;
        fixture.transform.rotation = Quaternion.identity;
        RockController rock = fixture.GetComponent<RockController>();
        SatelliteController satellite = fixture.GetComponent<SatelliteController>();
        if (rock != null) rock.enabled = false;
        if (satellite != null) satellite.enabled = false;
        Rigidbody2D body = fixture.GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.bodyType = RigidbodyType2D.Kinematic;
            body.velocity = Vector2.zero;
            body.angularVelocity = 0;
            body.gravityScale = 0;
        }
        Physics2D.SyncTransforms();
        return fixture.GetComponent<Collider2D>();
    }

    static void RemoveTargets()
    {
        foreach (RockController rock in Object.FindObjectsOfType<RockController>()) Object.DestroyImmediate(rock.gameObject);
        foreach (SatelliteController satellite in Object.FindObjectsOfType<SatelliteController>()) Object.DestroyImmediate(satellite.gameObject);
        Physics2D.SyncTransforms();
    }

    static int CountTargets()
    {
        return Object.FindObjectsOfType<RockController>().Length + Object.FindObjectsOfType<SatelliteController>().Length;
    }
    static bool BeamIsVisible(GameController game) { return game.laserbeam.GetComponent<LineRenderer>().enabled; }
    static void Invoke(object target, string method) { target.GetType().GetMethod(method, PrivateInstance).Invoke(target, null); }
    static void SetField(object target, string field, object value) { target.GetType().GetField(field, PrivateInstance).SetValue(target, value); }
    static void SetProperty(object target, string property, object value)
    {
        target.GetType().GetProperty(property).GetSetMethod(true).Invoke(target, new[] { value });
    }
    static void Check(bool condition, string description)
    {
        if (!condition) throw new InvalidOperationException(description);
        checks++;
        Debug.Log("GAZELAB_CHECK_PASS: " + description);
    }
    static void Finish(Exception exception)
    {
        SessionState.SetBool(ActiveKey, false);
        SessionState.SetBool(LiveTalonKey, false);
        EditorApplication.update -= Tick;
        if (exception == null) Debug.Log("GAZELAB_TEST_PASS: " + checks + " checks; injected gaze only, no hardware validation.");
        else Debug.LogError("GAZELAB_TEST_FAIL: " + exception);
        EditorApplication.Exit(exception == null ? 0 : 1);
    }
}
