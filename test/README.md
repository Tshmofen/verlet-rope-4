# Tests

Two lanes, one script. Both are gated on a process exit code, so both can be run by hand or by any CI step later.

```powershell
./run-tests.ps1                     # both lanes
./run-tests.ps1 -Lane Unit          # gdUnit4 only
./run-tests.ps1 -Lane Scenes        # scene runs only
```

Requirements: the .NET SDK the project targets and a Godot 4.7 mono install. Godot is found through `-Godot`, or through
`GODOT_BIN` when the parameter is omitted:

```powershell
$env:GODOT_BIN = 'E:\Apps\Godot\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe'
```

The script always builds first, because the engine loads the assembly from the project output folder and never builds it.
Both lanes run without a window: `.runsettings` passes `--headless` to the gdUnit4 host, and the scene runner is started
with it explicitly, so a run from an IDE behaves the same as a run from the shell.

## Why two lanes

`test/Unit` asserts on the addon in isolation and needs a Godot runtime, but no scene. `test/Scenes` runs the demo
scenes for real. They cannot be one lane: physics does tick in the gdUnit4 host as frames pass, but by an amount nobody
controls - measured on 4.7.2, twenty idle-frame awaits advanced `Engine.GetPhysicsFrames()` by 9, and
`await SimulateFrames(20)` by 8. A unit test therefore steps `_PhysicsProcess` itself to get exactly the number of steps
it asserts on, while the scene lane runs whole scenes with their drivers and the collision queries over a fixed physics
frame count. Both are needed and neither replaces the other.

## Lane: unit tests

`test/Unit/*.cs`, written against [gdUnit4](https://github.com/godot-gdunit-labs/gdUnit4) and run by `dotnet test`:

```powershell
dotnet test VerletRope.csproj --settings .runsettings
```

- A suite is `[TestSuite]` and every case is `[TestCase]`; assertions come from `AssertThat(...)` in `GdUnit4.Assertions`.
- **`[RequireGodotRuntime]` is mandatory on any suite or case that touches a Godot type.** That includes `Vector3`, so in
  practice it is every test here. With `TreatWarningsAsErrors` the gdUnit4 analyzer turns a missing annotation into a
  build error (`GdUnit0501`), not a silent difference in where the test runs.
- A node that has to be in the tree is loaded with `ISceneRunner.Load(node)`, which parents it and frees it with the
  runner. A node that only has to exist is created and passed to `AutoFree(...)` so the run is not reported as leaking
  orphans.
- **Step physics by hand**: `rope._PhysicsProcess(1.0 / 60.0)` in a loop. Frames do pass in this host and physics ticks
  with them, but not one step per await (measured: 20 idle-frame awaits gave 9 ticks, `SimulateFrames(20)` gave 8), so
  only the manual call gives a test the exact number of steps it asserts on. Write assertions as ranges or relations,
  not as exact poses.
- A property the node applies deferred - the particle count, a rigid rope's length, joint changes - lands on a frame
  boundary. Await `ToSignal(tree, SceneTree.SignalName.ProcessFrame)` and assert afterwards.
- Filter a single suite with `dotnet test VerletRope.csproj --filter 'FullyQualifiedName~SimulatedRopeTest'`, or
  `--filter 'Class=VerletRope4.Tests.Unit.SimulatedRopeTest'` - `Class` wants the fully qualified name, a bare
  `Class=SimulatedRopeTest` matches nothing.

Add a suite by dropping a `[TestSuite]` class in `test/Unit/`, named after the node or utility type it covers with the
`VerletRope` prefix dropped and a `Test` suffix (`SimulatedRopeTest`, `RigidRopeTest`, `SegmentPlaceUtilityTest`).

## Lane: scene integration

`test/Scenes/` holds a runner scene, the goal-checking helpers and one scenario per demo scene:

| File | Purpose |
| --- | --- |
| `scene_runner.tscn`, `SceneTestRunner.cs` | loads every scenario in one engine run, reports, quits with a code |
| `SceneScenario.cs` | base type: scene path, name, the goals to run |
| `SceneChecks.cs` | goal recording and the shared rope invariants |
| `Scenarios/*.cs` | one scenario per `demo/examples` scene, plus the purpose-built `rope_collision` |
| `rope_collision.tscn` | purpose-built scene: a rope falling onto a static floor, for collision coverage no demo has |

```powershell
$env:GODOT_BIN = '...\Godot_v4.7.2-stable_mono_win64_console.exe'
dotnet build VerletRope.csproj
& $env:GODOT_BIN --headless --path . res://test/Scenes/scene_runner.tscn
```

Output is one line per scene, with every goal that was not met under it:

```
[scenes] PASS 1_swinging_rope
[scenes] FAIL 5_fishing_rod - 1 goal(s) not met
          the demo cycle casts the line, phase is Casting
[scenes] 6 scenario(s) run, 1 failed, 1 goal(s) not met
```

The runner exits with `0` when every goal holds, `1` when any goal is missing, and `2` when the run was aborted after 180
s - so a scenario that waits on a frame that never comes fails instead of hanging. A scenario that throws fails its own
scene instead of stopping the run.

- A scenario drives a real scene with real physics: wait with `WaitPhysicsFrames(root, frames)`, never with a fixed
  `await Task.Delay`.
- Goals are physics invariants of that scene - particle count, attachment, length, body travel, joint leash, segment
  integrity, finite positions - not screenshots and not timings of individual frames.
- The runner disables the visibility-based skipping of every rope before the scene enters the tree
  (`UseVisibleOnScreenNotifier` and, for a simulated rope, `IsDisabledWhenInvisible`). Headless a screen notifier is never
  on screen, so a simulated rope with `IsDisabledWhenInvisible` left on would stay frozen for the whole scenario; the
  notifier flag alone is what keeps a rope from drawing, and for a rigid rope drawing is all visibility affects.
  `1_swinging_rope` and `2_tether_leash` ship without either flag set.
- Add a scenario by dropping a `SceneScenario` subclass in `test/Scenes/Scenarios/`, then adding it to the `Scenarios`
  list in `SceneTestRunner`. Keep each goal a single sentence that names the scene node it is about.

## Not covered

Rendering and anything visual, input handling, and editor-only code - `Engine.IsEditorHint()` is false in both lanes.
The runner does not scan the engine output for errors either: a goal has to be written as a check, otherwise it is not
tested.

## Known noise in the log

Judge a run by its exit code, not by these:

- `Detected <N> orphan nodes during test execution!` - gdUnit4 counts leftovers per test case while the runner frees
  what it loaded deferred, so a suite can report a few nodes that no single case reports on its own. A hint, not a
  failure; a test that constructs a node itself still has to pass it to `AutoFree(...)`.
- `ERROR: N RID allocations of type ... were leaked at exit`, `N ObjectDB instances were leaked at exit`,
  `N resources still in use at exit` and `Leaked instance dependency: ...` - the engine reporting what the host had not
  released when the process quit. Rendering resources show up in every run, and a rigid rope's body and joint RIDs join
  them after the rigid rope tests.
- `WARNING: 'VerletRopeMesh' configuration warning for ...: Consider checking 'UseVisibleOnScreenNotifier' ...`, with a C#
  backtrace, once per rope. Both lanes disable the screen notifier on purpose, and that is what the warning is about.
- `_DebugDraw3DAssetLibraryUpdateChecker: Failed to get a response from https://godotengine.org/...` and the matching
  `mbedtls error`, from the local-only `addons/debug_draw_3d` addon when the host has no network access.

One failure mode is not the tests: the engine process that gdUnit4 starts for its project rebuild - and the Godot import
step behind it - dies with an access violation on shutdown now and then (`Rebuilding Godot Project ends with exit code:
-1073741819`, then every selected test reported as `was not executed`). Measured on 4.7.2: twice in twelve runs, and
`godot --headless --import` by itself crashed once in three. Nothing ran in that case, so re-run - `run-tests.ps1` retries
the unit lane for exactly that signature, and also retries the scene lane when it dies before reporting a scenario. A
plain `dotnet test` needs the second invocation by hand.

## Troubleshooting

- `Settings file provided does not conform to required format` - an XML comment in `.runsettings` contains `--`. Keep
  engine flags in the `<Parameters>` element, not in prose.
- `GODOT_BIN` unset does not fail to connect: the adapter prints `Godot runtime is not configured. The environment
  variable 'GODOT_BIN' is not set or empty.` and then reports every selected test as `was not executed`. A whole red run
  with no assertion failure in it is a configuration problem, not a regression.
- `gdunit4_testadapter_v5/` and `TestResults/` are generated and gitignored. The adapter folder is excluded from the style
  rules in `.editorconfig` (`generated_code = true`); without that exclusion the first `dotnet test` run breaks the next
  build, because the generated runner scene does not follow this repository's formatting rules.
