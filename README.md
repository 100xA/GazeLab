# Gaze Lab – Eye Gaze Interaction exercise

Team: **Jan Biernacki, 3275061; Malte Lührs, 3138900; Kürsat Yurdakul, 3282791; Paul Prigge, 3097391**

This project adapts the Moodle Space Shooter starter for the Eye Gaze Interaction exercise. The stationary ship sits at the left of the playfield. Rocks give +20 points and satellites give −100. The laser only fires when the first object along the cannon path is the selected target. The game reads gaze from the **Tobii 4C** through the course **Tobii Unity SDK**. There is also a clearly labelled **mouse simulation** for development without a tracker.

## Open the project

1. Open this folder in **Unity 2019.4** (tested with 2019.4.41f2; the course uses 2019.4.13f1).
2. Open `Assets/Scenes/Main.unity`.
3. Select a 16:9 Game view, preferably **1920 x 1080**.
4. Press Play. The game starts with the **Tobii eye tracker**, so the mode menu can be operated with the eyes alone. Switch to **Mouse** in the top-left panel for development without a tracker; the next start uses the tracker again.

The Tobii native library crashes Unity if the installed Tobii runtime does not fit the SDK. The lab slides warn about Tobii Core Software 2.16.8. If Unity crashes at any point while Tobii is in use, the next start falls back to mouse simulation once and says so in the panel. In that case, install **Tobii Experience** (see below).

## Shooting modes (Midas touch)

The game starts with a mode menu. Look at a mode for **1 second** to start it. No click is needed. After game over, look at **Restart** (same mode) or **Change mode** for 1 second. New menu buttons ignore the gaze for 0.6 s after they appear, so a menu that pops up where the player is looking does not trigger by accident.

| # | Mode | How to fire | Simulation |
|---|------|-------------|------------|
| 1 | Aim + Hold (Dwell) | Keep looking at a target for 0.6 s – eyes only | move the mouse |
| 2 | Aim + Blink | Look at a target, then close the eyes for 0.15–1.5 s | hold **B** |
| 3 | Aim + Nod | Look at a target, then nod the head once (Tobii head pose) | press **N** |
| 4 | Aim + Space / Click | Look at a target, then press Space or the left mouse button | Space / click |

Mode 4 is a comparison baseline. The exercise does not allow a button press for shooting confirmation, so modes 1–3 are the gaze-only solutions.

- **Dwell:** a progress ring around the gaze marker fills up. Switching target or losing gaze resets it.
- **Blink:** blinks shorter than 0.15 s are natural blinks and are ignored. The Tobii sample only counts as missing after 100 ms, so the real closure is about 0.1 s longer than the measured one. Closures longer than 1.5 s are ignored too and pause the game. The target the player aimed at just before closing the eyes stays locked during the blink. A large "Eyes closed x.xx s" readout turns green inside the valid window. The eyes must also reopen close to where they closed (within 15 % of the screen height). Looking away from the screen and back is therefore not counted as a blink.
- **Nod:** a nod is a pitch movement of at least 9° away from a slowly adapting neutral head pose that returns within 0.9 s. Slow posture changes and head shakes are ignored. The panel shows the current head pitch.
- **Target highlight (all modes):** yellow corner brackets mark the target the player is currently aiming at.
- **Lock feedback (modes 2–4):** a full yellow ring and a hint (BLINK / NOD / SPACE) next to the gaze marker.
- **Shot feedback:** after every confirmation, a box at the bottom of the screen says what happened. It is green when the shot fired (e.g. "Dwell - fired", "Blink 0.32 s - fired") and orange when it was blocked or ignored, with the reason (e.g. "Blink 0.10 s - too short, ignored", "Nod - line of fire blocked"). A hit also shows a large "HIT!" at the top.

**Tracker tolerance:** small gaze jitter is smoothed, while large jumps (saccades) are followed immediately. A gaze point up to about 65 px (0.6 world units) next to a target still selects it. Dwell and menu progress survive jitter or blinks of up to 0.15 s (targets) or 0.3 s (menu). Menu buttons accept gaze up to 40 px outside their border. All values can be changed in the Inspector: **Settings** on the GameController and on GazeInput.

All modes show a crosshair that follows the gaze (the gaze tracing marker required by the exercise). Losing valid gaze pauses targets and spawning. In blink mode this happens only after the blink window has passed.

## Tobii 4C on Windows

1. Install **Tobii Experience** from Moodle (`Tobii.IS4C.Offline.Installer_4.124.0.15937.msi`). With Tobii Core Software 2.16.8, the SDK crashed Unity on our PC. Connect the 4C and calibrate it, and recalibrate for every person who plays.
2. In Unity, select **File > Build Settings > PC, Mac & Linux Standalone > Windows** (x86_64) and switch platform if needed.
3. The Tobii Unity SDK 4.0.5 is included under `Assets/Tobii`. Accept the SDK license if Unity asks.
4. Press Play, select **Tobii eye tracker**, and check that the marker lines up at several positions. Recalibrate if the tracker or monitor moves.

**Windows display scaling:** with scaling above 100 % (e.g. 150 % on a 4K monitor), the Tobii runtime reports gaze positions pulled towards the centre of the Game view by the scaling factor. When we looked at the top-left corner at 150 %, the marker landed about one third of the way to the centre. `DisplayScaling` stretches the gaze back around the window centre by the scaling factor. In the Editor this is `EditorGUIUtility.pixelsPerPoint`, in a build it is the window DPI / 96. The panel shows the detected scaling, and the **Scaling … fix on/off** pill toggles the correction for comparison.

The Tobii adapter uses `TobiiAPI.GetGazePoint().Screen` (bottom-left origin) and `TobiiAPI.GetHeadPose()` for nods. Samples older than 100 ms count as lost gaze.

## Code structure

All game scripts are in `Assets/Scripts` (namespace `GazeLab`):

| Folder | Contents |
|---|---|
| `Core` | `GameController` (round state, score, wiring), `GameSettings` (all tuning values), `Targeting` (target under the gaze, tolerance, line of fire, lock), `LaserCannon`, `TargetSpawner` |
| `Modes` | One class per shooting mode (`DwellMode`, `BlinkMode`, `NodMode`, `ButtonMode`) derived from `ShotMode`; `ShotModes.All` lists them in menu order |
| `Input` | `GazeInput` (reads the active `IGazeSource` each frame), `TobiiGazeSource`, `MouseGazeSource`, `GazeFilter` (smoothing), `DisplayScaling`, `BlinkDetector`, `HeadNodDetector`, `TobiiCrashGuard` |
| `UI` | `GazeHud` (IMGUI entry point), `StatusCard`, `MenuView` with `GazeButtons` (gaze-only buttons), `GameOverlay` (crosshair, target brackets, feedback), `HudTheme` (colours, fonts, drawing helpers) |
| `Targets` | `MovingTarget` base class with `RockController` and `SatelliteController`, `PlayerController` (ship) |
| `Visuals` | `ToonModels` (builds, caches and attaches the meshes), `ToonMeshBuilder` (low-poly parts); `Models/` one class per model (`ShipModel`, `RockModel`, `SatelliteModel`, `EffectModels`); `Effects/` `ShotEffects` (muzzle flash, explosions) with `EffectPiece`; drawn with `Resources/Shaders/GazeLabToon.shader` |
| `Audio` | `SoundSynth` (sound effects synthesised in code), `GameSounds` (plays them for game events) |

The ship, rocks and satellites are low-poly 3D models generated in code (no model files) and drawn with a cartoon shader: three light bands, a rim light and a dark outline. Rocks tumble and satellites roll while they drift. A shot is animated: muzzle flash and recoil, a bolt that races to the target and pulls its tail after it, and an explosion of fire puffs and tumbling fragments (`ShotEffects`).

Sound effects are synthesised in code as well (`Audio/SoundSynth`, played by `GameSounds`): laser, rock explosion, satellite crash with a penalty jingle, blocked shot, ship hit, round start, game over and a quiet blip when the gaze settles on a target (audible gaze feedback). The volume is **Sound Volume** in the GameController settings. Newer targets spawn closer to the camera, so they are drawn and selected in front. The HUD (`HudTheme`) uses the same colours as the models and two fonts under the SIL Open Font License, Lilita One and Varela Round. They are in `Assets/Resources/Fonts`, together with their licence files.

To add a shooting mode, create a class derived from `ShotMode` and add it to `ShotModes.All`. The menu, HUD texts and checks pick it up from there.

## Automated checks

`Assets/Editor/GazeLabValidation.cs` drives the real game code in Play Mode with a fake gaze source and explicit time (no reflection). It checks dwell, blink and nod confirmation, the gaze tolerance, line-of-fire blocking, scoring, game over, restart and the menu, as well as the head-nod detector and the display-scaling correction. Close Unity first, then run:

```
Unity.exe -projectPath <project-folder> -executeMethod GazeLabValidation.Run -logFile <log-file>
```

Unity opens, runs the checks and closes again (exit code 0 = passed). On our PC, `-batchmode` failed with a licence error, so it is left out. The checks do not replace a test with the real tracker.

`-executeMethod GazeLabPlay.Run` opens Unity directly in Play mode with the Main scene (`Assets/Editor/GazeLabPlay.cs`). That script also gives the Game view keyboard focus whenever Play mode starts, so Space, B and N work without clicking into the Game view first.

## Submission (three separate files)

1. `GazeLab-Biernacki_Luehrs_Yurdakul_Prigge.zip`: this Unity project. Keep `Assets`, `Packages`, `ProjectSettings` and this README. Leave out `Library`, `Temp`, `Obj`, `Logs` and builds.
2. `GazeLab-Biernacki_Luehrs_Yurdakul_Prigge.pdf`: about 400 words on C1 (Midas touch), C2 (feedback) and C3 (UI design).
3. `GazeLab-Biernacki_Luehrs_Yurdakul_Prigge.mp4`/`.mov`: compressed video of the game with the real tracker and the gaze marker visible.

The full team names and matriculation numbers appear above, in the source headers and in the in-game footer. Include the same attribution in the report and video.
