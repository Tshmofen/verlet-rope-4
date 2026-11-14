# Tests

Two lanes, one command. Both are ordinary VSTest runs - `dotnet test`, or `Run Tests` in the IDE - so the test explorer is
the entry point and there is no separate engine invocation to keep in step.

```powershell
./run-tests.ps1                     # both lanes
./run-tests.ps1 -Lane Unit          # test/Unit only
./run-tests.ps1 -Lane Integration   # test/Integration only
```

Requirements: the .NET SDK the project targets and a Godot 4.7 mono install. Godot is named once, in the gitignored
`godot.path` at the repository root: one line, the absolute path of the `_console.exe`.

```
E:\Apps\Godot\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe
```

`test/godot-test.targets` reads that file, writes `test.runsettings` into the intermediate output folder and points the
project's `RunSettingsFilePath` at it, so the adapter starts the engine without any environment variable being set in the
shell. A missing or empty `godot.path` warns on build and fails the test run.

Toolchain: `gdUnit4.api` 5.1.0-rc7, `gdUnit4.test.adapter` 3.2.0, `gdUnit4.analyzers` 1.1.0. The api is a release
candidate because the last stable one, 5.0.0, predates Godot 4.7 support; the price is paid once, and the adapter's
`TolerateExitCodeAfterCompletion` is switched on, so an engine crash after the results are reported is a warning instead of
a red run.

## Why two lanes

`test/Unit` covers the addon on its own, `test/Integration` drives the real demo scenes. Both run in the gdUnit4 host, in
one engine start per run, and both are selected by trait:

```powershell
dotnet test VerletRope.csproj --filter 'Trait.lane=unit'
dotnet test VerletRope.csproj --filter 'Trait.lane=integration'
```

Physics does tick in that host, so a whole scene can be driven there. What a test cannot rely on is a *step count*:
measured on 4.7.2, twenty idle-frame awaits advanced `Engine.GetPhysicsFrames()` by 9, and `await SimulateFrames(20)` by 8.
A unit test therefore steps `_PhysicsProcess` itself to get exactly the number of steps it asserts on, while a scenario
waits for physics frames and asserts on how the scene behaves.

## Lane: unit tests

`test/Unit/*.cs`, written against [gdUnit4](https://github.com/godot-gdunit-labs/gdUnit4):

```powershell
dotnet test VerletRope.csproj --filter 'Trait.lane=unit'
```

- A suite is `[TestSuite]` and every case is `[TestCase]`; assertions come from `AssertThat(...)` in `GdUnit4.Assertions`.
- **An asynchronous case is declared `async Task`.** gdUnit4 rejects a method that merely returns a `Task` with
  `Invalid method signature found at: <name>` and fails the case.
- **`[RequireGodotRuntime]` is mandatory on any suite or case that touches a Godot type.** That includes `Vector3`, so in
  practice it is every test here. With `TreatWarningsAsErrors` the gdUnit4 analyzer turns a missing annotation into a
  build error (`GdUnit0501`), not a silent difference in where the test runs.
- **A suite, a hook and a case are `public`.** The runner discovers only public members: a `private` method carrying
  `[TestCase]` is skipped without a word (verified: a three-case suite with one private case reported two). Roslyn would
  flag such a method as unused, which is why the unused-member rules are silenced for `test/**` in `.editorconfig`.
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
`VerletRope` prefix dropped and a `Test` suffix (`SimulatedRopeTest`, `RigidRopeTest`, `SegmentPlaceUtilityTest`), and give
it `[Trait("lane", "unit")]`.

## Lane: scene integration

`test/Integration/` holds one suite per scene, `test/Scenes/` holds what those suites drive:

| File | Purpose |
| --- | --- |
| `Integration/*ScenarioTest.cs` | one `[TestSuite]` per scene, with a single `[TestCase]` that awaits the harness |
| `Integration/SceneHarness.cs` | loads the scene, applies the headless rope settings, drives it under a timeout, reports the goals |
| `Scenes/SceneScenario.cs` | base type: scene path, name, the goals to run |
| `Scenes/SceneChecks.cs` | goal recording and the shared rope invariants |
| `Scenes/Scenarios/*.cs` | one scenario per `demo/examples` scene, plus the purpose-built `rope_collision` |
| `Scenes/rope_collision.tscn` | purpose-built scene: a rope falling onto a static floor, for collision coverage no demo has |

A failing goal fails its case with every goal the scene missed, so one run reports all of them:

```
`5_fishing_rod` did not meet 1 goal(s):
  the demo cycle casts the line, phase is Casting
```

- A scenario drives a real scene with real physics: wait with `WaitPhysicsFrames(root, frames)`, never with a fixed
  `await Task.Delay`.
- Goals are physics invariants of that scene - particle count, attachment, length, body travel, joint leash, segment
  integrity, finite positions - not screenshots and not timings of individual frames.
- The harness disables the visibility-based skipping of every rope before the scene enters the tree
  (`UseVisibleOnScreenNotifier` and, for a simulated rope, `IsDisabledWhenInvisible`). Headless a screen notifier is never
  on screen, so a simulated rope with `IsDisabledWhenInvisible` left on would stay frozen for the whole scenario; the
  notifier flag alone is what keeps a rope from drawing, and for a rigid rope drawing is all visibility affects.
  `1_swinging_rope` and `2_tether_leash` ship without either flag set.
- A scenario that throws fails its own case instead of stopping the run. One that does not finish within 180 s fails with
  that, so a goal waiting on a frame that never comes cannot stall the session.
- Add a scenario by dropping a `SceneScenario` subclass in `test/Scenes/Scenarios/`, then a `[TestSuite]` with one
  `async Task` `[TestCase]` in `test/Integration/` that awaits `SceneHarness.RunAsync(new ...)`, carrying
  `[Trait("lane", "integration")]`. Keep each goal a single sentence that names the scene node it is about.

## Not covered

Rendering and anything visual, input handling, and editor-only code - `Engine.IsEditorHint()` is false in both lanes.
Neither lane scans the engine output for errors: a goal has to be written as a check, otherwise it is not tested.

## Known noise in the log

Judge a run by its results, not by these:

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

One failure mode is not the tests, and is not worked around: the engine process gdUnit4 starts for its project rebuild -
and the Godot import step behind it - dies with an access violation on shutdown now and then (`Rebuilding Godot Project
ends with exit code: -1073741819`, then every selected test reported as `was not executed`). It hit twice while this
lane was written, both times before any result was reported. Nothing ran in that case, so re-run; the same crash after the
results are in is covered by `TolerateExitCodeAfterCompletion` and only shows as a warning.

## Troubleshooting

- `godot.path is missing or empty` - the build warns and the test run fails, by design. Write the absolute path of the
  `_console.exe` into `godot.path`; the gitignored file lives at the repository root.
- `godot.path names '...', which does not exist` - the file points at a Godot install that moved or was removed.
- `Settings file provided does not conform to required format` - an XML comment in `test/test.runsettings.template`
  contains `--`. Keep engine flags in the `<Parameters>` element, not in prose.
- `No test matches the given testcase filter ...` - the trait filter selected nothing, or `GODOT_BIN` never reached the
  adapter (see the two entries above). A whole red run with no assertion failure in it is a configuration problem, not a
  regression.
- `gdunit4_testadapter_v5/`, `TestResults/`, `godot.path` and the generated `test.runsettings` are machine-local. The
  adapter folder is excluded from the style rules in `.editorconfig` (`generated_code = true`); without that exclusion the
  first `dotnet test` run breaks the next build, because the generated runner scene does not follow this repository's
  formatting rules.
