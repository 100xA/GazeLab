# Gaze Lab – Eye Gaze Interaction exercise

Team: **Jan Biernacki, Kürsat, Paul, Malte**
Matriculation numbers: **TO BE PROVIDED**

This project adapts the Moodle Space Shooter starter for the Eye Gaze Interaction exercise. The stationary ship sits at the left of the playfield. Rocks give +20 points and satellites give −100. The laser only fires when the first object along the cannon path is the selected target. The game reads gaze from the **Tobii 4C** through the course **Tobii Unity SDK**. There is also a clearly labelled **mouse simulation** for development without a tracker.

## Open the project

1. Open this folder in **Unity 2019.4** (tested with 2019.4.41f2; the course uses 2019.4.13f1).
2. Open `Assets/Scenes/Main.unity`.
3. Select a 16:9 Game view, preferably **1920 x 1080**.
4. Press Play. The first start uses **mouse simulation**. Click **Tobii eye tracker** in the top-left panel once. After that, every start uses the tracker, so the mode menu can be operated with the eyes alone.

The Tobii native library crashes Unity if the installed Tobii runtime does not fit the SDK. The lab slides warn about Tobii Core Software 2.16.8. If Unity crashes at any point while Tobii is in use, the next start falls back to mouse simulation and says so in the panel. In that case, install **Tobii Experience** (see below).

## Shooting modes (Midas touch)

The game starts with a mode menu. Look at a mode for **1 second** to start it. No click is needed. After game over, look at **Restart** (same mode) or **Change mode** for 1 second.

| # | Mode | How to fire | Simulation |
|---|------|-------------|------------|
| 1 | Aim + Hold (Dwell) | Keep looking at a target for 0.6 s – eyes only | move the mouse |
| 2 | Aim + Blink | Look at a target, then close the eyes for 0.15–1.5 s | hold **B** |
| 3 | Aim + Nod | Look at a target, then nod the head once (Tobii head pose) | press **N** |
| 4 | Aim + Space / Click | Look at a target, then press Space or the left mouse button | Space / click |

Mode 4 is a comparison baseline. The exercise does not allow a button press for shooting confirmation, so modes 1–3 are the gaze-only solutions.

- **Dwell:** a progress ring around the gaze marker fills up. Switching target or losing gaze resets it.
- **Blink:** blinks shorter than 0.15 s are natural blinks and are ignored. The Tobii sample only counts as missing after 100 ms, so the real closure is about 0.1 s longer than the measured one. Closures longer than 1.5 s are ignored too and pause the game. The target the player aimed at just before closing the eyes stays locked during the blink. A large "Eyes closed x.xx s" readout turns green inside the valid window.
- **Nod:** a nod is a pitch movement of at least 9° away from a slowly adapting neutral head pose that returns within 0.9 s. Slow posture changes and head shakes are ignored. The panel shows the current head pitch.
- **Lock feedback (modes 2–4):** a green frame on the target, a full ring and a hint (BLINK / NOD / SPACE) next to the gaze marker. After every confirmation, the bottom of the screen says whether it fired, was blocked, or was ignored.

**Tracker tolerance:** small gaze jitter is smoothed, while large jumps (saccades) are followed immediately. A gaze point up to about 65 px (0.6 world units) next to a target still selects it. Dwell and menu progress survive jitter or blinks of up to 0.15 s (targets) or 0.3 s (menu). Menu buttons accept gaze up to 40 px outside their border. All values can be changed in the Inspector (GameController, GazeInput).

All modes show the gaze marker with a short trail (the gaze tracing marker required by the exercise). Losing valid gaze pauses targets and spawning. In blink mode this happens only after the blink window has passed.

## Tobii 4C on Windows

1. Install **Tobii Experience** from Moodle (`Tobii.IS4C.Offline.Installer_4.124.0.15937.msi`). With Tobii Core Software 2.16.8, the SDK crashed Unity on our PC. Connect the 4C and calibrate it, and recalibrate for every person who plays.
2. In Unity, select **File > Build Settings > PC, Mac & Linux Standalone > Windows** (x86_64) and switch platform if needed.
3. The Tobii Unity SDK 4.0.5 is included under `Assets/Tobii`. Accept the SDK license if Unity asks.
4. Press Play, select **Tobii eye tracker**, and check that the marker lines up at several positions. Recalibrate if the tracker or monitor moves.

**Windows display scaling:** with scaling above 100 % (e.g. 150 % on a 4K monitor), the Tobii runtime reports gaze positions pulled towards the centre of the Game view by the scaling factor. When we looked at the top-left corner at 150 %, the marker landed about one third of the way to the centre. `GazeInput` stretches the gaze back around the window centre by the scaling factor. In the Editor this is `EditorGUIUtility.pixelsPerPoint`, in a build it is the window DPI / 96. The panel shows the detected scaling, and **Scaling fix: ON/OFF** toggles the correction for comparison.

The Tobii adapter uses `TobiiAPI.GetGazePoint().Screen` (bottom-left origin) and `TobiiAPI.GetHeadPose()` for nods. Samples older than 100 ms count as lost gaze.

## Automated checks

`Assets/Editor/GazeLabValidation.cs` drives the real game code with injected gaze in Play Mode. It checks dwell, blink and nod confirmation, the menu, line-of-fire blocking, scoring, game over and restart, as well as the head-nod detector with synthetic head movements:

```
Unity.exe -batchmode -nographics -projectPath <project-folder> -executeMethod GazeLabValidation.Run -logFile <log-file>
```

Do not add `-quit`; the runner exits after its checks. These checks do not replace a test with the real tracker.

## Submission (three separate files)

1. `GazeLab-<names>.zip`: this Unity project. Keep `Assets`, `Packages`, `ProjectSettings` and this README. Leave out `Library`, `Temp`, `Obj`, `Logs` and builds.
2. `GazeLab-<names>.pdf`: about 400 words on C1 (Midas touch), C2 (feedback) and C3 (UI design).
3. `GazeLab-<names>.mp4`/`.mov`: compressed video of the game with the real tracker and the gaze marker visible.

Add all names and matriculation numbers to the report, the source headers and the video, and replace every **TO BE PROVIDED**.
