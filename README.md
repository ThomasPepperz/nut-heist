Nut Heist (Unity 6 / URP)
=======================

Third-person squirrel platformer foundation: movement + climb + Cinemachine cameras, interaction tiers, vents, placeholder blockout tooling, TMP HUD skeleton, nuts, audio hooks.

Requirements
-----------

- Unity **6000.0.x** (project version file targets **6000.0.52f1**; Hub will propose matching editor install if yours differs).

First open (recommended)
-----------

1. Add this repository root via **Unity Hub → Open** (Unity project folders are beside `Assets/`, `Packages/`, `ProjectSettings/`).
2. Let Package Manager fetch **URP**, **Input System**, **Cinemachine 2**, **ProBuilder**, and **Animation Rigging**.
3. If Unity asks about **Active Input Handling**, choose **New Input System** or **Both** (`ProjectSettings.asset` prefers the new Input System).
4. Use menu **`Nut Heist → Full Project Setup`** to create Tags/Layers, import TMP essentials when available, scaffold `MainLevel`, prefabs (including squirrel + nut), and baseline URP assets. This also procedurally authors the full world (yard, fence, four trees, garden beds, pool, shed, two-story brick house with 10 exterior entry points, six rooms + basement lab, branching vent network) at squirrel scale.
5. Use menu **`Nut Heist → Build House World`** to regenerate just the world (`HouseWorld_Root`) without touching the squirrel/HUD/cameras. Idempotent.
6. Open **`Assets/Scenes/MainLevel.unity`** and press **Play**.

Builds (`perf-build`)
-----------

Run **`Nut Heist → Builds → Mac Player`** or **`Windows Player`** from Unity Editor (`Assets/Editor/BuildNutHeistPlayer.cs`). This requires Unity licensing on the workstation and cannot execute from this scaffold alone.

Automated tests (Edit Mode)
-----------

- In the Editor: **Window → General → Test Runner**, switch to **Edit Mode**, then run the **NutHeist** tests (or **Run All**).
- Headless / CI (example): `Unity -batchmode -quit -projectPath "/path/to/nut-heist" -runTests -testPlatform EditMode -testResults "./TestResults.xml"` (use your Hub-installed `Unity` binary and an absolute project path).
