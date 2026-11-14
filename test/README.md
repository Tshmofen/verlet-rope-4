# Tests

**The addon ships with an automated test suite that runs its rope simulation, so a change can be verified without opening the editor.** The tests are written with [gdUnit4](https://github.com/godot-gdunit-labs/gdUnit4) and executed the usual .NET way - through `dotnet test` (or the IDE action):

```powershell
dotnet test VerletRope.csproj                                    # all tests
dotnet test VerletRope.csproj --filter 'Trait.lane=unit'         # unit tests only
dotnet test VerletRope.csproj --filter 'Trait.lane=integration'  # scene tests only
```

A run starts Godot once, headless, and the selected tests execute inside it against the real 3D physics server. A single suite can be selected while iterating on it, either by name or by its fully qualified class:

```powershell
dotnet test VerletRope.csproj --filter 'FullyQualifiedName~SimulatedRopeTest'
dotnet test VerletRope.csproj --filter 'Class=VerletRope4.Tests.Unit.SimulatedRopeTest'
```

> [!NOTE]
> The engine also prints leaked resource and orphan node messages while the test host shuts down - they belong to the host, not to a failed test. Judge a run by its test results.

## Prerequisites

* **Godot .NET** - the engine executes the tests, so the version the project targets is required.
* **`godot.path`** - a gitignored file in the repository root naming that engine binary. Create it and write the absolute path of the `_console.exe` on a single line, for example:

```
E:\Apps\Godot\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe
```

The path is read during the build, so it is set once per machine. A missing file only warns on build, while the test run fails and names the file it expects.

## Unit Tests

`test/Unit/` covers the addon code on its own: rope nodes, joints, particle data, mesh generation and the math utilities behind them. A test creates the node it needs, writes the properties under test, steps the simulation by hand where the step matters and asserts on the result - the particle structure, segment lengths, attachments, joint configuration warnings and other internal state.

## Scene Integration Tests

`test/Integration/` drives the real scenes of `demo/examples/`, plus a purpose-built collision scene, and asserts on the physics invariants of each one: the rope stays connected, keeps its length, its segments hold together, the body it pulls travels where it should and collisions stop it where they should. Every suite loads its scene headless, applies what that scene needs to simulate without a camera and waits for physics frames until its goals are met, reporting all the goals that were missed:

```
`5_fishing_rod` did not meet 1 goal(s):
  the demo cycle casts the line, phase is Casting
```

## Not Covered

Anything visual, any input and editor-only code - how a rope really looks is still checked by opening `demo/demo.tscn`. The engine output is not scanned for errors either, so whatever has to be guarded has to be written as an assertion.
